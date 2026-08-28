# Asset-Driven Card Definitions And Commit Transaction SDD - 20260614-1833

## Contexto

This specification defines the minimum architecture required to create multiple
different cards without adding enum values, tuning fields, and category
branches to `PlayerCardRuntime`.

The current implementation already proves the runtime behavior for five
prototype cards:

- Neutral knockback charges;
- Chain escalating damage;
- Chain doubled hit Energy;
- Finisher extra jump;
- Finisher base-damage overcharge.

However, card identity, selection, cost, tuning, and execution dispatch are
currently encoded directly in `PlayerCardRuntime`. Adding another card in an
existing category therefore requires changing runtime code even when it reuses
an existing effect.

Sources used:

- `gdd/gdd-canonico-20260526-2331.md`
- `specs/prototype-architecture-sdd-20260525-2200.md`
- `specs/code-conventions-20260526-0014.md`
- `specs/unity-editor-collaboration-workflow-20260612-1609.md`
- `specs/card-effects-and-energy-planning-20260614-1239.md`
- `specs/linked-supplemental-damage-sdd-20260614-1315.md`
- current card, Card Time, resource, movement, and combat code under
  `Assets/Scrips/Architecture`

This document implements Planning Slice 2 from
`specs/card-effects-and-energy-planning-20260614-1239.md`.

## Goal

After this slice, creating a card that uses an existing effect must require
only:

1. creating or reusing a card-effect asset;
2. creating a `CardDefinitionSO` asset;
3. assigning it to an equipped category slot.

Creating a genuinely new behavior may require one new concrete
`CardEffectSO`, but it must not require adding a card-specific branch to
`PlayerCardRuntime`, `PlayerController`, Card Time, or `DamageResolver`.

## Scope

This slice includes:

- authored card identity, category, fixed costs, effect, and text;
- one equipped card slot per Card Time category;
- reusable asset-backed effects;
- a prepared execution object;
- atomic Card Time commit from the player's perspective;
- migration of the five current prototype cards;
- debug presentation of equipped and committed cards;
- EditMode coverage and deterministic Editor asset/setup tooling.

This slice does not include:

- a deck, draw pile, discard pile, or random draws;
- multiple visible cards inside one Card Time session;
- card collection progression or save data;
- final card-selection UI;
- art, localization, VFX, sound, or final HUD;
- final tuning for card costs, caps, or overcharge variable input.

## Prototype Loadout Decision

Use exactly one equipped card per category:

```text
Neutral slot  -> one CardDefinitionSO
Chain slot    -> one CardDefinitionSO
Finisher slot -> one CardDefinitionSO
```

Card Time continues to expose only a category. The player runtime resolves the
equipped card in that category.

This is deliberately smaller than a hand or deck, but the runtime API must use
`CardDefinitionSO` identity rather than category enums so a later selector can
replace the slot resolver without changing execution.

## Ownership

### CardDefinitionSO

Owns authored card metadata and references:

```text
CardDefinitionSO
- stable id
- display name
- description
- Card Time category
- fixed costs
- CardEffectSO
```

It does not:

- hold mutable per-player state;
- spend resources;
- inspect Card Time;
- search the scene;
- directly mutate player components.

Suggested asset menu:

```text
TIC/Cards/Card Definition
```

Suggested asset location:

```text
Assets/Data/Cards/
```

### CardEffectSO

Defines one reusable behavior and its authored tuning.

It prepares an execution from a supplied context. The asset itself remains
stateless at runtime.

Suggested contract:

```csharp
public abstract class CardEffectSO : ScriptableObject
{
    /// <summary>
    /// Validates the requested use and creates an immutable execution that can
    /// be applied without further gameplay rejection.
    /// </summary>
    public abstract CardEffectPreparationResult TryPrepare(
        in CardExecutionContext context);
}
```

Every concrete `CardEffectSO` must be a top-level class in a matching `.cs`
file.

### PlayerCardRuntime

Owns the player's equipped card references and card transaction preparation.

It:

