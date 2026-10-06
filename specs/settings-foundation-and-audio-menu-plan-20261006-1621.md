# Settings Foundation And Audio Menu Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans for the preserved native execution preference. Steps use checkbox syntax for tracking.

**Goal:** Add immediate, persistent audio settings to title/pause menus and a shared settings foundation for future controls.

**Architecture:** A persistent UserSettingsService owns typed preferences and delegates storage to an interface and playback gain changes to IAudioSettingsService. A focused presenter binds the audio panel; the existing menu owns navigation. Native mixer readiness prevents startup defaults from overwriting saved preferences.

**Tech Stack:** Unity 6000.3.16f1, C#, uGUI, Input System UI module, PlayerPrefs, Unity Test Framework.

**Spec:** `specs/settings-foundation-and-audio-menu-sdd-20261006-1619.md` (approved).

## Contexto

Native execution in the current checkout was selected earlier and remains the
preference. Preserve all existing audio/gameplay changes and user-authored
mixer/prefab configuration. This plan is presented for review before execution.

## Global Constraints

- Add Settings to both the title and pause menus; preserve existing Controls help and Card Time guide.
- Five sliders: Master, SFX, UI, Music, Ambience; 0-100%, with 1% steps.
- Changes apply immediately. There are no Apply/Cancel buttons and Back does not revert changes.
- Reset Audio Defaults restores all five categories to 100% immediately.
- Save dirty preferences on Settings exit, application pause, focus loss, and orderly quit; no disk flush per slider tick.
- Closing Settings while paused does not resume gameplay.
- Input presets, automatic device switching, and rebinding are excluded.
- Keys: TIC.Settings.Audio.v1.Master, .Sfx, .Ui, .Music, .Ambience.
- Missing/non-finite loaded values default to 1; finite values clamp to [0,1].
- No scene/Build Settings rebuild to add this panel; preserve artwork and authored references.
- Current checkout; tests use an independent validation project while the live Editor is open.

## Review Focus

1. Settings load before mixer readiness: saved gains must supersede defaults exactly once (Task 2).
2. Save failure during exit/shutdown: dirty values remain available for retry without reverting audio (Task 1).
3. Held Escape/gamepad cancel: one press must close one panel level without unpausing gameplay (Task 3).
4. Refreshing sliders or reopening the panel: no callback recursion, duplicate writes, or accumulating listeners (Task 3).
5. Existing frontend overrides/setup reruns: upgrade preserves artwork, cue tuning, existing controls help, and Build Settings (Task 4).

## File Map

Create in `Assets/Scrips/Architecture/Settings/`:

- `AudioSettingsValues.cs`: typed five-category values and validation.
- `ISettingsStore.cs`, `PlayerPrefsSettingsStore.cs`: storage boundary/adapter.
- `IUserSettingsService.cs`, `UserSettingsService.cs`: persistent preference ownership.

Create in `Assets/Scrips/Architecture/Frontend/`:

- `AudioSettingsPanel.cs`: serialized slider/label/button references and view refresh.
- `AudioSettingsPresenter.cs`: settings binding, callbacks, open/close lifecycle.

Create `Assets/Scrips/Architecture/Editor/SettingsFrontendSetup.cs` for the
narrow upgrade and reusable panel construction. Modify PlaytestMenuView,
PlaytestPauseController (only if required to fence duplicate cancel),
PlaytestFrontendSetup, IGameplayServices, GameplayServicesRoot,
IAudioSettingsService, and AudioService at their existing responsibilities.

Update test doubles implementing changed interfaces. Tests go under
`Assets/Tests/EditMode/Architecture/Settings/` and
`Assets/Tests/PlayMode/Architecture/Settings/`. Generated prefab changes affect
only Resources/Runtime/GameplayServices.prefab and PlaytestSession.prefab.

## Verification Commands And Execution Record

Use `local-artifacts/SettingsWork/progress.md` for task status, decisions, and
test evidence; `local-artifacts/SettingsValidation` for the validation project.
Synchronize current Assets, Packages, and ProjectSettings into that copy,
preserving the live checkout. Never copy validation PlayerPrefs/settings back.

Run Unity 6000.3.16f1 with:

```text
Unity.exe -batchmode -projectPath <validation-project> -runTests
  -testPlatform EditMode -testFilter TicGame.Architecture.Tests.Settings
  -testResults <absolute-results.xml> -logFile <absolute-test.log>
```

