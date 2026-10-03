# Directional Ward Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add an asset-tuned Neutral Ward card with atomic payment, directional beam interception and live PlayMode tuning.

**Architecture:** PlayerWardRuntime owns a transient guard, independent of enemy selection or player health routing. The existing typed card operation/transaction pipeline arms it; enemies query a typed guard result before damage. Gargoyle integration follows the companion enemy plan.

**Tech Stack:** Unity6000.3.16f1, C#, NUnit EditMode and PlayMode tests, existing card/recovery systems.

**Spec:** `specs/ward-card-and-beam-interception-sdd-20261003-1128.md`; companion `specs/gargoyle-sentinel-implementation-plan-20261003-1205.md`.

## Contexto

First implementation plan following approved design and mandatory SO tuning. No card/runtime asset currently supplies Ward. Preserve ordinary loadouts and recovery formulas. Use companion plan's baseline and test commands.

## Global Constraints

- Namespace `TicGame.Architecture`; same-named files for concrete MonoBehaviours/SOs; explicit test initialization.
- WardDefinitionSO owns .60second duration,1.5height,.60forward offset. CardDefinitionSO owns20Energy cost. Enemy attack owns opening duration; enemy tuning owns stagger. No copied component defaults.
- Commit facing is locked; movement/airborne use remains allowed through existing Neutral availability. Pause/hold freezes timers and suppresses queries. Death/area/reset clears the guard.
- Only beam opening Countered consumes Ward; later Clipped preserves Ward until expiry. No global invulnerability or interception of melee/nova/volleys.
- Gameplay never mutates assets; live edits preserve elapsed time/facing and validate coherent revisions. Stale prepared costs/configurations fail without spending.

## Review Focus

1. A wallet callback reenters commit while guard is reserving: only one payment/arm (Task2).
2. A prepared quote outlives an Inspector cost/configuration edit: reject unpaid (Task2).
3. A moving guard intersects a beam after terrain clipping or from behind: geometry/order remains correct (Task1).
4. A second cast reuses a consumed guard or multiple colliders repeat a counter: no duplicate effect (Task1).
5. An area reset during slowed CardTime clears the guard without disrupting session clocks or recovery balance (Tasks1-2).

### Task 1: Guard geometry and lifetime

**Files:** Create `Assets/Scrips/Architecture/Player/Cards/{WardDefinitionSO,BeamGuardQuery,WardInterception,WardInterceptionKind}.cs`, `Assets/Scrips/Architecture/Player/Runtime/PlayerWardRuntime.cs`; test `Assets/Tests/EditMode/Architecture/Player/PlayerWardRuntimeTests.cs`.

**Interfaces:** BeamGuardQuery stores `long CastToken`, clipped `Vector2 Start/End`, `float Thickness`, `Vector2 EmitterPosition`, `bool IsOpening`. WardInterceptionKind = None, Clipped, Countered; WardInterception contains kind/clip position. Runtime `void Initialize(WardDefinitionSO definition)`, `bool CanArm`, `bool Arm(int facingSign)`, `void Tick(float scaledDelta)`, `WardInterception TryIntercept(in BeamGuardQuery query)`, `void Clear()`. A guarded internal reservation supports Task2's atomic arm; no enemy references.

- [ ] Write tests for forward/back emitter, crossing/tangent thickness, terrain-clipped short segment, moving/airborne root with fixed facing, opening consume once, late clip preserving guard, repeated cast, expiry. Mutate duration below/above elapsed and geometry after Arm; invalid edits retain valid state and diagnose once.
- [ ] Run PlayerWardRuntimeTests red using companion verification command.
- [ ] Implement required asset/typed geometry and elapsed duration. Bind pause/hold/death/reset/transition cleanup using existing lifecycle composition, not a competing player IDamageable. Configuration remains referenced; record immutable validated revision for stale-commit checks, including direct test mutations.
- [ ] Run fixture green; assert ordinary tick/query never changes SO fields.
- [ ] Commit `feat: add directional ward runtime and live guard tuning`.

