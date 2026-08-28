# Composable Card Effects And Gated Ability Bridge SDD - 20260614-1841

## Contexto

This specification revises the card-effect architecture so ordinary cards are
assembled from shared data rather than implemented as one concrete
`CardEffectSO` subclass per behavior.

Most cards are expected to:

- validate similar activation conditions;
- pay fixed or variable resource costs;
- apply small changes to existing player status;
- react to common gameplay events;
- expire through charges, misses, landing, attack completion, or encounter
  reset;
- differ mainly in authored values and combinations.

A smaller class of cards invokes a concrete gameplay ability. Extra jump is
the first example: locomotion already needs an explicit runtime behavior, and
movement abilities participate in metroidvania progression and world gates.
Those cards should reference that ability runtime rather than reproduce
movement behavior inside a generic status engine.

Sources used:

- `gdd/gdd-canonico-20260526-2331.md`
- `specs/prototype-architecture-sdd-20260525-2200.md`
- `specs/code-conventions-20260526-0014.md`
- `specs/card-effects-and-energy-planning-20260614-1239.md`
- `specs/linked-supplemental-damage-sdd-20260614-1315.md`
- `specs/asset-driven-card-definitions-and-commit-transaction-sdd-20260614-1833.md`
- current `AbilityDefinitionSO`, `CapabilitySet`, card, resource, combat, and
  movement code under `Assets/Scrips/Architecture`

This document supersedes the effect-extension model in
`specs/asset-driven-card-definitions-and-commit-transaction-sdd-20260614-1833.md`.
It retains that document's card definitions, equipped slots, prepared commit,
atomic Card Time transaction, Editor tooling, and migration goals unless this
document changes them explicitly.

## Decision

Use a hybrid card architecture:

```text
ordinary card
    -> composed status-effect definition
    -> shared triggers, conditions, operations, stacking, and lifetime rules
    -> mutable runtime effect instance

ability card
    -> authored ability-operation definition
    -> resolves an AbilityDefinitionSO
    -> delegates to an explicit player ability runtime
```

Do not create one C# effect type per card.

Add C# only when the game gains:

- a genuinely new reusable operation;
- a genuinely new trigger or condition;
- a new lifetime/stacking mechanism;
- a concrete ability runtime required by movement, combat, interaction, or
  world progression.

Differently tuned cards and new combinations of existing behaviors must be
asset-only work.

## Architectural Goals

The card system must support:

1. many cards sharing one common structure;
2. multiple operations on one card;
3. common activation, cost, stacking, and deactivation rules;
4. mutable status state without mutating ScriptableObject assets;
5. direct delegation to specialized ability runtimes;
6. stable authored identity for saves, gates, debug tools, and analytics;
7. no card-specific switch in `PlayerCardRuntime`, `PlayerController`, or
   `DamageResolver`.

It must avoid building a fully general visual-scripting language. The data
vocabulary should remain small, typed, testable, and derived from actual card
requirements.

## Card Definition

`CardDefinitionSO` remains the authored card identity:

```text
CardDefinitionSO
- stable id
- display name
- description
- Card Time category
- fixed resource costs
- CardEffectDefinitionSO effect
```

One equipped card remains assigned to each prototype category:

```text
Neutral
Chain
Finisher
```

Later hand or deck selection replaces only equipped-card resolution. It does
not change effect execution.

## Effect Definition

Use one concrete `CardEffectDefinitionSO` for composed effects:

```text
CardEffectDefinitionSO
- activation conditions[]
- commit operations[]
- reactive rules[]
- stacking policy
- lifetime policy
- deactivation rules[]
```

This asset contains immutable authored data. It never stores current charges,
stacks, owner references, attack ids, or subscription state.

### Commit Operations

Commit operations execute once when the card transaction succeeds.

Examples:

- gain a resource immediately;
- add charges to an existing status;
- add capacity to an existing status;
- arm attack-scoped supplemental damage;
- invoke a specialized ability runtime;
- install a reactive status instance.

### Reactive Rules

A reactive rule describes behavior after commit:

```text
ReactiveRule
- trigger
- conditions[]
- operations[]
- consumption rule
```

Example:

```text
Trigger: EffectivePrimaryAttackResolved
Condition: remaining charges > 0
Operation: multiply successful Energy gain amount by 2
Consumption: consume one charge for each matching attack request
```

Not every card creates a reactive rule. Immediate and ability cards may finish
during commit.

## Typed Data Vocabulary

Prefer serializable discriminated data using enums plus typed parameter
structures for the initial vocabulary. Do not use stringly typed operation
names, reflection-created method calls, or arbitrary serialized expressions.

