# Golem Charger Enemy SDD - 20260806-2146

## Contexto

This specification defines the first authored combat enemy after the training
dummy and patrol baseline: a small slow golem that prepares a charge attack,
becomes dangerous during the charge, and can be countered with the right damage
tags.

Sources used:

- `AGENTS.md`
- `gdd/gdd-canonico-20260526-2331.md`
- `specs/enemy-actor-baseline-and-training-dummy-sdd-20260612-1715.md`
- `specs/simple-grounded-and-aerial-patrol-implementation-sdd-20260614-0014.md`
- `specs/damage-system-sdd-20260526-0102.md`
- `specs/linked-supplemental-damage-sdd-20260614-1315.md`
- `specs/state-machine-owned-state-evolution-sdd-20260806-2146.md`
- current code under `Assets/Scrips/Architecture/Enemy`
- current code under `Assets/Scrips/Architecture/Damage`
- user-authored sprite: `Assets/basic_golen_sptire_baseline.png`

## Design Summary

The golem is a small defensive charger.

It moves slowly, spends a readable window preparing an attack, then performs a
fast charge toward the player. It is inefficient to defeat by hitting it while
idle. The intended skill expression is to read the windup, answer the charge
with the right card/impact tool, then punish its interrupted state and head
weak point.

## Gameplay Role

The golem tests:

- spacing before a slow but threatening attack;
- reading a long windup;
- using card-enhanced impact at the correct moment;
- punishing a vulnerable state after interruption;
- aiming or positioning for a top/head weak point.

It should not test:

- complex navigation;
- long combo memorization;
- projectile avoidance;
- parry systems not yet implemented;
- final art timing.

## Art Baseline

The first sprite is a one-frame placeholder:

```text
Assets/basic_golen_sptire_baseline.png
```

Before prefab setup, move or reimport it under:

```text
Assets/Art/Enemies/GolemCharger/golem-charger-idle-baseline.png
```

Recommended import settings:

```text
Sprite Mode: Single for first frame, Multiple for future sheet
Pixels Per Unit: 128
Filter Mode: Point
Compression: None or minimal
Pivot: Bottom Center
```

Recommended authored cell:

```text
Canvas: 96x96 px
Idle body height: 70-80 px
Maximum windup/charge silhouette: up to 96 px
```

The golem should read as smaller than the player but large enough for a visible
head weak point.

## State Model

Use a golem-specific enum rather than adding all phase names to
`EnemyAIState`:

```text
GolemChargerState
- Idle
- Patrol
- Windup
- Charge
- Interrupted
- Recovery
- Dead
```

`Idle` and `Patrol` may collapse into one implementation for the first slice if
the golem only stands still in the arena.

### Idle/Patrol

Meaning:

- slow movement or waiting;
- searches for the player in a short forward range;
- transitions to `Windup` when the player is targetable.

Damage rule:

- accepts damage at a highly reduced multiplier.

Initial tuning:

```text
idleDamageMultiplier = 0.15
moveSpeed = 0.75 units/second
detectionRange = 4 units
```

### Windup

Meaning:

- stops or nearly stops;
- locks charge direction toward the player;
- displays a slow, readable anticipation.

Damage rule:

- damage with `Damage.Impact` interrupts;
- damage with `Damage.Card` interrupts;
- if interrupted, damage still applies;
- damage without either tag applies according to windup tuning but does not
  interrupt.

Initial tuning:

```text
windupSeconds = 0.75
windupDamageMultiplier = 1.0
```

### Charge

Meaning:

- moves quickly in the locked direction;
- applies contact/charge damage to the player;
- stops on wall, elapsed duration, maximum distance, or explicit
  interruption.

Damage rule:

- ordinary damage is rejected;
- `Damage.Card` alone is rejected;
- `Damage.Impact` alone is rejected;
- only damage containing both `Damage.Card` and `Damage.Impact` interrupts and
  applies damage.

Initial tuning:

```text
chargeSpeed = 8 units/second
chargeSeconds = 0.45
chargeDamage = 1
chargeRequiredInterruptTags = Damage.Card + Damage.Impact
```

### Interrupted

Meaning:

- golem is stunned/open after a successful interrupt;
- movement stops;
- head weak point is active.

Damage rule:

- normal body damage receives an increased multiplier;
- head weak point damage receives a higher multiplier;
- damage tags are not required while already interrupted.

Initial tuning:

```text
interruptedSeconds = 1.25
interruptedBodyDamageMultiplier = 1.5
interruptedHeadDamageMultiplier = 3.0
```

### Recovery

Meaning:

- golem exits charge or interrupted state and returns to normal behavior;
- short downtime prevents immediate repeated charge.

Damage rule:

- accepts normal damage unless playtest shows it should keep partial armor.

Initial tuning:

```text
recoverySeconds = 0.35
recoveryDamageMultiplier = 1.0
```

### Dead

Meaning:

- movement stops;
- attack hitboxes disable;
- damage no longer applies.

## Damage Tags

Use the existing `GameplayTagSet` and `DamageContext.Tags` path. Create assets
under a stable data folder such as:

```text
Assets/Data/Tags/Damage/Damage_Impact.asset
Assets/Data/Tags/Damage/Damage_Card.asset
Assets/Data/Tags/Damage/Damage_Basic.asset
```

Optional later tags:

```text
Damage.PlayerMelee
Damage.Supplemental
Damage.GolemInterrupt
```

The golem's charge interruption should use `GameplayTagSet.ContainsAll` with
the configured required tag set.

