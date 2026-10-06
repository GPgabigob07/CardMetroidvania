# Repel Chain Card Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans for native execution or superpowers:subagent-driven-development if the user chooses delegation. Execute task-by-task and track the checkboxes.

**Goal:** Add the approved reusable 15-Energy Chain card that enables melee projectile reflection for 3 gameplay seconds.

**Architecture:** A player-local timed capability owns Repel availability and interception. The typed card operation arms it through the prepared transaction; active melee queries invoke projectile conversion without treating reflection as an enemy damage hit.

**Tech Stack:** Unity 6000.3.16f1, C#, ScriptableObject card data, Physics2D, Unity Test Framework.

**Spec:** `specs/repel-chain-card-sdd-20261005-1804.md` — approved by the user on 2026-10-05.

## Global Constraints

- Chain; 15 Energy; reusable; initial duration 3 scaled gameplay seconds.
- Reflect through melee execution overlap only; multiple projectiles, one conversion per projectile.
- Outward radial direction; coincident-position fallback reverses incoming direction.
- Preserve the documented converted damage rules, speed and remaining lifetime.
- Reject reactivation while active; no stacking, refresh or payment on rejection.
- Freeze on pause/world hold; clear on death, new run and area transition.
- Preserve normal loadouts, slot limits and existing Ward behavior.
- No card identity branches, enum renumbering, runtime asset writes or new dependencies.
- Use same-named files for concrete Unity script assets; follow repository conventions and Editor collaboration workflow.
- Preserve unrelated current working changes, including bat detection. No automatic commit of those changes.

## Review Focus

1. Multiple colliders must not reflect one projectile repeatedly (Task 1/3).
2. Cost/effect edits or reentrant resource events must not partially commit (Task 2).
3. Projectile-only attacks must work despite no damageable enemy candidates (Task 3).
4. Moving across an area or dying must not leave a stale Repel effect (Task 1/4).
5. Projectile/body damage ordering must not silently become invulnerability (Task 3/4).

## Verification procedure

Run each named fixture through Unity Test Runner/EditMode after each red/green cycle.
For an unattended runner when the main project is closed:

```powershell
& 'C:/Program Files/Unity/Hub/Editor/6000.3.16f1/Editor/Unity.exe' -batchmode -nographics -projectPath 'G:/UnityProjects/My project' -runTests -testPlatform EditMode -testFilter '<fixture>' -testResults 'Temp/RepelResults.xml' -logFile 'Temp/RepelTests.log'
```

Substitute the exact fixture named in each task. Inspect XML: expected failures must identify the missing behavior; green runs require zero failures and nonzero executed tests. Do not launch a competing batch process against an open project; use an isolated validation copy as in the bat work or a short Editor test handoff. Asset tests require the actual authored assets in that copy. Compilation failures do not establish regression behavior.

## Task 1: Timed capability and directional conversion

**Files:** Create `Assets/Scrips/Architecture/Player/Runtime/PlayerRepelRuntime.cs`; modify `Assets/Scrips/Architecture/Enemy/EnemyProjectile2D.cs`; create `Assets/Tests/EditMode/Architecture/Player/PlayerRepelRuntimeTests.cs`; extend `Assets/Tests/EditMode/Architecture/Enemy/EnemyProjectile2DTests.cs`.

**Interfaces:** `PlayerRepelRuntime.IsActive`, `RemainingSeconds`, `CanArm(float duration)`, `Tick(float scaledDelta)`, `Clear()`, and `TryRepel(EnemyProjectile2D projectile)` returning bool. Add internal reservation methods `TryReserve(float duration, out long lease)`, `CommitReserved(long lease)`, `ReleaseReservation(long lease)` and `PublishArmed()`. Add `EnemyProjectile2D.TryDeflect(GameObject playerSource, Vector2 direction)` returning bool; retain `Deflect(GameObject)` and its existing semantics by sharing conversion logic.

- [ ] Write failing tests: an armed 3-second effect remains active after 2.9 seconds and expires at 3; rearm/invalid duration is rejected; pause/hold freeze it; death clears it. A shot at player+(2,0) reflects right; a coincident shot reverses incoming direction. Second conversion fails; multiple separate shots succeed. Preserve health damage, speed, lifetime, 10 poise, provenance and proc suppression. Converted shots detach owner/budget.
- [ ] Run `TicGame.Architecture.Tests.PlayerRepelRuntimeTests` and `TicGame.Architecture.Tests.EnemyProjectile2DTests`; inspect the expected failures.
- [ ] Implement the signatures, validation, elapsed timer, health subscription and reservation. Use a single conversion helper; Repel requires a launched, unconverted hostile shot with a source other than the player. Publish feedback only after coherent arming.
- [ ] Run both fixtures; require all tests passing. Review lifecycle subscriptions and ownership checks.
- [ ] Record the completed task; commit only Repel-related files if committing is part of the selected execution workflow.

## Task 2: Typed card operation and atomic commit

**Files:** Modify `Assets/Scrips/Architecture/Player/Cards/CardEffectKinds.cs`, `CardOperationDefinition.cs`, `PlayerCardRuntime.cs`, `PreparedCardCommit.cs`; create sibling `PreparedRepelQuote.cs`; create `Assets/Tests/EditMode/Architecture/Player/RepelCardCommitTests.cs` and extend `PlayerCardCommitSnapshotTests.cs`.

