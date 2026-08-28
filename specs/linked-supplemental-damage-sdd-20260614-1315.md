# Linked Supplemental Damage SDD - 20260614-1315

## Contexto

This specification defines a reusable damage structure for cards and effects
that are committed during a fast attack but must resolve after the attack's
ordinary damage.

The first use is the Finisher overcharge card:

```text
fixed cost: 15 Energy
variable cost: X Energy
effect: amplify the finisher's eligible base damage
```

The player must be able to commit the card during Card Time without
retroactively changing damage that has already resolved. The ordinary finisher
therefore remains intact and a linked supplemental damage packet applies the
additional amount after a confirmed primary hit.

Sources used:

- `gdd/gdd-canonico-20260526-2331.md`
- `.docs/GDD-TIC.md`
- `specs/damage-system-sdd-20260526-0102.md`
- `specs/aerial-hit-confirm-and-hitstop-sdd-20260612-1753.md`
- `specs/card-effects-and-energy-planning-20260614-1239.md`
- current damage code under `Assets/Scrips/Architecture/Damage`

This document supersedes the direct `StrikeBonusPercent` implementation
recommendation for the overcharge finisher in
`specs/card-effects-and-energy-planning-20260614-1239.md`. The card still
amplifies the base-damage concept, but delivers the increase through linked
supplemental damage.

## Decision

Introduce **linked supplemental damage**.

Linked supplemental damage is a new damage request created from the completed
report of a primary damage request.

```text
primary finisher request
    -> ordinary finisher damage resolves
    -> accepted targets are identified
    -> armed card payload calculates its additional amount
    -> one linked supplemental request resolves against each eligible target
```

The supplemental request:

- uses the ordinary damage application path;
- preserves source and kill credit;
- references its parent damage instance;
- has explicit supplemental provenance;
- cannot trigger ordinary hit-based procs by default;
- cannot recursively create another copy of the same effect;
- may use separate hitstop, knockback, tags, and presentation.

It is a second damage transaction, but it is not a second player attack hit.

## Why Not Mutate The Primary Hit

Card Time may be committed too late to safely alter a damage instance already
being resolved. Retrospective mutation would require undoing or correcting:

- target health;
- defeat notifications;
- kill rewards;
- hitstop;
- stagger and knockback;
- damage listeners;
- attack hit confirmation;
- card stacks and resource gains.

Applying a later correction as ordinary damage without provenance would also
duplicate procs.

Linked supplemental damage preserves transaction ordering:

```text
primary transaction completes once
supplemental transaction completes once
observers can distinguish both
```

## Terminology

### Primary Damage

Damage created directly by an attack, hazard, or card action.

For this finisher, primary damage is the ordinary finisher hit that would occur
without the overcharge card.

### Supplemental Damage

Additional damage created because another damage request resolved.

Supplemental damage has a parent instance and an explicit trigger source.

### Gameplay Hit

An attack-level effective hit used by:

- hit confirmation;
- Chain increments;
- miss detection;
- limited-hit card charges;
- energy-on-hit rolls;
- combo statistics.

Supplemental damage is not a gameplay hit unless a future effect explicitly
opts into selected proc classes.

### Eligible Base Damage

The portion of the primary formula that the overcharge card is allowed to
amplify.

For the prototype:

```text
EligibleBaseDamage =
    StrikePercent
    * (Attack * (1 + AttackBuffPercent))
```

Excluded:

- `StrikeBonusPercent`;
- `FlatDamage`;
- `FinalDamagePercent`;
- `CritValue`;
- damage added by another supplemental effect.

This keeps the card tied to the authored base strength of the finisher rather
than multiplying every unrelated modifier.

## Example

Given:

```text
EligibleBaseDamage = 10
selected total multiplier = 500% = 5.0
```

The card adds only the amount beyond the original `100%`:

```text
SupplementalDamage =
    EligibleBaseDamage * max(0, TotalMultiplier - 1)

SupplementalDamage =
    10 * (5 - 1)
    = 40
```

