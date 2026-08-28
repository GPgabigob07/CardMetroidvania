# Persistent Gameplay Services And Card Time Ownership SDD - 20260614-1107

## Contexto

This specification confirms the open architecture decisions from:

- `specs/player-and-global-runtime-responsibility-refactor-sdd-20260614-1013.md`

That prior review remains the detailed responsibility inventory and migration
analysis. This revision exists to record the confirmed product decisions and
replace its provisional scene-lifetime recommendation with a persistent Unity
runtime bootstrap.

Additional sources:

- `AGENTS.md`
- `specs/prototype-architecture-sdd-20260525-2200.md`
- `specs/event-architecture-layout-20260526-0005.md`
- `specs/code-conventions-20260526-0014.md`
- `specs/unity-editor-collaboration-workflow-20260612-1609.md`
- `Assets/Scrips/Architecture/Core/IGameplayModule.cs`
- `Assets/Scrips/Architecture/Runtime/GameStateController.cs`
- current Unity project settings

## Confirmed Decisions

### One Card Time Session

There is exactly one authoritative Card Time session for the running game.

The runtime does not support:

- concurrent Card Time sessions;
- separate sessions per actor;
- session stacking;
- an enemy-owned Card Time session.

### Player-Owned Activation Authority

Only player-related sources may:

- publish Card Time availability;
- request Card Time activation;
- commit the active session;
- cancel the session as part of player action interruption or teardown.

An enemy, encounter, UI object, or arbitrary event-channel listener must never
activate Card Time.

This is an authority rule, not merely a current input limitation.

The service must validate a registered player-owned source before accepting a
state-changing command. Do not expose unrestricted activation through a public
ScriptableObject event channel.

### Awareness Is Observation Only

Card Time state is globally observable.

Awareness permits an actor or system to know:

- whether Card Time is active;
- which Card Time category is active;
- whether the session committed, cancelled, or timed out.

Awareness grants no authority and no time immunity.

All gameplay actors, including aware enemies, remain affected by Card Time's
resolved gameplay scale. Card Time's own session timer, input leniency, chord
timing, service coordination, and presentation timers that must remain
readable use unscaled time.

No gameplay actor is immune to Card Time in the baseline.

### Persistent Services

Global gameplay services start automatically before the first scene and
survive scene changes.

Use:

```csharp
RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)
```

to create the runtime root.

Use:

```csharp
RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)
```

to reset static bootstrap state. This is required for reliable Play Mode
behavior when Unity domain reload settings change.

The persistent object uses:

```csharp
DontDestroyOnLoad(gameObject);
```

## Naming Decision

Use `GameplayServices` rather than `GameManager`.

Suggested root:

```text
[Gameplay Services]
|- GameplayServicesRoot
|- GameplayTimeCoordinator
|- CardTimeSessionController
|- HitStopService
`- GameStateController
```

`GameplayServicesRoot` is a composition root and lifecycle coordinator. It
must not accumulate combat, player, enemy, room, save, or UI rules.

The user-facing concept may still be described casually as the game manager,
but the implementation name should communicate that it is a container of
focused services rather than one manager class.

## Bootstrap Asset

Create a configured prefab:

```text
Assets/Resources/Runtime/GameplayServices.prefab
```

The bootstrap loads it with:

```csharp
Resources.Load<GameObject>("Runtime/GameplayServices")
```

The prefab is appropriate for this baseline because:

- it starts before an ordinary scene can provide references;
- it can serialize service configuration and SO event-channel references;
- it remains inspectable and authorable in Unity;
- the project does not yet use Addressables;
- one small bootstrap resource does not justify a more complex loading system.

Do not put ordinary gameplay content in `Resources`.

If the prefab is missing, the bootstrap must log one clear error and avoid
creating a partially configured fallback service graph.

## Bootstrap Lifecycle

### GameplayServicesBootstrap

Implement as a static class, not a `MonoBehaviour`.

Responsibilities:

1. Reset its cached root reference during `SubsystemRegistration`.
2. Before scene load, check whether a valid root already exists.
3. Load and instantiate the configured services prefab.
4. Name the instance consistently.
5. mark the root `DontDestroyOnLoad`.
6. initialize the root exactly once.

The static class may retain the root reference for duplicate prevention. It
must not become a general-purpose service locator.

### Duplicate Protection

`GameplayServicesRoot.Awake` must also defend against duplicates.

This covers:

- accidentally placing the services prefab in a scene;
- returning to a boot scene that contains an old copy;
- additive scene loading;
- Play Mode lifecycle edge cases.

Policy:

```text
first initialized root survives
later duplicate destroys its entire GameObject
```

Duplicate destruction must happen before duplicate modules subscribe to
events or write global state.

### Shutdown

On application shutdown or destruction of the authoritative root:

- shut modules down in reverse initialization order;
- unsubscribe from `SceneManager.sceneLoaded`;
- clear active time modifiers;
- restore baseline Unity time settings;
- clear bootstrap ownership only when the authoritative root is destroyed.

Scene transitions must not trigger service shutdown.

## Module Lifecycle

Reuse the existing `IGameplayModule` contract:

```text
IsInitialized
Initialize()
Shutdown()
```

Initial modules:

```text
GameplayTimeCoordinator
CardTimeSessionController
HitStopService
GameStateController adapter or revised implementation
```

Initialization order:

1. `GameplayTimeCoordinator`
2. `CardTimeSessionController`
3. `HitStopService`
4. `GameStateController`

Shutdown occurs in reverse order.

`GameStateController` currently initializes itself in `Awake`. During this
refactor it should participate in explicit module initialization so boot order
and event publication are deterministic.

## Scene Dependency Injection

Persistent services cannot be assigned directly into ordinary scene objects
through serialized scene references.

Do not solve this with:

- `FindAnyObjectByType` calls scattered across gameplay components;
- a static global `Services.Get<T>()`;
- public singleton access from every system;
- command event channels that imitate synchronous method calls.

Use scene-load injection.

### IGameplayServicesConsumer

Add a narrow contract:

```csharp
public interface IGameplayServicesConsumer
{
    /// <summary>
    /// Receives the persistent gameplay services required by this object.
    /// </summary>
    void BindGameplayServices(IGameplayServices services);
}
```

`IGameplayServices` exposes focused interfaces, not concrete components:

```text
IGameplayTimeService Time
ICardTimeSession CardTime
IHitStopService HitStop
IGameStateService GameState
```

The root:

1. subscribes to `SceneManager.sceneLoaded`;
2. walks the loaded scene's root objects;
3. finds enabled and disabled `MonoBehaviour` consumers in that scene;
4. injects the service facade once;
5. also injects the active scene present when initialization completes.

Injection is a scene-bound operation, not a per-frame search.

Consumers must tolerate binding before `Start`. They must not perform global
service lookup in `Awake`.

### Player Binding

`PlayerCardTimeInput` implements `IGameplayServicesConsumer`.

After binding, it registers itself as a player-owned Card Time source and
receives a source token scoped to its lifetime.

On disable or destroy it:

- clears its published availability;
- unregisters the source token;
- cancels an active session only when that session is owned by its token and
  player interruption rules require cancellation.

`PlayerController` does not bind global time or hitstop services.

### Hitstop Binding

The preferred combat boundary remains:

```text
PlayerAttackHitDetector2D
    -> HitStopRequestEventChannelSO
    -> HitStopService
    -> GameplayTimeCoordinator
