# Gargoyle Sentinel Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Deliver a solo, grounded Gargoyle Sentinel encounter with readable shuffled attacks, two card counters, live asset tuning and an isolated test arena.

**Architecture:** Reuse EnemyActor, EnemyHealth, EnemyPoise and the owned-state FSM. A plain selector and phase runner own commitments; dedicated payload components own physics queries. Ward is an independently testable dependency, implemented through the companion plan before beam integration.

**Tech Stack:** Unity 6000.3.16f1, C#, NUnit EditMode and Unity PlayMode tests; no new AI package.

**Spec:** `specs/gargoyle-sentinel-enemy-sdd-20261003-1128.md`; `gdd/gargoyle-sentinel-design-20261003-1128.md`; companion `specs/ward-implementation-plan-20261003-1205.md`.

## Contexto

The author approved the encounter design and required all tuning in ScriptableObjects. This is the first implementation plan; it preserves both prior spec versions. The requested quota policy is stop at 95% used in the 300-minute window, checkpoint, and resume after reset. Scheduling is not a substitute for pending human approvals.

Fresh baseline on 2026-10-03: `.utmp/gargoyle/baseline.xml`, 417 tests, 394 passed, 20 failed, 3 skipped. Failure identities exactly match `.utmp/recovery/editmode-final.xml`. Existing failures remain explicit; a passing new fixture does not mean the full suite passes.

## Global Constraints

- Runtime namespace `TicGame.Architecture`; runtime paths retain the existing `Assets/Scrips` spelling.
- Each concrete MonoBehaviour/ScriptableObject has its own same-named file; follow code/testing/editor collaboration conventions listed in the spec.
- All authored tuning is asset-owned. Components bind references and keep instance state; no duplicate numeric fallbacks. Apply the spec's live-edit boundaries, last-valid coherent configuration and no gameplay writes to SO assets.
- Unit root scale; exact 200 x 200 sprite rects, ordinary alpha envelope <=128 x 128, fixed pivot (100,36), provisional 48 PPU. Human body-mask acceptance requires world area ratio >=1.5 excluding wings/effects.
- Death > core/global poise stun > beam stagger > normal advancement. Pending interrupts suppress emissions immediately, including synchronous damage callbacks.
- No adds, flight, contact damage, BT package, ordinary loadout replacement or silent player resizing.
- Every task uses failing behavioral tests first, then minimal implementation, focused green verification and a reviewed commit. Push checkpoints to the feature branch; do not merge master.

## Review Focus

1. Inspector edits during an attack preserve elapsed time and do not replay hits; cover in Tasks 1/3/5 and Ward Task 1.
2. Lethal damage and poise/core depletion in one callback choose death once; cover Task 2/4/7.
3. A large frame delta cannot skip a damaging physics sample or release twice; cover Task 3/5/7.
4. Multiple child colliders and supplemental card damage do not duplicate card charges, cast damage or reactor hits; cover Task 2/5/7.
5. Partial bags, lost targets and pause/hold cannot reroll attacks or leave damaging projectiles behind; cover Task 4/5/8.

## Verification commands

Use the installed editor at `C:/Program Files/Unity/Hub/Editor/6000.3.16f1/Editor/Unity.exe`. For each fixture, use this PowerShell command with its fully qualified fixture substituted for `<fixture>` and task-specific evidence name:

```powershell
& 'C:/Program Files/Unity/Hub/Editor/6000.3.16f1/Editor/Unity.exe' -batchmode -nographics -projectPath 'G:/UnityProjects/My project' -runTests -testPlatform EditMode -testFilter '<fixture>' -testResults 'G:/UnityProjects/My project/.utmp/gargoyle/focused.xml' -logFile 'G:/UnityProjects/My project/.utmp/gargoyle/focused.log'
```

Expected red: the new assertion fails for the intended missing behavior, not fixture initialization or an unrelated baseline issue. Expected green: results XML lists the requested fixture with zero failures and log contains no compiler errors. Preserve red/green evidence per task. If execution uses a worktree, substitute its returned absolute project path. Never run two Unity instances against one checkout. Full verification omits `-testFilter`; PlayMode uses `-testPlatform PlayMode`.

## File boundaries

New enemy code lives in `Assets/Scrips/Architecture/Enemy/Gargoyle/`; tests in `Assets/Tests/EditMode/Architecture/Enemy/`. Pure contracts are separate from concrete components. Editor setup lives in the existing `Assets/Scrips/Architecture/Editor/`; assets and prefab are generated only after runtime behavior is verified.

### Task 1: Asset contracts and deterministic selection