Resolution:

```text
ordinary finisher: 10
linked card effect: 40
combined requested damage: 50
```

The balance cap and exact Energy-to-multiplier conversion are deferred.
Runtime data must still reject negative multipliers and non-finite values.

## Runtime Model

### Damage Provenance

Add structured provenance to damage instances or requests.

Suggested types:

```text
DamageOriginKind
- Primary
- Supplemental

DamageProvenance
- OriginKind
- ParentInstanceId
- RootInstanceId
- EffectId
- ChainDepth
```

Rules:

- primary damage has no parent;
- a root primary instance uses its own instance id as `RootInstanceId`;
- supplemental damage records the immediate parent and root;
- `EffectId` identifies the card/effect that created it;
- `ChainDepth` starts at zero for primary damage and increments for derived
  requests.

Do not infer provenance from an instance-id string or presentation tag.

### Proc Policy

Add an explicit flags policy controlling which secondary systems may react.

Suggested baseline:

```text
DamageProcPolicy
- None
- ConfirmAttackHit
- AdvanceChain
- ResetMissState
- RollHitResourceGain
- ConsumeHitCharge
- TriggerOnDamageEffects
- RequestHitStop
- ApplyKnockback
- GrantKillRewards
```

The exact enum may group rules if implementation proves simpler, but policy
must remain structured and inspectable.

Default primary attack policy:

```text
ConfirmAttackHit
AdvanceChain
ResetMissState
RollHitResourceGain
ConsumeHitCharge
TriggerOnDamageEffects
RequestHitStop
ApplyKnockback
GrantKillRewards
```

Default overcharge supplemental policy:

```text
GrantKillRewards
```

The overcharge presentation may request its own small hitstop later, but the
prototype baseline does not duplicate ordinary finisher hitstop or knockback.

Kill reward policy remains enabled so an enemy killed by the supplemental
packet credits the player normally. Reward handling must still ensure one
defeat grants rewards once.

### Formula Snapshot

The current `DamageResolutionReport` exposes requested contexts and applied
results, but not the formula values used to calculate each target.

Add a per-target formula snapshot:

```text
ResolvedDamageFormula
- Attack
- StrikePercent
- StrikeBonusPercent
- AttackBuffPercent
- FlatDamage
- FinalDamagePercent
- CritValue
- EligibleBaseDamage
- RawDamage
- RequestedFinalDamage
```

`DamageTargetResult` should carry:

```text
DamageContext
DamageResult
ResolvedDamageFormula
```

The supplemental amount must use the formula snapshot, not
`DamageResult.AppliedAmount`.

Reason:

```text
requested primary damage = 10
target remaining health = 3
applied primary damage = 3
```

Using `AppliedAmount` would incorrectly treat the finisher's base as `3`.
The formula snapshot preserves the attack's actual calculated value.

### Armed Supplemental Effect

Committing the card installs an attack-scoped armed effect.

Suggested runtime data:

```text
ArmedSupplementalDamage
- EffectId
- SourceCard
- Owner
- ExpectedAttackExecutionId
- TriggerPolicy
- AmountPolicy
- ProcPolicy
- HitStopSeconds
- Consumed
```

For the overcharge finisher:

```text
TriggerPolicy = after effective primary finisher damage
AmountPolicy = eligible base damage * (selected multiplier - 1)
Consumed = after the matching primary request completes
```

The armed effect is scoped to a stable attack execution id, not merely
`PlayerActionState.Finisher`.

This prevents it from leaking into:

- a later finisher;
- another attack started during the same frame;
- damage caused by a separate player component;
- an enemy or environmental damage source.

## Finisher Card Flow

### Commit

```text
Finisher Card Time is active
    -> player selects X
    -> card runtime validates category and Energy
    -> Card Time commit succeeds
    -> wallet spends 15 + X atomically
    -> overcharge multiplier is calculated
    -> armed supplemental effect is attached to current finisher execution
```

