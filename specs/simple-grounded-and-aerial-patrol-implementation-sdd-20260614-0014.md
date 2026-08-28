# Simple Grounded And Aerial Patrol Implementation SDD - 20260614-0014

## Contexto

This document specifies the concrete implementation of:

- `specs/simple-grounded-and-aerial-patrol-enemy-brain-sdd-20260613-2229.md`

It preserves that document's design boundaries while fixing the exact runtime
files, APIs, update order, physics queries, tests, and Unity setup.

Additional implementation sources:

- `Assets/Scrips/Architecture/Enemy/EnemyActor.cs`
- `Assets/Scrips/Architecture/StateMachines/StateMachine.cs`
- `Assets/Scrips/Architecture/StateMachines/EnemyAIState.cs`
- `Assets/Scrips/Architecture/Player/CardTime/CardTimeSlowdownController.cs`
- `Assets/Tests/EditMode/Architecture/Enemy/EnemyBaselineTests.cs`

## Implementation Decision

Implement one `EnemyPatrolBrain` that owns patrol state and delegates all
Rigidbody2D behavior to `IEnemyPatrolMotor2D`.

Use two motor implementations:

- `GroundedEnemyPatrolMotor2D`;
- `AerialEnemyPatrolMotor2D`.

Do not change:

- `EnemyActor`;
- `EnemyHealth`;
- `EnemyDefinitionSO`;
- player damage detection;
- `CardTimeSlowdownController`;
- the existing `EnemyAIState` values.

## Runtime Files

Add these files under `Assets/Scrips/Architecture/Enemy/`:

```text
EnemyPatrolMoveResult.cs
IEnemyPatrolMotor2D.cs
EnemyPatrolBrain.cs
GroundedEnemyPatrolMotor2D.cs
AerialEnemyPatrolMotor2D.cs
```

Each concrete `MonoBehaviour` remains in its matching file.

Add tests under `Assets/Tests/EditMode/Architecture/Enemy/`:

```text
EnemyPatrolBrainTests.cs
GroundedEnemyPatrolMotor2DTests.cs
AerialEnemyPatrolMotor2DTests.cs
```

Add prefab tooling under `Assets/Scrips/Architecture/Editor/` only after the
runtime and tests pass:

```text
EnemyPatrolPrefabSetup.cs
```

## Shared Movement Contract

### EnemyPatrolMoveResult

```csharp
namespace TicGame.Architecture
{
    public enum EnemyPatrolMoveResult
    {
        Moving = 0,
        Arrived = 10,
        Blocked = 20
    }
}
```

### IEnemyPatrolMotor2D

```csharp
using UnityEngine;

namespace TicGame.Architecture
{
    public interface IEnemyPatrolMotor2D
    {
        /// <summary>
        /// Gets the current world-space position used by patrol decisions.
        /// </summary>
        Vector2 Position { get; }

        /// <summary>
        /// Gets the current horizontal facing as either -1 or 1.
        /// </summary>
        int FacingDirection { get; }

        /// <summary>
        /// Applies one fixed-rate movement step toward the target.
        /// </summary>
        EnemyPatrolMoveResult MoveTowards(
            Vector2 target,
            float speed,
            float arrivalDistance,
            float fixedDeltaTime);

        /// <summary>
        /// Stops patrol-owned velocity without violating the motor's vertical
        /// movement policy.
        /// </summary>
        void Stop();

        /// <summary>
        /// Updates horizontal facing when the supplied direction is nonzero.
        /// </summary>
        void SetFacing(int direction);
    }
}
```

The interface exposes facts and commands needed by the brain. It does not
expose `Rigidbody2D`, collision probes, or motor type.

## EnemyPatrolBrain

### Component Requirements

```csharp
[RequireComponent(typeof(EnemyActor))]
public sealed class EnemyPatrolBrain : MonoBehaviour
```

Do not require a concrete motor type because the same brain supports either
implementation.

### Serialized Fields

```text
Actor
actor: EnemyActor

Motor
motorComponent: MonoBehaviour

Route
firstPointOffset: Vector2 = (-2, 0)
secondPointOffset: Vector2 = (2, 0)
startTowardSecondPoint: bool = true

Movement
moveSpeed: float = 2
arrivalDistance: float = 0.05
turnDelay: float = 0.2
```

Validation:

- `moveSpeed` uses `[Min(0f)]`;
- `arrivalDistance` uses `[Min(0.001f)]`;
- `turnDelay` uses `[Min(0f)]`;
- all non-obvious fields use `Tooltip`;
- `motorComponent` must implement `IEnemyPatrolMotor2D`.

Unity cannot serialize an interface field directly, so the Inspector stores a
`MonoBehaviour`. `Initialize` casts it to `IEnemyPatrolMotor2D` and logs a
clear development error if the contract is missing.

### Public Surface

```text
CurrentState: EnemyAIState
CurrentTarget: Vector2
FirstPoint: Vector2
SecondPoint: Vector2
IsInitialized: bool

Initialize()
Tick(float deltaTime)
FixedTick(float fixedDeltaTime)
ConfigureRoute(Vector2 firstOffset, Vector2 secondOffset)
SetDependenciesForTests(EnemyActor actor, IEnemyPatrolMotor2D motor)
```

`SetDependenciesForTests` assigns runtime references without requiring a fake
motor to be a Unity component. It must unsubscribe from the previous actor
before changing dependencies.

### Internal State

```text
StateMachine<EnemyAIState> stateMachine
IEnemyPatrolMotor2D motor
Vector2 routeOrigin
Vector2 firstPoint
Vector2 secondPoint
bool targetingSecondPoint
float turnRemaining
bool subscribed
```

Register three private nested state implementations:

- `PatrolState`;
- `IdleState`;
- `DeadState`.

They are ordinary C# classes, not Unity script assets. Each implements
`IState<EnemyAIState>` and forwards its work to narrowly named private methods
on the owning brain.

Do not add placeholder classes for unimplemented states.

### Unity Lifecycle

```text
Awake
    -> resolve actor and Inspector motor component

Start
    -> Initialize

Update
    -> Tick(Time.deltaTime)

FixedUpdate
    -> FixedTick(Time.fixedDeltaTime)

OnDisable
    -> motor.Stop when initialized

OnDestroy
    -> unsubscribe from actor
```

`Initialize` must be explicit and idempotent:

1. Resolve dependencies.
2. Validate actor, motor, and actor initialization.
3. Unsubscribe and resubscribe to actor events exactly once.
4. Capture `routeOrigin` from `motor.Position`.
5. Calculate both world-space patrol points once.
6. Reset the active endpoint from `startTowardSecondPoint`.
7. Register states only once.
8. Enter `Dead` when the actor is defeated; otherwise enter `Patrol`.
9. Mark the brain initialized.

Calling `Initialize` again deliberately resets the patrol route around the
motor's current position. Normal runtime ticks never recapture the route.

### State Flow

#### Patrol

On enter:

- clear `turnRemaining`;
- face the target when horizontal delta is nonzero.

On fixed tick:

```text
result = motor.MoveTowards(
    CurrentTarget,
    moveSpeed,
    arrivalDistance,
    fixedDeltaTime)
```

When the result is `Arrived` or `Blocked`:

1. call `motor.Stop`;
2. set `turnRemaining = turnDelay`;
3. change to `Idle`.

The brain does not distinguish arrival from obstruction in this first slice.
Both mean pause and reverse.

#### Idle

On enter:

- call `motor.Stop`.

On variable-rate tick:

```text
turnRemaining = Max(0, turnRemaining - deltaTime)
```

When the timer reaches zero:

1. invert `targetingSecondPoint`;
2. update facing toward the new target;
3. change to `Patrol`.

No Rigidbody2D writes occur from variable-rate `Tick`.

#### Dead

On enter:

- call `motor.Stop`.

All ticks:

- perform no patrol work.

### Actor Events

Subscribe to:

```text
EnemyActor.Defeated
EnemyActor.Restored
```

On defeat:

- change to `Dead` unless already dead.

On restoration:

- ignore ordinary healing while `Idle` or `Patrol`;
- resume only when the current state is `Dead` and
  `actor.IsOperational` is true;
- select a recovery target;
- change to `Patrol`.

Recovery target:

```text
if within arrivalDistance of firstPoint -> target secondPoint
else if within arrivalDistance of secondPoint -> target firstPoint
else -> target the nearest endpoint
```

This avoids treating every health restoration as an AI reset.

### Gizmos

`OnDrawGizmosSelected` draws:

- first route point;
- second route point;
- a line between them;
- the current target when playing.

