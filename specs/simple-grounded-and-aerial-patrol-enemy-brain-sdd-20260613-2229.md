# Simple Grounded And Aerial Patrol Enemy Brain SDD - 20260613-2229

## Contexto

This specification defines the first moving enemy behavior for the prototype:
a reusable patrol brain that can drive either a grounded or an aerial patrol
motor.

This version exists to turn the deferred `EnemyBrain` and `EnemyMotor2D`
boundaries into a small playable slice without introducing chase, attacks,
navigation, or a universal enemy superclass.

Sources used:

- `gdd/gdd-canonico-20260526-2331.md`
- `specs/prototype-architecture-sdd-20260525-2200.md`
- `specs/enemy-actor-baseline-and-training-dummy-sdd-20260612-1715.md`
- `specs/code-conventions-20260526-0014.md`
- `specs/testing-conventions-20260526-0122.md`
- `specs/unity-editor-collaboration-workflow-20260612-1609.md`
- current enemy and state-machine code under
  `Assets/Scrips/Architecture/Enemy` and
  `Assets/Scrips/Architecture/StateMachines`

## Goal

Create two readable patrol enemies:

- a grounded enemy that walks horizontally between two limits and does not
  intentionally walk through a wall or off a ledge;
- an aerial enemy that flies between two authored 2D limits without gravity.

Both variants share actor lifecycle and patrol decisions. They differ only in
their movement and environment-sensing policies.

## Non-Goals

This slice does not implement:

- player detection;
- alert, chase, or attack behavior;
- enemy attacks or contact damage;
- pathfinding, NavMesh, or platform navigation;
- jumping, dropping through platforms, or choosing routes;
- knockback, stagger, or poise;
- animation mapping;
- spawn pooling, room reset, or save persistence;
- random wandering or procedural patrol points.

## Composition

Grounded patrol enemy:

```text
GroundedPatrolEnemy
|- EnemyActor
|- EnemyHealth
|- EnemyPatrolBrain
|- GroundedEnemyPatrolMotor2D
|- Rigidbody2D
|- body collider
|- hurtbox
|- GroundCheck
|- LedgeCheck
`- WallCheck
```

Aerial patrol enemy:

```text
AerialPatrolEnemy
|- EnemyActor
|- EnemyHealth
|- EnemyPatrolBrain
|- AerialEnemyPatrolMotor2D
|- Rigidbody2D
|- hurtbox
`- visuals
```

`EnemyActor` remains the identity and lifecycle coordinator. It does not gain
AI or movement responsibilities.

## Patrol Model

Each enemy has two patrol limits stored as offsets from its initialization
position:

```text
firstPointOffset
secondPointOffset
```

Offsets are preferable to required scene `Transform` references for this first
slice because a prefab instance remains self-contained and can be moved without
rewiring child or scene objects.

Suggested defaults:

```text
grounded: (-2, 0) to (2, 0)
aerial:   (-2, 0) to (2, 1)
```

The brain captures the world-space points during explicit initialization. It
must not continuously recompute them from the enemy transform because doing so
would move the route with the actor.

Initial behavior:

1. Enter `Patrol` and move toward the second point.
2. When the motor reports arrival or obstruction, stop.
3. Enter `Idle` for `turnDelay`.
4. Reverse the target, update facing, and return to `Patrol`.
5. On actor defeat, enter `Dead`, stop the motor, and cease decisions.
6. If an explicit actor reset or health restoration makes the actor
   operational again, reinitialize the nearest valid patrol target and resume.

The turn delay should be short but visible. Initial tuning:

```text
turnDelay = 0.2 seconds
arrivalDistance = 0.05 units
```

## State Ownership

`EnemyPatrolBrain` owns `StateMachine<EnemyAIState>` and initially registers
only:

```text
Idle
Patrol
Dead
```

Do not add empty implementations for `Alert`, `Chase`, `Attack`, or `Stagger`.
Those states enter the brain only when their behavior is implemented.

State meaning in this slice:

- `Patrol`: request movement toward the active patrol point;
- `Idle`: wait at a patrol limit before reversing;
- `Dead`: stop movement and ignore normal ticks.

