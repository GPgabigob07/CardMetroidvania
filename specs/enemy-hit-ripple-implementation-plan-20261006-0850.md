# Enemy Hit Ripple Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans for native execution, or superpowers:subagent-driven-development if the user chooses delegation. Execute task-by-task and track the checkboxes.

**Goal:** Add full-circle enemy sprite hit ripples with three independent simultaneous waves and blue, white, and blue/red outcomes.

**Architecture:** Damage results and per-target contacts feed explicit recipient adapters into one enemy-local presenter. A bounded wave runtime and data profile drive a shared URP 2D lit sprite shader without changing damage or card transaction semantics.

**Tech Stack:** Unity 6000.3.16f1, URP 17.3.0/Renderer2D, C#, ShaderLab/HLSL, ScriptableObjects, Unity Test Framework.

**Spec:** `specs/enemy-hit-ripple-sdd-20261006-0840.md` — written specification approved on 2026-10-06.

## Contexto

Follows `specs/enemy-hit-ripple-assessment-20261006-0832.md` and the approved SDD.
Plans live in timestamped `specs/` files under the repository's versioning rules.
Package inspection confirms that golem and bat body material GUID
`a97c105638bdf8b4a8650670310a4cd3` resolves to URP Sprite-Lit-Default.
Preserve that lit baseline. No runtime implementation has started.

## Global Constraints

- Full circular expansion; clip to existing sprite alpha; no directional clipping.
- Three wave slots; fourth replaces oldest; independent progress and expiry.
- Default radial width 4 source pixels; target traversal duration 0.25 seconds.
- Blue for accepted positive damage; white for classified gameplay rejection on a living enemy.
- Fatal requires Accepted && AppliedAmount > 0 && Killed; blue leading/red trailing band.
- Unscaled progression during hitstop/Card Time; freeze during playtest pause.
- One wave per eligible resolved transaction, including linked supplemental transactions.
- Strongest highlight wins; newest breaks ties; no additive whitening.
- Preserve one multi-target request/report and existing modifier/proc/consumption semantics.
- Preserve alpha, tint, animation, sorting and 2D lighting outside the highlight.
- No new dependencies, SFX, particles, bloom requirement, or corpse lifetime subsystem.
- Same-named files for concrete Unity script assets; preserve GUIDs and unrelated changes.
- Follow code/testing conventions and the Unity Editor collaboration workflow.

## Review Focus

1. A valid contact at world (0,0) must not be mistaken for missing contact (Task 2).
2. Child regions and health events must not create duplicate waves (Task 4).
3. Flips, negative scale, and differently pivoted animation frames must not move established origins (Task 5).
4. Reset/disable and pause must not leave stale renderer parameters (Task 4).
5. Fatal reports and supplemental transactions must not alter card consumption or hitstop (Tasks 1/2/6).

## Verification procedure

Run named fixtures in Unity Test Runner/EditMode for each red/green cycle.
When the project is closed, the existing unattended procedure is:

```powershell
& 'C:/Program Files/Unity/Hub/Editor/6000.3.16f1/Editor/Unity.exe' -batchmode -nographics -projectPath 'G:/UnityProjects/My project' -runTests -testPlatform EditMode -testFilter 'TicGame.Architecture.Tests.<fixture>' -testResults 'Temp/HitRippleResults.xml' -logFile 'Temp/HitRippleTests.log'
```

Substitute the fixture below. Require nonzero executed tests and zero failures in
the XML for green runs; red failures must identify missing behavior. Do not launch
a competing Unity process against an open project. Use an isolated validation copy
or a small Editor handoff. Shader rendering checks require graphics; nographics
and dotnet compilation cannot establish visual correctness.

## File structure

New runtime files belong in `Assets/Scrips/Architecture/Feedback/`:
`HitRippleKind.cs`, `HitRippleOutcome.cs`, `HitRippleWave.cs`,
`HitRippleRuntime.cs`, `HitRippleProfileSO.cs`, `EnemyHitRipplePresenter.cs`,
`EnemyHitRippleDamageListener.cs`. Shader: `Assets/Shaders/EnemyHitRipple.shader`.
Editor: `Assets/Scrips/Architecture/Editor/EnemyHitRippleSetup.cs`.
Tests: `Assets/Tests/EditMode/Architecture/Feedback/` plus existing damage/enemy fixtures.
Authored data: `Assets/Data/Feedback/HitRippleProfile.asset` and
`Assets/Data/Feedback/EnemyHitRipple.mat`. Preserve Unity-generated .meta files.

