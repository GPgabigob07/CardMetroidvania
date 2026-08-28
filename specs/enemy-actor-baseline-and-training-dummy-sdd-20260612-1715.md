# Enemy Actor Baseline And Training Dummy SDD - 20260612-1715

## Contexto

This specification defines the first reusable enemy baseline and applies it to
the grounded and aerial training dummies required for the June 12, 2026
gameplay review.

The training dummy must behave as an enemy target instead of becoming a
parallel test-only damage architecture. At the same time, the project does not
yet need a universal enemy superclass containing health, movement, AI,
animation, attacks, and presentation.

This version complements:

- `specs/prototype-architecture-sdd-20260525-2200.md`
- `specs/damage-system-sdd-20260526-0102.md`
- `specs/testing-conventions-20260526-0122.md`
- `specs/card-time-and-training-dummy-review-slice-sdd-20260612-1701.md`

Existing implementation considered:

- `Assets/Scrips/Architecture/Runtime/SimpleHealth.cs`
- `Assets/Scrips/Architecture/StateMachines/EnemyAIState.cs`
- `Assets/Scrips/Architecture/Core/DamageTypes.cs`

## Decision

Enemy behavior uses composition around a small `EnemyActor` root component.

Do not create an inherited `EnemyBase` class that owns all enemy systems.
Concrete enemies and training dummies share actor, health, definition, and
presentation components. AI, movement, attacks, regeneration, and special
rules remain optional capabilities.

Expected composition:

```text
Enemy GameObject
|- EnemyActor
|- EnemyHealth
|- Collider2D
|- EnemyPresentation
|- EnemyBrain          optional
|- EnemyMotor2D        optional
|- EnemyAttackSource   optional
`- specialized behavior optional
```

Training dummy:

```text
Training Dummy
|- EnemyActor
|- EnemyHealth
|- Collider2D
|- EnemyPresentation
`- TrainingDummy
```

## Goals

- Provide one damageable enemy identity used by dummies and future enemies.
- Keep enemy lifecycle independent from AI state.
- Reuse the existing damage resolver and `IDamageable` contract.
- Allow stationary, aerial, mobile, passive, and hostile enemies to share the
  same health and presentation baseline.
- Make health reset and regeneration explicit capabilities.
- Avoid forcing dummy-only behavior into ordinary enemy data.
- Support EditMode tests without depending on Unity lifecycle timing.

## Non-Goals

This baseline does not implement:

- patrol, chase, attack, or navigation behavior;
- enemy attack hitboxes;
- stagger or poise;
- knockback response;
- loot, experience, or drops;
- spawn pooling;
- save persistence;
- factions or friendly fire;
- resistances, armor, blocking, or parry;
- boss phase logic;
- global enemy management.

## Component Responsibilities

### EnemyDefinitionSO

`EnemyDefinitionSO` contains authored values shared by instances of one enemy
type.

Initial fields:

```text
id
displayName
maxHealth
```

Inspector requirements:

- stable `id` with tooltip;
- display name;
- `maxHealth` with `[Min(1f)]`;
- values grouped with `Header` and `Tooltip`.

Do not place training regeneration settings in this asset. Regeneration is a
specialized dummy capability, not a baseline rule for all enemies.

Future versions may add:

- damage profiles;
- movement tuning references;
- attack-set references;
- tags;
- reward definitions;
- presentation references.

### EnemyActor

`EnemyActor` is the root coordinator and identity of an enemy instance.

Responsibilities:

- reference `EnemyDefinitionSO`;
- expose stable identity and display name;
- reference required `EnemyHealth`;
- initialize the actor explicitly;
- expose whether the actor is operational;
- forward high-level health and defeat notifications;
- provide a stable lookup point for future enemy capabilities;
- optionally capture the initial transform for reset-capable specializations.

Suggested public surface:

```text
Definition
Health
Id
DisplayName
IsDefeated
Initialize()
ResetActor()
```

`EnemyActor` should implement `IIdentified`.

`ResetActor` restores baseline actor state and health. Transform restoration
should be opt-in or owned by a reset-capable specialization so ordinary
enemies are not unexpectedly teleported.

`EnemyActor` must not:

- implement `IDamageable`;
- run AI decisions;
- apply movement;
- perform attacks;
- regenerate health every frame;
- manipulate HUD directly;
- contain enemy-type conditionals.

### EnemyHealth