- resolves the equipped card for a category;
- verifies card/category agreement;
- builds `CardExecutionContext`;
- combines fixed and effect-provided variable costs;
- checks affordability;
- returns a prepared commit transaction;
- exposes the equipped cards and last result for debug presentation.

It does not contain card-specific effect switches or tuning fields.

### Existing Player Runtimes

Mutable effect state remains in the current specialized owners:

```text
PlayerCombatEffects
    Chain capacity and increments
    Energy-gain charges
    knockback charges
    armed supplemental damage

PlayerExtraJumpRuntime
    temporary extra-jump charges

PlayerResourceWallet
    resource balances and atomic payment
```

Card-effect assets install or update state in these owners. They do not retain
that state themselves.

## Proposed Data Types

### CardDefinitionSO

Suggested fields:

```csharp
[Header("Identity")]
string id
string displayName
[TextArea] string description

[Header("Card Time")]
PlayerCardTimeState category

[Header("Cost")]
List<ResourceAmount> fixedCosts

[Header("Effect")]
CardEffectSO effect
```

Rules:

- `Id` falls back to the asset name when empty;
- `category` cannot be `None`;
- fixed costs may contain multiple resources;
- repeated resource entries are valid and aggregate during payment;
- a null resource entry makes the definition invalid;
- `effect` is required;
- presentation metadata is deferred rather than mixed into gameplay fields.

`AbilityDefinitionSO` remains separate. Its broad ability classification,
single generic cost, cooldown, input, and unlock concerns do not represent a
Card Time transaction.

### CardExecutionContext

Use a readonly value carrying the facts and collaborators needed to prepare a
card:

```text
CardExecutionContext
- CardDefinitionSO Card
- PlayerCardTimeState Category
- GameObject Owner
- string AttackExecutionId
- bool IsAirborne
- float RequestedVariableResource
- PlayerResourceWallet Wallet
- PlayerCombatEffects CombatEffects
- PlayerExtraJumpRuntime ExtraJump
```

`RequestedVariableResource` is the temporary prototype input for overcharge.
The final UI may later replace this scalar with a structured selection
payload.

The context is created by `PlayerCardRuntime`; effects must not call
`FindObjectOfType`, `GetComponent` on unrelated objects, or access global
singletons.

### IPreparedCardEffect

Preparation returns an immutable runtime object:

```csharp
public interface IPreparedCardEffect
{
    /// <summary>
    /// Gets additional resource costs determined while preparing this use.
    /// </summary>
    IReadOnlyList<ResourceAmount> AdditionalCosts { get; }

    /// <summary>
    /// Applies an already validated effect without another gameplay rejection.
    /// </summary>
    void Apply();
}
```

`Apply()` is intentionally not a second validation step. Any expected failure,
such as being grounded, lacking an attack execution id, or already holding the
maximum extra-jump charges, must reject during preparation.

Prepared executions are single-use. The transaction wrapper prevents applying
one more than once.

### CardPreparationResult

Return structured diagnostics rather than only `bool`:

```text
CardPreparationResult
- bool Succeeded
- CardCommitFailure Failure
- CardDefinitionSO Card
- IReadOnlyList<ResourceAmount> TotalCosts
- PreparedCardCommit Commit
```

Suggested failure values:

```text
None
NoActiveCategory
NoEquippedCard
CategoryMismatch
InvalidDefinition
MissingDependency
InvalidContext
EffectRejected
InsufficientResources
CardTimeRejected
AlreadyApplied
```

The result should support debug UI and tests without parsing log text.

### PreparedCardCommit

The prepared commit contains:

- resolved card identity;
- aggregated total costs;
- the prepared effect;
- the wallet used during preparation;
- an internal single-use flag.

Its transaction method:

```text
TryApply()
    -> reject if already applied
    -> wallet.TrySpend(total costs)
    -> prepared effect Apply()
    -> mark applied
    -> return success
```

Because the game runs this transaction synchronously on the main thread and
the prepared effect has no expected rejection path, no resource reservation
system is required for this prototype.

Unexpected exceptions are programming errors. They should be logged with card
identity and must be covered by tests; card effects must not use exceptions for
normal rejection.

## Atomic Card Time Commit

