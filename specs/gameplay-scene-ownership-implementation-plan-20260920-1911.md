# Gameplay Scene Ownership Implementation Plan

> **For agentic workers:** Use superpowers:executing-plans for native execution, or superpowers:subagent-driven-development if the user selects delegation. Execute task by task; track the checkboxes below.

**Goal:** Keep one player and presentation scene loaded while Blue and Pink stream independently, with valid startup, respawn and session progression.

**Architecture:** Retain the existing DontDestroyOnLoad services bootstrap. Gameplay owns player/presentation and session coordination; area scenes own disposable content. Extend the current streaming mechanism instead of introducing another loader.

**Tech Stack:** Unity 6000.3.16f1, C#, Input System, Physics2D, NUnit/Unity Test Framework, Unity Editor scene APIs. No new packages.

**Spec:** specs/gameplay-scene-ownership-sdd-20260920-1853.md. The user approved this design on 2026-09-20. That approval includes the proposed default: gates persist during a run; enemies reset on area reload. The earlier spec remains historical and unchanged.

## Global constraints

- No mandatory transition room or separate elevator scene is required.
- Keep Gameplay as the active Unity scene during play.
- Use the default shared 2D physics world; isolated physics scenes are out of scope.
- Do not change energy/deck/death balancing as part of this migration.
- Keep the existing temporary follow camera for the first slice.
- Preserve existing scene geometry, trigger placement, prefab overrides and GUIDs.
- Each concrete MonoBehaviour/ScriptableObject has a matching source filename.
- Read repository code/testing conventions and Editor collaboration workflow before execution.
- No disk saves, Addressables, advanced camera system, checkpoints or universal state snapshots.
- Scene migration happens through an explicit idempotent Editor command, not manual YAML rewriting.
- Existing standalone test scenes retain their local player/respawn behavior.

## Review focus

1. Death while loading is in progress: settle the request, then respawn exactly once (tasks 2/5).
2. Already-loaded areas and changed callback order: readiness still includes binding and progress restoration (task 4).
3. Teleport between trigger halves with several colliders: no stale direction/contact survives respawn (task 5).
4. Repeated migration and domain reload disabled: no duplicate player, service source or persistent gate state from an earlier run (tasks 4/6).
5. Reloading Blue too early: its geometry must not collide with the player inside Pink's overlapping shaft (task 7).

## File map and contracts

New runtime files go in `Assets/Scrips/Architecture/Runtime/`; new Editor files in
`Assets/Scrips/Architecture/Editor/`. Player hold logic belongs in the existing
`Player/Runtime/` directory. Test files go in
`Assets/Tests/EditMode/Architecture/Player/`, following the current test assembly.

| New file | Responsibility |
| --- | --- |
| AreaDefinition.cs | ScriptableObject: AreaId, ScenePath, DefaultSpawnId |
| AreaSpawnPoint.cs | MonoBehaviour: SpawnId and its authored transform |
| SpawnAddress.cs | Immutable area/spawn string pair; no scene object references |
| RunProgress.cs | In-memory opened gate keys, guide discovery and respawn address |
| SceneRequestResult.cs | Succeeded, Superseded or Failed plus diagnostic message |
| GameplaySceneRoot.cs | Serialized composition and session lifetime; creates one RunProgress |
| GameplayAreaCoordinator.cs | Initial area readiness, respawn orchestration and retry state |
| Player/Runtime/PlayerWorldHold.cs | Scoped input/physics hold without disabling the whole player root |
| CardTimeGuideUI.cs | Existing tutorial guide presentation moved to Gameplay |
| AreaDefinitionEditor.cs | SceneAsset picker backed by runtime scene path |
| GameplaySceneSetup.cs | Deterministic migration, composition validation and asset creation |
| GameplayAreaPlaySetup.cs | Explicit area test command and Editor setup restoration |

Reuse `SceneStreamingService`, `SceneLoadRequest`, `GameplayServicesRoot`,
`DirectionalSceneTrigger`, `SceneTriggerVolume`, `CardMeleeGate`,
`CardTimeTutorialZone` and `PlayerDeathRespawn`. Avoid a second service locator.

## Task 1: Area addresses and session progress

**Files:** Create AreaDefinition.cs, AreaSpawnPoint.cs, SpawnAddress.cs,
RunProgress.cs, AreaDefinitionEditor.cs, and AreaRunProgressTests.cs in the
directories above. Create matching .meta files through Unity import.

**Interfaces:**