If Unity Inspector usability becomes poor, the same vocabulary may later move
to small polymorphic `ScriptableObject` definitions. That is a storage change,
not a card-per-class model.

### CardTriggerKind

Initial triggers:

```text
OnCardCommitted
OnEffectivePrimaryAttackResolved
OnPrimaryAttackCompleted
OnPlayerLanded
OnAttackExecutionCompleted
OnPlayerDeath
OnSceneTransition
```

Rules:

- effective attack triggers occur once per completed primary damage request;
- multi-target results do not duplicate the trigger;
- supplemental damage does not trigger ordinary gameplay-hit rules unless its
  proc policy explicitly opts in;
- attack completion with zero effective hits is the shared miss boundary.

Add a trigger only when at least one real card needs a new event boundary.

### CardConditionKind

Initial conditions:

```text
IsAirborne
IsGrounded
HasAttackExecution
HasEffectiveHit
WasMiss
HasRemainingCharges
ResourceAtLeast
AbilityAvailable
AbilityUnlocked
```

Conditions are conjunctive within one rule for the prototype. More complex
boolean expression trees are deferred.

Every condition is evaluated through `CardExecutionContext` or a runtime event
context. Conditions must not search the scene.

### CardOperationKind

Initial ordinary operations:

```text
GainResource
AddStatusCharges
AddStatusCapacity
ModifyDamage
ModifyKnockback
ModifyResourceGain
ArmSupplementalDamage
ClearStatusStacks
RemoveStatus
```

Specialized operation:

```text
InvokeAbility
```

`InvokeAbility` delegates to a registered ability runtime. It does not encode
extra-jump or another movement implementation inside the operation executor.

### CardLifetimeKind

Initial lifetime policies:

```text
Immediate
UntilChargesExhausted
UntilMiss
UntilLanding
UntilAttackExecutionCompletes
UntilPlayerDeath
UntilSceneTransition
PersistentUntilExplicitRemoval
```

One effect may have multiple deactivation rules. The first matching rule
removes the runtime instance.

### CardStackingKind

Initial stacking policies:

```text
RejectIfActive
ReplaceExisting
RefreshLifetime
AddCharges
AddCapacity
AddStacks
```

Authored limits:

```text
maximum charges
maximum capacity
maximum stacks
```

Limits are enforced by the runtime status owner, not by mutating effect assets.

## Status Identity

Every persistent ordinary effect needs stable status identity:

```text
CardStatusId
```

For the prototype, use a stable authored string or a dedicated
`CardStatusDefinitionSO`. Prefer the dedicated asset when multiple cards are
expected to affect the same status.

Examples:

```text
Status_KnockbackBoost
Status_EscalatingDamage
Status_DoubleHitEnergy
```

Status identity determines stacking and shared runtime state. Card identity
determines source, cost, text, and analytics.

Two different cards may intentionally write to the same status.

## Runtime Status Model

Introduce a player-local `PlayerCardEffectRuntime`.

Responsibilities:

- owns active `CardEffectInstance` values;
- receives player-local gameplay events;
- evaluates reactive rules;
- applies stacking and charge consumption;
- removes instances when deactivation rules match;
- delegates operations to narrow runtime collaborators;
- exposes read-only debug snapshots.

It does not:

- own resource balances;
- calculate raw damage independently of the damage system;
- implement locomotion abilities;
- own Card Time session state;
- store equipped cards.

### CardEffectInstance

Mutable runtime data:

```text
CardEffectInstance
- instance id
- source card
- effect definition
- owner
- status identity
- remaining charges
- current stacks
- current capacity
- source attack execution id
- activation sequence
- active flag
```

Instances are ordinary C# runtime objects, not cloned ScriptableObjects and not
new scene components per card use.

### Event Input

Use direct player-local calls or narrow interfaces:

```text
PlayerCombatEffects
    -> effective primary request
    -> resource-gain opportunity
    -> primary attack completed/missed

PlayerLocomotionController
    -> landed

PlayerActionRunner
    -> attack execution completed

Player lifecycle
    -> death or scene transition
```

Do not add global event channels for synchronous state owned by one player.

## Operation Execution

Use a central executor for the small shared vocabulary:

```text
CardOperationExecutor
```

The executor dispatches by reusable operation kind, not by card identity.

Allowed example:

```text
ModifyResourceGain
    -> PlayerCombatEffects applies an authored amount multiplier
```

Forbidden example:

```text
if card.Id == "double-energy"
```

The executor depends on narrow runtime services supplied through
`CardExecutionContext`:

```text
IResourceWallet
IPlayerCombatEffectSink
IPlayerCardEffectRuntime
IPlayerAbilityRuntimeRegistry
```

