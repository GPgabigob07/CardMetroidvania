# State Machine Owned State Evolution SDD - 20260806-2146

## Contexto

This specification records a small state-machine evolution for enemy combat
brains. It exists because the project already has a minimal typed
`StateMachine<TStateId>` and `IState<TStateId>`, while the external reference
repository `GameDesign_AI_M1` demonstrates useful owner-aware state classes and
animation hooks.

Sources used:

- `AGENTS.md`
- `specs/code-conventions-20260526-0014.md`
- `specs/testing-conventions-20260526-0122.md`
- `specs/simple-grounded-and-aerial-patrol-implementation-sdd-20260614-0014.md`
- `Assets/Scrips/Architecture/StateMachines/StateMachine.cs`
- `Assets/Scrips/Architecture/StateMachines/IState.cs`
- `Assets/Scrips/Architecture/Enemy/EnemyPatrolBrain.cs`
- external reference: `GameDesign_AI_M1/M1/State Machine/BaseFSM.cs`

## Decision Summary

Do not replace the current state-machine system and do not introduce a behavior
tree for the first combat enemy.

Add only the missing owner-aware pattern needed by combat brains:

- states can receive a strongly typed owner when entered;
- states keep `Enter`, `Tick`, `FixedTick`, and `Exit`;
- animation events can be forwarded without binding gameplay truth to Animator;
- the state machine remains explicit and testable outside Unity lifecycle.

This preserves the current project style while borrowing the useful shape from
the external reference.

## Goals

- Support enemy-specific state enums such as `GolemChargerState`.
- Keep state logic split into small classes when a brain becomes complex.
- Avoid broad `switch` statements for state behavior.
- Avoid requiring MonoBehaviour state classes.
- Allow EditMode tests to advance states through explicit `Tick` and
  `FixedTick`.
- Add animation hooks only as optional signals, not as required gameplay
  authority.

## Non-Goals

This slice does not implement:

- behavior trees;
- visual graph editing;
- hierarchical state machines;
- blackboards;
- utility AI or scoring;
- global enemy AI orchestration;
- Animator-driven gameplay timing as the default rule.

## Proposed Runtime Additions

Add files under `Assets/Scrips/Architecture/StateMachines/` only if the first
combat enemy needs them:

```text
IOwnedState.cs
OwnedState.cs
```

The existing `StateMachine<TStateId>` may be extended in-place if the change is
small and remains compatible with existing patrol tests.

## Public Contracts

### IOwnedState

`IOwnedState<TStateId, TOwner>` is a state contract for states that need a
typed owner.

Suggested surface:

```csharp
public interface IOwnedState<out TStateId, in TOwner>
{
    TStateId Id { get; }
    void Enter(TOwner owner);
    void Tick(float deltaTime);
    void FixedTick(float fixedDeltaTime);
    void Exit();
    void OnAnimationEvent(string eventName);
    void OnAnimationFinished();
}
```

Interface members require XML docs following the code convention spec.

### OwnedState

`OwnedState<TStateId, TOwner>` is an optional abstract base class that stores
the owner after `Enter(owner)`.

Suggested behavior:

- `Owner` is available to derived states after enter;
- `Enter(owner)` stores the owner and calls `OnEnter()`;
- default tick, fixed tick, exit, and animation hooks are no-ops;
- derived classes override only the hooks they need.

This mirrors the useful part of the external `BaseFSMState` without inheriting
from MonoBehaviour or owning Unity lifecycle.

### StateMachine Extensions

The current `StateMachine<TStateId>` may gain:

```text
bool TryChangeState(TStateId id, bool restart = false)
void ForwardAnimationEvent(string eventName)
void ForwardAnimationFinished()
```

Rules:

- changing to the current state should no-op unless `restart` is true;
- `Exit` must run before the next state enters;
- `StateChanged(previous, next)` still fires after successful transition;
- missing states return `false` and do not exit the current state.

Animation forwarding should be optional. If the active state does not implement
an animation-aware contract, forwarding does nothing.

## Enemy Brain Pattern

Combat brains should own one typed state machine and register concrete private
or sibling state classes during initialization:

```text
GolemChargerBrain
|- StateMachine<GolemChargerState>
|- GolemIdleState
|- GolemWindupState
|- GolemChargeState
|- GolemInterruptedState
|- GolemRecoveryState
`- GolemDeadState
```

The brain remains the Unity component and dependency hub. States may call
narrowly named methods on the brain, but they should not directly perform
broad scene queries or mutate unrelated systems.

## Lifecycle Rules

Brains should expose:

```text
Initialize()
Tick(float deltaTime)
FixedTick(float fixedDeltaTime)
```

Unity `Awake`, `Start`, `Update`, and `FixedUpdate` may call these methods, but
tests must be able to call them directly without depending on lifecycle order.

## Animation Hook Rules

Animation hooks are allowed for presentation and marker synchronization:

- windup visual finished;
- active hitbox marker;
- recovery finished;
- footstep or charge impact VFX marker.

Animation hooks must not be the only source of critical combat truth unless a
future animation-event spec authorizes that for a specific move. For the first
golem slice, timers and explicit state durations remain authoritative.

## Test Strategy

EditMode tests should cover:

- entering an initial state calls `Enter` once;
- changing state calls previous `Exit` before next `Enter`;
- changing to the same state does not restart by default;
- changing to the same state with `restart` exits and re-enters;
- `Tick` and `FixedTick` forward to the active state;
- animation events forward to animation-aware states and are ignored safely
  otherwise;
- missing target state returns false and keeps the current state.

## Migration Strategy

Do not migrate `EnemyPatrolBrain` immediately. It already works and represents
a simple patrol case.

Use the evolved pattern first in `GolemChargerBrain`. If a second combat enemy
reuses the same shape, consider migrating or extracting shared helpers.

## Risks

- Over-generalizing too early could slow the golem slice.
- Letting animation events own gameplay timing could make tests fragile.
- Adding both `IState` and `IOwnedState` without clear usage could confuse
  future implementers.

Mitigation: keep the first implementation narrow and driven by the golem's
actual needs.