### Problem In The Current Flow

The current controller performs:

```text
Card Time TryCommit()
    -> PlayerCardRuntime.Commit()
```

If Card Time succeeds and payment or effect application then fails, the
session has already been consumed.

Paying first has the inverse problem: Card Time may reject after resources are
spent.

### Decision

Card Time owns the state transition but accepts a synchronous transaction:

```csharp
bool TryCommit(Func<bool> transaction);
```

Required semantics:

1. verify that the session can currently commit;
2. invoke `transaction` exactly once;
3. if it returns `false`, leave the Card Time session active and unchanged;
4. if it returns `true`, transition the session to `Committed`;
5. publish the committed transition once;
6. never invoke the callback when the session cannot commit.

The controller flow becomes:

```text
PlayerCardRuntime.TryPrepare(category, attack id, airborne, variable input)
    -> failure: show reason; Card Time remains active
    -> success: receive PreparedCardCommit

Card Time TryCommit(prepared.TryApply)
    -> false: session remains active and no cost/effect is applied
    -> true: cost and effect apply once; session becomes Committed
```

The transaction callback must be synchronous and must not retain the Card Time
service for later invocation.

The parameterless `TryCommit()` may be removed after call sites and tests are
migrated. Do not preserve two production commit paths that can drift.

## Equipped Card Resolution

Use serialized `CardDefinitionSO` references on `PlayerCardRuntime`:

```text
neutralCard
chainCard
finisherCard
```

Resolution is category-only:

```text
Neutral  -> neutralCard
Chain    -> chainCard
Finisher -> finisherCard
None     -> no card
```

This switch maps stable Card Time categories to slots; it does not dispatch
effect behavior and is allowed to remain.

Validation rejects:

- an empty slot;
- a card whose authored category differs from its slot;
- a card with `None` category;
- an invalid effect or cost.

Provide a test/configuration method accepting the three definition references.
Do not expose mutable public fields.

## Prototype Effect Assets

### KnockbackChargesCardEffectSO

Authored values:

```text
charges per use
knockback multiplier
optional maximum stored charges
```

Preparation requires `PlayerCombatEffects`.

Application calls the existing knockback-charge installation API. If a maximum
is authored, clamping belongs in `PlayerCombatEffects`, where mutable charge
state is owned.

### EscalatingDamageCardEffectSO

Authored values:

```text
capacity added per use
optional maximum total capacity
```

Preparation requires `PlayerCombatEffects`.

Application adds Chain capacity. Damage per increment remains combat-effect
tuning until a later spec decides whether it belongs to each card definition
or to the shared Chain rule.

### DoubleHitEnergyCardEffectSO

Authored values:

```text
charges per use
gain multiplier
optional maximum stored charges
```

Preparation requires `PlayerCombatEffects`.

Application installs Energy-gain charges. Charges continue to be consumed by
effective primary hit opportunities, including failed 30% rolls.

### ExtraJumpCardEffectSO

Authored values:

```text
charges granted
optional maximum charges
```

Prototype assets grant one charge and use a cap of one.

Preparation requires:

- an airborne execution context;
- `PlayerExtraJumpRuntime`;
- capacity to grant the authored charge.

Application grants the already validated charge. The runtime needs an API that
can validate and grant an authored amount without introducing card-specific
logic into the asset.

### BaseDamageOverchargeCardEffectSO

Authored values:

```text
variable resource
minimum variable spend
maximum variable spend
multiplier per resource
effect id
optional supplemental damage profile
```

Preparation:

1. requires a non-empty attack execution id;
2. clamps or rejects the requested amount according to the authored policy;
3. creates one additional `ResourceAmount`;
4. calculates the committed total multiplier;
5. captures an execution that arms supplemental damage for the exact attack.

For the prototype, reject values outside the authored range rather than
silently changing a confirmed selection. The debug Inspector value may be
clamped before it is submitted.

Application uses the linked supplemental-damage path specified by
`specs/linked-supplemental-damage-sdd-20260614-1315.md`.

## Initial Card Assets

Create:

```text
Assets/Data/Cards/Effects/
    Effect_KnockbackCharges.asset
    Effect_EscalatingDamage.asset
    Effect_DoubleHitEnergy.asset
    Effect_ExtraJump.asset
    Effect_BaseDamageOvercharge.asset

Assets/Data/Cards/Definitions/
    Card_Neutral_KnockbackCharges.asset
    Card_Chain_EscalatingDamage.asset
    Card_Chain_DoubleHitEnergy.asset
    Card_Finisher_ExtraJump.asset
    Card_Finisher_BaseDamageOvercharge.asset
```

Initial definitions preserve the current prototype values unless a newer GDD
decision supersedes them:

| Card | Category | Fixed cost |
| --- | --- | ---: |
| Knockback Charges | Neutral | 5 Energy |
| Escalating Damage | Chain | 15 Energy |
| Double Hit Energy | Chain | 15 Energy, provisional |
| Extra Jump | Finisher | 5 Energy |
| Base Damage Overcharge | Finisher | 15 Energy plus X |

The default scene loadout remains:

```text
Neutral  = Knockback Charges
Chain    = Escalating Damage
Finisher = Extra Jump
```

## Migration

Remove after asset migration:

- `PrototypeNeutralCard`;
- `PrototypeChainCard`;
- `PrototypeFinisherCard`;
- card-specific tuning fields from `PlayerCardRuntime`;
- `SetPrototypeSelection`;
- card-specific `CommitChain`, `CommitFinisher`, and cost branches.

Retain and adapt:

- `PlayerResourceWallet`;
- `PlayerCombatEffects`;
- `PlayerExtraJumpRuntime`;
- `PlayerCardDebugPresenter`;
- existing attack execution identity and supplemental damage structures.

The migration must preserve current scene and prefab changes unrelated to this
slice.

## Editor Tooling

Extend or add an idempotent Editor command that:

1. creates the card folders when absent;
2. creates or loads the Energy resource;
3. creates each effect asset;
4. creates each card definition asset;
5. assigns references and current prototype tuning;
6. equips the default three cards on the Sample Scene player;
7. marks only changed assets and the target scene dirty;
8. saves assets and the scene.

Running the command twice must reuse the same asset paths and must not create
duplicates.

Use `SerializedObject` for private serialized fields where no explicit
configuration API is appropriate. Do not manually author `.meta` GUIDs or
scene YAML.

Suggested menu:

```text
TIC/Setup/Update Prototype Cards And Player Loadout
```

## Debug Presentation

Update `PlayerCardDebugPresenter` to display:

```text
ENERGY current/max
N: card display name
C: card display name
F: card display name
LAST: card name or failure reason
active Chain, Energy-hit, knockback, and extra-jump state
```

The debug presenter reads state only. It does not select, prepare, pay for, or
execute cards.

## Validation Rules

`CardDefinitionSO` should expose an explicit validation method usable by
runtime and Editor tests.

Minimum validation:

- stable id is non-empty after fallback;
- category is not `None`;
- effect exists;
- every fixed cost has a resource;
- every fixed cost is finite and non-negative;
- effect-authored numeric values are finite and inside declared bounds;
- overcharge minimum does not exceed maximum;
- overcharge variable resource exists;
- effect-specific identifiers required for provenance are non-empty.

Use `OnValidate` only to clamp representationally invalid Inspector values and
surface warnings. Runtime correctness must not depend on `OnValidate`, because
tests and dynamically created assets may bypass it.

## Test Plan

### Card Definition

- valid card definition passes validation;
- `None` category is rejected;
- missing effect is rejected;
- null resource cost is rejected;
- duplicate resource costs aggregate;
- two definitions may share one effect asset with different fixed costs and
  text.

### Equipped Slots

- each category resolves its equipped definition;
- empty slot rejects without committing or spending;
- category mismatch rejects;
- two different Chain definitions can be equipped in sequence without changing
  runtime dispatch code.

### Preparation

- grounded extra jump rejects during preparation;
- full extra-jump capacity rejects during preparation;
- overcharge without attack execution id rejects;
- overcharge contributes its variable cost;
- out-of-range overcharge selection rejects;
- missing runtime dependencies return structured failures;
- preparation does not spend resources or install an effect.