**Files:** Create `Enemy/Gargoyle/{GargoyleTuningSO,GargoylePresentationSO,EnemyAttackDefinitionSO,GargoyleAttackSelector,GargoyleAttackFamily,EnemyAttackStep,EnemyAttackPhase}.cs` under the runtime root; tests `GargoyleConfigurationTests.cs`, `GargoyleAttackSelectorTests.cs`.

**Interfaces:** `GargoyleAttackFamily` = Wingbreaker, Volley, Beam. `EnemyAttackPhase` = Windup, Active, Recovery, Completed. `GargoyleAttackSelector(GargoyleTuningSO tuning, System.Random random)`, `GargoyleAttackFamily PeekFamily()`, `GargoyleAttackFamily CommitFamily()`, `int CommitVolleyCount()`, `int RemainingFamilies`, `void Reset(System.Random random)`. Attack definition exposes ordered `IReadOnlyList<EnemyAttackStep> Steps`; each step has stable ID, phase durations, payload kind, geometry/aim/motion and DamageProfileSO reference. Tuning references Basic, each family, Nova and presentation. Validation returns diagnostics without mutating authoring data.

- [ ] Write tests asserting each bag contains A/B/C once, next first differs from previous last, all six orders occur over seeds 0..255, identical seeds reproduce sequences, Peek does not consume, interruption preserves remaining entries and volley bags contain 1/3/5 once. Add asset tests for negative/nonfinite durations, malformed variants, required references and immutable runtime use.
- [ ] Run both fixtures and observe the intended red assertions.
- [ ] Implement assets and Fisher-Yates bags. Keep current bag contents across repertoire/seed edits until refill; use the exact initial values in the approved spec when authoring fixtures, not runtime fallbacks.
- [ ] Run both fixtures green; mutate tuning between draws to verify refill-only changes.
- [ ] Commit `feat: add gargoyle asset contracts and shuffled attack selection` with Unity metadata.

### Task 2: Explicit damage regions and provenance

**Files:** Modify `Core/DamageTypes.cs`, `Damage/DamageResolver.cs`, `Player/Runtime/PlayerAttackHitDetector2D.cs`, `Player/Runtime/PlayerCombatEffects.cs`, `Enemy/EnemyHealth.cs`, `Enemy/EnemyPoise.cs`; create `Enemy/IEnemyDamageRegion.cs`, `Enemy/Gargoyle/{GargoyleDamagePolicy,GargoyleHurtbox,GargoyleRegionKind}.cs`; tests `GargoyleDamagePolicyTests.cs` and extend existing damage/player/poise fixtures.

**Interfaces:** Append optional `DamageProvenance? provenance = null, string attackExecutionId = null` to DamageContext constructor and expose read-only properties; forward the existing string DamageInstance.AttackExecutionId without inventing a second player execution identity. Preserve metadata through all rebuilt contexts. `IEnemyDamageRegion` exposes `EnemyActor Owner`, `GameObject DamageRecipient`, `int Priority`. GargoyleHurtbox implements it and IDamageable, routing to `DamageResult GargoyleDamagePolicy.ApplyDamage(GargoyleRegionKind region, in DamageContext context)`. Add `void EnemyHealth.UpdateMaximumHealth(float maximum)` and `void EnemyPoise.UpdateConfiguration(float maximum, float regenerationPerSecond)` to clamp current absolute values without refill/revive. No new competing root IDamageable.

- [ ] Write tests: body health multiplier1; staggered head1.5; duplicate regions choose one canonical actor; primary enhanced strike charges once; supplemental/converted damage cannot qualify for core stability; simultaneous lethal/poise causes no living stun; ceiling edits clamp without initialization/revival. Exercise both existing Golem and Bat routing.
- [ ] Run new and affected fixtures red; separate the two inherited DamageResolver failures by exact name.
- [ ] Implement the narrow region contract, metadata forwarding and ceiling-update APIs. Retain existing Golem/Bat behavior; audit every DamageContext construction for preserved metadata.
- [ ] Run focused fixtures; no new failures and all new assertions green.
- [ ] Commit `feat: route gargoyle hurtboxes with primary damage identity`.

### Task 3: Authoritative phase runner

**Files:** Create `Enemy/Gargoyle/{EnemyAttackRunner,EnemyAttackExecutionSnapshot}.cs`; test `EnemyAttackRunnerTests.cs`.

**Interfaces:** `bool Begin(EnemyAttackDefinitionSO definition, long executionToken)`, `void SetAim(Vector2 direction)`, `void Tick(float scaledDelta)`, `void ConfirmPhysicsSample(long executionToken, string stepId)`, `bool TryConsumeRelease(long executionToken, string stepId)`, `void Cancel()`, `EnemyAttackExecutionSnapshot Current`. Snapshot exposes execution token, stable step ID, phase, elapsed time, release-due state and locked aim. SetAim is ignored after commitment; emission acknowledgment belongs to the runner so repeated queries cannot repeat releases. Running executions retain step identities while current valid numeric tuning remains live.