**Interfaces:** Append `CardOperationKind.ArmProjectileRepel`; its `CardOperationDefinition.Amount` is the authored positive duration. `PreparedRepelQuote` snapshots the operation duration, effect revision, selection/session and costs, mirroring `PreparedWardQuote` with Chain category. The runtime consumes Task 1's reservation API. No extra duration SO is needed.

- [ ] Write failing tests: successful Chain commit spends exactly 15 and arms 3 seconds without consuming inventory; Neutral/Finisher, insufficient funds, active Repel, stale session/selection, changed cost/duration and invalid duration reject without payment/effect. Resource-event reentrancy cannot double spend or arm twice.
- [ ] Run `TicGame.Architecture.Tests.RepelCardCommitTests` and `TicGame.Architecture.Tests.PlayerCardCommitSnapshotTests`; inspect failures.
- [ ] Extend validation, supported operation dispatch, readiness and prepared quote handling. Reserve before payment, use deferred resource notifications, commit effect coherently, then publish. Release reservations in failure/finally paths. Preserve Ward's path.
- [ ] Run these fixtures plus `TicGame.Architecture.Tests.WardCardCommitTests` and `TicGame.Architecture.Tests.PlayerCardRuntimeTests`; require zero failures.
- [ ] Record/commit this task without unrelated files.

## Task 3: Melee interception

**Files:** Modify `Assets/Scrips/Architecture/Player/Runtime/PlayerAttackHitDetector2D.cs`; create `Assets/Tests/EditMode/Architecture/Player/PlayerProjectileRepelHitTests.cs` and a PlayMode fixture `Assets/Tests/PlayMode/Architecture/Player/ProjectileRepelCollisionTests.cs`.

**Interfaces:** The detector resolves `PlayerRepelRuntime` locally and calls `TryRepel` for canonical projectile components found by the active melee box. If needed for meaningful tests, expose `public void ResolveActiveAttackHits()` as the same phase-gated method called by Update; do not provide an unguarded public reflection path.

- [ ] Write failing tests: execution+buff reflects a projectile-only overlap; no buff, windup, recovery or pause does not. Child/duplicate colliders convert once; multiple shots convert separately. Enemy damage still resolves in the same swing. Reflections alone do not confirm a hit, advance chain, grant Energy, consume hit effects or request hitstop.
- [ ] Add PlayMode cases: melee interception processed first prevents that shot's player damage; a missed shot can damage the player; prior accepted damage is not refunded. Use real colliders/physics and the real detector path.
- [ ] Run both named fixtures in their respective test platforms and confirm expected failures.
- [ ] Process projectiles before the no-enemy-target early return using the current box geometry and execution gate. Ensure projectile-layer coverage separately from ordinary damage filtering. Deduplicate per query and rely on conversion eligibility across frames.
- [ ] Run both fixtures plus existing projectile/damage/attack confirmation tests relevant to the changed path; require zero failures. Review collision ordering and player-source child exclusion.
- [ ] Record/commit this task.

## Task 4: Authored card, lifecycle, feedback and playtest loadout

**Files:** Create `Assets/Data/Cards/Definitions/Card_Chain_Repel.asset` and `Assets/Data/Cards/Effects/Effect_Repel.asset`; create `Assets/Scrips/Architecture/Editor/RepelCardSetup.cs`; modify `PlayerCardRuntime.ClearNewCardEffects` and the existing player lifecycle/setup hooks where needed; create `Assets/Tests/EditMode/Architecture/Player/RepelCardAssetTests.cs` and `RepelLifecycleTests.cs`.

**Interfaces:** `RepelCardSetup.CreateOrUpdateRepelPlaytest()` is an idempotent Editor menu command. It creates stable id `repel`, display name Repel, Chain, reusable, Energy cost 15 and operation duration 3; adds the catalog entry and player capability, and creates a separate Bat/Repel test inventory using an existing Chain slot. Source production inventory assignments stay unchanged.

- [ ] Write failing tests for authored values, supported shape, unchanged normal loadouts, repeated setup without duplicates, death/reset/area-transition clearing, combo/Card Time completion retaining the effect, and active/expiry feedback without changing gameplay. Duration description derives from the operation data.
- [ ] Run `TicGame.Architecture.Tests.RepelCardAssetTests` and `TicGame.Architecture.Tests.RepelLifecycleTests`; inspect failures.
- [ ] Implement setup using Unity serialization APIs and preserve GUIDs. Trace real transition/death/new-run paths; add clearing at the same transient-effect boundary as Ward, without treating every temporary hold as a transition. Connect existing feedback where supported.
- [ ] Run setup twice and both fixtures, then broader EditMode tests and the collision PlayMode fixture. Require nonzero executed tests and zero failures; inspect `git diff --check` and scene/prefab overrides.
- [ ] Hand off the focused test scene: activate Repel through Chain, reflect grounded/aerial shots, reflect several within 3 seconds, verify expiry and missed-shot damage, and verify converted damage on an enemy. Save a timestamped verification note with actual results and pending visual checks.
- [ ] Review the whole change against the approved spec and record/commit the final task if applicable.

## Handoff

Plan self-reviewed against the approved spec: activation, transactional failure,
geometry, direction, documented damage, timer/lifecycle, feedback, assets and
playtest each have an owning task. No runtime implementation has started.
Recommended execution: native in this chat; the four tasks depend closely on
the capability and transaction interfaces. Await plan review and execution-method
selection before implementing.