### Transaction

- unaffordable card does not invoke Card Time commit;
- Card Time rejection does not spend or apply;
- successful commit spends exactly once and applies exactly once;
- repeated invocation of one prepared commit is rejected;
- a transaction returning false leaves Card Time active;
- Card Time invokes the transaction exactly once;
- no Card Time committed event is published for a failed transaction.

### Effects

- each migrated effect preserves its current behavior;
- definitions with different tuning produce different values through the same
  effect type;
- effect assets retain no per-player mutable state;
- overcharge remains scoped to one attack execution id;
- supplemental overcharge does not duplicate gameplay-hit procs.

### Editor Setup

- setup creates all expected assets;
- a second run creates no duplicates;
- definitions reference the expected effects and Energy asset;
- the Sample Scene player receives the default loadout;
- unrelated scene objects and prefab overrides remain unchanged.

## Implementation Order

### Phase 1: Data And Preparation Contracts

1. Add `CardDefinitionSO`.
2. Add `CardEffectSO`.
3. Add execution context, preparation result, failure enum, and prepared
   execution contracts.
4. Add definition validation tests.

### Phase 2: Atomic Card Time Transaction

1. Change Card Time commit to accept a synchronous transaction.
2. Update the session controller and source implementation.
3. Add transaction ordering and failure tests.
4. Update `PlayerController` to prepare, then commit the prepared transaction.

### Phase 3: Effect Assets

1. Add concrete assets for the five prototype behaviors.
2. Add only the small runtime APIs needed to install authored values.
3. Reuse the current effect state and supplemental damage implementation.
4. Add focused tests for each effect.

### Phase 4: Runtime Migration

1. Replace enum selections with three definition references.
2. Remove card-specific dispatch and tuning from `PlayerCardRuntime`.
3. Update debug presentation.
4. Delete obsolete prototype enums after all references are removed.

### Phase 5: Asset And Scene Setup

1. Add idempotent card asset creation.
2. Equip the default loadout.
3. Save and inspect serialized changes.
4. Run EditMode tests and perform Play Mode validation.

## Play Mode Validation

In `Assets/Scenes/SampleScene.unity`:

1. confirm the debug HUD shows three named equipped cards;
2. commit each default category card and verify the exact Energy cost;
3. swap the Chain slot from Escalating Damage to Double Hit Energy and verify
   behavior changes without code changes;
4. swap the Finisher slot from Extra Jump to Base Damage Overcharge;
5. verify grounded Extra Jump rejection leaves Card Time active and spends
   nothing;
6. verify insufficient Energy leaves Card Time active and spends nothing;
7. verify a successful card closes Card Time and applies exactly once;
8. verify overcharge remains attached only to the matching finisher.

## Acceptance Criteria

- cards are represented by `CardDefinitionSO` assets with stable identity;
- two or more cards can exist in the same Card Time category;
- `PlayerCardRuntime` contains no card-specific behavior switch;
- existing effects are configured through concrete `CardEffectSO` assets;
- creating a differently tuned card from an existing effect requires no C#
  change;
- one equipped card is supported per Neutral, Chain, and Finisher category;
- preparation performs all expected validation before Card Time is consumed;
- failed payment or transaction leaves Card Time uncommitted;
- successful payment and effect installation occur exactly once;
- mutable effect state remains in player-local runtime components;
- the five current prototype cards are migrated to authored assets;
- Editor setup is idempotent and does not require manual YAML editing;
- EditMode tests cover definitions, selection, preparation, transaction
  ordering, and migrated effects.

## Deferred Decisions

- multiple equipped slots per category;
- hand, deck, draw, discard, and reshuffle rules;
- selecting among multiple cards while Card Time is active;
- card unlocks, collection ownership, and save serialization;
- card rarity, upgrades, levels, and procedural modifiers;
- final UI for variable resource selection;
- localization and presentation definitions;
- whether damage-per-Chain-increment is global or authored per card;
- final caps and balance values for the five prototype cards;
- refund policies for future delayed or externally cancellable effects.