Repeat with PlayMode. Use graphics-capable execution for layout captures and
runtime mixer checks. Before implementing each task, add/run its regression
tests and observe the expected failure; afterward require all task tests to
pass. Existing broad-suite failures are recorded in the audio checkpoint;
compare any affected-suite failures to that baseline rather than masking them.

Do not automatically stage/commit the mixed checkout. Record completed tasks
in the ledger. Final review covers only settings work, not unrelated changes.

### Task 1: Preference Model, Storage, And Ownership

**Interfaces:**
- `AudioSettingsValues.GetGain(AudioCategory category) -> float`.
- `AudioSettingsValues.TrySetGain(AudioCategory category, float gain) -> bool`;
  reject non-finite gains/invalid categories; clamp finite gains.
- `ISettingsStore.TryReadFloat(string key, out float value) -> bool`.
- `ISettingsStore.WriteFloat(string key, float value) -> void`.
- `ISettingsStore.Flush() -> bool`; false indicates save failure.
- `IUserSettingsService.GetAudioVolume(AudioCategory category) -> float`.
- `IUserSettingsService.TrySetAudioVolume(AudioCategory category, float gain) -> bool`;
  true means the desired preference was accepted, including while audio is not ready.
- `IUserSettingsService.ResetAudioDefaults() -> void`;
  `Save() -> bool`; `IsDirty { get; }`; `event Action Changed`.
- `UserSettingsService.Configure(IAudioSettingsService audio, ISettingsStore store) -> void`
  provides explicit test dependencies before Initialize. Production serializes
  a MonoBehaviour audio dependency on the same persistent prefab and uses the
  PlayerPrefs adapter by default.

- [ ] Add model/store/service tests: defaults all 1; missing/NaN/infinity
  storage values resolve to 1; -1 resolves to 0; 2 resolves to 1; invalid writes
  are rejected; unchanged values remain clean. Assert five exact storage keys
  and unrelated key preservation using an isolated fake store.
- [ ] Add dirty/save tests: accepted changes update current values and notify
  once, Reset updates all categories in one notification, Save writes current
  values then flushes once, flush/write failure preserves dirty state and
  permits retry. Loading and initial audio application do not mark dirty.
- [ ] Run tests and confirm missing types/behavior fail.
- [ ] Implement the contracts, values, adapter, and IGameplayModule service.
  Catch storage failures at the service boundary with actionable diagnostics.
  PlayerPrefs tests use uniquely prefixed temporary keys and clean up only
  those keys; never delete all preferences.
- [ ] Verify green tests and record Task 1 completion in the ledger.

### Task 2: Mixer Readiness And Persistent Service Composition

**Interfaces:** add `bool IsReady { get; }` and `event Action Ready` to
IAudioSettingsService; add `IUserSettingsService Settings { get; }` to
IGameplayServices. `UserSettingsService.ApplyAudioSettings() -> bool` applies
the desired profile and reports whether every mixer category succeeded.

- [ ] Add readiness tests: load SFX=0.25 before audio ready; no early apply;
  readiness applies that value once after initial audio defaults. Reopening
  menus/extra frames never resets it. Already-ready binding applies once.
- [ ] Add failure/lifecycle tests: missing mixer parameter retains desired
  preference with a diagnostic and returns failed application; explicit retry
  succeeds. Shutdown unsubscribes and saves dirty values; reinitialize loads
  correctly. Application pause/focus-loss/quit flush only dirty state.
- [ ] Run tests and confirm failure.
- [ ] Extend AudioService readiness: emit once after first runtime default
  application; reset during Shutdown. Keep native SetFloat outside Awake.
  Subscribe UserSettingsService during Initialize and unsubscribe on Shutdown.
  Changes apply the selected category immediately when ready; retain desired
  values if application fails. No per-frame settings reapplication.
- [ ] Expose Settings through GameplayServicesRoot; append settings module
  after audio in configured initialization order. Update service test doubles
  without changing their unrelated semantics.
- [ ] Verify service/readiness tests plus existing Audio tests; record results.

### Task 3: Audio Panel And Menu Navigation

**Interfaces:**
- `AudioSettingsPanel.Refresh(IUserSettingsService settings) -> void`: update
  five slider values/percentage labels using SetValueWithoutNotify.
