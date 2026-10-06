# Audio Service Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox syntax for tracking.

**Goal:** Add positional world SFX, native sound variation, centralized playback, and independently adjustable mixer categories.

**Architecture:** A persistent AudioService participates in GameplayServices lifecycle and injection. Presenters submit cue assets and explicit captured positions; native Audio Random Containers own variation. A separate settings interface controls mixer gains.

**Tech Stack:** Unity 6000.3.16f1, C#, AudioSource/AudioResource, Audio Random Container, AudioMixer, Unity Test Framework.

**Spec:** `specs/audio-service-and-mixer-design-20261006-1430.md` (approved by the user).

## Contexto

First implementation plan for the approved audio spec. Prior design version
`specs/audio-service-and-mixer-design-20261006-1426.md` is historical only.
Follow code conventions, testing conventions, and Editor collaboration specs.
The checkout contains ongoing gameplay/feedback changes: preserve them and
include them in the execution baseline rather than checking out a stale branch.

## Global Constraints

- Every world SFX request requires an explicit world-space Vector3 captured at invocation.
- One-shot voices remain at that captured position for their full lifetime.
- UI sounds have a separate non-positional playback request.
- No duplicate clip lists or random pitch/volume ranges.
- Master/SFX/UI/Music/Ambience volume parameters; Combat/Movement inherit SFX.
- World playback continues through hitstop/Card Time; pause suppresses world requests while UI remains audible.
- Scene teardown stops scene-owned voices, while the service survives.
- No settings screen, preference persistence, following emitters, music player, or looping ambience in this slice.
- One concrete MonoBehaviour/ScriptableObject per matching .cs file; preserve metadata GUIDs.

## Review Focus

1. Non-finite positions/gains: reject invalid positions; invalid gains must not poison the mixer (Tasks 1/2).
2. Pool reuse from world to UI and back: no inherited spatial settings (Task 2).
3. Pause during source completion: paused voices must not be reclaimed as finished (Task 2).
4. Additive scenes/destroyed emitters: cleanup must affect only the correct scene/owner (Task 3).
5. Supplemental or rejected damage: no duplicate primary hit cues (Task 4).

## Files And Responsibilities

Create runtime types under `Assets/Scrips/Architecture/Audio/`:
`SoundCueSO.cs`, `AudioCategory.cs`, `AudioVoicePolicy.cs`,
`AudioService.cs`, `IAudioService.cs`, `IAudioSettingsService.cs`,
`AudioVolumeMath.cs`, `PlayerAudioPresenter.cs`, `DamageAudioPresenter.cs`,
`CardTimeAudioPresenter.cs`, and `UiAudioPresenter.cs`.
AudioVoicePolicy is a plain C# policy object; AudioService owns Unity objects.
Presenters have one responsibility each and serialized cue references.

Modify `Assets/Scrips/Architecture/Core/IGameplayServices.cs` and
`Assets/Scrips/Architecture/Runtime/GameplayServicesRoot.cs` for injection.
Modify `Assets/Resources/Runtime/GameplayServices.prefab` through Editor tooling.
Create `Assets/Scrips/Architecture/Editor/AudioPrototypeSetup.cs` and audio
tests under `Assets/Tests/EditMode/Architecture/Audio/` and
`Assets/Tests/PlayMode/Architecture/Audio/`.
Create mixer/cue assets under `Assets/Data/Audio/`; vendor files and licenses
under `Assets/ThirdParty/Kenney/Audio/`.

## Verification Procedure

Use Unity Test Runner filtered to `TicGame.Architecture.Tests.Audio` for
EditMode and PlayMode separately, recording result XML where available.
Expected: zero failed tests, no compilation errors. If using batchmode,
discover the installed executable matching ProjectVersion.txt and run
`Unity.exe -batchmode -projectPath "G:\UnityProjects\My project" -runTests -testPlatform EditMode -testFilter TicGame.Architecture.Tests.Audio -testResults <absolute-result-path> -logFile <absolute-log-path>`.
Repeat with PlayMode. Do not run another Editor against an already open
project: use a project copy or a short Test Runner handoff.
Before each task's implementation, run its newly added tests and confirm the
expected failure; afterward rerun them and confirm success. Commit only
task-owned changes in the execution checkout, never unrelated existing work.

