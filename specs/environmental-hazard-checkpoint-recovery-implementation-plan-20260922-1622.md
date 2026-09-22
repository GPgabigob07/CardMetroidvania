# Environmental Hazard And Checkpoint Recovery Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make authored hazard triggers damage the player and return them to the last touched area checkpoint under a black cover, holding movement for at least two seconds.

**Architecture:** Checkpoint volumes update the existing in-memory `SpawnAddress`. Hazards use the existing damage resolver, then request nonlethal recovery from `GameplayAreaCoordinator`; lethal damage is already routed there by `PlayerDeathRespawn`. The coordinator owns one covered recovery operation, scene reconciliation, health policy, and the `PlayerWorldHold` lease through fade-out.

**Tech Stack:** Unity 6000.3.16f1, C# 9, Unity 2D physics and UI, existing Gameplay/Blue/Pink additive scenes and `TicGame.Architecture` assembly.

**Spec:** `specs/environmental-hazard-checkpoint-recovery-sdd-20260922-1616.md`

## Global Constraints

- Nonlethal hazard damage persists after recovery; lethal recovery retains the existing health restoration policy.
- Recovery disables input and physics immediately and releases them only when the cover is fully clear, at least 2.0 unscaled seconds after contact; loading may extend the hold.
- Fade duration starts at 0.2 seconds each direction and is configurable in the persistent Gameplay cover.
- Use `RunProgress.Respawn` (`AreaId`, `SpawnId`), never a persistent reference to an area `Transform`.
- One hazard contact causes at most one damage transaction and one recovery; a lethal hit must not start a second recovery after `PlayerDeathRespawn` reacts.
- Keep Blue/Pink area scenes, BatMachine prefab, project settings, and untracked `tic/` user changes out of code commits. Before Editor tooling or serialized asset work, follow `specs/unity-editor-collaboration-workflow-20260612-1609.md`.
- The user previously chose to skip Unity unit-test execution for now. Compile both runtime and EditMode test assemblies, inspect focused tests and interfaces, and use a Play Mode handoff. Do not report EditMode tests as run.

## Review Focus

1. The hazard's lethal `SimpleHealth.Changed` callback starts death respawn synchronously; its returned damage report must not start a second nonlethal recovery.
2. Retrying a failed nonlethal recovery must still preserve reduced health; it must not silently change to death's restore policy.
3. A slow or failed area load must keep the cover opaque and the player held; release happens only after a successful fade-out.
4. Checkpoint contact after an area reload must update the same `RunProgress`, and a missing/duplicate spawn ID must fail before teleport.
5. Repeated contact callbacks and multiple player colliders must produce one damage event while the first recovery is active.

---

## File Map

| File | Responsibility |
| --- | --- |
| `Assets/Scrips/Architecture/Runtime/AreaRespawnCheckpoint.cs` | Trigger, same-scene marker validation, and `RunProgress` address update |
| `Assets/Scrips/Architecture/Runtime/RespawnCoverUI.cs` | Persistent black cover, unscaled fades, and opaque/clear completion |
| `Assets/Scrips/Architecture/Runtime/GameplayAreaCoordinator.cs` | Area binding, one recovery task, retry mode, scene reconciliation, hold/timing ownership |
| `Assets/Scrips/Architecture/Runtime/GameplaySceneRoot.cs` | Serialized cover reference passed into coordinator |
| `Assets/Scrips/Architecture/Player/Runtime/PlayerDeathRespawn.cs` | Existing lethal request and retry delegation |
| `Assets/Scrips/Architecture/Runtime/EnvironmentalHazard2D.cs` | Per-trigger authored damage and duplicate-contact guard |
| `Assets/Scrips/Architecture/Editor/RespawnCoverSetup.cs` | Idempotent Gameplay-only cover composition and wiring |
| `Assets/Tests/EditMode/Architecture/Player/GameplayRespawnTests.cs` | Compile-aligned sequence checks for outer hold and health policy; execution deferred |
| `Assets/Tests/EditMode/Architecture/Player/AreaProgressBindingTests.cs` | Checkpoint address binding cases; execution deferred |