Concrete project types may implement these interfaces, but operation data must
not fetch components itself.

## Gated Ability Bridge

### Purpose

Some card effects are not status arithmetic. They activate a concrete ability
whose behavior belongs in movement, combat, or interaction runtime.

Examples:

- extra jump;
- wall jump;
- air dash;
- grapple;
- phase movement;
- a future parry or interaction ability with dedicated state.

These abilities may also:

- unlock routes;
- satisfy world gates;
- require animation and input integration;
- participate in permanent progression;
- exist independently of one particular card.

They remain explicit runtime systems.

### Ability Identity

Use the existing `AbilityDefinitionSO` as stable identity.

`AbilityDefinitionSO` continues to own:

- stable id and display metadata;
- broad ability kind;
- default unlock state;
- capability tags used by gates;
- input and animation metadata where appropriate.

Card costs remain in `CardDefinitionSO`; the generic legacy
`AbilityDefinitionSO.resourceCost` must not become the source of truth for a
card transaction.

### Ability Runtime Contract

Add a narrow runtime contract:

```csharp
public interface IPlayerAbilityRuntime
{
    /// <summary>
    /// Gets the authored ability implemented by this runtime.
    /// </summary>
    AbilityDefinitionSO Definition { get; }

    /// <summary>
    /// Validates and prepares one invocation without applying it.
    /// </summary>
    AbilityPreparationResult TryPrepare(in AbilityExecutionContext context);
}
```

Preparation returns an immutable single-use invocation:

```text
IPreparedAbilityInvocation.Apply()
```

This mirrors card preparation so an ability cannot reject after Card Time and
resource payment have committed.

### Ability Runtime Registry

The player owns or exposes an `IPlayerAbilityRuntimeRegistry`:

```text
TryGet(AbilityDefinitionSO definition, out IPlayerAbilityRuntime runtime)
```

Registration is explicit during player composition. An operation never
searches by component type.

For the prototype:

```text
PlayerExtraJumpRuntime
    implements IPlayerAbilityRuntime
    references Ability_ExtraJump.asset
```

### Capability And Unlock Checks

`InvokeAbility` may require both:

1. a matching runtime exists;
2. `ICapabilityProvider.HasAbility(definition)` is true.

This separates:

- ability implementation;
- progression/unlock ownership;
- card availability and cost;
- world-gate queries.

The same `AbilityDefinitionSO` used by the card invocation should be used by
`CapabilitySet` and world gates.

Whether owning/equipping a card automatically unlocks its linked ability is a
progression rule and remains deferred. The prototype may mark Extra Jump
unlocked by default or configure it in `CapabilitySet`.

### Extra Jump

The Extra Jump card is represented as:

```text
CardDefinitionSO
    category = Finisher
    fixed cost = 5 Energy
    effect = composed definition

Activation conditions
    IsAirborne
    AbilityAvailable(Ability_ExtraJump)
    AbilityUnlocked(Ability_ExtraJump)

Commit operation
    InvokeAbility(Ability_ExtraJump)
```

`PlayerExtraJumpRuntime` owns:

- charge capacity;
- validation of whether a charge can be granted;
- charge consumption when an extra jump executes;
- landing reset;
- locomotion-facing query methods.

The generic card system knows none of those movement details.

## Prepared Card Transaction

Retain the prepared transaction model from the superseded SDD.

Preparation performs:

1. equipped-card resolution;
2. card definition validation;
3. activation-condition evaluation;
4. operation preparation;
5. specialized ability preparation where referenced;
6. fixed and variable cost aggregation;
7. affordability validation;
8. creation of a single-use `PreparedCardCommit`.

Preparation does not:

- spend resources;
- install a status;
- invoke an ability;
- transition Card Time.

Application performs synchronously:

```text
wallet.TrySpend(total costs)
    -> apply prepared immediate operations
    -> install prepared status instances
    -> apply prepared ability invocations
    -> success
```

Every expected rejection must occur during preparation. Application must be a
guaranteed operation under normal gameplay conditions.

Card Time accepts the prepared transaction callback and transitions to
`Committed` only when the callback succeeds.

## Initial Prototype Cards

### Neutral: Knockback Charges

```text
status = Status_KnockbackBoost
cost = 5 Energy
commit = install/add 3 charges
reactive trigger = effective primary attack resolved
operation = outgoing knockback multiplier 2
consume = one charge per matching request
lifetime = until charges exhausted
stacking = add charges
```

No specialized card class is required.

### Chain: Escalating Damage