`EnemyHealth` is the shared health component and implements `IDamageable`.

Responsibilities:

- initialize maximum and current health from `EnemyDefinitionSO`;
- accept resolved `DamageContext`;
- return `DamageResult`;
- reject additional damage while defeated;
- expose current health, maximum health, normalized health, and defeated state;
- notify local listeners about health changes, damage, and defeat;
- support explicit restoration and reset methods.

Suggested public surface:

```text
CurrentHealth
MaximumHealth
NormalizedHealth
IsDefeated
Initialize(maximumHealth)
ApplyDamage(context)
Restore(amount)
RestoreToFull()
```

Events should carry enough information for presentation and specialized
behavior without polling:

```text
HealthChanged(previous, current, maximum)
Damaged(context, result)
Defeated(context, result)
Restored(previous, current, maximum)
```

Exact event payload types may be small structs if this keeps signatures clear.

Initialization must be explicit and idempotent. `Awake` may call
`Initialize`, but tests and Editor-created objects must also be able to call it
directly.

### EnemyPresentation

`EnemyPresentation` reacts to actor and health events.

Initial review responsibilities:

- flash the configured renderer when damage is accepted;
- display current health;
- restore the original renderer color after the flash;
- remain responsive during Card Time slowdown.

Presentation timers should use unscaled time for the review so a hit flash does
not remain on screen ten times longer at `Time.timeScale = 0.1`.

The health display may be:

- a world-space health bar;
- a simple text value;
- a minimal combination of both.

Presentation must not apply damage, restore health, or decide lifecycle state.

### EnemyBrain

`EnemyBrain` is a future optional component that owns AI state and decisions.

When introduced, it may use:

```text
EnemyAIState
Idle
Patrol
Alert
Chase
Attack
Stagger
Dead
```

`EnemyAIState` belongs to the brain, not `EnemyActor`. A passive dummy has no
brain and therefore does not need a fake Idle/Dead AI state machine.

The brain observes actor defeat and disables decision-making when appropriate.

### EnemyMotor2D

`EnemyMotor2D` is a future optional component for velocity, grounded movement,
gravity policy, facing, and knockback.

Stationary and floating dummies do not require a motor. Their transforms remain
fixed by scene configuration.

### TrainingDummy

`TrainingDummy` is a specialized behavior attached to the shared enemy
baseline.

Responsibilities:

- observe damage and defeat;
- wait for a configurable regeneration delay;
- restore health at a configurable rate;
- optionally restore immediately after defeat;
- expose review/debug statistics when useful;
- ensure the dummy returns to a damageable state without scene reload.

Initial fields:

```text
regenerationDelay
regenerationPerSecond
restoreImmediatelyWhenDefeated
```

Initial review policy:

```text
restoreImmediatelyWhenDefeated = true
```

An immediate defeat restoration may occur on the next frame or after a short
configurable feedback delay so zero health remains visually legible.

Regeneration is dummy-only. Ordinary enemies do not regenerate merely because
they use `EnemyDefinitionSO` or `EnemyHealth`.

## Lifecycle

The actor lifecycle is deliberately smaller than AI state:

```text
Operational
Defeated
```

Flow:

```text
EnemyActor.Initialize
    -> EnemyHealth.Initialize
    -> Operational

accepted damage
    -> health changed
    -> presentation reacts

health reaches zero
    -> EnemyHealth.Defeated
    -> EnemyActor reports Defeated

ordinary enemy
    -> remains Defeated until external despawn/reset logic

training dummy
    -> TrainingDummy restores health
    -> EnemyActor returns to Operational
```

Returning to Operational must result from health restoration, not by manually
overriding a separate defeated flag that can disagree with health.

## Damage Integration

Player hit detection targets `IDamageable`, not concrete enemy classes.

Expected path:

```text
PlayerAttackHitDetector2D
    -> DamageRequest
    -> DamageResolver
    -> EnemyHealth.ApplyDamage
    -> DamageResult
    -> EnemyHealth events
    -> EnemyActor / EnemyPresentation / TrainingDummy
```

The hit detector may resolve `IDamageable` on the collider GameObject or its
parents according to the existing damage resolver lookup behavior.

One attack must not damage the same `EnemyHealth` more than once, even if the
enemy has multiple hurtbox colliders.

Enemy presentation and dummy regeneration consume damage results; they do not
repeat damage calculations.

## SimpleHealth Migration