Each new concrete `MonoBehaviour` has its own matching `.cs` file and Unity `.meta` GUID. Do not hand-edit complex `.unity` YAML.

### Task 1: Bind Area Checkpoints To Run Progress

**Files:** Create `Assets/Scrips/Architecture/Runtime/AreaRespawnCheckpoint.cs`; modify `Assets/Scrips/Architecture/Runtime/GameplayAreaCoordinator.cs`; extend `Assets/Tests/EditMode/Architecture/Player/AreaProgressBindingTests.cs`.

**Interfaces:** Produce `AreaRespawnCheckpoint.Bind(RunProgress progress, string areaId, PlayerController player)`, `AreaRespawnCheckpoint.Configure(AreaSpawnPoint spawn)`, and `AreaRespawnCheckpoint.TryActivate(PlayerController player)` returning `bool`. The coordinator's existing `BindLoadedArea(Scene, AreaDefinition)` supplies `area.AreaId`, `progress`, and its configured `player`.

- [ ] **Step 1: Specify the checkpoint behavior in the existing EditMode test source.** Add one case that binds a checkpoint to `pink`/`pink-corridor-begin`, activates it, and asserts `progress.Respawn == new SpawnAddress("pink", "pink-corridor-begin")`; add separate cases for a foreign player and a marker in another scene leaving the address unchanged. Keep the tests unrun per the user's preference.
- [ ] **Step 2: Implement `AreaRespawnCheckpoint`.** Require a `Collider2D`, validate `isTrigger`, nonempty `spawn.SpawnId`, and `spawn.gameObject.scene == gameObject.scene`; compare the contacting `PlayerController` with the bound persistent player. `OnTriggerEnter2D(Collider2D other)` resolves `other.GetComponentInParent<PlayerController>()` and calls `TryActivate`. Re-entering the same address returns successfully without a second state mutation. Provide `OnDrawGizmos` showing the trigger and marker relation, without adding run state to the scene object.

```csharp
public bool TryActivate(PlayerController candidate)
{
    if (progress == null || candidate == null || candidate != boundPlayer
        || spawn == null || spawn.gameObject.scene != gameObject.scene
        || string.IsNullOrWhiteSpace(spawn.SpawnId)
        || !GetComponent<Collider2D>().isTrigger) return false;
    var address = new SpawnAddress(areaId, spawn.SpawnId);
    if (progress.Respawn != address) progress.SetRespawn(address);
    return true;
}
```
- [ ] **Step 3: Bind during every area load.** In `GameplayAreaCoordinator.BindLoadedArea`, visit all `AreaRespawnCheckpoint` components under loaded roots and call `Bind(progress, area.AreaId, player)`. Preserve existing gate and guide binding.
- [ ] **Step 4: Verify the boundary.** Inspect the same-scene check and authority check. After Unity imports the new script, compile `TicGame.Architecture.csproj` and `TicGame.Architecture.EditModeTests.csproj` with the local intermediate paths if default `obj` is inaccessible. Check only Task 1 paths and commit them.

### Task 2: Build The Persistent Black Cover

**Files:** Create `Assets/Scrips/Architecture/Runtime/RespawnCoverUI.cs` and `Assets/Scrips/Architecture/Editor/RespawnCoverSetup.cs`; modify `Assets/Scrips/Architecture/Runtime/GameplaySceneRoot.cs`. The Editor command writes only `Assets/Scenes/Gameplay.unity` when the user runs it.

**Interfaces:** Produce `RespawnCoverUI.FadeToOpaqueAsync()` and `FadeToClearAsync()` returning `Task`, `RespawnCoverUI.IsOpaque`, and `GameplaySceneRoot`'s serialized cover reference passed as the final optional argument to `GameplayAreaCoordinator.Configure(..., RespawnCoverUI configuredCover = null)` in Task 3.

- [ ] **Step 1: Implement the visual state.** `RespawnCoverUI` owns a serialized `CanvasGroup`, exposes `[Min(0f)] fadeSeconds = 0.2f`, starts with alpha 0, and uses `Update` plus `Time.unscaledDeltaTime` to complete fade tasks on Unity's main thread. Starting another fade cancels/completes the previous one deterministically; disable/destroy cannot leave a waiting task unresolved. An opaque result means alpha 1; a clear result means alpha 0. The cover does not depend on `Time.timeScale`. Provide a main-thread `WaitUnscaledAsync(float seconds)` on this component for Task 3's minimum duration.

