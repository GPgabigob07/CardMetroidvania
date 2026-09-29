# External Playtest Build Implementation Plan — review draft

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox syntax for tracking. This is a proposed execution draft prepared at the user's request to plan everything; neither the written design nor this plan has been approved for implementation. Resolve review changes before execution.

**Goal:** Deliver a self-contained external playtest with drafted menus, reliable session transitions, and a verified Windows ZIP.

**Architecture:** Keep Gameplay and its streamed areas as the run composition. Add a persistent front-end session coordinator, scene-owned uGUI views, owner-scoped pause, and deterministic Editor setup/build tooling. Reuse existing services and input assets.

**Tech Stack:** Unity 6000.3.16f1, C#, uGUI, Input System, Unity Test Framework, Windows x64.

**Spec:** `specs/external-playtest-build-design-20260929-1147.md`.

## Contexto

New timestamped planning artifact for an external tester today. Sources and the behavioral requirements live in the paired design; existing design/history files are preserved. Visual reference: `specs/playtest-ui-draft-20260929-1147.svg`. Branch baseline: `4beafe6`.

## Global Constraints

- Windows x64 ZIP, English, 15–20 minute exploratory session are proposed defaults awaiting confirmation.
- Unity stays at 6000.3.16f1; no new package or Unity upgrade.
- Existing Blue/Pink content, no disk save, no automatic upload or tester messaging.
- Preserve momentum and Card Time ownership across pause; new runs reset progress.
- One concrete MonoBehaviour or ScriptableObject per same-named file, with .meta files generated/preserved by Unity.
- Follow repository Inspector/XML documentation and explicit test initialization conventions.
- Use idempotent Editor tooling for complex serialized changes, not hand-written scene YAML.
- UI uses unscaled time; transitions show an error after 15 seconds rather than unsafe forced unloading.
- Runtime changes await design/plan review. Proposed names below become contracts only after approval.

## Review Focus

1. Enter is both submit and attack: menu input must not leak into gameplay (Task 2).
2. Pause during an airborne attack/Card Time: momentum and timer ownership must survive (Task 2).
3. Return while an additive load is finishing: no stale scene or callback may resurrect the run (Task 1).
4. Start is currently the Card Time guide button: only one UI consumes it (Task 3).
5. Direct Editor Gameplay entry and a second run: exactly one session controller/services root and fresh progress (Tasks 1/4).

## File and responsibility map

Proposed new runtime files under `Assets/Scrips/Architecture/Frontend/`:

- `PlaytestSessionController.cs`: serialized scene references, single-flight transitions, service reset, readiness/error status; persistent MonoBehaviour.
- `PlaytestSessionBootstrap.cs`: static initialization that ensures exactly one controller, including direct Gameplay entry.
- `PlaytestPauseController.cs`: pause modifier ownership, game-state/input gate, focus-loss policy; MonoBehaviour.
- `PlaytestMenuView.cs`: title/pause/control/confirmation panels and UI focus; MonoBehaviour.
- `PlaytestLoadingView.cs`: unscaled loading/error presentation; MonoBehaviour.
- `PlaytestControlsContent.cs`: pure mapping from configured action/profile data to display rows; no second binding authority.

Modified runtime files: `Runtime/GameplaySceneRoot.cs` (public readiness/retry), `Player/Runtime/PlayerController.cs` (input pause gate), `Runtime/CardTimeGuideUI.cs` (single input ownership). Inspect Card Time source/session update methods before choosing the narrow pause guard insertion point; preserve its existing contracts and tests.

Editor: `Assets/Scrips/Architecture/Editor/PlaytestFrontendSetup.cs` and `PlaytestBuild.cs`.

Assets: `Assets/Scenes/MainMenu.unity`, `Assets/Prefabs/UI/PlaytestMenu.prefab`, `Assets/Prefabs/UI/PlaytestLoading.prefab`, `Assets/Resources/Runtime/PlaytestSession.prefab`; modify Gameplay via setup tooling and input actions without changing existing IDs. Optional art under `Assets/Art/UI/Playtest/`.