```csharp
public readonly struct SpawnAddress // AreaId and SpawnId getters
public sealed class RunProgress {
    public RunProgress(SpawnAddress initialSpawn);
    public SpawnAddress Respawn { get; }
    public bool GuideDiscovered { get; }
    public bool IsGateOpen(string areaId, string gateId);
    public void OpenGate(string areaId, string gateId);
    public void DiscoverGuide();
}
```

- [ ] Add tests before implementation; the same local gate ID in different areas must not collide:

```csharp
var state = new RunProgress(new SpawnAddress("blue", "start"));
state.OpenGate("blue", "seal-1");
Assert.IsTrue(state.IsGateOpen("blue", "seal-1"));
Assert.IsFalse(state.IsGateOpen("pink", "seal-1"));
var nextRun = new RunProgress(new SpawnAddress("blue", "start"));
Assert.IsFalse(nextRun.IsGateOpen("blue", "seal-1"));
```

- [ ] Run AreaRunProgressTests in EditMode; confirm missing implementation failures.
- [ ] Implement a HashSet of `(areaId, gateId)` keys; reject empty/whitespace IDs with ArgumentException. Keep progress on a per-session object, never static or stored in assets.
- [ ] Implement serialized definition/marker fields with tooltips. The Editor picker writes the complete asset path. Reject duplicate area IDs, paths, spawn IDs and gate IDs during composition validation.
- [ ] Test empty IDs, duplicate gate writes, guide discovery and new-run reset. Rerun this fixture and record results.

## Task 2: Awaitable streaming results and protected ownership

**Files:** Modify SceneLoadRequest.cs and SceneStreamingService.cs; create
SceneRequestResult.cs; extend DirectionalSceneStreamingTests.cs.

**Interfaces:** Preserve `Request(string path, bool loaded)` for existing callers.
Add an overload returning `Task<SceneRequestResult>` named
`RequestAsync(string path, bool loaded)`. Add `Task WaitForIdleAsync()` and
`IDisposable ProtectScene(string path)`. Protection is reference-counted and reset
by SubsystemRegistration. All Unity calls and completions remain on the main thread.

- [ ] Extend the pure coordinator tests before changing behavior. Keep all four existing tests.
- [ ] Pin request semantics: matching requests join; opposite requests supersede outstanding callers; every caller completes once. A superseded Unity operation still finishes before the next operation begins.

```csharp
var first = request.RequestAsync(true);
var reverse = request.RequestAsync(false);
Assert.AreEqual(SceneRequestOutcome.Superseded, first.Result.Outcome);
loaded = true;
complete(null); // completes physical load; begins unload
loaded = false;
complete(null);
Assert.AreEqual(SceneRequestOutcome.Succeeded, reverse.Result.Outcome);
```

Use the existing test's `loaded`, `complete` and callback-driven SceneLoadRequest
fixture. Define SceneRequestOutcome in SceneRequestResult.cs; expose Outcome/Error.
The pure SceneLoadRequest gets the same RequestAsync(bool) contract as its adapter.

- [ ] Implement waiters with TaskCompletionSource; clear waiter collections before completing them to tolerate reentrant callbacks. Already-satisfied requests complete immediately. Backend exceptions complete Failed without automatic retry.
- [ ] Test three direction changes during one operation, repeated same-direction callers, synchronous exceptions and explicit retry. WaitForIdleAsync must include work queued by the current completion, not merely the first operation.
- [ ] Reject protected-scene unload before starting it, including a scene with no player. Retain existing player/last-scene guards and full-path identity. Test protection acquire/release and reset.
- [ ] Run DirectionalSceneStreamingTests and compile runtime/Editor assemblies. No Task.Run around Unity APIs.

## Task 3: Bind disposable gates and persistent guide

**Files:** Modify CardMeleeGate.cs and CardTimeTutorialZone.cs; create
CardTimeGuideUI.cs and AreaProgressBindingTests.cs.

**Interfaces:** `CardMeleeGate.BindProgress(RunProgress progress, string areaId)`
uses a serialized GateId. `CardTimeTutorialZone.BindGuide(CardTimeGuideUI guide)`
connects the area trigger to presentation. `CardTimeGuideUI.Bind(RunProgress progress)`
and `Discover(string guideText)` retain the current controls/content.

- [ ] Write gate restoration test: bind a fresh gate to progress with its key open;
  assert `IsOpen == true` and barrier/renderer disabled before any hit.