Card Time controls timing and authority. The card runtime controls selection,
cost, and effect installation.

### Primary Hit

```text
finisher active frame detects targets
    -> primary DamageInstance is built
    -> ordinary DamageResolver.Resolve runs
    -> report contains target formula snapshots and results
```

The ordinary finisher behaves exactly as it would without the card.

### Supplemental Trigger

After the full primary request resolves:

1. verify the report belongs to the expected attack execution;
2. verify the report is primary damage;
3. select targets with accepted positive primary damage;
4. calculate supplemental amount separately for each target snapshot;
5. skip non-positive amounts;
6. skip targets no longer capable of receiving damage;
7. create linked supplemental requests;
8. resolve them through `DamageResolver`;
9. mark the armed effect consumed after processing the primary request.

The effect is consumed by the matching primary request even when all targets
reject damage. It must not remain armed for a later attack after a miss.

## Target Rules

### Effective Primary Hit

A target qualifies when:

```text
DamageResult.Accepted
&& DamageResult.AppliedAmount > 0
```

### Multi-Target Finisher

One primary request may hit multiple targets.

Rules:

- each effective primary target receives its own supplemental packet;
- each packet uses that target's primary formula snapshot;
- one card cost covers the whole finisher request;
- the card is consumed once after the request;
- target ordering follows the primary report order.

If the same damageable appears through multiple colliders, existing target
deduplication must ensure it appears only once before supplemental processing.

### Primary Hit Kills

If primary damage defeats the target:

- do not apply further gameplay damage to the defeated target;
- allow the overcharge presentation to play as a confirmed flourish;
- do not emit another defeat or reward;
- report the supplemental outcome as suppressed because the target was already
  defeated.

The combined requested amount remains useful for debug display, but health is
not driven below zero through a redundant request.

### Supplemental Hit Kills

If the target survives the primary hit and the supplemental packet defeats it:

- the supplemental request receives player/card kill credit;
- enemy defeat occurs once;
- defeat rewards occur once;
- ordinary gameplay-hit procs remain disabled.

### Blocked Or Rejected Primary Damage

No supplemental damage occurs for a target that rejected the primary damage.

Future blocked-damage behavior may opt in through a separate trigger policy.
It is not part of this card.

### Interrupted Or Missed Finisher

If the matching finisher never produces a primary damage request, the armed
effect expires when the attack execution ends.

Payment policy for the prototype:

- committing the card spends Energy;
- interruption or miss does not refund Energy.

This makes commitment a risk decision and avoids delayed refund complexity.

## Resolver Integration

Do not make `DamageResolver.Resolve` recursively generate arbitrary damage
requests from tags.

Recommended boundary:

```text
DamageResolver
    resolves one request
    returns a complete report

PlayerCombatEffects or SupplementalDamageCoordinator
    observes the completed primary report
    matches armed effects
    builds linked requests
    calls DamageResolver for each linked request
```

This keeps the pure resolver focused on one transaction while allowing
controlled derived transactions.

The coordinator must enforce:

- maximum chain depth;
- effect identity;
- parent/root provenance;
- proc policy;
- no duplicate processing of one primary report;
- deterministic target order.

Prototype maximum chain depth:

```text
1
```

This supports primary-to-supplemental damage and forbids supplemental damage
from generating further linked damage.

The data model may retain `ChainDepth` for future rebounds or multi-stage card
effects, but deeper chains are rejected in this slice.

## Damage Listener Semantics

Existing damage listeners may continue receiving both transactions because
both apply real damage.

However, listeners that implement gameplay-hit rules must inspect provenance
and proc policy.

Expected distinction:

```text
health and damage presentation
    react to primary and supplemental damage

attack confirmation, Chain, energy-on-hit, hit charges
    react only when their proc flag is enabled

kill attribution and enemy defeat
    react to either transaction, once
```