```text
status = Status_EscalatingDamage
cost = 15 Energy
commit = add 5 capacity
reactive trigger = effective primary attack resolved
operation = add one stack up to capacity
modifier = final damage percentage per stack
deactivation reaction = miss clears current stacks
long lifetime = death or scene transition
stacking = add capacity
```

No specialized card class is required.

### Chain: Double Hit Energy

```text
status = Status_DoubleHitEnergy
cost = 15 Energy, provisional
commit = add 5 charges
reactive trigger = effective primary hit Energy opportunity
operation = multiply successful gain amount by 2
consume = one charge whether the roll succeeds or fails
lifetime = until charges exhausted
stacking = add charges
```

No specialized card class is required.

### Finisher: Extra Jump

```text
cost = 5 Energy
conditions = airborne, ability available, ability unlocked
operation = InvokeAbility(Ability_ExtraJump)
runtime = PlayerExtraJumpRuntime
```

This uses the generic ability operation but retains a concrete movement
runtime.

### Finisher: Base Damage Overcharge

```text
cost = 15 Energy + authored variable X
condition = attack execution id exists
operation = ArmSupplementalDamage
lifetime = matching attack execution
deactivation = matching attack completes
```

The shared operation uses authored multiplier conversion and the linked
supplemental-damage runtime. No overcharge-specific card class is required.

## Extending The System

### Asset-Only Extension

No C# is required when a new card can be expressed by:

- existing activation conditions;
- existing fixed or variable costs;
- existing operations;
- existing triggers;
- existing stacking;
- existing lifetime/deactivation rules;
- an already implemented ability runtime.

Example:

```text
Neutral card
    cost: 8 Energy
    commit: add 2 Status_KnockbackBoost charges
    multiplier: 3
```

This may reuse the same status and operation vocabulary with different authored
values.

### New Reusable Primitive

Add one typed primitive when several cards need behavior that cannot be
expressed safely.

Example:

```text
new trigger: OnPerfectParry
```

Implement the trigger once, then author any number of cards against it.

### New Gated Ability

Add a concrete ability runtime when gameplay requires dedicated state,
movement/action integration, animation, or world-gate identity.

Example:

```text
Ability_WallJump.asset
PlayerWallJumpRuntime
InvokeAbility operation in one or more cards
CapabilitySet/world gate references Ability_WallJump
```

The card system does not implement wall-jump physics.

## Suggested Types

Authored data:

```text
CardDefinitionSO
CardEffectDefinitionSO
CardStatusDefinitionSO
CardReactiveRule
CardConditionDefinition
CardOperationDefinition
CardLifetimeDefinition
CardStackingDefinition
AbilityDefinitionSO
```

Runtime:

```text
PlayerCardRuntime
PlayerCardEffectRuntime
CardEffectInstance
CardOperationExecutor
CardExecutionContext
CardRuntimeEvent
PreparedCardCommit
IPlayerAbilityRuntime
IPlayerAbilityRuntimeRegistry
IPreparedAbilityInvocation
```

Existing collaborators:

```text
PlayerResourceWallet
PlayerCombatEffects
PlayerExtraJumpRuntime
CapabilitySet
```

## Migration From Current Code

Remove card identity enums and card-specific dispatch from
`PlayerCardRuntime`.

Move current state gradually:

1. represent knockback, Chain capacity/stacks, and doubled Energy as named
   status instances;
2. keep actual damage and reward integration in `PlayerCombatEffects`;
3. let `PlayerCardEffectRuntime` decide which active status operations apply;
4. adapt `PlayerExtraJumpRuntime` to the ability-runtime contract;
5. represent overcharge through the generic supplemental-damage operation;
6. delete obsolete prototype enums only after asset migration and tests pass.

Do not move all combat formula code into the card runtime. Card status runtime
answers authored modifier queries; `PlayerCombatEffects` remains the combat
integration boundary.

## Editor Authoring

The idempotent setup command should create:

```text
Assets/Data/Abilities/Ability_ExtraJump.asset

Assets/Data/Cards/Statuses/
    Status_KnockbackBoost.asset
    Status_EscalatingDamage.asset
    Status_DoubleHitEnergy.asset

Assets/Data/Cards/Effects/
    Effect_KnockbackCharges.asset
    Effect_EscalatingDamage.asset
    Effect_DoubleHitEnergy.asset
    Effect_ExtraJump.asset
    Effect_BaseDamageOvercharge.asset

Assets/Data/Cards/Definitions/
    five current card assets
```

It also:

- registers `PlayerExtraJumpRuntime` with the player ability registry;
- links it to `Ability_ExtraJump`;
- configures `CapabilitySet` for the prototype unlock policy;
- equips the default Neutral, Chain, and Finisher cards;
- reuses existing assets on repeated execution.