`SimpleHealth` currently provides a small generic `IDamageable` implementation.

Migration policy:

1. Add `EnemyHealth` for enemy actors without immediately deleting
   `SimpleHealth`.
2. Preserve existing damage tests that use `SimpleHealth`.
3. Add equivalent focused tests for `EnemyHealth`.
4. Keep `SimpleHealth` available for non-enemy destructibles or test fixtures
   until another shared health abstraction is justified.
5. Do not make `EnemyHealth` inherit from `SimpleHealth`, because
   `SimpleHealth` is sealed and its current serialized/event ownership is not a
   stable inheritance contract.

Shared pure health arithmetic may be extracted later only if real duplication
appears.

## Prefab Structure

Create one baseline enemy prefab or prefab-ready hierarchy:

```text
EnemyActorRoot
|- Collider2D
|- Visual
`- HealthDisplay
```

Root components:

```text
EnemyActor
EnemyHealth
EnemyPresentation
```

Create a training dummy prefab using the baseline and adding
`TrainingDummy`.

For the review scene, use two instances:

### Ground Dummy

- fixed at ground height;
- reachable by all three grounded attacks;
- no Rigidbody2D required unless collision behavior proves necessary;
- collider configured as a hurtbox target.

### Aerial Dummy

- fixed above the ground;
- reachable through the current jump and shared aerial attack sequence;
- no gravity;
- no knockback;
- same definition and runtime behavior as the ground dummy unless tuning
  requires a separate asset.

## Layers And Collision

Use a dedicated target/hurtbox layer if one already exists. Otherwise create a
clear project layer for damageable enemies and use it in the player attack
target mask.

The environmental collider and hurtbox may be separate when future movement
requires it. For stationary review dummies, one collider is acceptable if it
does not obstruct the player's movement unexpectedly.

Prefer trigger hurtboxes for the review targets so they do not become physical
walls unless physical collision is intentionally desired.

## Tests

### EnemyDefinitionSO

- default maximum health is valid;
- authored maximum health is clamped or validated above zero.

### EnemyHealth

- explicit initialization sets full health;
- initialization may be called safely more than once;
- accepted damage reduces health and returns the correct result;
- lethal damage reports defeat;
- damage after defeat is rejected;
- restoration clamps to maximum health;
- restoration from zero makes the target damageable again;
- events report health changes once per operation.

### EnemyActor

- initialization wires definition health into `EnemyHealth`;
- actor identity comes from the definition with a safe fallback;
- actor defeat mirrors health defeat;
- reset restores health;
- missing required references fail clearly in development.

### TrainingDummy

- damage delays regeneration;
- regeneration restores health over time;
- additional damage restarts the delay;
- defeat restoration returns the dummy to an operational state;
- regeneration never exceeds maximum health;
- regeneration uses the configured time domain.

### Integration

- one attack damages an enemy with multiple colliders only once;
- ground and aerial dummy instances share the same baseline components;
- dummy hit presentation reacts to accepted damage;
- Card Time slowdown does not break regeneration or presentation timing under
  the selected time-domain policy.

## Acceptance Criteria

- Training dummies use the same enemy identity and health baseline intended for
  future enemies.
- No dummy-specific damage interface or resolver exists.
- Ground and aerial dummies differ by placement, not duplicated code.
- Dummies can receive repeated grounded and aerial combo tests.
- Health and hit feedback are visible.
- Dummies recover without scene reload.
- Ordinary enemies are not implicitly regenerative.
- Adding AI later does not require changing player hit detection or enemy
  health.
- `EnemyActor` remains a coordinator rather than a universal behavior class.

## Implementation Order

1. Add `EnemyDefinitionSO`.
2. Add `EnemyHealth` with explicit initialization and events.
3. Add `EnemyActor`.
4. Add focused EditMode tests.
5. Add `EnemyPresentation`.
6. Add `TrainingDummy`.
7. Create baseline and dummy prefab structures through Unity Editor tooling.
8. Place ground and aerial instances.
9. Connect player hit detection to the shared `IDamageable` path.

## Deferred Decisions

- Whether ordinary enemies use separate body and hurtbox colliders.
- Whether actor identity is instance-specific or definition-specific for saves.
- Final health-bar visual style.
- Defeat animation and despawn timing.
- Knockback and stagger ownership.
- Enemy AI state-machine implementation.
- Pooling and room-reset behavior.