- [ ] Write tests for Begin while running, idempotent Cancel, obsolete tokens, shorter/longer edited durations, surplus delta, zero delta, active-phase physics sampling and one emission. Assert Tick(10) cannot skip the first damaging Active until ConfirmPhysicsSample.
- [ ] Run fixture red.
- [ ] Implement elapsed-time transitions and mandatory sample barrier; lock aim at windup end and discard stale callbacks after cancellation.
- [ ] Run fixture green with asset mutations after Begin.
- [ ] Commit `feat: add interruptible asset-driven enemy attack phases`.

### Task 4: Grounded brain, melee and stun

**Files:** Create `Enemy/Gargoyle/{GargoyleBrain,GargoyleState,EnemyMeleeAttack2D}.cs`; tests `GargoyleBrainTests.cs`, `EnemyMeleeAttack2DTests.cs`.

**Interfaces:** `void GargoyleBrain.Initialize(EnemyActor actor, EnemyPoise poise, GargoyleTuningSO tuning, IEnemyPatrolMotor2D motor)`, `void SetTarget(GameObject target)`, `void Tick(float scaledDelta)`, `void TickPhysics(float scaledDelta)`, `void RequestStun()`, `void RequestBeamStagger(long castToken)`, `void ResetEncounter()`. Brain exposes current state/runner snapshot for presentation. Melee `void Sample(in EnemyAttackExecutionSnapshot execution)` and `void Cancel()` deduplicates accepted targets per step token.

- [ ] Write tests for Basic three strikes then committed family; one stun restoration; death priority; post-response resistance0.75; stunned1.25; stalled reposition1.5 then recovery0.8 retaining selection; held/lost target cancellation; edited stun duration and maximum poise on initialized enemy. Query overlapping player colliders once per strike.
- [ ] Run fixtures red.
- [ ] Implement owned-state FSM, explicit pending interrupt arbitration, grounded motor use and live tuning synchronization. Bind scaled Update/FixedUpdate with pause/world-hold guards; never initialize health to apply edits. No offense after a reentrant damage callback invalidates the actor.
- [ ] Run new fixtures and existing enemy baseline/Golem/Bat tests; compare inherited failures.
- [ ] Commit `feat: add grounded gargoyle melee and stun lifecycle`.

### Task 5: Volley patterns and shared cast budgets

**Files:** Create `Enemy/Gargoyle/{EnemyProjectilePatternLauncher,EnemyCastHitBudget}.cs`; modify `Enemy/EnemyProjectile2D.cs` through optional launch hooks that preserve current callers; tests `EnemyProjectilePatternLauncherTests.cs`, extend `EnemyProjectile2DTests.cs`.

**Interfaces:** `EnemyCastHitBudget(long castToken, int acceptedHitLimit)` with `bool TryReserve(GameObject canonicalTarget)` and `void Complete(GameObject canonicalTarget, bool accepted)`. Launcher `void Release(EnemyAttackDefinitionSO definition, int count, long castToken, Vector2 origin, Vector2 direction)` and `void CancelOwnedProjectiles()`. Projectiles receive optional shared budget/ownership handle; accepted hit limit is authored, not duplicated in projectiles.

- [ ] Write tests for count1/3/5 and exact angles [0],[-12,0,12],[-30,-15,0,15,30]; total damage1HP across a cast, terrain expiry, normal completion preserving shots, interrupt cleanup only this encounter, transferred ownership excluded. Launch edits affect the next shot; existing shots retain6unit/s and3second launch motion values.
- [ ] Run fixtures red.
- [ ] Implement pattern releases, atomic reservation against reentrant hits and ownership registry; preserve Bat/projectile APIs and proc None enemy payloads.
- [ ] Run fixtures green, including large delta/no duplicate release and pause/hold suppression.
- [ ] Commit `feat: add gargoyle volley casts and owned projectile cleanup`.

### Task 6: Ward dependency and beam

**Dependency:** Complete companion Ward Tasks1-2 before this task.

**Files:** Create `Enemy/Gargoyle/EnemyBeamAttack2D.cs`; test `EnemyBeamAttack2DTests.cs`.

**Interfaces:** `void Begin(long castToken, EnemyAttackDefinitionSO definition)`, `void Sample(Vector2 origin, Vector2 lockedDirection, float activeElapsed)`, `void Cancel()`. Use companion `BeamGuardQuery` and `WardInterception`; request brain stagger only for Countered once per token, after checking death priority.