```

This supports player, enemy, hazard, and scripted damage sources without
giving them direct time-control authority.

Hitstop requests are notifications with validated duration, not ownership of
the global clock.

## Card Time Authority Model

### Registration

`CardTimeSessionController` registers sources explicitly:

```csharp
ICardTimeSourceToken RegisterPlayerSource(Object owner);
```

The token is:

- issued only through the player binding path;
- required for availability, activation, commit, and cancel commands;
- invalid after unregistering;
- not serialized into assets;
- not transferable to another actor.

The use of `Object owner` is for lifecycle identity and debugging. The service
must not infer authority from tags, names, layers, or `PlayerController`
searches.

### Public Observation

Observation remains available through:

```text
ICardTimeSession.Current
CardTimeSessionEventChannelSO
```

Observers never receive a source token.

The SO channel broadcasts `CardTimeSessionTransition` for:

- HUD;
- audio;
- camera;
- VFX;
- encounter scripting;
- future enemy awareness.

### Pre-Wired Awareness

Add the protocol and event channel during the Card Time ownership migration:

```text
ICardTimeAwareness
CardTimeAwareness
CardTimeSessionEventChannelSO
```

Do not add Card Time behavior to the current patrol brain yet.

`CardTimeAwareness`:

- listens to the transition channel;
- stores the latest observation snapshot;
- exposes read-only awareness;
- has no activation methods;
- uses no unscaled movement policy;
- does not alter Rigidbody2D, Animator, or AI timing.

This pre-wires the architectural option without designing an enemy that does
not yet exist.

## Time Policy

`GameplayTimeCoordinator` remains the only production writer of:

```text
Time.timeScale
Time.fixedDeltaTime
```

Card Time itself consists of two domains:

```text
Card Time gameplay effect
    scaled global time modifier

Card Time control runtime
    unscaled session duration and input timing
```

Everything else uses the effective scaled gameplay time unless explicitly
classified as presentation or service control.

No actor-level immunity API should be introduced in this refactor.

## Revised Migration Order

### Phase 1: Persistent Root

1. Add `IGameplayServices` and `IGameplayServicesConsumer`.
2. Add `GameplayServicesRoot`.
3. Add `GameplayServicesBootstrap`.
4. Add idempotent Editor tooling that creates the configured Resources prefab.
5. Move `GameStateController` under explicit module lifecycle.
6. Validate duplicate-root and scene-load injection behavior with plain code
   checks first.

### Phase 2: Unified Time

1. Add pure time-modifier resolution.
2. Add `GameplayTimeCoordinator`.
3. Add `HitStopRequestEventChannelSO`.
4. Add `HitStopService`.
5. route damage hitstop through the event channel.
6. remove `HitStopController`.

### Phase 3: Card Time Service

1. Add player source-token authority.
2. Add `CardTimeSessionController`.
3. retain and wrap the existing pure `PlayerCardTimeRuntime`.
4. add `CardTimeSessionEventChannelSO`.
5. route Card Time scale through `GameplayTimeCoordinator`.
6. add passive `CardTimeAwareness`.
7. migrate debug/editor presentation.
8. remove `CardTimeSlowdownController`.

### Phase 4: Player Decomposition

1. Add `PlayerCardTimeInput`.
2. bind it through `IGameplayServicesConsumer`.
3. move chord, availability, activation, and commit routing out of
   `PlayerController`.
4. serialize player hit detection and Card Time input components explicitly.
5. remove dynamic global/review component creation from `PlayerController`.
6. move `TrainingDummyReviewBootstrap` to a dedicated review object.

## Plain Checks

Before any Unity test run:

- `git diff --check` passes for authored source and specs;
- every concrete `MonoBehaviour` and `ScriptableObject` has a matching file;
- only `GameplayTimeCoordinator` assigns Unity global time fields;
- only `GameplayServicesBootstrap` uses `BeforeSceneLoad`;
- the bootstrap has a `SubsystemRegistration` reset;
- the services root is the only owner of `DontDestroyOnLoad`;
- no gameplay consumer calls a static service locator;
- Card Time mutation methods require a valid player source token;
- the Card Time event channel exposes transitions but no activation command;
- awareness components contain no immunity or unscaled locomotion behavior;
- `PlayerController` creates no global service or review component.

Unity compilation, EditMode tests, and Play Mode scene-transition validation
remain a later explicit validation pass.

## Acceptance Criteria

- services start before the first scene without manual scene placement;
- one authoritative services root survives all scene changes;
- duplicate roots cannot initialize;
- scene consumers receive focused service interfaces after each load;
- one authoritative Card Time session exists;
- only registered player-owned sources can mutate Card Time;
- non-player systems can observe but never activate Card Time;
- no actor is immune to Card Time;
- Card Time's own control runtime remains functional through unscaled time;
- one coordinator owns all global Unity time writes;
- the design supports future scenes without copying manager objects into each
  scene;
- the root remains a composition boundary rather than a new gameplay blob.