Outside Play Mode, calculate preview points from `transform.position` and the
serialized offsets. Do not mutate runtime route state from gizmo code.

## GroundedEnemyPatrolMotor2D

### Component Requirements

```csharp
[RequireComponent(typeof(Rigidbody2D))]
public sealed class GroundedEnemyPatrolMotor2D :
    MonoBehaviour,
    IEnemyPatrolMotor2D
```

### Serialized Fields

```text
Body
body: Rigidbody2D

Environment
environmentLayer: LayerMask
ledgeProbeOffset: Vector2 = (0.45, -0.55)
wallProbeOffset: Vector2 = (0.45, 0)
probeRadius: float = 0.08
```

Probe offsets are authored for right-facing movement. For left-facing
movement, multiply only the local X component by `-1`.

Using offsets instead of child transforms keeps prefab setup deterministic and
avoids coupling sensor placement to visual flipping.

### Move Algorithm

1. Compute horizontal delta: `target.x - body.position.x`.
2. If `Abs(deltaX) <= arrivalDistance`, stop horizontal velocity and return
   `Arrived`.
3. Resolve direction as `deltaX > 0 ? 1 : -1`.
4. Calculate wall and ledge probe positions using direction-adjusted offsets.
5. Query `Physics2D.OverlapCircle` using `environmentLayer`.
6. If wall exists or ledge support does not exist:
   - stop horizontal velocity;
   - return `Blocked`.
7. Set facing.
8. Preserve `body.linearVelocity.y`.
9. Set velocity to `(direction * speed, existingY)`.
10. Return `Moving`.

`fixedDeltaTime` is accepted to satisfy the shared contract but is not needed
for constant horizontal velocity.

### Stop Policy

```text
body.linearVelocity = (0, body.linearVelocity.y)
```

Do not change gravity scale or vertical velocity.

### Test Hooks

Provide:

```text
SetBody(Rigidbody2D body)
ConfigureProbes(LayerMask layer, Vector2 ledgeOffset,
    Vector2 wallOffset, float radius)
```

These methods support deterministic tests and Editor tooling.

## AerialEnemyPatrolMotor2D

### Component Requirements

```csharp
[RequireComponent(typeof(Rigidbody2D))]
public sealed class AerialEnemyPatrolMotor2D :
    MonoBehaviour,
    IEnemyPatrolMotor2D
```

### Initialization

Resolve the body in `Awake` and set:

```text
body.gravityScale = 0
```

The prefab setup also authors zero gravity so the serialized configuration
matches runtime behavior.

### Move Algorithm

1. Compute `toTarget = target - body.position`.
2. If `toTarget.magnitude <= arrivalDistance`:
   - set `body.position = target`;
   - clear velocity;
   - return `Arrived`.
3. Calculate `stepDistance = speed * fixedDeltaTime`.
4. If `toTarget.magnitude <= stepDistance`:
   - set `body.position = target`;
   - clear velocity;
   - return `Arrived`.
5. Set facing from `toTarget.x` when its absolute value exceeds a small
   epsilon.
6. Set `body.linearVelocity = toTarget.normalized * speed`.
7. Return `Moving`.

Directly assigning the final small Rigidbody2D position is allowed only for the
arrival snap. Normal travel remains velocity-driven.

### Stop Policy

```text
body.linearVelocity = Vector2.zero
```

### Test Hooks

Provide:

```text
SetBody(Rigidbody2D body)
```

## Facing Policy

Both motors store:

```text
FacingDirection = 1
```

`SetFacing(0)` does nothing. Other values normalize to `-1` or `1`.

This slice exposes facing as state only. It does not flip sprites, transforms,
or colliders. A later presentation/animation component may consume the value.

## Card Time Integration

No patrol class references Card Time.

The existing `CardTimeSlowdownController` changes both:

```text
Time.timeScale
Time.fixedDeltaTime
```

Patrol uses:

```text
Tick(Time.deltaTime)
FixedTick(Time.fixedDeltaTime)
```

Therefore:

- patrol turn waits slow with global gameplay time;
- Rigidbody2D patrol movement slows in real time;
- physics update cadence remains consistent with the scaled fixed step;
- restoring Card Time restores ordinary patrol timing automatically.

Never use `Time.unscaledDeltaTime` in the brain or motors.

## EditMode Tests

