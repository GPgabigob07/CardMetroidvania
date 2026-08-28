# Player And Global Runtime Responsibility Refactor SDD - 20260614-1013

## Contexto

This document reviews the current player runtime after the Card Time, combat,
hitstop, animation, and patrol slices accumulated around `PlayerController`.

It exists because the current implementation works as a prototype but no
longer follows the intended ownership boundaries. In particular, global time
state and globally observable Card Time state are currently owned by the
player object.

Sources used:

- `AGENTS.md`
- `gdd/gdd-canonico-20260526-2331.md`
- `specs/prototype-architecture-sdd-20260525-2200.md`
- `specs/event-architecture-layout-20260526-0005.md`
- `specs/code-conventions-20260526-0014.md`
- `specs/player-movement-controller-sdd-20260604-2107.md`
- `specs/player-animation-state-projection-sdd-20260609-0009.md`
- `specs/attack-chain-buffer-and-speed-sdd-20260612-1015.md`
- `specs/card-time-and-training-dummy-review-slice-sdd-20260612-1701.md`
- `specs/aerial-hit-confirm-and-hitstop-sdd-20260612-1753.md`
- `specs/simple-grounded-and-aerial-patrol-implementation-sdd-20260614-0014.md`
- current code under `Assets/Scrips/Architecture`

## Executive Decision

Do not introduce one broad `GameManager`.

Introduce a scene-level `GameplayRuntimeRoot` that composes focused runtime
services. The root is an ownership and wiring boundary, not a place where
gameplay rules accumulate.

Initial global services:

```text
GameplayRuntimeRoot
|- GameplayTimeCoordinator
|- CardTimeSessionController
`- HitStopService
```

Use ScriptableObject event channels for one-to-many notifications and
cross-scene wiring. Use narrow C# interfaces for synchronous commands and
queries.

This distinction is intentional:

- event channels answer "what happened?";
- interfaces answer "can you do this?" and "what is the current state?";
- runtime state remains in scene/runtime objects, not event assets.

## Current Findings

`PlayerController` currently has 416 lines and owns responsibilities from four
different architectural layers.

### Player Simulation

These responsibilities belong to the player:

- building `PlayerContext`;
- owning `PlayerLocomotionController`;
- owning `PlayerActionRunner`;
- coordinating locomotion and action ticks;
- combining locomotion and action frames;
- applying the final frame through `PlayerMotor2D`;
- refreshing `PlayerSensors2D`;
- tracking facing;
- publishing player animation snapshots;
- applying animation-authored `PlayerActionFrame` values;
- resolving player attack-chain requests.

These operations describe the player's own simulation and should remain on
the player object, although input and attack orchestration may be extracted
into smaller player-local collaborators.

### Player Intent And Ability Input

These responsibilities are player-specific but do not all need to remain in
`PlayerController`:

- reading move, jump, dash, and attack actions;
- reading the two-button Card Time chord;
- deciding that Attack commits an active Card Time session;
- deciding that Dash takes priority over ordinary attack start;
- publishing the current player attack's Card Time availability;
- showing invalid Card Time activation feedback.

They belong under the player feature because they translate player input and
player animation windows into gameplay intent. They should move into focused
player-local components rather than into a global manager.

Suggested split:

```text
PlayerController
|- PlayerInputReader
|- PlayerCombatController
`- PlayerCardTimeInput
```

The split may be implemented incrementally. It is not necessary to create all
three classes in the first migration step.

### Global State Incorrectly Owned By Player

These responsibilities do not belong to the player:

- constructing and owning the authoritative Card Time session runtime;
- ticking Card Time's globally observable active duration;
- changing `Time.timeScale`;
- changing `Time.fixedDeltaTime`;
- restoring global time when the player is disabled;
- owning hitstop;
- arbitrating overlap between Card Time, hitstop, pause, and future cinematic
  slowdown;
- serving as the only access point for Card Time state;
- creating review-only training dummies.

The current scene also serializes only the player controller, motor, and
sensors. Card Time, hit detection, hitstop, debug presentation, and training
dummy bootstrap components are added dynamically by `PlayerController.Awake`.
This hides scene composition and makes unrelated behavior depend on whether
the player has initialized.

## Confirmed Time Ownership Defect

`CardTimeSlowdownController` and `HitStopController` independently capture and
restore global Unity time settings.

This is unsafe when effects overlap:

1. Card Time captures `1.0` and applies `0.1`.
2. Hitstop captures `0.1` and applies `0.0`.
3. Card Time ends during hitstop and restores `1.0`.
4. Hitstop ends and restores its stale captured value, `0.1`.

Other orderings can incorrectly cancel hitstop or pause. The issue cannot be
made robust by adding more capture-and-restore controllers. All writes to
Unity global time must have one owner.