- [ ] Write tests for thick terrain clipping, one accepted health hit/cast, first0.20second Ward cancellation before damage, late clipping without consumption, expiry, back-facing guard, emitter blocked by terrain, multiple colliders and synchronous death on counter notification.
- [ ] Run beam/Ward fixtures red.
- [ ] Implement0.80windup with first0.50tracking/final0.30locked,0.70active/0.75recovery from attack assets. Query guard before health and use live thickness/geometry.
- [ ] Run fixtures green, mutate opening duration/thickness during a cast and verify no replay.
- [ ] Commit `feat: integrate ward interruption with gargoyle beam`.

### Task 7: Reactor nova and bounded feint

**Files:** Create `Enemy/Gargoyle/EnemyNovaAttack2D.cs`; modify brain/policy; tests `EnemyNovaAttack2DTests.cs`, extend brain/runner fixtures.

**Interfaces:** `void Begin(long castToken, EnemyAttackDefinitionSO definition)`, `bool TryApplyCoreHit(in DamageContext context)`, `void Release(Vector2 center)`, `void Cancel()`, read-only stability/telegraph state. Deduplicate qualifying primary execution IDs; keep accumulated core damage when stability threshold changes.

- [ ] Write tests: Nova due only after two exhausted bags and12gameplay seconds at pass boundary; countered attempt consumes cadence; stability4.8 broken by two2.4 primary enhanced core hits; body/supplemental hits do not contribute; ordinary positive core hit can confirm Chain; global poise cancels; death wins; release radius3.5 and damage2 once. Test threshold/radius live edits and Tick large delta.
- [ ] Run new/affected fixtures red.
- [ ] Implement Nova2.4windup/.15active/.90recovery and finite escapeable radial query. Add max-one feint/pass sampled at20%, cue at.15 with>=.35 response; delayed claw only; no branch after aim lock or reroll family.
- [ ] Run fixtures green; assert interrupted partial bag survives and nova is not forced after every stun.
- [ ] Commit `feat: add interruptible reactor nova and bounded gargoyle feint`.

### Task 8: Presentation, reproducible arena and delivery

**Files:** Create `Enemy/Gargoyle/{GargoyleAnimationPresenter,GargoyleDebugPresenter}.cs`, `Editor/GargoylePrefabSetup.cs`; tests `GargoylePrefabSetupTests.cs`, `Assets/Tests/PlayMode/Architecture/GargoyleEncounterPlayModeTests.cs`. Generated assets under `Assets/Data/Enemies/GargoyleSentinel/`, draft art under `Assets/Art/Enemies/GargoyleSentinel/`, prefab `Assets/Prefabs/Enemies/GargoyleSentinel.prefab`, scene `Assets/Scenes/Test_GargoyleSentinel.unity`.

**Interfaces:** `void GargoylePrefabSetup.CreateOrUpdate()` through `TIC/Setup/Create Or Update Gargoyle Sentinel`; presenter consumes brain snapshots, never drives damage. Setup authors every initial default from the specs and validates references/ranges/probes/card slots/pixels. Separate Editor apply from live simulation settings.

- [ ] Write serialization tests for exact200rect/<=128alpha/fixedpivot/unit scale, one enemy, movement escape room, loadout recovery slots, repeated setup serialized hashes equal, assets unmodified by combat. Add PlayMode tests for actual Update/FixedUpdate, pause/hold/death/transition cleanup and budget arbitration.
- [ ] Run fixtures red.
- [ ] Implement setup and clear telegraphs/core/guard/stun feedback using current assets; visibly label the single-frame draft pending artist animations. Supply body-mask area report rather than claiming visual acceptance from collider dimensions.
- [ ] Run focused/full EditMode and PlayMode, compare all inherited failure identities, inspect serialized diff and git diff --check. Perform a fresh whole-branch review using the required executing-plans workflow and resolve findings. Save timestamped verification report.
- [ ] Request the short human PlayMode acceptance from both specs: all attack orders, core/Chain two-hit route, Ward timing, movement/zero-energy escapes, readable feint/stun, keyboard/gamepad, Inspector timing/damage/radius edits, pixel/body scale. Record actual observations; missing artist animation or human acceptance remains open.
- [ ] Commit and push verified delivery checkpoint. Disable the continuation only when remaining implementation, verification and human acceptance are complete. Never mark a partial build complete or merge automatically.

## Self-review

All enemy SDD sections map to Tasks1-8; Ward/card transaction/economy ownership maps to its companion plan. Five review-focus conditions have explicit owning fixtures. Asset defaults remain in specs and task fixtures/setup, not gameplay code. Signatures above are the shared contracts; metadata and projectile extensions preserve existing callers. Human art/body-mask/timing acceptance is deliberately a delivery gate rather than fabricated automated evidence.