- `AudioSettingsPresenter.Bind(IUserSettingsService settings) -> void`;
  `Open() -> void`; `Close() -> void`; `ResetDefaults() -> void`.
- Add narrow configuration methods to connect generated panel references;
  serialize concrete uGUI Slider/Text/Button types with Inspector annotations.
- PlaytestMenuView owns the settings panel state and parent focus; presenter
  Close saves dirty values and detaches callbacks without reverting.

- [ ] Add presenter tests: open reflects saved values; slider at 25 displays
  25% and requests gain 0.25 once; refresh does not invoke change callbacks;
  repeated opens do not duplicate listeners; Reset updates all rows to 100;
  close flushes once only when dirty and retains current values.
- [ ] Add menu integration tests: Settings works on title without player;
  pause Settings Back restores the Settings entry while remaining paused;
  one Escape/B press closes exactly one level; next press from pause root can
  resume. Loading/error transitions close the panel and save; destruction and
  quit also preserve dirty preferences.
- [ ] Run tests and confirm failure.
- [ ] Implement panel/presenter and extend menu render routing. Title order:
  Start playtest, Settings, Controls, Quit. Pause order: Resume, Settings,
  Controls / Card Time guide, Return to title, Quit. Preserve existing help
  and confirmation semantics. Explicitly route pending focus to panel controls
  so the old delayed button focus cannot steal focus from a slider.
- [ ] Configure explicit uGUI navigation: five sliders top to bottom, then
  Reset Audio Defaults and Back; left/right changes only the selected slider.
  Centralize/consume cancel handling as needed to avoid PauseController and
  menu processing the same press twice. Retain gameplay input suppression.
- [ ] Verify keyboard/mouse/gamepad and title/pause tests; record results.

### Task 4: Safe Prefab Upgrade, Layout, And Final Verification

**Interfaces:** Editor menu `TIC/Settings/Setup Audio Settings`;
`SettingsFrontendSetup.Setup() -> void` performs narrow prefab upgrade;
`SettingsFrontendSetup.ConfigureFrontend(GameObject root) -> void` is reused
by deliberate frontend regeneration after its existing base layout is created.

- [ ] Add asset tests: one settings module with audio dependency; five rows
  with range 0-100 and wholeNumbers=true; five menu choices; explicit navigation;
  complete serialized references; rerun adds no duplicates and preserves
  existing artwork, references, controls text, cue tuning, and Build Settings.
- [ ] Run tests before setup and observe missing configuration failure.
- [ ] Implement idempotent prefab upgrade via LoadPrefabContents/SaveAsPrefabAsset.
  Match Ink/Paper/Cyan styling. Position five compact audio rows within the
  existing body area; hide the old body text/buttons while Settings is open.
  Place Reset/Back below the rows and clear of the footer. Fit the pause root's
  five buttons by adjusting only menu choice spacing/positions where required.
- [ ] Extend PlaytestFrontendSetup so regeneration includes these settings
  through the shared helper; keep the narrow upgrade command independent of
  scene rebuilding or Build Settings writes.
- [ ] Run setup in the validation copy and inspect serialized prefab diffs.
  Transfer only validated prefab changes to the current checkout after checking
  that its source prefabs have not changed since synchronization; otherwise
  run the narrow upgrade against the latest prefab instead of overwriting work.
- [ ] Run all Settings and Audio tests and affected frontend regression tests.
  Test two Play sessions with domain reload disabled, one settings owner each,
  loaded gains surviving audio readiness, and preferences retained across
  return-to-title/respawn/area changes.
- [ ] Capture/inspect menu images at 1280x720 and 1920x1080. Fix overlapping
  labels/rows/buttons and verify focus indications. Ask for a short Play Mode
  check only for interaction/listening that cannot be established from artifacts.
- [ ] Request one fresh-context review of the settings change, correct important
  findings, verify fixes, and save a timestamped settings verification report.

## Self-Review And Execution Handoff

Checked spec coverage, interface consistency, startup ordering, save failure
semantics, cancel/focus handling, safe Editor upgrade, and test isolation.
Input configuration remains deferred; no Controls settings UI is added.

The plan has four sequential tasks. Preserved execution method: native in the
current checkout. Review approval of this plan is required before implementation.