## Task 1: Typed rejection and outcome classification

**Files:** Create `Assets/Scrips/Architecture/Damage/DamageRejectionReason.cs` and Feedback/`HitRippleKind.cs`, `HitRippleOutcome.cs`; modify `Core/DamageTypes.cs`, `Enemy/EnemyHealth.cs`, `Enemy/GolemChargerDamagePolicy.cs`, `Enemy/BatMachineDamagePolicy.cs`, `Enemy/Gargoyle/GargoyleDamagePolicy.cs` and region fallback returns. All shortened runtime paths are under `Assets/Scrips/Architecture/`.

**Interfaces:** Append optional `DamageRejectionReason rejectionReason = DamageRejectionReason.Unspecified` to DamageResult's constructor; expose `RejectionReason`. Enum values: Unspecified, GameplayBlocked, AlreadyDefeated, InvalidTarget, NonPositiveDamage, DuplicateExecution. `HitRippleKind`: None, Damage, Rejected, Fatal. `HitRippleOutcome.Classify(in DamageResult result)` returns HitRippleKind.

- [ ] Add `HitRippleOutcomeTests`: accepted amount 1/killed false => Damage; accepted amount 1/killed true => Fatal; rejected/killed true => None; rejected GameplayBlocked with remaining health 1 => Rejected; remaining health 0 => None; invalid/duplicate/nonpositive/unspecified => None. Existing constructor still compiles.
- [ ] Run that fixture and observe expected failures.
- [ ] Implement classification and annotate policy rejection returns. Golem charge rejection for missing interrupt tags is GameplayBlocked; distinguish dead/invalid/zero/duplicate paths. Do not change damage amounts or accepted behavior.
- [ ] Run `HitRippleOutcomeTests`, `GolemChargerTests`, `BatMachineDamagePolicyTests`, `GargoyleDamagePolicyTests`, `EnemyBaselineTests`; require green.
- [ ] Record completion and commit only this task's files if commits are part of the selected execution workflow.

## Task 2: Correct per-target contact without splitting damage requests

**Files:** Modify `Damage/DamageRequest.cs`, `Damage/DamageResolver.cs`, `Player/Runtime/PlayerAttackHitDetector2D.cs`; extend `Assets/Tests/EditMode/Architecture/Damage/DamageResolverTests.cs` and create Feedback/`HitRippleContactTests.cs`.

**Interfaces:** Add optional `IReadOnlyDictionary<GameObject, Vector2> targetHitPoints = null` to DamageRequest's constructor and a `TargetHitPoints` field. `Vector2 GetHitPoint(GameObject target)` returns mapped contact or existing HitPoint. Resolver uses it only for target DamageContext construction; request-level prioritization retains its existing HitPoint semantics.

- [ ] Test two recipients mapped to (0,0) and (5,2), fallback for an unmapped recipient, old constructor compatibility, one completed source report, and unchanged target ordering/amounts. Test a melee overlap with two initialized enemies yields distinct contacts in one report.
- [ ] Run `DamageResolverTests` and `HitRippleContactTests`; confirm expected failures.
- [ ] Populate contacts from each selected candidate collider's ClosestPoint(center); retain one request and first-contact request fallback. Forward per-target contacts through resolution; supplemental damage retains its existing target context contact.
- [ ] Run both fixtures plus `PlayerCombatEffectsTests`, `DamageProvenanceTests`, `DamageIdentityAndCeilingTests`; verify multi-target consumption/hitstop expectations remain green.
- [ ] Record completion; optionally commit this task's changes.

## Task 3: Bounded waves and profile data

**Files:** Create Feedback/`HitRippleWave.cs`, `HitRippleRuntime.cs`, `HitRippleProfileSO.cs`; create test fixture `HitRippleRuntimeTests.cs`.