## Target Ownership

### GameplayRuntimeRoot

Responsibilities:

- hold explicit references to scene-level runtime services;
- validate that required services exist once;
- establish deterministic initialization order;
- expose no gameplay rules of its own;
- avoid `DontDestroyOnLoad` until the project has a deliberate scene-loading
  and persistence policy.

The first implementation can live in `SampleScene`. A persistent boot scene is
deferred.

### GameplayTimeCoordinator

This is the only runtime component allowed to write:

```text
Time.timeScale
Time.fixedDeltaTime
```

It owns a collection of active time modifiers. Each modifier has:

```text
owner/key
kind
requestedScale
optional unscaled duration
```

Initial kinds:

```text
CardTime
HitStop
Pause
Cinematic
```

Policy:

- effective time scale is the minimum active requested scale;
- no modifiers means scale `1`;
- hitstop and pause request `0`;
- Card Time requests its configured scale;
- releasing one modifier recomputes from remaining modifiers instead of
  restoring a captured value;
- timed modifiers advance with unscaled time;
- duplicate hitstop requests extend the active duration to the longest
  remaining request;
- `fixedDeltaTime` is derived from a captured baseline while effective scale
  is greater than zero;
- at effective scale zero, keep the baseline fixed step because fixed updates
  do not advance while Unity time is paused;
- disabling the coordinator clears modifiers and restores its baseline.

Suggested contract:

```csharp
public interface IGameplayTimeService
{
    /// <summary>
    /// Creates or updates the time modifier owned by the supplied key.
    /// </summary>
    void SetModifier(object owner, GameplayTimeModifier modifier);

    /// <summary>
    /// Removes the time modifier owned by the supplied key.
    /// </summary>
    bool RemoveModifier(object owner);

    /// <summary>
    /// Gets the currently resolved gameplay time scale.
    /// </summary>
    float EffectiveTimeScale { get; }
}
```

The concrete implementation may use a more test-friendly stable token instead
of `object`. The contract must prevent anonymous modifiers that cannot be
released.

### CardTimeSessionController

Responsibilities:

- own the authoritative Card Time runtime;
- own `PlayerCardTimeConfigSO`;
- tick active duration and leniency using unscaled time;
- expose the current snapshot through `ICardTimeSession`;
- receive availability published by the current player attack;
- accept activation, commit, and cancel commands;
- request or release the Card Time time modifier;
- raise state transitions through a Card Time event channel;
- cancel safely when its source player disappears or gameplay exits.

The existing `PlayerCardTimeRuntime` pure C# rules should be retained initially.
It should be wrapped by the scene controller rather than rewritten during the
ownership migration.

The `Player` prefix may remain during the first migration because the current
availability states are authored by player attacks. A later semantic rename to
`CardTimeRuntime` is appropriate only if non-player actors can open or own Card
Time sessions.

Suggested contract:

```csharp
public interface ICardTimeSession
{
    /// <summary>
    /// Gets the latest authoritative Card Time session snapshot.
    /// </summary>
    CardTimeSessionSnapshot Current { get; }

    /// <summary>
    /// Publishes the Card Time opportunity currently offered by a source.
    /// </summary>
    void PublishAvailability(object source, PlayerCardTimeState state);

    /// <summary>
    /// Requests activation and returns the immediate routing result.
    /// </summary>
    CardTimeActivationRequestResult RequestActivation(object source);

    /// <summary>
    /// Attempts to commit the active Card Time session.
    /// </summary>
    bool TryCommit(object source);

    /// <summary>
    /// Cancels the active Card Time session.
    /// </summary>
    bool Cancel(object source);
}
```

Source identity is required so destroying or disabling one actor cannot leave
stale availability or cancel another future source's session.

### Card Time Event Channel

Add a concrete ScriptableObject channel:

```text
CardTimeSessionEventChannelSO
    payload: CardTimeSessionTransition
```

Expected listeners:

- HUD and debug presentation;
- audio;
- camera and post-processing;
- Card Time-aware enemies;
- encounter scripting;
- analytics/debug logging.

The event channel broadcasts transitions. It is not the authoritative state
store. A listener that requires the current state immediately on enable should
also receive `ICardTimeSession` or initialize from a scene-provided snapshot.

An enemy aware of Card Time can use a small component:

```text
CardTimeAwareness
|- subscribes to CardTimeSessionEventChannelSO
|- stores the latest relevant awareness state
`- exposes facts to EnemyBrain
```

`EnemyBrain` should depend on a narrow awareness contract, not on
`PlayerController`, `CardTimeSessionController`, or a specific channel asset.

Example:

```csharp
public interface ICardTimeAwareness
{
    /// <summary>
    /// Gets whether Card Time is currently active in gameplay.
    /// </summary>
    bool IsCardTimeActive { get; }