The state machine remains a decision model. Rigidbody writes happen only
during fixed-rate movement updates through the motor.

## Contracts

### EnemyPatrolBrain

`EnemyPatrolBrain` is a concrete `MonoBehaviour`.

Responsibilities:

- reference `EnemyActor`;
- resolve one component implementing `IEnemyPatrolMotor2D`;
- capture patrol points during explicit initialization;
- own the patrol state machine and turn timer;
- pass the active target and speed to the motor in `FixedUpdate`;
- observe actor defeat and restoration;
- expose current state and active target for tests/debugging;
- draw route gizmos when selected.

Suggested public surface:

```text
CurrentState
CurrentTarget
IsInitialized
Initialize()
Tick(deltaTime)
FixedTick(fixedDeltaTime)
ConfigureRoute(firstOffset, secondOffset)
```

`Tick` owns timers and state transitions. `FixedTick` requests physics motion.
Both methods remain public so EditMode tests can advance behavior explicitly.

### IEnemyPatrolMotor2D

This small interface prevents the brain from branching on grounded versus
aerial enemy types.

Suggested contract:

```text
Position
FacingDirection
MoveTowards(target, speed, arrivalDistance, fixedDeltaTime)
Stop()
SetFacing(direction)
```

`MoveTowards` returns:

```text
Moving
Arrived
Blocked
```

All interface methods and contract-relevant properties require XML
documentation.

### GroundedEnemyPatrolMotor2D

Responsibilities:

- preserve Rigidbody2D vertical velocity and gravity;
- set only the intended horizontal velocity;
- report `Arrived` when the horizontal target is within tolerance;
- report `Blocked` when a wall is ahead or supporting ground is absent ahead;
- stop horizontal velocity without cancelling vertical velocity;
- update facing from horizontal travel direction.

The grounded motor uses physics queries from authored check points:

```text
groundCheck
ledgeCheck
wallCheck
environmentLayer
checkRadius
```

The enemy must begin on supported ground. Recovery from being spawned in the
air is outside this slice; normal Rigidbody2D gravity may settle it.

### AerialEnemyPatrolMotor2D

Responsibilities:

- enforce zero gravity while active;
- move toward the target in both axes at constant configured speed;
- stop exactly at the target when the remaining distance is smaller than the
  next fixed-step displacement;
- update facing from the horizontal component when that component is nonzero;
- never perform ground, ledge, or wall probes in this baseline.

The baseline uses direct velocity rather than acceleration so patrol timing and
spacing remain immediately readable.

## Data And Tuning

Keep patrol tuning on `EnemyPatrolBrain` for the first slice:

```text
firstPointOffset
secondPointOffset
moveSpeed
turnDelay
arrivalDistance
startTowardSecondPoint
```

Initial values:

```text
moveSpeed = 2 units/second
turnDelay = 0.2 seconds
arrivalDistance = 0.05 units
startTowardSecondPoint = true
```

Do not add these values to `EnemyDefinitionSO` yet. Health and identity are
shared enemy-type data, while this first patrol route is instance-specific.
Extract a `PatrolMovementDefinitionSO` only after multiple enemy types need the
same authored movement profile.

Inspector fields must use `Header`, `Tooltip`, `Min`, and other relevant editor
annotations.

## Time Domain And Card Time

Patrol decisions and motion use scaled gameplay time:

```text
Time.deltaTime
Time.fixedDeltaTime
```

This makes patrol speed and turn delays slow naturally during Card Time. Do not
use unscaled time for AI or locomotion. Presentation effects may continue using
unscaled time under their existing policy.

## Physics Policy

- Movement is applied through `Rigidbody2D.linearVelocity`.
- The brain never writes transforms or rigidbody velocity directly.
- Grounded movement preserves vertical velocity.
- Aerial movement uses `gravityScale = 0`.
- Rigidbody interpolation and collision detection remain prefab tuning
  decisions.
- The patrol route is not clamped by teleporting every frame.
- On arrival, the aerial motor may snap the final small remainder to avoid
  oscillation; the grounded motor stops horizontal movement at its tolerance.

## Defeat And Reset

On `EnemyActor.Defeated`:

- transition to `Dead`;
- call `Stop`;
- ignore further patrol ticks.

On `EnemyActor.Restored`:

- resume only when `EnemyActor.IsOperational` is true;
- choose the farther endpoint as the next target when the enemy is already
  close to one endpoint;
- otherwise choose the nearest endpoint for a predictable recovery;
- enter `Patrol`.

No collider disabling, death animation, or despawn behavior is added here.

## Tests

### Pure State And Brain Tests

- initialization enters `Patrol` and selects the configured starting target;
- motor `Arrived` changes `Patrol` to `Idle`;
- `Idle` remains active until `turnDelay` elapses;
- finishing `Idle` reverses the target and returns to `Patrol`;
- motor `Blocked` follows the same stop, wait, and reverse flow;
- defeat enters `Dead` and stops the motor exactly once;
- ticks in `Dead` do not request movement;
- restoration resumes patrol with a valid target;
- repeated initialization does not duplicate actor event subscriptions.

Use a fake `IEnemyPatrolMotor2D` for brain tests so physics is not required.

### Grounded Motor Tests

- horizontal motion preserves existing vertical velocity;
- arrival tolerance stops horizontal velocity;
- missing ground ahead returns `Blocked`;
- wall ahead returns `Blocked`;
- `Stop` preserves vertical velocity;
- facing ignores zero direction.

Physics-query integration may use focused PlayMode tests if EditMode physics
simulation becomes brittle.

### Aerial Motor Tests

- movement uses the normalized 2D direction and configured speed;
- gravity is disabled;
- a short final step reaches the target without overshoot;
- arrival stops velocity;
- vertical-only travel preserves the previous horizontal facing;
- `Stop` clears velocity.

### Integration Checks

- grounded and aerial prefabs share `EnemyPatrolBrain`;
- neither prefab requires changes to player damage detection;
- Card Time slows patrol movement and turn waiting;
- defeating either enemy stops patrol immediately;
- the grounded enemy reverses before walking off a platform;
- moving a prefab instance preserves its local patrol route.

## Editor And Prefab Workflow

After runtime code and tests compile, add an idempotent Editor command that:

1. Creates or updates one grounded patrol prefab.
2. Creates or updates one aerial patrol prefab.
3. Adds the required actor, health, brain, motor, body, collider, and check
   objects.
4. Assigns deterministic component references.
5. Reuses an existing enemy definition when appropriate.

The user then performs a short Play Mode pass for:

- route placement;
- grounded ledge and wall behavior;
- aerial diagonal motion;
- turn-delay readability;
- behavior during Card Time;
- defeat shutdown.

Animation and final visual tuning remain a later slice.

## Implementation Order

1. Add patrol motor result and `IEnemyPatrolMotor2D`.
2. Add focused fake-motor tests for brain behavior.
3. Add `EnemyPatrolBrain` with `Idle`, `Patrol`, and `Dead`.
4. Add `GroundedEnemyPatrolMotor2D` and its focused tests.
5. Add `AerialEnemyPatrolMotor2D` and its focused tests.
6. Compile and run EditMode tests.
7. Add idempotent prefab/editor setup.
8. Perform the small Unity Play Mode validation pass.

## Acceptance Criteria

- One shared patrol brain drives both grounded and aerial enemies.
- Grounded and aerial movement contain no enemy-type branching in the brain.
- The grounded enemy reverses at route limits, walls, and unsafe ledges.
- The aerial enemy traverses arbitrary 2D endpoints without gravity or
  overshoot.
- Both enemies visibly pause before reversing.
- Defeat stops decisions and movement.
- Card Time slows the enemies through the normal scaled gameplay time domain.
- Existing enemy health and player damage contracts remain unchanged.
- No chase, attack, or navigation scaffolding is added prematurely.

## Deferred Decisions

- player sensing and alert rules;
- chase leash and return-to-route behavior;
- attack selection and attack cooldowns;
- contact damage;
- knockback and stagger interaction with patrol;
- animation snapshot and presentation mapping;
- route assets with more than two points;
- moving-platform support;
- room reset and spawn restoration;
- whether patrol tuning later belongs in a shared ScriptableObject.