- [ ] Implement one idempotent gate-open method used by both restoration and real
  damage. Only real qualifying damage publishes a new progress fact; retain damage results and charge consumption behavior.
- [ ] Move current OnGUI/Update guide presentation into CardTimeGuideUI. Trigger entry
  still unlocks Card Time on the contacted player; discovery opens the guide once
  per run. Reloading the trigger must not reopen it. F1/Start/manual button still work.
- [ ] Keep legacy standalone trigger behavior through a local guide component when
  authored by existing standalone setup tools; runtime loading must not create a
  second guide in the streamed composition.
- [ ] Test gate IDs in different areas, non-enhanced hits, discovery on reload and guide
  access after destroying the trigger. Run new fixture plus CardMeleeGateTests.

## Task 4: Gameplay startup, readiness and player hold

**Files:** Create GameplaySceneRoot.cs, GameplayAreaCoordinator.cs,
Player/Runtime/PlayerWorldHold.cs and GameplayStartupTests.cs. Modify
PlayerController.cs only at input/tick/hold boundaries; reuse GameplayServicesRoot.BindScene.

**Interfaces:** Root has serialized player, HUD/guide, coordinator, area definitions
and initial SpawnAddress configuration. Coordinator implements
`Task<bool> StartAtAsync(SpawnAddress address)`, `string LastError` and
`Task<bool> RetryAsync()`. PlayerWorldHold exposes `IDisposable Acquire()`;
PlayerController exposes `SetWorldHeld(bool held)` and a read-only service-ready flag.

- [ ] Test that the player is held until all three conditions hold: scene loaded,
  services bound, spawn unique. Missing/duplicate spawn keeps it held and records error.
- [ ] Implement hold by retaining previous Rigidbody2D.simulated state, disabling its
  simulation and suppressing player input/ticks/slot callbacks. Zero velocity and
  clear buffered commands. Do not disable the player GameObject, unregister Card Time
  authority or rewrite global time. Nested holds restore only when the final lease ends.
- [ ] Root initializes one RunProgress, protects its scene, makes it active and begins
  startup. Duplicate roots fail before creating a second player session. Explicitly
  ensure Gameplay receives existing service binding; do not assume event order.
- [ ] Coordinator looks up definitions, awaits RequestAsync, then binds gate/guide
  consumers and validates markers before positioning. Already-loaded scenes take
  the same binding path. Scene-loaded area callbacks use that path for traversal too.
- [ ] Use a session cancellation/generation check after every await; destroyed roots
  cannot teleport players or publish readiness. Awaited failures leave the hold intact
  and expose an explicit Inspector Retry action; no frame-by-frame retries.
- [ ] Run startup/hold tests: loaded destination, delayed services, failed scene,
  missing marker, cancellation, nested holds and retained original simulation state.

## Task 5: Respawn arbitration and crossing reset

**Files:** Modify PlayerDeathRespawn.cs, GameplayAreaCoordinator.cs,
DirectionalSceneTrigger.cs, SceneTriggerVolume.cs and SceneStreamingService.cs;
create GameplayRespawnTests.cs.

**Interfaces:** Coordinator adds `Task<bool> RespawnAsync()`.
PlayerDeathRespawn adds `BindCoordinator(GameplayAreaCoordinator coordinator)`;
when unbound it preserves existing local-scene behavior. Service adds
`IDisposable SuspendDirectionalRequests()` and `bool DirectionalRequestsAllowed`.
Trigger adds `ResetCrossing()`; volume adds `ResetContacts()`.

- [ ] Write sequencing test asserting this trace:

```csharp
CollectionAssert.AreEqual(new[] {
    "hold", "suspend-triggers", "settle-requests", "unload-other-areas",
    "load-destination", "restore-progress", "resolve-spawn", "teleport",
    "restore-health", "reset-crossings", "resume-triggers", "release-hold"
}, trace);
```

Use injected callbacks in the coordinator's orchestration tests; exercise the real
Unity adapter separately. Error traces end before teleport/release-hold.

- [ ] Reuse ResetTransientState and current restoreHealthOnRespawn behavior. Deduplicate
  concurrent death/retry commands. Keep energy and deck policy unchanged.
- [ ] Suspend only directional requests, then await all in-flight operations. Unload
  registered areas other than the destination before loading it to prevent overlap.
  Retain Gameplay and services. A failed unload prevents teleport and is retryable.
- [ ] Reset both direction history and multi-collider contact sets before simulation
  resumes. Ignore contacts during suspension so they cannot seed a post-respawn action.