    /// <summary>
    /// Gets the active Card Time category, or None when inactive.
    /// </summary>
    PlayerCardTimeState ActiveCardTime { get; }
}
```

This permits enemy-specific reactions:

- ignore global slowdown through an explicitly unscaled behavior policy;
- change state when Card Time begins;
- parry or counter only during a Card Time category;
- move normally while other actors remain slowed;
- alter attack selection after commit or cancellation.

Whether an enemy ignores slowdown is separate from whether it is aware of the
session. Awareness must not automatically imply unscaled locomotion.

### HitStopService

Responsibilities:

- receive hitstop requests;
- ignore non-positive durations;
- request a timed zero-scale modifier from `GameplayTimeCoordinator`;
- extend repeated requests using the largest remaining unscaled duration;
- expose active state for tests and debug tooling.

Add a ScriptableObject channel:

```text
HitStopRequestEventChannelSO
    payload: HitStopRequest
```

Suggested payload:

```text
duration
source object
damage instance id
```

`PlayerAttackHitDetector2D` remains player-owned because it detects the
player's melee attack shape and confirms the current player attack. It must
stop creating or directly calling `HitStopController`. After accepted damage,
it raises `HitStopRequestEventChannelSO`.

Future enemy attacks can raise the same channel without depending on player
code.

### PlayerCardTimeInput

Player-owned responsibilities:

- own the Card Time chord runtime;
- read left/right Card Time input;
- route activation to `ICardTimeSession`;
- route Attack to commit while the session is active;
- publish current attack availability using the player as source;
- raise invalid activation feedback through a local presentation signal or
  event channel.

It must not:

- own the session runtime;
- tick global session duration;
- write Unity time;
- expose the authoritative global state.

### PlayerController After Migration

The controller remains the player simulation coordinator.

Target responsibilities:

- initialize player context, locomotion, action runner, and animation snapshot
  projection;
- tick player locomotion and actions;
- build and apply movement frames;
- expose the player action/context facts required by player-local adapters;
- receive already-read player input or retain basic input reading during the
  first incremental step.

Remove from `PlayerController`:

- `PlayerCardTimeRuntime`;
- `CardTimeSlowdownController`;
- `PlayerCardTimeDebugPresenter`;
- `PlayerCardTimeChordRuntime`;
- Card Time global transition handling;
- dynamic creation of hit detector and review bootstrap;
- dynamic creation of unrelated runtime components.

The attack combo runtime remains player-owned. Its orchestration can stay in
`PlayerController` for the first migration, then move to
`PlayerCombatController` if the controller remains difficult to read.

## ScriptableObject Event Policy

Use SO channels for:

- Card Time session transitions;
- hitstop requests;
- optional invalid Card Time activation feedback;
- future global gameplay-state changes.

Do not use SO channels for:

- reading current player facing every frame;
- returning whether an action start succeeded;
- synchronous Card Time activation result;
- direct locomotion frame construction;
- state that must exist independently of whether a transition was observed.

This avoids converting local, synchronous control flow into hidden broadcast
coupling.

Event assets should live under:

```text
Assets/Data/Events/Gameplay/
```

Concrete channel types should follow the established code layout:

```text
Assets/Scrips/Architecture/Events/Concrete/Bus/
```

## Scene Composition

Target hierarchy:

```text
Gameplay Runtime
|- GameplayRuntimeRoot
|- GameplayTimeCoordinator
|- CardTimeSessionController
`- HitStopService

Player
|- PlayerController
|- PlayerMotor2D
|- PlayerSensors2D
|- PlayerAttackHitDetector2D
`- PlayerCardTimeInput

Review Tools
`- TrainingDummyReviewBootstrap
```

`TrainingDummyReviewBootstrap` must be opt-in and separate from the player. A
production player must not create test targets as a side effect of `Awake`.

Use an idempotent Editor setup command to create and wire this deterministic
scene composition. Do not hand-edit broad scene YAML.

## Migration Plan

### Phase 1: Time Authority

1. Add pure time-modifier resolution rules and tests.
2. Add `GameplayTimeCoordinator`.
3. Add `HitStopRequest` and `HitStopRequestEventChannelSO`.
4. Add `HitStopService`.
5. Route player hitstop requests through the channel.
6. Remove `HitStopController`.

This phase fixes the highest-risk defect without changing Card Time rules.

### Phase 2: Card Time Ownership

1. Add `ICardTimeSession`.
2. Add `CardTimeSessionEventChannelSO`.
3. Add `CardTimeSessionController` around the existing
   `PlayerCardTimeRuntime`.