### Task 2: Typed card transaction and recovery cost basis

**Files:** Modify `Assets/Scrips/Architecture/Player/Cards/{CardEffectKinds,CardOperationDefinition,CardDefinitionSO,PlayerCardRuntime,PreparedCardCommit}.cs`. Verify `Assets/Scrips/Architecture/Player/Recovery/{PlayerRecoveryController,PlayerRecoveryEconomy}.cs` with regression tests before changing them: current NeutralCostBasis already resolves equipped costs on every access, so preserve it if the live-edit tests pass. Extend `Assets/Tests/EditMode/Architecture/Player/PlayerCardRuntimeTests.cs` and the existing recovery fixture located by `rg --files Assets/Tests | rg Recovery`; create `Assets/Tests/EditMode/Architecture/Player/WardCardCommitTests.cs`.

**Interfaces:** Append `ArmDirectionalWard = 170` without renumbering. Operation references WardDefinitionSO. PreparedCardCommit records immutable quoted costs/configuration revision; existing TryApply and TryApplyPreparedCommit retain their return/failure contracts. Use reservation plus existing deferred resource notification transaction: validate session/readiness/revision, reserve guard, pay/arm coherently, then publish notifications. Roll back reservation on rejected commit, never after publishing partial state.

- [ ] Write tests for one20Energy payment/one arm, AlreadyApplied, active guard, stale category/session, cancelled selection, insufficient energy, player death and reentrant wallet notifications. Change cost or Ward configuration after preparation: payment0/guardinactive, then fresh selection succeeds.
- [ ] Add recovery tests: equipped highest positive Neutral cost15->20 changes passive reserve22.5->30, energy-to-health30->40 and BloodCharge15->20 without changing wallet balance/formulas; do not exempt Ward. Normal loadout remains unchanged.
- [ ] Run card/commit/recovery fixtures red.
- [ ] Implement typed dispatch, revision validation and transaction reservation. Recompute cost basis on relevant live edits; avoid a second generic card framework. Audit all card validation/support-shape/readiness paths and lifecycle reset bindings.
- [ ] Run all affected card/opportunity/recovery fixtures green relative to baseline, including held/paused guard and unchanged CardTime unscaled session behavior.
- [ ] Commit `feat: commit ward cards atomically with live cost validation`.

### Task 3: Catalog, feedback and test profile

**Files:** Modify `Assets/Scrips/Architecture/Editor/PrototypeCardAssetSetup.cs`; add Ward authoring in companion `Editor/GargoylePrefabSetup.cs`; extend setup tests and relevant card/HUD presenter rather than adding a second feedback system.

**Interfaces:** Stable card ID `card.neutral.ward`, Neutral reusable operation with20Energy cost. Separate Gargoyle inventory profile replaces JumpBoost's slot with Ward; retains GroundedDoubleJump, DashEnabler and recovery cards; Chain equips PoiseDamage. Player copy: "Briefly guard against beams. Catch the opening pulse to interrupt the enemy."

- [ ] Add tests for category/operation/cost/slot counts, unchanged ordinary profile, setup twice yielding identical references, directional shield/expiry/counter feedback and active readiness rejection.
- [ ] Run new tests red.
- [ ] Implement idempotent catalog and separate test profile plus guard visual feedback; assign visual dimensions/colors/lifetime settings through authored assets.
- [ ] Run focused setup/HUD/card tests and companion beam integration/PlayMode checks. Human acceptance must obtain real Neutral availability, select/orient Ward during the beam telegraph and test early/late/wrong-facing/no-energy paths on keyboard/gamepad.
- [ ] Commit `feat: author ward test loadout and beam guard feedback`.

## Self-review

Ward SDD behavior/geometry/lifetime maps to Task1, transaction/live-cost/recovery to Task2, catalog/profile/feedback to Task3. Enemy owns beam/stagger numbers in the companion plan. All five review-focus cases have explicit tests; full delivery and continuation shutdown remain governed by the companion acceptance gate.