Do not redefine `DamageResolutionReport.EffectiveHitCount` as a global
gameplay-hit count. It describes accepted positive target results inside that
request.

Add a separate query where needed:

```text
report.IsGameplayHitEligible
```

or inspect the request's proc policy directly.

## Presentation

The supplemental packet should be visually distinct from the ordinary
finisher impact.

Suggested sequence:

```text
primary impact
    -> ordinary hit flash and hitstop
    -> short authored delay, possibly zero
    -> card-specific attack effect
    -> supplemental damage number/flash
```

Presentation configuration may include:

- VFX prefab or presentation id;
- delay in unscaled or scaled time;
- color;
- sound;
- camera impulse;
- supplemental damage-number style;
- hitstop override.

Presentation delay must not determine gameplay ownership or target lookup.
Targets and amounts are captured when the primary report completes.

Prototype recommendation:

- gameplay supplemental damage resolves immediately after the primary report;
- presentation may visually trail by a short delay;
- no target is held in an unresolved damage state while waiting for VFX.

If a later design requires dodgeable delayed damage, that is a different
effect with its own targeting and cancellation rules.

## Data Authoring

The overcharge card definition should author:

```text
fixed Energy cost = 15
variable resource = Energy
Energy-to-total-multiplier conversion
minimum X
maximum X, optional during prototype
supplemental damage profile
presentation definition
```

The balance cap is deliberately deferred. The runtime should not introduce a
design cap beyond representational safety and authored values.

The supplemental damage profile should default to:

```text
hitstop = 0
knockback = 0
supplemental/card damage tags
```

## Source And Credit

The supplemental `DamageInstance.SourceObject` remains the player damage
source so:

- enemy defeat credit belongs to the player;
- player-owned damage listeners can observe the result;
- analytics can group damage by actor.

Provenance additionally identifies the card:

```text
EffectId
SourceCard
ParentInstanceId
RootInstanceId
```

Do not replace the source object with a transient VFX GameObject. Presentation
objects are not combat ownership.

## Proposed Types

Names remain subject to implementation review, but the responsibilities should
remain separate:

```text
DamageOriginKind
DamageProvenance
DamageProcPolicy
ResolvedDamageFormula
SupplementalDamageDefinitionSO
ArmedSupplementalDamage
SupplementalDamageCoordinator
```

Likely existing type changes:

```text
DamageInstance
    add provenance
    add proc policy
    optionally add attack execution id

DamageTargetResult
    add resolved formula snapshot

DamageResolutionReport
    expose provenance and policy convenience queries

DamageResolver
    populate per-target formula snapshots
    honor resolver-level policies only where appropriate
```

Attack execution identity should preferably live in the damage instance or a
small attack context, not in card-specific fields.

## Implementation Order

### Phase 1: Formula Reporting

1. Add `ResolvedDamageFormula`.
2. Populate it for each target in `DamageResolver`.
3. Add it to `DamageTargetResult`.
4. Preserve all current damage values and tests.
5. Add tests proving requested and applied damage remain distinct.

### Phase 2: Provenance And Proc Policy

1. Add primary/supplemental provenance.
2. Add explicit proc-policy flags.
3. Default existing player attacks to primary policy.
4. Update hitstop routing to inspect `RequestHitStop`.
5. Prepare attack confirmation and future resource/card listeners to inspect
   their respective flags.
6. Add depth and root-parent validation.

### Phase 3: Coordinator

1. Add an attack-scoped supplemental damage coordinator under the player.
2. Arm one effect for a stable attack execution id.
3. Match one completed primary report.
4. Build one supplemental request per eligible target.
5. mark the effect consumed after processing.
6. expire it when the matching attack ends without a request.

### Phase 4: Overcharge Card Integration

1. Add the card definition and fixed `15 + X` Energy transaction.
2. calculate the committed multiplier from `X`.
3. arm the effect during Finisher Card Time commit.
4. resolve linked supplemental damage after the primary finisher.
5. add debug output for primary, supplemental, and combined requested damage.