Do not edit serialized GUID references manually.

## Test Plan

### Data Composition

- two cards reuse the same status with different authored values;
- one card contains multiple commit operations;
- one card contains multiple reactive rules;
- invalid operation parameters reject definition validation;
- definitions never retain runtime stacks or owner references.

### Common Lifecycle

- charges consume once per qualifying primary request;
- multi-target requests consume once;
- supplemental damage does not consume ordinary hit charges;
- miss clears only statuses authored to react to miss;
- landing removes only statuses authored to deactivate on landing;
- attack-scoped statuses cannot leak to another execution;
- scene transition and death apply their authored removal policies.

### Stacking

- add-charges respects its maximum;
- add-capacity preserves current stacks;
- replace removes the previous instance once;
- reject-if-active spends nothing;
- two cards writing to the same status use the authored stacking policy;
- unrelated statuses do not merge.

### Operations

- resource gain uses the wallet;
- damage and knockback modifiers route through combat effects;
- resource-gain modifiers change amount independently from chance;
- supplemental damage preserves provenance and proc policy;
- no operation branches on card id.

### Ability Bridge

- missing ability runtime rejects preparation;
- locked ability rejects preparation;
- unlocked Extra Jump prepares while airborne;
- grounded Extra Jump rejects without spending or committing Card Time;
- full extra-jump capacity rejects during preparation;
- successful invocation grants exactly one runtime charge;
- landing behavior remains owned by `PlayerExtraJumpRuntime`;
- world gates and card invocation reference the same
  `AbilityDefinitionSO`;
- invoking an ability does not mutate its unlock state.

### Transaction

- preparation has no gameplay side effects;
- unaffordable cards do not install statuses or invoke abilities;
- Card Time rejection does not spend or apply;
- successful application spends and applies exactly once;
- a failed transaction leaves Card Time active;
- prepared operations and ability invocations cannot be replayed.

## Implementation Order

### Phase 1: Shared Data Vocabulary

1. Add status, trigger, condition, operation, lifetime, and stacking data.
2. Add validation without runtime application.
3. Add asset-only composition tests.

### Phase 2: Runtime Status Engine

1. Add `PlayerCardEffectRuntime`.
2. Add effect instances and stacking.
3. Route current combat and locomotion events into it.
4. Add charge and deactivation tests.

### Phase 3: Prepared Transaction

1. Add card and operation preparation.
2. add atomic Card Time callback commit;
3. ensure all expected rejection happens before payment;
4. add ordering and single-use tests.

### Phase 4: Ability Bridge

1. Add ability runtime and registry contracts.
2. adapt `PlayerExtraJumpRuntime`;
3. link Extra Jump to `AbilityDefinitionSO` and `CapabilitySet`;
4. add ability and gate identity tests.

### Phase 5: Prototype Migration

1. migrate knockback;
2. migrate escalating damage;
3. migrate doubled Energy;
4. migrate Extra Jump;
5. migrate overcharge;
6. remove prototype enums and card-specific switches.

### Phase 6: Assets And Play Mode

1. create assets through idempotent Editor tooling;
2. equip the default loadout;
3. inspect serialized changes;
4. run EditMode tests;
5. validate card swapping and gated ability behavior in Play Mode.

## Acceptance Criteria

- ordinary cards are composed from shared authored data;
- no concrete effect class is required per ordinary card;
- costs, activation, reactive behavior, stacking, and deactivation use a common
  structure;
- runtime charges and stacks live in player-local instances, not assets;
- adding a card from existing primitives requires only asset creation;
- new C# primitives are reusable and never keyed by card id;
- specialized gameplay abilities use explicit runtimes;
- Extra Jump is invoked through its runtime rather than generic movement
  mutation;
- ability cards, `CapabilitySet`, and world gates share
  `AbilityDefinitionSO` identity;
- ability unlock state remains separate from card payment and invocation;
- prepared card transactions remain atomic with Card Time;
- the five current prototype cards can be represented by the hybrid model;
- the architecture can later support hand/deck selection without rewriting
  effect execution.

## Deferred Decisions

- whether card ownership, card equipment, or another progression reward unlocks
  a linked ability;
- whether a gate may require possession of a specific card in addition to an
  ability/capability;
- final hand, deck, draw, and discard rules;
- save format for active long-lived statuses;
- whether condition groups need OR or nested expressions;
- authoring UI beyond the standard Inspector;
- final status caps and card balance;
- whether Chain damage percentage belongs to a shared status or each card;
- ability cooldowns and charges that persist independently of cards;
- permanent versus temporary variants of the same movement ability.
