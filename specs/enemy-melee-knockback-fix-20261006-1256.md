# Enemy melee knockback fix — 20261006-1256

## Contexto

The user reported that player hits did not repel either Bat Machine or Golem Charger and requested visible knockback in neutral states. This note records a narrow repair alongside existing, uncommitted hit-ripple work.

Sources: `gdd/gdd-canonico-20260526-2331.md`, `specs/bat-machine-enemy-sdd-20260901-1202.md`, `specs/golem-charger-enemy-sdd-20260806-2146.md`, `specs/unity-editor-collaboration-workflow-20260612-1609.md`, and `specs/hit-ripple-lab-and-contact-fix-20261006-1232.md`.

## Cause and correction

PlayerAttackHitDetector2D already requests base knockback force 1. PlayerCombatEffects already finds IKnockbackReceiver up the accepted child hurtbox's hierarchy. Neither combat enemy prefab had EnemyKnockbackReceiver. Furthermore, the existing receiver suppressed only EnemyPatrolBrain, while these enemies use dedicated brains; golem patrol overwrites horizontal velocity outright, and bat steering counters the impulse (or Stop clears it).

Both authored prefab roots now expose EnemyKnockbackReceiver wired to their dynamic Rigidbody2D. Their idempotent prefab setup commands also install it. The receiver pauses their dedicated AI movement for its existing configurable 0.15 scaled seconds before applying the impulse, with resistance multiplier 1. No extra Inspector assignment or setup menu invocation is required for existing prefab instances.

The brains defer state updates that can enter movement-stopping states during that window. Poise processing and immediate death/stun events still run; the bat's falling/grounded lifecycle continues. Rejected damage, including ordinary golem charge hits, continues through the existing policy and never triggers PlayerCombatEffects knockback. Attack force, armor multipliers, and card force scaling are unchanged.

## Verification

Unity 6000.3.16f1, isolated `local-artifacts/HitRippleLabValidation`:

- Before repair, all four regressions failed: missing receivers on both prefabs, golem velocity overwritten from +4 to -0.75, bat steering modified (+4,0).
- Focused related suite: 90 passed, zero failed.
- Eight new regressions passed in final broad run: prefab wiring, impulse survival/resumed AI for both brains, and real player damage resolved through each child hurtbox followed by Physics2D simulation showing left/right displacement at base force 1.
- Full EditMode: 666 total, 643 passed, 20 failed, 3 skipped. Exact failed-test names match `local-artifacts/HitRippleLabWork/all-editmode.xml`; no added failures.
- Runtime, Editor and test assemblies compiled in Unity; git diff --check passes.

Logs/results: `local-artifacts/EnemyKnockbackWork/`. Existing unrelated edits and recovery assets preserved.

## Play Mode handoff

Allow the open Unity project to recompile. In Test_BatMachine, Test_GolemCharger, or gameplay scenes, hit a patrolling enemy from each side: it should briefly move with the hit before resuming AI. Ordinary hits during the golem's protected charge should still be rejected. Tune EnemyKnockbackReceiver's Resistance Multiplier or Movement Suppression Duration on the prefab if the displacement needs adjustment. The subjective feel remains an Editor playtest check.

## Existing suite failures

- TicGame.Architecture.Tests.AreaProgressBindingTests.CheckpointRejectsMarkerFromAnotherSceneWithoutChangingRespawnAddress
- TicGame.Architecture.Tests.AreaProgressBindingTests.StreamedZoneDoesNotUseSerializedLocalGuideWithoutBinding
- TicGame.Architecture.Tests.CardFeedbackPresenterTests.HudPresenter_RendersViewModelTextWithoutPlayerCounters
- TicGame.Architecture.Tests.CardTimeSelectionHudUITests.BindSelection_UsesInputDisplayMapperForActiveSchemeLabels
- TicGame.Architecture.Tests.CardTimeSelectionHudUITests.BindSelection_UsesSchemeLabelsForSlots
- TicGame.Architecture.Tests.DamageResolverTests.DamageResult_DefaultsToPointOneSecondHitStop
- TicGame.Architecture.Tests.DamageResolverTests.Resolve_UsesDamageProfileTagsWhenInstanceTagsAreNotProvided
- TicGame.Architecture.Tests.EnemyBaselineTests.Dummy_ImmediateDefeatRestore_UsesConfiguredFeedbackDelay
- TicGame.Architecture.Tests.EnemyBaselineTests.Dummy_RegenerationWaitsAndAdditionalDamageRestartsDelay
- TicGame.Architecture.Tests.GameplayRespawnTests.CancelAndReconfigureFenceAPendingRespawnFromTheNextSession
- TicGame.Architecture.Tests.GameplayRespawnTests.ResetCrossingClearsEveryColliderContactForThePlayer
- TicGame.Architecture.Tests.GameplaySceneSetupTests.TemporaryFixtureSceneCanBeCreatedAndRemovedWithoutProjectAssets
- TicGame.Architecture.Tests.GameplayServicesRootTests.BindScene_Repeated_DoesNotBindConsumerTwice
- TicGame.Architecture.Tests.GameplayServicesRootTests.Initialize_ConfiguredRoot_InitializesModulesAndBindsSceneConsumers
- TicGame.Architecture.Tests.GameplayStartupTests.Coordinator_MissingSceneKeepsThePlayerHeldAndAllowsAnExplicitRetry
- TicGame.Architecture.Tests.GameplayStartupTests.SceneRoot_InvalidInitialAddressKeepsTheConfiguredPlayerHeldAndReportsARetryableError
- TicGame.Architecture.Tests.GameplayStartupTests.WorldHold_NestedLeasesKeepPhysicsDisabledUntilTheFinalLeaseIsReleased
- TicGame.Architecture.Tests.GameplayTimeCoordinatorTests.RemovingCardTime_DuringHitStop_KeepsTimeStopped
- TicGame.Architecture.Tests.GameplayTimeCoordinatorTests.Shutdown_RestoresCapturedBaseline
- TicGame.Architecture.Tests.PlayerMovementControllerTests.PlayerDeathRespawn_ResetsPositionVelocityAndHealth_WhenHealthReachesZero


## Independent review

Read-only review found no P0–P2 correctness issues in the scoped changes. The reviewer checked body references, effective-hit routing, suppression, charge rejection, and immediate defeat/interrupt callbacks. Play Mode feel remains for the user.