**Interfaces:** `HitRippleWave` exposes Origin (Vector2 owner-local), Kind, Radius, TravelDistance, WidthPixels, StartSequence. `HitRippleRuntime`: `int ActiveCount`, `HitRippleWave GetWave(int slot)`, `void Add(Vector2 origin, HitRippleKind kind, float travelDistance, float duration, float widthPixels)`, `void Tick(float unscaledDeltaTime, bool paused)`, `void Clear()`. Radius/distance use owner-local units; presenter converts pixel width per renderer. Profile exposes WidthPixels=4, TraversalSeconds=.25, BlueColor, RejectedColor, FatalLeadingColor, FatalTrailingColor, Strength, FadeOutFraction.

- [ ] Test three waves starting at different times/origins retain independent radii; fourth replaces oldest; tick .125 gives half traversal and .25 expires; pause does not advance; Clear empties all slots. None, negative/nonfinite distance/duration/width, and negative/nonfinite delta must not poison state. Use positive distance encompassing full band travel, including width padding.
- [ ] Run `HitRippleRuntimeTests`; confirm expected failures.
- [ ] Implement fixed capacity and monotonic sequence tracking without per-frame allocations; sanitize invalid input and retain stable slots. Validate profile fields with Inspector attributes and finite checks.
- [ ] Run the fixture; require green.
- [ ] Record completion; optionally commit this task's changes.

## Task 4: Owner presenter and explicit recipient adapters

**Files:** Create Feedback/`EnemyHitRipplePresenter.cs`, `EnemyHitRippleDamageListener.cs`; modify `Enemy/EnemyActor.cs` to expose an explicit reset notification; create `HitRippleRoutingTests.cs` and `HitRipplePresenterTests.cs`.

**Interfaces:** Presenter: `Configure(EnemyActor owner, HitRippleProfileSO profile, SpriteRenderer[] visuals)`, `Present(in DamageContext context, in DamageResult result)`, `Tick(float unscaledDeltaTime, bool paused)`, `Clear()`, `ActiveCount`. Listener: `Configure(EnemyHitRipplePresenter presenter)`; implement all IDamageListener methods, forwarding only OnDamageReceived. Add `EnemyActor.ResetPerformed` event emitted after explicit reset/initialization, without treating every heal as reset.

- [ ] Test initialized root and child recipients notify exactly once through DamageResolver; accepted/fatal/blocked/suppressed classification; disable and ResetActor clear all waves; ordinary healing does not clear; Defeated does not clear. Missing profile/renderer/presenter fails harmlessly. Pause freezes both runtime and shader parameters.
- [ ] Run both fixtures; confirm expected failures.
- [ ] Implement origin conversion, impact-time furthest visual bounds extent including band padding, runtime ticking, explicit reset subscription and safe teardown. Update uses Time.unscaledDeltaTime and PlaytestPauseController.IsGamePaused. Never subscribe to Damaged for wave creation.
- [ ] Run both fixtures and `EnemyBaselineTests`; require green.
- [ ] Record completion; optionally commit this task's changes.

## Task 5: URP lit shader and renderer coordinate upload

**Files:** Create `Assets/Shaders/EnemyHitRipple.shader`; complete Feedback/`EnemyHitRipplePresenter.cs`; create `HitRippleRendererTests.cs`.

**Interfaces:** Shader properties `_RippleCount`, `_RippleOriginRadius0..2` (origin xy, radius z, owner-space band width w), `_RippleLeadingColor0..2`, `_RippleTrailingColor0..2`, `_RippleStrength0..2`, `_RippleVisualToOwner` (matrix), `_RipplePixelsPerUnit`, `_RipplePixelGridOrigin`. Pack waves sorted oldest to newest; ties select later slots. Inactive slots have zero strength. Presenter uploads reusable properties without clobbering unrelated entries.