- [ ] Test death during load/unload, repeated death, failure followed by retry, spawn
  destroyed during await, multiple player colliders and legacy unbound respawn.
- [ ] Run respawn and streaming fixtures; inspect Card Time cancellation/authority and
  input readiness after revival in Play Mode.

## Task 6: Idempotent scene migration and area testing workflow

**Files:** Create GameplaySceneSetup.cs and GameplayAreaPlaySetup.cs. Modify
BlueAreaTutorialSetup.cs and PlayerHudSetup.cs as needed to distinguish streamed
composition from standalone scenes. Generate Assets/Scenes/Gameplay.unity,
Assets/Data/Areas/BlueArea.asset and PinkArea.asset; update Blue/Pink through Unity APIs.
Create GameplaySceneSetupTests.cs using temporary test scenes.

- [ ] Build a dry-run inventory of player/HUD roots, all scene object references,
  cameras/listeners, spawn markers, gate IDs and prefab overrides. Report ambiguous
  ownership and abort without saving partial migration.
- [ ] Implement `TIC/Setup/Create Or Update Gameplay Scene`: move configured player
  root and HUD with SceneManager.MoveGameObjectToScene; preserve child camera and
  references. Convert respawn target to a marker address, wire root/coordinator/guide,
  add stable gate IDs and area definitions. Reuse existing saved coordinates.
- [ ] Remove runtime overview cameras if present; preserve unrelated test scenes.
  Validate serialized object references using SerializedObject traversal: scene-object
  targets must be in the same scene after migration; shared asset references are valid.
- [ ] Add Gameplay first and required areas to the enabled build scene list while
  preserving unrelated entries. Detect an overriding active Build Profile scene list
  and update/validate that list rather than assuming global EditorBuildSettings wins.
- [ ] Update Blue setup reruns to reuse Gameplay and never recreate area-local player/HUD.
  Keep standalone movement/review setup commands explicitly standalone.
- [ ] Implement explicit `TIC/Play/Area With Gameplay` selection UI. Store selected
  area/spawn and prior EditorSceneManager.GetSceneManagerSetup in SessionState,
  save any user-confirmed dirty changes, open composition and enter play. On return
  to Edit Mode restore the prior scene setup and active scene; consume the override
  once, including when domain reload is disabled. Cancelling save cancels the command.
- [ ] Test migration twice on temporary fixtures: stable root counts/IDs/positions,
  no lost overrides or cross-scene references. Test cancelled dirty-scene prompt,
  already-open composition and play setup restoration. Save via Unity, then inspect
  resulting asset diffs before declaring migration complete.

## Task 7: Integrated verification and handoff

**Files:** New timestamped verification note under specs/; fix only files implicated
by failures. No speculative additional framework.

- [ ] Run Unity EditMode fixtures named in tasks 1-6, including existing gate and
  streaming regressions. Record actual runner/results; a C# compile is not a Unity test.
- [ ] Follow repository Editor collaboration workflow: ask the user to run the setup
  menu and save Gameplay/Blue/Pink/assets, then inspect serialized changes.
- [ ] Verify startup in build and Editor area command: one player/camera/listener,
  one services root and correct HUD/input. Repeat Play Mode with domain reload off.
- [ ] In Pink unload/reload Blue repeatedly; record unchanged player instance ID,
  health, energy, deck, unlock and Card Time source. Verify opened gates and guide.
- [ ] Kill the player while Blue is unloaded and while a request is pending; confirm
  one valid respawn and no stale crossing action or retained conflicting area.
- [ ] Measure Blue unload/reload completion and maximum-speed traversal in both
  directions. Include a deliberately delayed-load test. If geometry/timing fails,
  report the constraint; do not quietly add a travel freeze or change the shaft layout.
- [ ] Create an area-parented temporary object while Gameplay is active and unload its
  area; confirm it disappears. Check remaining player effects release destroyed targets.
- [ ] Verify missing build entry/marker and protected unload diagnostics/retry. Run
  git diff --check; confirm only intended changes and provide exact remaining limitations.

## Execution and review boundary

The tasks are sequential because startup, progress and respawn share the same
composition and request contracts. Recommend native execution in this session,
followed by an independent review after implementation. Subagent-driven execution
is available if the user prefers separate implementer/reviewer passes per task.
Execution begins after plan review and method selection; this file changes no code.
Preserve unrelated uncommitted work and stage only this feature if commits are made.