### Phase 5: Presentation

1. Add a distinct card impact presentation.
2. suppress duplicate ordinary hitstop and knockback.
3. distinguish primary and supplemental damage in debug display.
4. validate multi-target and lethal-primary behavior in Play Mode.

## Tests

### Formula Snapshot

- snapshot records every formula input used for a target;
- `EligibleBaseDamage` excludes strike bonus, flat damage, final amplification,
  and critical multiplier;
- requested final damage is preserved when applied damage is health-clamped;
- modifiers applied before target resolution are represented in the snapshot.

### Provenance

- primary damage has no parent and depth zero;
- supplemental damage references parent and root ids;
- supplemental depth is one;
- depth greater than one is rejected in the prototype;
- effect id is preserved through the report.

### Proc Policy

- primary attacks confirm hits and may roll Energy;
- overcharge supplemental damage does not confirm another attack hit;
- supplemental damage does not increment Chain;
- supplemental damage does not consume limited-hit charges;
- supplemental damage does not roll Energy-on-hit;
- supplemental damage requests no ordinary knockback or hitstop;
- a supplemental kill still credits the player and grants one defeat reward.

### Amount

- a `500%` total multiplier applied to eligible base `10` creates supplemental
  damage `40`;
- `100%` creates no supplemental damage;
- multipliers below `100%` create no supplemental damage for this card;
- flat damage does not increase the supplemental amount;
- final amplification does not increase the supplemental amount;
- critical multiplier does not increase the supplemental amount;
- applied primary damage clamping does not reduce the supplemental amount.

### Targeting

- only effective primary targets receive supplemental damage;
- rejected primary targets receive none;
- a multi-target finisher creates one packet per effective unique target;
- target order is deterministic;
- a target killed by primary damage receives no second gameplay damage;
- a target killed by supplemental damage emits defeat once;
- destroying or disabling a target between transactions fails safely.

### Lifetime

- an armed effect matches only its attack execution id;
- one primary report consumes it once;
- duplicate observation of the same report produces no duplicate packet;
- a missed finisher expires the effect;
- interruption expires the effect;
- miss and interruption do not refund Energy in the prototype;
- the effect cannot leak into the next finisher.

## Plain Validation

Before Unity execution:

- `git diff --check` passes;
- all concrete Unity script-assets use matching filenames;
- no card-specific branch is added inside `DamageResolver`;
- supplemental processing has an explicit depth limit;
- proc policy is structured rather than inferred from string tags;
- no supplemental VFX object becomes the damage source;
- existing effective-hit tests continue to describe request-local accepted
  damage;
- current scene and project-setting changes remain untouched.

## Acceptance Criteria

- the ordinary finisher resolves unchanged;
- committing the overcharge card does not mutate already-resolved damage;
- the additional amount resolves only after an effective primary finisher hit;
- the amount derives from the primary formula snapshot, not applied health
  loss;
- primary and supplemental damage have explicit parent/root provenance;
- supplemental damage uses the shared damage application and defeat path;
- supplemental damage does not duplicate hit confirmation, Chain, Energy
  rolls, hit charges, knockback, or ordinary hitstop;
- primary lethal damage does not damage or defeat the target twice;
- supplemental lethal damage credits the player and grants rewards once;
- one implementation can later support echoes, explosions, delayed cuts,
  elemental bursts, and similar linked effects without changing the primary
  attack transaction;
- balance caps remain deferred to a later balance pass.

## Deferred Decisions

- Energy-to-multiplier conversion;
- `X` selection controls and UI;
- authored maximum `X`;
- whether future supplemental effects may opt into selected proc classes;
- target defense/resistance stages and whether supplemental amounts should
  pass through them differently;
- delayed gameplay damage versus immediate gameplay with delayed presentation;
- chain depths beyond one;
- whether combined damage should appear as one or two UI numbers;
- refund rules for other cards or interruption causes.