```csharp
public float FadeSeconds => fadeSeconds;
public bool IsOpaque => group != null && group.alpha >= 1f;
public Task FadeToOpaqueAsync() => FadeToAsync(1f);
public Task FadeToClearAsync() => FadeToAsync(0f);
public Task WaitUnscaledAsync(float seconds); // completed by Update, never Task.Delay
```
- [ ] **Step 2: Add the narrow Editor command.** `TIC/Setup/Create Or Update Respawn Cover` opens/reuses `Gameplay.unity`, finds or creates one `Respawn Cover` canvas under `[Gameplay Composition]`, sets screen-space overlay and a sorting order above `[Player HUD]`, creates one full-stretch black `Image` plus `CanvasGroup`, adds/wires `RespawnCoverUI`, and assigns it on `GameplaySceneRoot`. The command aborts if any open scene is dirty, runs outside Play Mode, and saves only Gameplay. Rerunning it reuses the named objects and components.
- [ ] **Step 3: Wire composition without changing startup semantics.** Add the serialized `RespawnCoverUI` field to `GameplaySceneRoot`; pass it to the coordinator's `Configure` call once Task 3 supplies the optional parameter. In this task, leave the coordinator call compiling by adding the optional parameter and storing the reference, without changing respawn behavior yet.
- [ ] **Step 4: Verify code and setup scope.** Compile both assemblies. Review the Editor command for any scene save outside Gameplay, any duplicate creation path, and `RectTransform` anchors `(0,0)` to `(1,1)` with zero offsets. Commit code only. Have the user run the command and save Gameplay after Task 3 integration, then inspect the serialized result before Play Mode.

### Task 3: Keep Recovery Held Through Cover And Preserve Health Policy

**Files:** Modify `Assets/Scrips/Architecture/Runtime/GameplayAreaCoordinator.cs`, `Assets/Scrips/Architecture/Player/Runtime/PlayerDeathRespawn.cs`, and `Assets/Tests/EditMode/Architecture/Player/GameplayRespawnTests.cs`.

**Interfaces:** Keep `Task<bool> RespawnAsync()` for lethal death. Produce `Task<bool> RecoverFromHazardAsync()` for nonlethal hazard calls, `Task<bool> RetryRecoveryAsync()` preserving the previous health policy, `bool IsRecovering`, and `bool IsConfiguredPlayer(PlayerController candidate)` for Task 4. `Configure` receives the cover supplied in Task 2.

- [ ] **Step 1: Move hold ownership outward.** Refactor the pure nested `RespawnSequence` so it performs only suspended streaming, destination load/bind/resolve, teleport, conditional health restore, and crossing reset. Remove its `hold` and `releaseHold` callbacks. The outer coordinator acquires `PlayerWorldHold` before fading and releases it only after the clear fade. Make the `restoreHealth` callback nullable and invoke it with `restoreHealth?.Invoke()`. Update existing constructor calls in `GameplayRespawnTests.cs`; add cases showing the sequence does not release a hold itself and a null restore callback leaves health alone.

```csharp
var sequence = new RespawnSequence(
    suspendTriggers: SceneStreamingService.SuspendDirectionalRequests,
    settleRequests: async () =>
    {
        await SceneStreamingService.WaitForIdleAsync();
        return IsCurrent(generation);
    },
    unloadOtherAreas: () => UnloadOtherAreasAsync(generation, destination),
    loadDestination: () => LoadRespawnAreaAsync(generation, destination),
    restoreProgress: () => RestoreRespawnProgressAsync(generation, destination),
    resolveSpawn: () => ResolveRespawnSpawn(generation, destination, address),
    teleport: TeleportToResolvedRespawn,
    restoreHealth: () => { if (activeRestoreHealth) RestoreRespawnHealth(); },
    resetCrossings: ResetDirectionalCrossings);
```
- [ ] **Step 2: Add one recovery task and health policy.** Route `RespawnAsync()` to a private `RequestRecoveryAsync(restoreHealth: true)` and `RecoverFromHazardAsync()` to `RequestRecoveryAsync(restoreHealth: false)`. While an operation is active, return its same `Task<bool>` instead of starting another. Store the last requested health policy for `RetryRecoveryAsync`; a retry after failure reuses that policy. The lethal path may upgrade an already-pending nonlethal operation to restore health, but a later nonlethal request never downgrades a lethal one.