## Damage Policy

Do not place golem-specific armor rules inside `EnemyHealth`.

Add a golem-specific damage policy or a reusable state-gated wrapper:

```text
GolemChargerDamagePolicy : MonoBehaviour, IDamageable
```

Responsibilities:

- reference `EnemyHealth`;
- reference `GolemChargerBrain` or a narrow state provider;
- inspect `DamageContext.Tags`;
- inspect hit region/body part;
- decide acceptance, rejection, interruption, and damage multiplier;
- forward accepted damage to `EnemyHealth` with adjusted amount;
- return rejected damage as `DamageResult.Accepted = false`.

`EnemyHealth` remains the shared health component and should not know about
golem phases, head weak points, or card/impact combinations.

## Hit Regions

The golem needs separate hurtbox regions:

```text
GolemCharger
|- BodyHurtbox
`- HeadWeakPointHurtbox
```

The first implementation may use a small component such as:

```text
EnemyHurtboxRegion
- rootDamageable
- region = Body | HeadWeakPoint
```

Damage resolution must ultimately hit the policy on the root, but the policy
must know whether the contact came from the head weak point. If the current
damage resolver cannot carry this cleanly, prefer a small forwarding component
on the hurtbox over adding region logic to `EnemyHealth`.

## Runtime Composition

Target prefab composition:

```text
GolemCharger
|- EnemyActor
|- EnemyHealth
|- GolemChargerBrain
|- GolemChargerDamagePolicy
|- GolemChargeAttack2D
|- Rigidbody2D
|- BodyCollider2D
|- BodyHurtbox
|- HeadWeakPointHurtbox
|- ChargeHitbox
`- VisualRoot/SpriteRenderer
```

`EnemyActor` remains identity/lifecycle. `GolemChargerBrain` owns state.
`GolemChargerDamagePolicy` owns armor/weak-point rules. `GolemChargeAttack2D`
owns the active charge attack and player damage.

## Brain Responsibilities

`GolemChargerBrain` should:

- expose current `GolemChargerState`;
- initialize dependencies explicitly;
- locate the player through a serialized layer mask and range;
- lock charge direction during windup;
- own state timers;
- request charge movement through a focused motor/attack collaborator;
- receive interrupt requests from the damage policy;
- stop movement on death;
- expose `Tick` and `FixedTick` for tests.

Suggested public surface:

```text
CurrentState
ChargeDirection
IsInterruptedWindowActive
Initialize()
Tick(float deltaTime)
FixedTick(float fixedDeltaTime)
TryInterrupt(DamageContext context)
```

## Charge Attack

`GolemChargeAttack2D` should:

- move the Rigidbody2D in the locked direction during charge;
- enable charge hitbox only while active;
- apply configured damage to player targets;
- stop on wall, duration, or brain cancellation;
- avoid using broad always-on contact damage.

The existing `EnemyContactAttack2D` can remain for simple contact enemies. The
golem charge deserves a focused component because its hitbox, movement, and
state timing are coupled.

## Initial Tuning

```text
maxHealth = 12
idleDamageMultiplier = 0.15
windupDamageMultiplier = 1.0
chargeDamageMultiplier = 0.0 unless Card+Impact
interruptedBodyDamageMultiplier = 1.5
interruptedHeadDamageMultiplier = 3.0
moveSpeed = 0.75
detectionRange = 4.0
windupSeconds = 0.75
chargeSpeed = 8.0
chargeSeconds = 0.45
recoverySeconds = 0.35
interruptedSeconds = 1.25
chargeDamageToPlayer = 1
```

These values are placeholders for playtest. The important behavior is the state
relationship, not the exact numbers.

## Test Strategy

EditMode tests should cover:

- idle/patrol damage is reduced;
- windup damage with `Damage.Impact` interrupts and applies damage;
- windup damage with `Damage.Card` interrupts and applies damage;
- charge damage without tags is rejected;
- charge damage with only `Damage.Card` is rejected;
- charge damage with only `Damage.Impact` is rejected;
- charge damage with both `Damage.Card` and `Damage.Impact` interrupts and
  applies damage;
- interrupted body damage is increased;
- interrupted head weak point damage is increased more than body damage;
- death forces `Dead` and disables charge behavior;
- `Tick` and `FixedTick` can advance the brain without relying on Unity
  lifecycle.

## Editor And Asset Setup

After runtime tests pass:

1. Move the sprite to the enemy art folder.
2. Import with `128` PPU and point filtering.
3. Create a `GolemCharger` enemy definition asset.
4. Create required damage tag assets.
5. Create or update a prefab through idempotent Editor tooling.
6. Place one instance in the training arena.

The user should verify sprite framing, collider placement, head weak point
placement, and charge readability in the Unity Editor before treating the
prefab as stable.

## Open Questions

- Should recovery have normal damage or partial armor?
- Should windup non-interrupting damage be normal or reduced?
- Should the head weak point be active only while interrupted, or visually
  present but mechanically inactive before interruption?
- Should the golem charge pass through the player, stop on hit, or bounce back?
- Which existing card should produce `Damage.Card + Damage.Impact` first?

## Implementation Order

1. Add tag assets and a tiny helper for building tag sets in tests if needed.
2. Add golem-specific state enum and brain skeleton.
3. Add damage policy tests before prefab work.
4. Add body/head hurtbox forwarding.
5. Add charge attack component and tests.
6. Move/import sprite and create prefab.
7. Place golem in a small arena and tune timings.