- [ ] Add renderer tests for PPU conversion (4 pixels/100 PPU=.04 local units), different pivots, child offsets, explicit flipX/flipY and negative scale, property preservation, independent enemies sharing a material, and zeroed parameters after Clear/disable. Owner-space radial width uses the renderer pixel density transformed into owner space; nonuniform scale must be tested/documented as transformed circular geometry.
- [ ] Run `HitRippleRendererTests`; confirm expected failures.
- [ ] Derive passes from installed Sprite-Lit-Default behavior without modifying PackageCache. Keep sprite instancing/flip, normals, lighting and debug behavior compatible. Evaluate quantized rendered positions in owner space; mask the band behind its leading radius, clamp trailing radius to zero, preserve texture alpha, and blend a bounded highlight after lighting. Apply fatal blue-to-red across the band and final fade using FadeOutFraction. Refresh sprite metadata on frame changes; do not use global _Time to advance waves.
- [ ] Run renderer tests; import shader in Unity and require no shader errors. With a graphics-enabled preview, check full circles across three overlapping waves, transparency, 2D lighting, sorting and SpriteMask compatibility. Visual inspection must verify pixel-grid flips and animation, not just uploaded matrices.
- [ ] Record compilation and visual evidence separately; optionally commit this task's changes.

## Task 6: Idempotent enemy setup and flash coexistence

**Files:** Create Editor/`EnemyHitRippleSetup.cs`; modify `Enemy/EnemyDebugPresentation.cs`, `Enemy/Gargoyle/GargoyleAnimationPresenter.cs`; generate profile/material and modify `Assets/Prefabs/Enemies/GolemCharger.prefab`, `BatMachine.prefab`, `GargoyleSentinel.prefab` through Unity APIs. Inspect the current training dummy composition and wire its existing setup owner, using `Enemy/TrainingDummyReviewBootstrap.cs` if applicable. Create `HitRippleSetupTests.cs`; extend `GargoylePresenterTests.cs`.

**Interfaces:** Editor menu `TicGame/Feedback/Setup Enemy Hit Ripples`; public `EnemyHitRippleSetup.Setup()` for Editor tests. Add `SetHitFlashEnabled(bool enabled)` to existing flash presenters; only hit flash behavior is gated. Configure one presenter on each owner and one relay on each actual recipient, including root fallback recipients; bind only body visual renderers.

- [ ] Test setup twice creates no duplicates, preserves component references/GUIDs and unrelated renderer properties, excludes telegraphs/projectiles, writes profile defaults 4/.25, and wires root/child recipients. Assert disabling old hit flashes retains gargoyle state tint/attack cues and debug health display.
- [ ] Run `HitRippleSetupTests` and `GargoylePresenterTests`; confirm expected failures.
- [ ] Implement setup using PrefabUtility/AssetDatabase and reuse existing assets. Select lit shader for body visuals confirmed in current prefabs. Explicitly configure flash suppression and dummy wiring; do not broadly reserialize scenes or replace telegraph materials.
- [ ] Run fixtures, `GargoylePrefabSetupTests`, `BatMachinePrefabTests`, damage/card regression fixtures from Task 2, and the existing PlayMode `GargoyleEncounterPlayModeTests`; require green. Inspect serialized diffs and retain fatal wave visibility through existing death presentation.
- [ ] Record completion; optionally commit this task's changes.

## Final verification and Editor handoff

- [ ] Compile runtime, Editor, EditMode and PlayMode assemblies and run all changed fixtures; record real results, including any external blockers.
- [ ] Run the setup command, save generated assets/prefabs, inspect diffs for unrelated churn.
- [ ] In the existing test/review scene, validate dummy blue/fatal; golem charge white and weak-point blue; bat/gargoyle animated blue/fatal; three rapid waves on a large enemy; distinct origins on a multi-enemy swing; flips/scales; pause, hitstop and Card Time.
- [ ] If user visual action is required, provide the exact scene/object/menu, save instructions and short observation checklist. Do not claim visual completion before results arrive.
- [ ] Profile a representative enemy population with Frame Debugger/Profiler; record property-block batching cost and investigate material alternatives only if measured performance requires it.
- [ ] Save a timestamped verification note in specs with actual checks and any remaining visual limitations. Review the complete change before claiming completion.

## Plan self-review

Coverage: classification/rejection (1), contacts and combat invariants (2), stacking/profile (3),
routing/lifecycle/time (4), shader/coordinates/pixel width/blending (5), setup/tint preservation (6).
All five review-focus items have owning verification steps. Deferred damage conditions
remain in the assessment. Execution method and plan review remain pending.
