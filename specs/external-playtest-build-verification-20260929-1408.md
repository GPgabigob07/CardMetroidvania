# External playtest build — verification record

## Contexto

Execution of the approved 20260929-1147 external-playtest design/plan, with the user explicitly selecting uGUI. This record supplements those preserved planning drafts. Build is a candidate until hands-on acceptance; no upload or tester message is authorized.

## Implemented

- MainMenu entry, editable uGUI prefab with title/controls/pause/guide/confirmation/loading/error panels.
- Persistent session transitions, single-flight gate, streamed-run cleanup and service reset.
- Pause time ownership, preserved velocity, input suppression/release, frozen Card Time/hitstop timers.
- Reserved gamepad Start for pause; discovery/F1 guide flows use uGUI.
- Idempotent setup and Windows builder, accurate serialized action bindings, release debug-overlay suppression.
- Tester README, feedback form, known issues and build manifest.

## Decisions during implementation

- Kept the user-requested feature branch in the current checkout; avoided a second Unity import/worktree.
- Retained Windows/English defaults after the clarification opportunity.
- Shared one persistent Canvas/view with session controller to avoid duplicate UI/input during scene changes. Loading is a panel in MenuView rather than another class.
- Used geometric card/castle draft art; optional generated raster art is deferred so the build path takes priority.
- CardTimeSessionController and HitStopService are the unscaled timer owners guarded during pause.
- No finish/merge operation: full inherited suite is not green, and gameplay acceptance remains pending.

## Automated evidence

- Initial frontend red tests: 2/2 failed for absent gate/lease, then 2/2 passed.
- Final expanded PlayMode scene tests: 2/2 passed; cover three new runs, direct Gameplay entry, streaming return, pause velocity, Card Time timer/slowdown ownership.
- Full feature EditMode suite: 381 total, 358 passed, 20 failed, 3 skipped.
- Baseline 4beafe6 EditMode suite: 379 total, 356 passed, the identical 20 failed and 3 skipped.
- Baseline source was tested through a backed-up, automatically restored temporary source swap; Git branch/HEAD never changed.
- XML/log evidence retained under ignored `.utmp/playtest/`: red.xml, green-ui.xml, playmode3.xml, editmode-full.xml, baseline.xml.
- Rendered title/controls/pause captures retained there. Title and controls visually inspected at 1280x720. These are PlayMode render captures, not a hands-on standalone playthrough.
- Fresh reviewer dispatch failed due account usage limit; no independent review result exists. Author review checked lifecycle, input ownership, service reset, guide routing, scene inclusion and build metadata.

## Remaining acceptance

User must verify physical keyboard/gamepad navigation, focus loss, Blue/Pink route, death with destination unloaded, hazard recovery, feel, and 1080p/16:10 layout in the extracted standalone. Do not label the candidate release-ready before this pass. Build/ZIP details follow once generated.

## Inherited failing tests
- `TicGame.Architecture.Tests.AreaProgressBindingTests.CheckpointRejectsMarkerFromAnotherSceneWithoutChangingRespawnAddress`
- `TicGame.Architecture.Tests.AreaProgressBindingTests.StreamedZoneDoesNotUseSerializedLocalGuideWithoutBinding`
- `TicGame.Architecture.Tests.CardFeedbackPresenterTests.HudPresenter_RendersViewModelTextWithoutPlayerCounters`
- `TicGame.Architecture.Tests.CardTimeSelectionHudUITests.BindSelection_UsesInputDisplayMapperForActiveSchemeLabels`
- `TicGame.Architecture.Tests.CardTimeSelectionHudUITests.BindSelection_UsesSchemeLabelsForSlots`
- `TicGame.Architecture.Tests.DamageResolverTests.DamageResult_DefaultsToPointOneSecondHitStop`
- `TicGame.Architecture.Tests.DamageResolverTests.Resolve_UsesDamageProfileTagsWhenInstanceTagsAreNotProvided`
- `TicGame.Architecture.Tests.EnemyBaselineTests.Dummy_ImmediateDefeatRestore_UsesConfiguredFeedbackDelay`
- `TicGame.Architecture.Tests.EnemyBaselineTests.Dummy_RegenerationWaitsAndAdditionalDamageRestartsDelay`
- `TicGame.Architecture.Tests.GameplayRespawnTests.CancelAndReconfigureFenceAPendingRespawnFromTheNextSession`
- `TicGame.Architecture.Tests.GameplayRespawnTests.ResetCrossingClearsEveryColliderContactForThePlayer`
- `TicGame.Architecture.Tests.GameplaySceneSetupTests.TemporaryFixtureSceneCanBeCreatedAndRemovedWithoutProjectAssets`
- `TicGame.Architecture.Tests.GameplayServicesRootTests.BindScene_Repeated_DoesNotBindConsumerTwice`
- `TicGame.Architecture.Tests.GameplayServicesRootTests.Initialize_ConfiguredRoot_InitializesModulesAndBindsSceneConsumers`
- `TicGame.Architecture.Tests.GameplayStartupTests.Coordinator_MissingSceneKeepsThePlayerHeldAndAllowsAnExplicitRetry`
- `TicGame.Architecture.Tests.GameplayStartupTests.SceneRoot_InvalidInitialAddressKeepsTheConfiguredPlayerHeldAndReportsARetryableError`
- `TicGame.Architecture.Tests.GameplayStartupTests.WorldHold_NestedLeasesKeepPhysicsDisabledUntilTheFinalLeaseIsReleased`
- `TicGame.Architecture.Tests.GameplayTimeCoordinatorTests.RemovingCardTime_DuringHitStop_KeepsTimeStopped`
- `TicGame.Architecture.Tests.GameplayTimeCoordinatorTests.Shutdown_RestoresCapturedBaseline`
- `TicGame.Architecture.Tests.PlayerMovementControllerTests.PlayerDeathRespawn_ResetsPositionVelocityAndHealth_WhenHealthReachesZero`


## Candidate artifact

Unity BuildReport succeeded for Windows x64 using the installed Mono backend.
Code checkpoint: `8bd8b7d`. Build ID: `playtest-20260929-170922`.
ZIP: `Builds/playtest-20260929-170922/CardMetroidvania-Windows.zip`.
Size: 42,691,856 bytes. SHA-256: `D078A8F150DAF48377B54AA1D3A7E54DF4F3A54835C24CCD9C73B528426BDCB3`.

The ZIP includes executable, data, runtime dependencies and tester documents; the generated Burst DoNotShip debug folder was excluded. Extracted to a fresh OS temporary folder outside the project and launched in headless mode. Process remained responsive with no reported startup errors/exceptions, then the agent stopped its own smoke-test process. This verifies runtime packaging/startup, not graphical input or a full playthrough.

Manifest correctly reports dirty=true: the builder applied its timestamped version and windowed 1280x720 settings after the code checkpoint. Those exact settings are retained in the branch. Full build log is `.utmp/playtest/build.log`; standalone log is `.utmp/playtest/standalone.log`.

The only whitespace warnings during staging were Unity-generated empty YAML values. No gameplay geometry was changed. No push, merge, upload or external message occurred.

## Handoff

Extract the ZIP, launch `CardMetroidvania.exe`, and check title → Start → Blue/Pink route → pause/resume → death/hazard recovery → title → fresh run. Try the intended controller and verify menus at the tester's resolution. Send any Console/Player.log errors or observed blockers before calling this candidate ready to share.
