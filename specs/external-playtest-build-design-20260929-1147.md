# External playtest build — proposed design

Status: draft for user review. No runtime implementation or build verification has occurred.

## Contexto

The user wants a proper build today to send to another person, with drafted UI and permission to generate artwork where useful. This moves a small presentation/release slice forward from the November schedule; it does not replace that schedule or promise its unfinished content.

Branch: `codex/playable-build-menu-20260929`, created from freshly fetched `origin/master` at `4beafe6`.

Sources: `gdd/gdd-canonico-20260526-2331.md`, `gdd/cronograma-build-novembro-20260828-1401.md`, `specs/gameplay-scene-ownership-sdd-20260920-1853.md`, `specs/unity-editor-collaboration-workflow-20260612-1609.md`, `specs/code-conventions-20260526-0014.md`, `specs/testing-conventions-20260526-0122.md`, and the current startup, streaming, services, input, guide and time code.

## Intent and assumptions

Success means a first-time tester can extract the package, launch it without Unity, understand the controls, explore Blue/Pink, recover from death, pause, leave safely, and report useful feedback with a build identifier.

Proposed defaults: Windows x64 ZIP, English interface, keyboard/mouse and gamepad, 15–20 minute exploratory playtest. Platform/language are awaiting user confirmation. Working display title: **Card Metroidvania**, subtitle **Playtest 01**; this is not a final game name. No disk saves: explain that leaving the run resets progress. Existing area content is the scope; no invented completion point or claim that the game is finished.

## Alternatives and recommendation

1. Title-only launcher: quickest, but weak onboarding and no comfortable exit during play.
2. **Compact playtest shell (recommended):** title, controls, pause, loading/error recovery, clean new runs, and a tested distributable. Enough for an independent tester.
3. Full front end with saves, rebinding, graphics/audio settings and credits presentation: defer beyond today's slice.

## Draft UI direction

See `playtest-ui-draft-20260929-1147.svg` beside this document. It is a static concept board, not a Unity screenshot or finished asset.

- Near-black navy background `#101521`; warm white text `#F3EFE5`; cyan `#7CD8EB` focus accents; muted pink `#D78FB5` secondary accents.
- Abstract castle silhouettes and three floating card outlines imply the medieval/sci-fi setting. Left-aligned title/menu, artwork on the right. No invented protagonist design.
- Reference canvas 1920×1080, scalable to 1280×720; 16:9 and 16:10 checks. Minimum target body text 24 reference pixels, buttons 56 reference pixels tall. Focus uses an outline and pointer, not color alone.
- Real UI text remains editable, separate from artwork. Existing licensed project fonts first; do not add a font dependency for the draft.
- Use the existing Unity UI stack (uGUI) and Input System. Pointer, keyboard navigation and gamepad focus are equally usable. Back restores the previous selection; dialogs default to Cancel.
- Optional generated asset: one 1920×1080 background, empty dark space on left, distant castle on right, blue/pink atmospheric light, no lettering, logos or recognizable borrowed character. Generate only after the direction is approved; keep provenance and review at target resolutions. Solid/vector background remains a valid fallback.

## Screen and interaction contract

| Screen | Content and actions | Acceptance |
| --- | --- | --- |
| Title | Working title, Playtest 01, Start playtest, Controls, Quit, build ID, “Progress is not saved.” | Start selected on entry; repeated submit starts one run |
| Controls | Actual movement, jump, melee and Card Time bindings; pause; brief test objective; Back | Available before a run; in pause it keeps gameplay paused |
| Loading | “Preparing the area…” and animated unscaled indicator | No fabricated percentage; input cannot reach player |
| Pause | Resume, Controls / Card Time guide, Return to title, Quit | Escape/Start toggles pause; cancel closes a child panel before resuming |
| Return/Quit confirmation | “Leave this run? Progress will be reset.” Leave / Cancel | Cancel selected; no accidental progress loss |
| Failure | Friendly load error, Retry, Return to title, build ID; detailed cause in log | Never an indefinite blank screen or player in an unloaded world |

No Continue button, save slots, settings placeholders, inactive buttons or fake feedback submission. Feedback instructions live in the package; sending messages or uploading the build is a separate user action.

The control sheet must read the action references actually used by the configured player and Card Time profile. Current action asset includes Space/gamepad south for Jump, mouse-left/Enter/gamepad west for Attack, J/K or shoulder buttons for Card Time. Verify movement, dash action reference, slot navigation and chord instructions against the shipped prefab before publishing copy; do not assume action names imply bindings.

Gamepad Start currently toggles `CardTimeGuideUI`. Reserve Escape/Start for pause and expose the guide through pause. F1 may remain a gameplay guide shortcut. The pause guide must honor discovery state: before unlock, show basic controls and a note that Card Time is introduced in the tutorial. One press must never activate both menus. Releasing menu submit/cancel before enabling player input prevents Enter from also attacking.

## Runtime design

### Ownership