### Task 1: Cue Contract, Volume Math, And Voice Policy

**Interfaces:**
- `IAudioService.TryPlayWorld(SoundCueSO cue, Vector3 position, GameObject owner) -> bool`.
- `IAudioService.TryPlayUi(SoundCueSO cue, GameObject owner) -> bool`.
- `IAudioSettingsService.SetVolume(AudioCategory category, float normalizedGain) -> bool`.
- `IAudioSettingsService.GetVolume(AudioCategory category) -> float`.
- `AudioVolumeMath.ToDecibels(float normalizedGain) -> float`.
- Categories: Master, Sfx, Ui, Music, Ambience. Mixer strings are private configuration.
- `AudioVoicePolicy.TryAcquire(SoundCueSO cue, double unscaledNow, out int slot) -> bool`;
  `Release(int slot) -> void`. Lower numeric priority means higher importance.

- [ ] Add `AudioVolumeMathTests.cs`: assert 1 -> 0 dB, 0 -> -80 dB,
  0.1 -> -20 dB, and finite out-of-range input clamps to [0,1].
- [ ] Add `AudioVoicePolicyTests.cs`: cue overlap drops new requests; cooldown
  blocks requests before expiry; global saturation replaces oldest strictly
  lower-priority voice and otherwise drops new request; denied requests do not
  extend cooldown. Explicitly use capacity 2 and priorities 10/20 in tests.
- [ ] Run new tests and confirm failure.
- [ ] Implement the listed contracts/types; cue holds resource, mixer group,
  priority, cooldown, overlap count, pause behavior, min/max distance, rolloff.
  Use a 24-source default pool, per-cue defaults of 4 voices and zero cooldown;
  keep these authorable. No variation settings in custom code.
- [ ] Rerun tests; verify pass; commit task-owned files and metadata.

### Task 2: Native Playback And Mixer Settings

**Files:** AudioService.cs; AudioServiceTests.cs; AudioPlaybackPlayModeTests.cs.
**Consumes:** Task 1 contracts and policy.
**Produces:** `AudioService : MonoBehaviour, IGameplayModule, IAudioService,
IAudioSettingsService`; `SetWorldPaused(bool paused) -> void`.

- [ ] Add tests: missing resource/route returns false; NaN/infinite position
  returns false; NaN/infinite gain returns false and preserves prior gain.
  Concurrent sources at (2,3,0) and (-4,1,0) retain different positions; moving
  either owner does not move its voice. UI has spatialBlend 0, world has 1,
  Doppler 0; world/UI/world reuse resets every source setting.
- [ ] Add PlayMode tests: native randomized resource plays; mixer gain changes
  apply independently; paused voice is retained, new world request is denied,
  UI still plays, and resume continues paused voice. Initialize explicitly.
- [ ] Run new tests and confirm failure.
- [ ] Verify native APIs against Unity 6.3 documentation and installed API
  assemblies; use AudioSource.resource plus Play for native resources.
  Implement source pooling/reclamation, priority replacement, explicit mixer
  routing, and reset. Do not assume resource is an AudioClip for duration.
- [ ] Implement settings via exposed mixer parameters, default gains 1,
  applied after mixer initialization. Mixer snapshots must not overwrite user
  gain parameters. Clear voices and subscriptions on Shutdown.
- [ ] Rerun tests; verify pass; commit task-owned files and metadata.

### Task 3: Persistent Lifetime, Pause, And Scene Ownership

**Files:** IGameplayServices.cs; GameplayServicesRoot.cs;
AudioServiceLifecycleTests.cs; AudioService.cs.
**Interfaces:** add `IAudioService Audio { get; }` and
`IAudioSettingsService AudioSettings { get; }` to service facade.

- [ ] Add tests for one surviving root/service after duplicate bootstrap;
  restart without domain reload; additive-scene unload removes only its
  voices; destroyed owner releases its voice. Persistent sources still work
  after scene transition. Pause entry/exit drives world pause exactly once.