Tests: `Assets/Tests/EditMode/Architecture/Frontend/` and `Assets/Tests/PlayMode/Architecture/Frontend/`; add a PlayMode asmdef referencing the actual existing runtime assembly. Preserve the repository's `Scrips` spelling.

## Task 1: New-run and return-to-title lifecycle

**Files:** session controller/bootstrap, GameplaySceneRoot; new `PlaytestSessionControllerTests.cs` and `PlaytestSessionLifecycleTests.cs` in the test folders above.

**Interfaces:** controller exposes `Task<bool> StartPlaytestAsync()`, `Task<bool> ReturnToTitleAsync()`, `bool IsTransitioning`, `string LastError`, and `event Action Changed`. GameplaySceneRoot exposes `bool IsReady` and `Task<bool> RetryStartupAsync()` that owns both coordinator readiness and startup-hold release. Consume existing `GameplayAreaCoordinator.CancelSession()`, `SceneStreamingService.WaitForIdleAsync()`, directional-suspension lease and `GameplayServicesRoot.Shutdown()/Initialize()`.

- [ ] Write failing tests: double Start results in one scene transition; readiness remains false until coordinator ready; retry releases the root hold; stale startup completion after cancellation cannot enter Gameplay; service reset clears time/Card Time and creates fresh run progress on next start.
- [ ] Run selected EditMode tests in Unity Test Runner, capture failed assertions before implementation. Use real PlayMode scene tests for Unity lifetime behavior rather than pretending EditMode verifies scene loading.
- [ ] Implement the controller/bootstrap and public root contract; serialize transitions, cancel the old session before waiting for streaming, release leases in finally paths, and guard every async continuation with operation identity.
- [ ] Add unscaled timeout/error state. Retry cannot overlap a pending operation; errors retain Return/Quit access as safe. Keep title transition pending if streaming has not settled.
- [ ] Run tests, including three new runs, direct Gameplay entry, missing scene/spawn, and return during area streaming. Expected: one player/services/controller, no stale loaded area, no stale progress.
- [ ] Review the diff and commit this tested slice.

## Task 2: Pause without changing movement or Card Time

**Files:** pause controller; PlayerController; actual Card Time timer owner determined by the inspection below; `PlaytestPauseControllerTests.cs` and PlayMode pause tests.

**Interfaces:** pause controller exposes `bool IsPaused`, `bool TryPause()`, `void Resume()`, `event Action<bool> PauseChanged`. PlayerController exposes `void SetMenuInputSuppressed(bool suppressed)`; this gates input and gameplay updates without applying PlayerWorldHold. Menu transitions can use the same suppression contract; existing startup/respawn still use world holds.

- [ ] Trace scaled/unscaled updates through player, Card Time source/session and hitstop. Record the exact timer owner and narrow pause guard in this plan before modifying it.
- [ ] Write failing tests: airborne velocity survives pause/resume; effective time is zero while paused; resume removes only the pause modifier; Card Time remaining window is unchanged while paused; Enter release prevents an immediate attack; recovery rejects pause; losing focus pauses once.
- [ ] Run targeted tests and confirm failures describe missing pause behavior.
- [ ] Implement the pause gate with existing time modifiers; reserve Escape/Start input; UI animations use unscaled time. Require release of held submit/cancel/gameplay buttons before accepting fresh gameplay presses. Preserve other owners' modifiers.
- [ ] Run targeted time/Card Time/startup tests and PlayMode airborne-pause tests. Confirm the opening and closing frame does not consume a gameplay action.
- [ ] Review and commit.

## Task 3: Draft UI and useful controls

**Files:** menu/loading views, controls-content helper, CardTimeGuideUI, InputSystem_Actions.inputactions; new UI/control tests.

**Interfaces:** menu view calls controller transition methods and pause Resume; loading view reads IsTransitioning/LastError and subscribes to Changed. Controls helper exposes `IReadOnlyList<string> CreateRows(InputActionAsset actions, string controlScheme)` for ordinary action bindings; Card Time instructions additionally consume the configured existing control-scheme profile. Do not infer advanced chord semantics from this method alone.