Add `MainMenu.unity` as entry scene. Keep `Gameplay.unity` owning the player, camera, HUD, coordinator and RunProgress, with Blue/Pink additive as today. Preserve the service bootstrap and standalone Editor test flows. Introduce a small persistent `PlaytestSessionController` for front-end transitions; it must also initialize when entering Gameplay directly in the Editor. Scene-local views subscribe/unsubscribe; it does not own combat or duplicate RunProgress.

Use existing GameState values MainMenu, LoadGame, Gameplay and Pause. Session transition state is separate from combat/death state, with explicit busy/error status. One operation at a time; stale asynchronous completions cannot restore a destroyed run.

### Start and return

Start: disable menu submission, enter LoadGame, load Gameplay, let its existing coordinator load `blue` / `blue-start`, and dismiss the loading cover only after `IsReady`. Surface `LastError` and provide retry through the root so its startup hold is released correctly. Add a public readiness/retry contract to GameplaySceneRoot rather than having UI reach into private fields.

Return: disable gameplay/menu submission, cancel the area's session, suspend directional requests, await existing streaming operations, unload the run via a single-mode title load, then reset persistent services using their explicit Shutdown/Initialize lifecycle. Ensure time and Card Time owners clear, consumers rebind, scene protection leases dispose, and new RunProgress is created only with the next Gameplay instance. Do not destroy the bootstrap singleton and assume BeforeSceneLoad reruns on ordinary scene transitions.

During startup/recovery, pause is unavailable; a visible loading or error panel owns input. If streaming never settles, show a recoverable transition error after 15 seconds of unscaled time; do not unload under a live operation. Quit remains available. Retry rechecks settlement without starting a second transition.

### Pause and time

Pause adds an owner-scoped zero-time modifier through GameplayTimeCoordinator and suppresses gameplay action consumption. Resume removes only that modifier, retaining Card Time/hitstop ownership. UI animation/navigation use unscaled time. Freeze timers that currently tick on unscaled time through explicit pause checks, including Card Time; otherwise the card window would expire behind the menu.

Do not use PlayerWorldHold directly for ordinary pause: Acquire currently zeroes velocity and disables simulation, which would erase aerial momentum. Add an input-only pause gate that preserves velocity, buffers and run progress. Ignore opening pause during recovery/startup and test pausing during aerial movement and active Card Time. Loss of application focus requests pause once during ready gameplay; it never auto-resumes.

## Build and distribution

Unity stays at 6000.3.16f1. Validate Windows build support before promising an artifact. No upgrade or new package is required for the proposed UI.

Add an idempotent Editor setup command for the title/pause UI and references, preserving gameplay geometry/prefab overrides. Release builder uses an explicit scene list: MainMenu, Gameplay, BlueArea_Tutorial, PinkArea_Perimeters. SampleScene and enemy test scenes stay out of the release. Inspect area references before excluding any scene. Preserve the ordinary Editor testing workflow.

Output `Builds/Playtest-<timestamp>/Windows/` and a sibling ZIP with the full executable, *_Data directory and runtime DLLs. Use a non-development player with logs enabled. Disable debug overlays through a release setting, without removing useful logs or test tools. Set meaningful product/version metadata; do not invent a studio name.

Include README.txt, KNOWN_ISSUES.txt, FEEDBACK.txt and build-info.json (UTC timestamp, Unity version, platform, commit, dirty status). Compute a SHA-256 for the ZIP. Artifacts remain outside Git in the ignored Builds directory. No automatic upload or message to the tester.

README: extract the entire ZIP, launch executable, controls, no-save notice, test route and log location derived from final company/product settings. FEEDBACK: build ID, controller, screen resolution, where they got stuck, reproduction steps, expected/actual result and optional screenshot/log. Ask where controls or Card Time became confusing before requesting balance opinions.

## Validation and release gate

Automated: session single-flight behavior, failure/retry and stale completions; startup hold release; pause ownership and input isolation; repeated run reset; setup idempotence and build-scene validation. Run existing relevant startup, streaming, time, Card Time and respawn tests, then the EditMode suite once before packaging. Add PlayMode coverage for real scene lifecycle; use explicit initialization in EditMode tests.

Manual in the standalone player: cold launch, keyboard-only and gamepad-only menu navigation, disconnect/reconnect, 720p/1080p/16:10 layout, focus loss, jump→pause→resume momentum, active Card Time→pause→resume, Blue↔Pink traversal, death with Blue unloaded, hazard recovery, three title→new-run cycles, and fresh extraction outside the project. Verify no debug overlay, duplicated player/camera/audio listener, missing floor or retained progress.

The user owns visual/play-feel validation per the Editor collaboration spec. Provide short exact Editor actions after tooling exists. Never claim a tested deliverable based only on compilation. Keep any failed requirement visible in the release checklist; fix launch/route/input/reset blockers before handoff.

## Delivery order and time pressure

1. Agree on this scope and UI draft.
2. Implement session lifecycle and pause; prove repeatable runs.
3. Wire readable UI and honest controls/error states.
4. Build and smoke-test the ZIP.
5. Add optional background polish only if the core path and packaging already pass.

When time is short, cut generated art and decorative motion first. Do not cut controls, loading failures, no-save disclosure, clean restart, or standalone verification. This plan does not assert that all work will fit today before compilation and build support are checked.