- [ ] Run new tests and confirm failure.
- [ ] Resolve audio module using existing root initialization conventions;
  expose focused interfaces and inject presenters. Require non-null world/UI
  request owners, record their scene at invocation, and clean up destroyed
  owners/unloaded scenes. Persistent owner belongs to persistent scene.
- [ ] Connect actual pause ownership to SetWorldPaused; use existing
  PlaytestPauseController.PauseChanged where appropriate, with binding and
  teardown, rather than guessing pause from timeScale. Avoid double-subscription.
- [ ] Rerun lifecycle/pause tests; verify pass; commit task-owned files.

### Task 4: Gameplay Audio Presenters

**Files:** the four presenter types; AudioPresenterTests.cs.
**Consumes:** injected IAudioService and serialized SoundCueSO references.
**Produces:** discrete requests for the nine cue families in the spec.

- [ ] Add recording-service tests: jump/landing/dash/swing transitions emit
  once with actor position captured at the transition; effective primary hit
  emits impact position; absent impact point uses captured recipient position;
  rejected hit emits none and supplemental damage does not duplicate it.
- [ ] Add tests for player hurt, Card Time entry/exit, and UI selection/confirm:
  each observable transition emits once, teardown prevents further emissions,
  Card Time uses world actor position while selection/confirm uses UI playback.
- [ ] Run new tests and confirm failure.
- [ ] Implement small presenters around authoritative observations. Use
  IDamageListener for resolved damage and existing animation/action snapshots
  for movement/action transitions. If a repeated attack has no observable
  transition, add the narrow successful-action-start notification at its
  authoritative source; do not infer starts from input or run audio every frame.
  Check Card Time session transitions and selection transaction ownership
  before wiring; confirmation sound follows successful commit.
- [ ] Rerun tests; verify pass; commit task-owned files.

### Task 5: Assets, Deterministic Setup, And Listening Acceptance

**Files:** AudioPrototypeSetup.cs; AudioAssetSetupTests.cs; generated audio
assets/bootstrap changes; `specs/audio-service-verification-<timestamp>.md`.
**Interfaces:** Editor menu `TIC/Audio/Setup Prototype Audio`; rerun reuses
assets and components. Existing Inspector tuning must be preserved.

- [ ] Add asset checks for mixer hierarchy/parameter names, non-null cue
  resources/routes, one service module, assigned presenter cues, and a second
  setup run without duplicate assets/components or overwritten authored tuning.
- [ ] Run checks before setup and confirm failure.
- [ ] Obtain Impact Sounds, RPG Audio, Interface Sounds, and Casino Audio
  from Kenney official pages; preserve supplied licenses and record source URLs.
  Select candidate clips by cue purpose, retaining originals.
- [ ] Create Master -> SFX -> Combat/Movement, plus UI/Music/Ambience;
  expose MasterVolume/SfxVolume/UiVolume/MusicVolume/AmbienceVolume.
  Author manual-trigger single-play containers for variations and cue assets.
  Verify supported Editor authoring APIs; use an explicit small Editor handoff
  for unsupported native authoring rather than private unsupported APIs/YAML.
- [ ] Implement idempotent pool/bootstrap/presenter wiring for the player,
  damage recipients, Card Time, and playtest UI. Validate the existing listener:
  exactly one active AudioListener; test panning/attenuation with actual camera
  depth and tune cue distances accordingly. Do not blindly add a second listener.
- [ ] Rerun asset checks and all audio tests; run affected bootstrap, damage,
  Card Time, and frontend regression tests once. Record observed results.
- [ ] Perform a user listening handoff: left/right emitters, near/far falloff,
  moving after invoking a sound, rapid hits, multi-enemy combat, pause/UI,
  Card Time, and scene transitions. Tune levels and variants using listening
  feedback; record unverified subjective checks honestly.
- [ ] Save verification report and commit only task-owned changes.

## Self-Review And Handoff

Coverage checked against approved spec: positional capture, native variation,
mixer gains, bounded policy, lifecycle, presenters, licenses, Editor setup,
and listening acceptance all have tasks. Public contracts are consistent;
settings UI and loop playback remain deferred. No implementation has begun.

Recommend native execution because five sequential tasks share contracts and
Unity asset setup. User must review this plan and select native or subagent-driven
execution before implementation. Follow the corresponding execution skill.