```csharp
public Task<bool> RespawnAsync() => RequestRecoveryAsync(restoreHealth: true);
public Task<bool> RecoverFromHazardAsync() => RequestRecoveryAsync(restoreHealth: false);
public Task<bool> RetryRecoveryAsync() => RequestRecoveryAsync(lastRecoveryRestoresHealth);
// When active: activeRestoreHealth |= restoreHealth; return the existing task.
// On a new request: remember policy, create one task, then begin covered recovery.
```
- [ ] **Step 3: Enforce presentation order and minimum time.** Acquire hold immediately, record `Time.unscaledTime`, fade to opaque before unloading or teleporting, run the world sequence, then wait until the clear fade can finish no earlier than `start + 2.0f`. Fade clear and finally release hold. If `cover == null`, allow legacy standalone test scenes to use the same hold/area logic without visual fade, but still honor the two-second lock only for configured Gameplay recovery. Use Unity main-thread timing (`Update`/`Time.unscaledDeltaTime` or a small cover-owned wait), not `Task.Delay` on a thread pool. On failure keep the hold and opaque cover for retry; on `CancelSession` dispose the hold and cancel outstanding waits.

```csharp
EnsureRespawnHeld();
var startedAt = Time.unscaledTime;
if (cover != null) await cover.FadeToOpaqueAsync();
if (!await sequence.RunAsync()) return false; // hold and opaque cover remain
if (cover != null)
{
    var wait = Mathf.Max(0f, startedAt + 2f - cover.FadeSeconds - Time.unscaledTime);
    await cover.WaitUnscaledAsync(wait);
    await cover.FadeToClearAsync();
}
ReleaseRespawnHold();
return true;
```
- [ ] **Step 4: Preserve death integration.** `PlayerDeathRespawn.HandleHealthChanged` continues clearing lethal-only temporary effects and calls `RespawnAsync()`. Change `RetryCoordinatedRespawnAsync()` to call `RetryRecoveryAsync()`. Nonlethal hazard recovery does not call `SimpleHealth.Initialize()` or `ResetCardTimeForFullRun()`; `TeleportToResolvedRespawn()` still calls `ResetTransientState()` and clears velocity. Keep `RunProgress.Respawn` unchanged by recovery.
- [ ] **Step 5: Verify failure cases.** Inspect the synchronous lethal health callback, slow load, missing/duplicate marker, failed retry, repeated request, and session destruction paths against Review Focus items 1–3. Compile runtime and EditMode test assemblies. Commit only Task 3 code/test sources.

### Task 4: Apply Hazard Damage Once And Route Recovery

**Files:** Create `Assets/Scrips/Architecture/Runtime/EnvironmentalHazard2D.cs`; modify `GameplayAreaCoordinator.BindLoadedArea` to bind it. Optionally extend `Assets/Tests/EditMode/Architecture/Player/GameplayRespawnTests.cs` with a source-level contact case; Unity physics behavior remains for Play Mode.

**Interfaces:** Produce `EnvironmentalHazard2D.Bind(GameplayAreaCoordinator coordinator, PlayerController player)`, `EnvironmentalHazard2D.ConfigureDamage(float amount)`, and `EnvironmentalHazard2D.TryContact(PlayerController candidate)` returning `bool`. It consumes `RecoverFromHazardAsync()`, `IsRecovering`, and `IsConfiguredPlayer` from Task 3.

- [ ] **Step 1: Implement contact authority and damage.** Require a trigger `Collider2D`, expose `[Min(0.01f)] damageAmount = 1f`, and reject null/foreign players, disabled triggers, a nonpositive amount, or an ongoing recovery. `OnTriggerEnter2D` resolves the player through the contacting collider's parent, including the current root capsule. Set a contact latch before resolving damage. Build a `DamageInstance` with `flatDamage: damageAmount` and resolve one target (the configured player's root) using `DamageResolver.Resolve`.