- [ ] Confirm platform/language/title assumptions and approve the paired SVG direction; adapt labels before authoring Unity assets.
- [ ] Audit action references on the actual player prefab, including dash, selection and Card Time profile. Finalize truthful controls copy and record expected rows in controls tests.
- [ ] Test binding display fallback for absent gamepad, title/pause Back focus, Cancel as confirmation default, and Start opening pause without also opening the guide.
- [ ] Implement panels from the screen table. Keep no-save notice and build ID visible; handle empty/unavailable bindings honestly. Pre-unlock controls do not falsely promise Card Time availability.
- [ ] Replace the guide's independent gamepad Start listener with menu routing; preserve F1 only where input ownership allows it.
- [ ] Verify keyboard, pointer and gamepad navigation, device reconnect, readable error copy, 720p/1080p/16:10 layouts, and zero-time UI interaction.
- [ ] Commit the working UI slice. Optional generated background is a separate polish change after the core build passes; use the imagegen skill/tool and the approved art brief if needed.

## Task 4: Deterministic Unity composition and release builder

**Files:** PlaytestFrontendSetup.cs, PlaytestBuild.cs, scene/prefab assets listed above; `PlaytestBuildValidationTests.cs`.

**Interfaces:** `PlaytestFrontendSetup.CreateOrUpdate()` under `TIC/Setup/Create Or Update Playtest Frontend`; `PlaytestBuild.BuildWindows()` under `TIC/Build/Windows Playtest`. Builder fails on validation/build failure, writes a manifest only on success, and uses a unique output folder.

- [ ] Test missing scene/reference rejection, exact release scene list, exclusion of test scenes, and setup run twice yielding identical object counts/references.
- [ ] Implement setup using Unity serialization APIs, preserve scene geometry and overrides, register MainMenu as entry without breaking direct Editor testing.
- [ ] Verify exactly one EventSystem and AudioListener per active composition, including transition frames; title camera does not survive into gameplay.
- [ ] Validate Unity installation/build support, product metadata, referenced area scene inclusion, and release debug-overlay configuration. Record the actual build backend; use the installed supported backend rather than adding a toolchain unnecessarily.
- [ ] Build non-development Windows x64 with logs enabled; fail if BuildReport is not successful. Save build log, manifest, exact output path and scene list.
- [ ] Run relevant tests then the complete EditMode suite once. Save result XML and report failures honestly. If Unity has the project open, use its Test Runner/build menu or arrange a clean close; do not launch a competing batch process against the same project.
- [ ] Review serialized diff and commit.

## Task 5: External-tester package and acceptance

**Files:** proposed tracked `playtest/README-template.txt`, `playtest/FEEDBACK-template.txt`, `playtest/KNOWN_ISSUES-template.txt`; generated files and ZIP under ignored Builds.

**Interfaces:** builder manifest contains `buildId`, `builtAtUtc`, `unityVersion`, `target`, `commit`, `dirty`. Generated README uses actual executable/product/log path and tested bindings.

- [ ] Write launch/no-save/controls/15–20 minute playtest instructions. Explain this is an unfinished exploratory slice; request confusion, blockers and reproduction details.
- [ ] Perform standalone acceptance from the design: cold start, Blue/Pink traversal both ways, unloaded-area respawn, hazards, pause/Card Time, three run resets, device reconnect, focus loss and resolution checks. Record pass/fail and any user-provided observations separately from automated results.
- [ ] Fix launch/input/route/reset blockers and rerun only affected checks. Document remaining nonblocking known issues from observed evidence.
- [ ] Package the entire successful Windows output plus README, known issues, feedback form and manifest. Extract to a fresh folder outside the Unity project and launch that copy.
- [ ] Compute ZIP SHA-256 with `Get-FileHash -Algorithm SHA256`; retain final path, hash and tested commit/dirty state. Do not zip an older output after a rebuild.
- [ ] Deliver local links to ZIP and test record; do not upload or contact the tester automatically.

## Execution handoff

Recommended execution: **native in this chat**, because session, pause and menus share lifecycle/input assumptions and today's scope is small enough to keep together. Run the required whole-branch review after implementation; use subagents only as required by the chosen execution skill/user authorization.

Before execution, user reviews the written design, UI board and proposed plan; incorporate corrections and confirm execution approach. No gameplay code, scene or build setting was changed by this planning pass.