4. Move Card Time configuration and unscaled ticking to the session
   controller.
5. Route slowdown through `GameplayTimeCoordinator`.
6. Move debug presentation to event/session observation.
7. Move Card Time diagnostics out of `PlayerControllerEditor` and onto the
   session controller/runtime root inspectors.
8. Remove `CardTimeSlowdownController`.

### Phase 3: Player Decomposition

1. Add `PlayerCardTimeInput`.
2. Move chord input, activation, availability publication, and commit routing
   out of `PlayerController`.
3. Serialize `PlayerAttackHitDetector2D` and `PlayerCardTimeInput` explicitly
   on the player.
4. Remove dynamic component creation from `PlayerController`.
5. Move `TrainingDummyReviewBootstrap` to a dedicated review object.
6. Evaluate whether attack combo orchestration warrants a separate
   `PlayerCombatController`.

### Phase 4: Enemy Awareness Slice

1. Add `ICardTimeAwareness`.
2. Add a channel-backed `CardTimeAwareness` component.
3. Add one focused Card Time-aware enemy behavior.
4. Keep awareness and time-domain policy separate.
5. Validate ordinary enemies continue using scaled gameplay time unchanged.

## Tests

### Time Coordinator

- no modifiers resolves to baseline time;
- Card Time alone resolves to its configured scale;
- hitstop overrides Card Time with zero;
- ending hitstop while Card Time remains active restores Card Time scale;
- ending Card Time during hitstop keeps effective scale zero;
- pause and hitstop do not release each other's ownership;
- repeated hitstop requests extend duration;
- disabling the coordinator restores baseline settings;
- only the coordinator writes global time in production code.

### Card Time Session

- availability can be published by the player source;
- activation raises a transition event;
- active session requests the Card Time time modifier;
- commit and cancel release only the Card Time modifier;
- timeout uses unscaled time;
- source disable clears stale availability;
- a late state query returns the current snapshot without requiring a previous
  event;
- existing Card Time leniency tests remain valid.

### Player Integration

- Card Time input requests activation without owning the runtime;
- Attack commits active Card Time and does not start an attack on that frame;
- ordinary attack, dash, combo, locomotion, and animation behavior remain
  unchanged;
- hit confirmation still reaches the current attack action;
- successful damage raises one hitstop request;
- the player does not dynamically add global or review components.

### Enemy Awareness

- awareness updates on active, committed, cancelled, and timeout transitions;
- enemy logic can query active Card Time without referencing the player;
- disabling awareness unsubscribes from the SO channel;
- an unaware patrol enemy remains affected only through scaled gameplay time.

## Plain Validation Before Unity Execution

For each implementation phase:

- run `git diff --check` for authored source and specs;
- inspect all new concrete Unity script filenames;
- use `rg` to confirm only `GameplayTimeCoordinator` writes
  `Time.timeScale` or `Time.fixedDeltaTime`;
- use `rg` to confirm `PlayerController` no longer constructs global services;
- inspect serialized scene/event asset references after Editor setup;
- compile and run Unity tests only when the user requests the Unity validation
  pass.

## Open Decisions

### 1. Card Time Scope

Recommended baseline: one authoritative gameplay Card Time session at a time.

Open alternative: multiple simultaneous actor-owned sessions. This adds source
selection, stacking, UI, and time-modifier policy and is not justified by the
current prototype.

### 2. Player Availability During Another Source's Session

Recommended baseline: only the player can open a session, while any actor may
observe it. This supports Card Time-aware enemies without prematurely
generalizing who can activate Card Time.

### 3. Enemy Time Immunity

Recommended baseline: awareness does not grant immunity. Add an explicit enemy
time-domain policy only for a designed encounter that needs it.

### 4. Runtime Lifetime

Recommended baseline: scene-owned services in `SampleScene`.

Defer `DontDestroyOnLoad`, boot-scene persistence, and duplicate-root handling
until room loading and game-state flow are implemented.

### 5. Event Assets Versus Direct References

Recommended baseline:

- inspector-wired SO channels for broadcasts;
- scene-wired interfaces for commands and current-state queries;
- no static service locator and no global singleton.

## Acceptance Criteria

- `PlayerController` no longer owns or exposes global Card Time slowdown.
- `PlayerController` does not own hitstop.
- exactly one production component writes Unity global time.
- overlapping Card Time, hitstop, and pause cannot restore stale values.
- Card Time state is observable without referencing the player.
- a Card Time-aware enemy can be implemented through an awareness contract.
- the player remains responsible for its own input intent, movement, actions,
  attacks, and animation projection.
- review tooling is not created by the production player.
- event channels are used for broadcasts without replacing clear synchronous
  contracts.
- migration is incremental and preserves the existing pure Card Time and
  player action rules.