```csharp
var instance = new DamageInstance(
    instanceId: $"{name}-environment",
    sourceObject: gameObject,
    profile: null,
    formula: new DamageFormulaValues(
        attack: 0f, strikePercent: 0f, strikeBonusPercent: 0f,
        attackBuffPercent: 0f, flatDamage: damageAmount,
        finalDamagePercent: 0f, critValue: 1f));
var report = DamageResolver.Resolve(new DamageRequest(
    instance, new[] { candidate.gameObject }, transform.position, Vector2.zero));
```
- [ ] **Step 2: Route the result exactly once.** If the report has no effective hit, clear the latch and return false. If `KilledTargets > 0`, allow `PlayerDeathRespawn`'s synchronous health event to own `RespawnAsync()`; do not call `RecoverFromHazardAsync()`. Otherwise call `RecoverFromHazardAsync()` once and clear the latch when its task finishes or when the component is destroyed. Repeated trigger callbacks during the operation return without another damage request. Do not reset health or card effects in the hazard component.
- [ ] **Step 3: Bind loaded hazards and verify.** During `BindLoadedArea`, bind each hazard to the coordinator and its persistent player. Compile both assemblies and inspect that a lethal result cannot issue a second request. Commit only hazard and coordinator binding code.

### Task 5: Editor Handoff And Focused Play Mode Pass

**Files:** User-saved `Assets/Scenes/Gameplay.unity` and `Assets/Scenes/PinkArea_Perimeters.unity`; create a timestamped `specs/environmental-hazard-checkpoint-recovery-verification-*.md` after results. Preserve unrelated scene edits.

- [ ] **Step 1: Run deterministic cover setup.** After scripts import, ask the user to run `TIC → Setup → Create Or Update Respawn Cover` while no scene is dirty, save `Gameplay.unity`, and report the first Console error if any. Inspect the saved cover, root reference, canvas sorting, and alpha configuration; do not assume the command succeeded from its invocation alone.
- [ ] **Step 2: Author one Pink checkpoint and one hazard.** In `PinkArea_Perimeters.unity`, the user places a `BoxCollider2D` trigger before the chosen hazard and adds `AreaRespawnCheckpoint`, assigning `Respawn` (`pink-corridor-begin`) or another safe Pink `AreaSpawnPoint` in the same scene. Add `EnvironmentalHazard2D` to one existing Pink hazard trigger, set `Damage Amount = 1`, and ensure the trigger overlaps the player's root capsule. Save Pink. Inspect serialized components, trigger flags, marker reference, unique spawn ID, and preservation of unrelated Pink edits.
- [ ] **Step 3: Play Mode matrix.** Start Gameplay, touch the checkpoint, then the 1-damage hazard: 5 health becomes 4, screen reaches black before teleport, and controls return after at least two seconds when fully clear. Touch it repeatedly to confirm one damage per accepted contact. Verify lethal damage restores health, no duplicate respawn, and Card Time works afterward. Test a streamed return to the checkpoint area and an intentionally missing spawn marker in a disposable Editor setup to confirm opaque held failure and retry; restore the marker afterward.
- [ ] **Step 4: Record evidence and scope.** In a new timestamped verification note, separate compilation results, unrun Unity unit tests, serialized asset checks, and observed Play Mode outcomes. Stage only code/doc and the user-approved Gameplay/Pink scene changes that belong to this mechanism; leave other user-owned modifications untouched. Run `git diff --cached --check`, review staged files, and commit the verified slice.

## Plan Self-Review Checklist

- [x] Every spec section maps to a task: checkpoint, hazard damage, nonlethal/lethal policy, cover timing, loading, retry, authoring, and verification.
- [x] Each Review Focus case has a corresponding inspection or manual case in its owner task.
- [x] No task introduces a cross-scene Transform reference or resets nonlethal health/card effects.
- [x] All new concrete `MonoBehaviour` names match their source filenames and Unity-generated `.meta` files are preserved.
- [x] No Unity unit test execution is claimed until the user changes that preference.