### EnemyPatrolBrainTests

Use:

- a real `GameObject`, `EnemyHealth`, and initialized `EnemyActor`;
- a plain C# fake implementing `IEnemyPatrolMotor2D`;
- explicit calls to `Initialize`, `Tick`, and `FixedTick`.

Required tests:

```text
Initialize_OperationalActor_EntersPatrolAndSelectsConfiguredTarget
FixedTick_Arrived_StopsAndEntersIdle
FixedTick_Blocked_StopsAndEntersIdle
Tick_BeforeTurnDelay_RemainsIdle
Tick_AfterTurnDelay_ReversesTargetAndEntersPatrol
Defeat_StopsMotorAndEntersDead
FixedTick_WhileDead_DoesNotMove
OrdinaryRestore_WhilePatrolling_DoesNotResetStateOrTarget
Restore_FromDead_ResumesPatrol
Initialize_Repeated_DoesNotDuplicateActorSubscriptions
```

The fake records move calls, stop calls, facing, position, and the next move
result.

### GroundedEnemyPatrolMotor2DTests

Use a Rigidbody2D and small colliders on a dedicated test layer where possible.

Required tests:

```text
MoveTowards_TargetAhead_PreservesVerticalVelocity
MoveTowards_InsideArrivalDistance_ReturnsArrived
MoveTowards_WallAhead_ReturnsBlocked
MoveTowards_NoLedgeSupport_ReturnsBlocked
Stop_PreservesVerticalVelocity
SetFacing_Zero_DoesNotChangeFacing
```

If EditMode physics queries do not refresh reliably after creating colliders,
call `Physics2D.SyncTransforms`. Move only the two probe integration tests to
PlayMode if that remains unreliable.

### AerialEnemyPatrolMotor2DTests

Required tests:

```text
Awake_DisablesGravity
MoveTowards_DiagonalTarget_UsesNormalizedVelocity
MoveTowards_FinalStep_SnapsWithoutOvershoot
MoveTowards_InsideArrivalDistance_ReturnsArrived
MoveTowards_VerticalTarget_PreservesFacing
Stop_ClearsVelocity
```

Tests must destroy all created GameObjects and restore any modified global
physics or time settings.

## Prefab Setup Tool

After runtime verification, add an idempotent menu command:

```text
TIC/Setup/Create Or Update Patrol Enemy Prefabs
```

Suggested output:

```text
Assets/Prefabs/Enemies/GroundedPatrolEnemy.prefab
Assets/Prefabs/Enemies/AerialPatrolEnemy.prefab
```

The command must reuse existing assets and components when rerun.

Grounded body defaults:

```text
bodyType = Dynamic
gravityScale = 1
freezeRotation = true
interpolation = Interpolate
```

Aerial body defaults:

```text
bodyType = Dynamic
gravityScale = 0
freezeRotation = true
interpolation = Interpolate
```

Both prefabs require:

- `EnemyActor`;
- `EnemyHealth`;
- `EnemyPatrolBrain`;
- the appropriate motor;
- body collider;
- hurtbox configuration compatible with current player damage targeting;
- an `EnemyDefinitionSO` reference.

Do not create attack components or animation controllers.

## Validation Order

1. Add shared result and motor interface.
2. Add grounded and aerial motors.
3. Add `EnemyPatrolBrain`.
4. Add focused EditMode tests.
5. Compile the Unity assemblies.
6. Run all EditMode tests, not only the new patrol tests.
7. Add and run the prefab setup command.
8. Inspect serialized prefab changes.
9. Validate in Play Mode:
   - grounded route endpoint;
   - wall reversal;
   - ledge reversal;
   - aerial horizontal and diagonal route;
   - visible turn pause;
   - defeat shutdown;
   - global Card Time slowdown and restoration.

## Completion Criteria

Implementation is complete when:

- both patrol variants use the same brain;
- all Rigidbody2D writes are motor-owned;
- only `Idle`, `Patrol`, and `Dead` are implemented;
- ordinary healing does not reset AI;
- defeat and revival behavior is deterministic;
- ground probes prevent intentional wall and ledge traversal;
- aerial movement cannot overshoot its endpoints;
- Card Time integration requires no direct coupling;
- all existing and new EditMode tests pass;
- prefab setup is repeatable without duplicate objects or components;
- Play Mode confirms readable patrol behavior.
