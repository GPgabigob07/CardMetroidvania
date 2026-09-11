# Bat Machine Enemy Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `superpowers:subagent-driven-development` (recommended) or `superpowers:executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build the half-bat, half-machine ranged aerial enemy, including reusable card-authored poise and reflected converted projectiles.

**Architecture:** Keep committed behavior in `BatMachineBrain` states and put monitoring, dodge probability, shot prediction, steering, poise, and projectile behavior in focused collaborators. Extend the shared damage transaction with poise and converted provenance, while retaining `EnemyActor` and `EnemyHealth` as composition roots.

**Tech Stack:** Unity 6000.3.16f1, C#, Unity 2D physics, NUnit EditMode tests, ScriptableObject card and damage data.

**Spec:** `specs/bat-machine-enemy-sdd-20260901-1202.md`

## Global Constraints

- Preserve `EnemyActor` as identity/lifecycle and `EnemyHealth` as shared health; do not create an `EnemyBase` superclass.
- Each concrete `MonoBehaviour` and `ScriptableObject` is a top-level class in a same-named `.cs` file.
- Use explicit `Initialize`, `Tick`, and `FixedTick` methods for brain/capability tests; Unity lifecycle only forwards to them.
- All Rigidbody2D writes are owned by focused movement or projectile components during fixed-rate updates.
- Poise defaults to zero for non-damage cards and normal attacks; only damage-capable card operations may supply a positive value.
- Deflected projectiles retain health damage, gain exactly 10 poise damage, become player-owned `Converted` damage, and never home.
- Use idempotent Unity Editor tooling for prefab/scene composition; request Play Mode verification for visual and physics judgement.

---

## File Structure

| Path | Responsibility |
| --- | --- |
| `Assets/Scrips/Architecture/Core/DamageTypes.cs` | Carry resolved poise damage with every damage context. |
| `Assets/Scrips/Architecture/Damage/DamageInstance.cs` / `DamageResolver.cs` | Propagate authored poise into per-target contexts. |
| `Assets/Scrips/Architecture/Damage/DamageOriginKind.cs` / `DamageProvenance.cs` | Represent converted projectile provenance. |
| `Assets/Scrips/Architecture/Player/Cards/CardOperationDefinition.cs` | Store optional poise data only on damage-capable card operations. |
| `Assets/Scrips/Architecture/Player/Runtime/PlayerCombatEffects.cs` | Arm and emit card-sourced poise on primary and supplemental damage. |
| `Assets/Scrips/Architecture/Enemy/EnemyPoise.cs` | Reusable regenerating poise capability. |
| `Assets/Scrips/Architecture/Enemy/AerialSteeringMotor2D.cs` | Smooth aerial patrol/follow/evade velocity control and gravity handoff. |
| `Assets/Scrips/Architecture/Enemy/BatThreatMonitor.cs` | Player monitoring and base-reach/closing threat facts. |
| `Assets/Scrips/Architecture/Enemy/BatDodgeEvaluator.cs` | Cooldown-gated, poise-scaled dodge decision. |
| `Assets/Scrips/Architecture/Enemy/BatShotPredictor.cs` | Windup motion sampling and bounded aim prediction. |
| `Assets/Scrips/Architecture/Enemy/EnemyProjectile2D.cs` | Projectile movement, one-hit resolution, and conversion. |
| `Assets/Scrips/Architecture/Enemy/BatProjectileLauncher.cs` | Windup-complete projectile and burst spawn coordination. |
| `Assets/Scrips/Architecture/Enemy/BatMachineState.cs` / `BatMachineBrain.cs` | Bat state ownership, transitions, patrol, fire, stun fall, and recovery. |
| `Assets/Scrips/Architecture/Enemy/BatMachineDamagePolicy.cs` | Health/poise forwarding and bat-specific stun entry. |
| `Assets/Scrips/Architecture/Editor/BatMachinePrefabSetup.cs` | Idempotently create data, prefab, and test-arena wiring. |
| `Assets/Tests/EditMode/Architecture/Damage/*` | Damage-context and provenance regression tests. |
| `Assets/Tests/EditMode/Architecture/Enemy/*` | Poise, decisions, projectile, steering, and bat-brain behavior tests. |
| `Assets/Tests/EditMode/Architecture/Player/*` | Card poise propagation tests. |

## Task 1: Carry Card-Authored Poise Through Damage Resolution

**Files:**
- Modify: `Assets/Scrips/Architecture/Core/DamageTypes.cs`
- Modify: `Assets/Scrips/Architecture/Damage/DamageInstance.cs`
- Modify: `Assets/Scrips/Architecture/Damage/DamageResolver.cs`
- Modify: `Assets/Scrips/Architecture/Player/Cards/CardOperationDefinition.cs`
- Modify: `Assets/Scrips/Architecture/Player/Runtime/PlayerCombatEffects.cs`
- Modify: `Assets/Tests/EditMode/Architecture/Damage/DamageResolverTests.cs`
- Modify: `Assets/Tests/EditMode/Architecture/Player/PlayerCombatEffectsTests.cs`

**Consumes:** Existing `DamageInstance`, `DamageContext`, `DamageResolver`, `CardOperationDefinition`, and `PlayerCombatEffects` contracts.

**Produces:** `DamageInstance.PoiseDamage`, `DamageContext.PoiseDamage`, and card runtime methods that arm positive poise only for a damage-capable card effect.

- [ ] **Step 1: Write failing resolver tests for poise propagation**

```csharp
[Test]
public void Resolve_InstanceWithPoiseDamage_ForwardsPoiseToTargetContext()
{
    var report = DamageResolver.Resolve(CreateRequest(poiseDamage: 12f));

    Assert.AreEqual(12f, report.TargetResults[0].Context.PoiseDamage);
}

[Test]
public void Resolve_InstanceWithNegativePoiseDamage_ClampsToZero()
{
    var report = DamageResolver.Resolve(CreateRequest(poiseDamage: -5f));

    Assert.AreEqual(0f, report.TargetResults[0].Context.PoiseDamage);
}
```

- [ ] **Step 2: Run the resolver tests to verify they fail**

Run:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.3.16f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'G:\UnityProjects\My project' -runTests -testPlatform EditMode -testFilter 'TicGame.Architecture.Tests.DamageResolverTests' -testResults 'Temp\damage-poise-red.xml' -logFile 'Temp\damage-poise-red.log'
```

Expected: compilation failure because `PoiseDamage` is absent.

- [ ] **Step 3: Implement minimal transaction propagation**

```csharp
// DamageInstance constructor
PoiseDamage = Mathf.Max(0f, poiseDamage);

// DamageResolver.Resolve
poiseDamage: instance.PoiseDamage

// DamageContext constructor
this.poiseDamage = Mathf.Max(0f, poiseDamage);
```

Add `poiseDamage` to `CardOperationDefinition`; its `IsValid` rule must reject
positive poise on every operation except `ModifyDamage` and
`ArmSupplementalDamage`. Extend `PlayerCombatEffects` so an armed qualifying
effect attaches the authored value to the next matching primary/supplemental
`DamageInstance`, then clears it using the same execution-id ownership as the
existing armed supplemental effect.

- [ ] **Step 4: Write failing player-runtime tests for card ownership**

```csharp
[Test]
public void BuildPrimaryDamageInstance_ArmedDamageCard_EmitsItsPoiseDamage()
{
    effects.ArmSupplementalDamage("attack-1", "overcharge", 2f, card, poiseDamage: 12f);

    var instance = effects.BuildPrimaryDamageInstance("primary", "attack-1", 1f, 1f, 0f, 1);

    Assert.AreEqual(12f, instance.PoiseDamage);
}
```

- [ ] **Step 5: Run focused damage and player tests to verify they pass**

Run the two fixtures from Steps 2 and 4. Expected: zero failed tests.

- [ ] **Step 6: Commit the isolated transaction change**

```powershell
git add Assets/Scrips/Architecture/Core/DamageTypes.cs Assets/Scrips/Architecture/Damage/DamageInstance.cs Assets/Scrips/Architecture/Damage/DamageResolver.cs Assets/Scrips/Architecture/Player/Cards/CardOperationDefinition.cs Assets/Scrips/Architecture/Player/Runtime/PlayerCombatEffects.cs Assets/Tests/EditMode/Architecture/Damage/DamageResolverTests.cs Assets/Tests/EditMode/Architecture/Player/PlayerCombatEffectsTests.cs
git commit -m "feat: propagate card poise through damage"
```

## Task 2: Add Reusable Enemy Poise

**Files:**
- Create: `Assets/Scrips/Architecture/Enemy/EnemyPoise.cs`
- Create: `Assets/Tests/EditMode/Architecture/Enemy/EnemyPoiseTests.cs`

**Consumes:** `Mathf`, explicit component initialization conventions, and `DamageContext.PoiseDamage` from Task 1.

**Produces:** `EnemyPoise` with `Initialize(float maximum, float regenerationPerSecond)`, `Tick(float deltaTime)`, `ApplyPoiseDamage(float amount)`, `Restore(float amount)`, `RestoreToFull()`, `CurrentPoise`, `MaximumPoise`, `NormalizedPoise`, and `Depleted`.

- [ ] **Step 1: Write failing poise tests**

```csharp
[Test]
public void Tick_RegeneratesPoiseWithoutExceedingMaximum()
{
    poise.Initialize(30f, 0.33f);
    poise.ApplyPoiseDamage(12f);
    poise.Tick(10f);

    Assert.AreEqual(21.3f, poise.CurrentPoise, 0.001f);
}

[Test]
public void ApplyPoiseDamage_ReachingZero_RaisesDepletedOnce()
{
    var count = 0;
    poise.Depleted += () => count++;
    poise.Initialize(30f, 0.33f);

    poise.ApplyPoiseDamage(30f);
    poise.ApplyPoiseDamage(1f);

    Assert.AreEqual(1, count);
}
```

- [ ] **Step 2: Run the poise fixture to verify it fails**

Run the Unity EditMode filter `TicGame.Architecture.Tests.EnemyPoiseTests`.
Expected: compilation failure because `EnemyPoise` is absent.

- [ ] **Step 3: Implement the minimal capability**

```csharp
public float ApplyPoiseDamage(float amount)
{
    var applied = Mathf.Min(Mathf.Max(0f, amount), CurrentPoise);
    CurrentPoise -= applied;
    if (CurrentPoise <= 0f && !hasDepleted)
    {
        hasDepleted = true;
        Depleted?.Invoke();
    }
    return applied;
}
```

`Tick` restores only while above zero and clamps at maximum. `Restore` clears
the depletion latch once poise becomes positive. Do not add any enemy-type
conditionals or health logic.

- [ ] **Step 4: Run the poise fixture to verify it passes**

Expected: all `EnemyPoiseTests` pass with no Unity compile errors.

- [ ] **Step 5: Commit reusable poise**

```powershell
git add Assets/Scrips/Architecture/Enemy/EnemyPoise.cs Assets/Tests/EditMode/Architecture/Enemy/EnemyPoiseTests.cs
git commit -m "feat: add reusable enemy poise"
```

## Task 3: Support Converted Projectiles

**Files:**
- Modify: `Assets/Scrips/Architecture/Damage/DamageOriginKind.cs`
- Modify: `Assets/Scrips/Architecture/Damage/DamageProvenance.cs`
- Create: `Assets/Scrips/Architecture/Enemy/EnemyProjectile2D.cs`
- Create: `Assets/Tests/EditMode/Architecture/Damage/DamageProvenanceTests.cs`
- Create: `Assets/Tests/EditMode/Architecture/Enemy/EnemyProjectile2DTests.cs`

**Consumes:** Tasks 1-2 and the shared `DamageResolver` request/report flow.

**Produces:** `DamageOriginKind.Converted`, `DamageProvenance.Converted(...)`, and `EnemyProjectile2D.Deflect(GameObject playerSource)`.

- [ ] **Step 1: Write failing converted-provenance tests**

```csharp
[Test]
public void Converted_RetainsOriginalInstanceAsRootAndMarksConvertedOrigin()
{
    var provenance = DamageProvenance.Converted("projectile-1", "projectile-1", "card.deflect");

    Assert.AreEqual(DamageOriginKind.Converted, provenance.OriginKind);
    Assert.AreEqual("projectile-1", provenance.RootInstanceId);
}
```

- [ ] **Step 2: Write failing projectile-deflection tests**

```csharp
[Test]
public void Deflect_ReversesDirectionAndBuildsPlayerOwnedTenPoiseDamage()
{
    projectile.Launch(Vector2.right, enemySource, 4f);
    projectile.Deflect(playerSource);

    Assert.AreEqual(Vector2.left, projectile.Direction);
    Assert.AreEqual(playerSource, projectile.SourceObject);
    Assert.AreEqual(10f, projectile.PoiseDamage);
    Assert.AreEqual(DamageOriginKind.Converted, projectile.Provenance.OriginKind);
}
```

- [ ] **Step 3: Run both new fixtures to verify they fail**

Expected: missing `Converted` enum member, provenance factory, and projectile
component API.

- [ ] **Step 4: Implement minimal conversion and projectile lifetime**

`EnemyProjectile2D` uses a serialized Rigidbody2D, speed, lifetime, damage
profile, target layer mask, and trigger collider. `FixedTick` sets velocity to
`Direction * speed`; collision resolves one `DamageInstance` against the
contact target. `Deflect` changes the source to the player, multiplies
direction by `-1`, sets poise to `10`, replaces provenance with `Converted`,
and retains the original health formula. Destroy/disable after the first
accepted hit or lifetime expiration. The projectile performs no target search.

- [ ] **Step 5: Run fixtures to verify they pass**

Expected: converted provenance and projectile test fixtures have zero failures.

- [ ] **Step 6: Commit converted projectile support**

```powershell
git add Assets/Scrips/Architecture/Damage/DamageOriginKind.cs Assets/Scrips/Architecture/Damage/DamageProvenance.cs Assets/Scrips/Architecture/Enemy/EnemyProjectile2D.cs Assets/Tests/EditMode/Architecture/Damage/DamageProvenanceTests.cs Assets/Tests/EditMode/Architecture/Enemy/EnemyProjectile2DTests.cs
git commit -m "feat: add converted deflectable projectiles"
```

## Task 4: Build Aerial Decision Collaborators

**Files:**
- Create: `Assets/Scrips/Architecture/Enemy/AerialSteeringMotor2D.cs`
- Create: `Assets/Scrips/Architecture/Enemy/BatThreatMonitor.cs`
- Create: `Assets/Scrips/Architecture/Enemy/BatDodgeEvaluator.cs`
- Create: `Assets/Scrips/Architecture/Enemy/BatShotPredictor.cs`
- Create: `Assets/Tests/EditMode/Architecture/Enemy/AerialSteeringMotor2DTests.cs`
- Create: `Assets/Tests/EditMode/Architecture/Enemy/BatThreatMonitorTests.cs`
- Create: `Assets/Tests/EditMode/Architecture/Enemy/BatDodgeEvaluatorTests.cs`
- Create: `Assets/Tests/EditMode/Architecture/Enemy/BatShotPredictorTests.cs`

**Consumes:** `Rigidbody2D`, `IRandomRollSource`, and player transform/velocity facts.

**Produces:** Testable movement/fact/decision APIs used by Task 5.

- [ ] **Step 1: Write failing tests for steering, threat, dodge, and prediction**

```csharp
[Test]
public void Evaluate_FullPoise_UsesSeventyPercentChance()
{
    evaluator.SetRollSource(new FixedRollSource(0.69f));
    Assert.IsTrue(evaluator.TryEvaluate(threat, currentPoise: 30f, maximumPoise: 30f));
}

[Test]
public void Evaluate_LowPoise_RejectsRollAboveTwentyPercent()
{
    evaluator.SetRollSource(new FixedRollSource(0.21f));
    Assert.IsFalse(evaluator.TryEvaluate(threat, currentPoise: 10f, maximumPoise: 30f));
}

[Test]
public void Predict_SampledRightwardMotion_LeadsAimRightwardWithinConfiguredLimit()
{
    predictor.BeginSample(playerPosition: Vector2.zero);
    predictor.Sample(new Vector2(1f, 0f), 0.1f);

    Assert.Greater(predictor.LockPrediction().x, 1f);
}
```

- [ ] **Step 2: Run the four fixtures to verify they fail**

Expected: missing collaborator types and APIs.

- [ ] **Step 3: Implement the focused collaborators**

`AerialSteeringMotor2D` exposes `MoveTowards(Vector2 target, float maxSpeed,
float acceleration, float fixedDeltaTime)`, `Stop()`, `BeginFall()`, and
`ResumeFlight()`. Flight forces gravity to zero; `BeginFall` restores the
serialized gravity scale and ceases velocity control.

`BatThreatMonitor` accepts a target transform and reports monitor membership,
relative closing speed, and whether distance lies within the authored outer
band of base melee reach. It must not read card-modified reach.

`BatDodgeEvaluator` accepts threat facts, poise values, and cooldown state;
its chance uses `Mathf.Lerp(0.2f, 0.7f, InverseLerp(10f, maximumPoise,
currentPoise))` with explicit clamping. It consumes its cooldown only on a
successful roll.

`BatShotPredictor` samples movement during a fixed windup window, derives
average velocity, clamps lead magnitude, and returns a locked point. It does
not access physics or spawn projectiles.

- [ ] **Step 4: Run the collaborator fixtures to verify they pass**

Expected: zero failures, including no dodge roll during cooldown and no
prediction movement after locking.

- [ ] **Step 5: Commit aerial decision collaborators**

```powershell
git add Assets/Scrips/Architecture/Enemy/AerialSteeringMotor2D.cs Assets/Scrips/Architecture/Enemy/BatThreatMonitor.cs Assets/Scrips/Architecture/Enemy/BatDodgeEvaluator.cs Assets/Scrips/Architecture/Enemy/BatShotPredictor.cs Assets/Tests/EditMode/Architecture/Enemy/AerialSteeringMotor2DTests.cs Assets/Tests/EditMode/Architecture/Enemy/BatThreatMonitorTests.cs Assets/Tests/EditMode/Architecture/Enemy/BatDodgeEvaluatorTests.cs Assets/Tests/EditMode/Architecture/Enemy/BatShotPredictorTests.cs
git commit -m "feat: add bat aerial decision collaborators"
```

## Task 5: Implement Bat State Ownership, Damage Policy, And Fire Modifiers

**Files:**
- Create: `Assets/Scrips/Architecture/Enemy/BatMachineState.cs`
- Create: `Assets/Scrips/Architecture/Enemy/BatMachineBrain.cs`
- Create: `Assets/Scrips/Architecture/Enemy/BatMachineDamagePolicy.cs`
- Create: `Assets/Scrips/Architecture/Enemy/BatProjectileLauncher.cs`
- Create: `Assets/Tests/EditMode/Architecture/Enemy/BatMachineBrainTests.cs`
- Create: `Assets/Tests/EditMode/Architecture/Enemy/BatMachineDamagePolicyTests.cs`
- Create: `Assets/Tests/EditMode/Architecture/Enemy/BatProjectileLauncherTests.cs`

**Consumes:** Tasks 1-4, `EnemyActor`, `EnemyHealth`, `EnemyPoise`, and owned-state machine support.

**Produces:** The complete state flow from the approved SDD and projectile burst/cooldown selection.

- [ ] **Step 1: Write failing state-flow tests**

```csharp
[Test]
public void PoiseDepleted_WhileEngaging_EntersStunnedFallAndEnablesGravity()
{
    rig.EnterEngage();
    rig.Poise.ApplyPoiseDamage(30f);

    Assert.AreEqual(BatMachineState.StunnedFall, rig.Brain.CurrentState);
    Assert.Greater(rig.Body.gravityScale, 0f);
}

[Test]
public void FirePlan_LowHealthAndLowPoise_UsesShortCooldownAndTwoShots()
{
    rig.Health.ApplyDamage(CreateContext(rig.Root, amount: 6f));
    rig.Poise.ApplyPoiseDamage(16f);

    Assert.AreEqual(2, rig.Brain.CurrentFirePlan.ProjectileCount);
    Assert.Less(rig.Brain.CurrentFirePlan.Cooldown, rig.Brain.NormalFireCooldown);
}
```

- [ ] **Step 2: Run the bat brain/policy/launcher fixtures to verify they fail**

Expected: all three types and `BatMachineState` are absent.

- [ ] **Step 3: Implement the brain and policy minimally**

Register private owned states for `PatrolRandom`, `Engage`, `WindupFire`,
`Evade`, `StunnedFall`, `GroundedRecovery`, and `Dead`. Subscribe once to
`EnemyActor` defeat/restoration and `EnemyPoise.Depleted`. The poise event
transitions only active airborne states to `StunnedFall`; `StunnedFall` waits
for a terrain landing callback from the motor/collision relay.

`BatMachineDamagePolicy.ApplyDamage` first forwards accepted health damage to
`EnemyHealth`, then forwards `context.PoiseDamage` to `EnemyPoise`. It rejects
damage only after death and never gives normal unmodified attacks poise.

`BatProjectileLauncher` accepts an immutable fire plan `{ cooldown,
projectileCount, interShotDelay, lockedDirection }`; low health selects the
short cooldown and low poise selects two shots. It spawns at most the planned
count and never recalculates target direction during a burst.

- [ ] **Step 4: Add failing landing and recovery tests**

```csharp
[Test]
public void Landing_NonLethalFall_EntersGroundedRecoveryRatherThanFlyingRecovery()
{
    rig.EnterStunnedFall(descendingSpeed: 5f);
    rig.ReportTerrainLanding();

    Assert.AreEqual(BatMachineState.GroundedRecovery, rig.Brain.CurrentState);
}

[Test]
public void Landing_LethalFall_EntersDead()
{
    rig.EnterStunnedFall(descendingSpeed: 30f);
    rig.ReportTerrainLanding();

    Assert.AreEqual(BatMachineState.Dead, rig.Brain.CurrentState);
}
```

- [ ] **Step 5: Implement landing damage and recovery**

Record descending speed on entering fall. On valid terrain landing, calculate
`max(0, impactSpeed - safeImpactSpeed) * damagePerSpeedUnit`, apply it through
the bat damage policy with zero poise, then select `Dead` or
`GroundedRecovery`. Grounded recovery restores the configured positive poise
amount only after its timer completes; it then resumes Engage if monitored,
otherwise random patrol.

- [ ] **Step 6: Run all bat fixtures to verify they pass**

Expected: zero failures for brain, damage policy, and launcher fixtures.

- [ ] **Step 7: Commit bat runtime behavior**

```powershell
git add Assets/Scrips/Architecture/Enemy/BatMachineState.cs Assets/Scrips/Architecture/Enemy/BatMachineBrain.cs Assets/Scrips/Architecture/Enemy/BatMachineDamagePolicy.cs Assets/Scrips/Architecture/Enemy/BatProjectileLauncher.cs Assets/Tests/EditMode/Architecture/Enemy/BatMachineBrainTests.cs Assets/Tests/EditMode/Architecture/Enemy/BatMachineDamagePolicyTests.cs Assets/Tests/EditMode/Architecture/Enemy/BatProjectileLauncherTests.cs
git commit -m "feat: add bat machine combat brain"
```

## Task 6: Compose Assets, Prefab, And Arena, Then Verify

**Files:**
- Create: `Assets/Scrips/Architecture/Editor/BatMachinePrefabSetup.cs`
- Create: `Assets/Data/Enemies/Enemy_BatMachine.asset`
- Create: `Assets/Data/Damage/Damage_BatProjectile.asset`
- Create: `Assets/Prefabs/Enemies/BatMachine.prefab`
- Create or Modify: `Assets/Scenes/Test_BatMachine.unity`

**Consumes:** All runtime components and the Unity Editor collaboration workflow.

**Produces:** Repeatable authored data, a complete prefab, and a dedicated test arena.

- [ ] **Step 1: Write an Editor-safe setup verification test**

```csharp
[Test]
public void BatMachinePrefab_HasRequiredCombatComposition()
{
    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/BatMachine.prefab");

    Assert.NotNull(prefab.GetComponent<EnemyActor>());
    Assert.NotNull(prefab.GetComponent<EnemyPoise>());
    Assert.NotNull(prefab.GetComponent<BatMachineBrain>());
    Assert.NotNull(prefab.GetComponent<BatProjectileLauncher>());
}
```

- [ ] **Step 2: Run the prefab verification to verify it fails**

Expected: prefab asset is absent.

- [ ] **Step 3: Implement idempotent Unity Editor setup**

Add menu command `TIC/Setup/Create Or Update Bat Machine`. It must create or
reuse the enemy definition, projectile profile, prefab root, Rigidbody2D,
colliders, `EnemyActor`, `EnemyHealth`, `EnemyPoise`, bat components,
`ProjectileSpawn`, and visual placeholder. Author `maximumPoise = 30` and
regeneration `0.33`. Ensure aerial gravity is zero outside `StunnedFall` and
ensure the projectile is on the damage/query layers used by the player and
enemies. A second command, `TIC/Setup/Create Or Update Bat Machine Test Scene`,
creates/reuses a ground platform, player reference, and one bat instance.

- [ ] **Step 4: Run setup in Unity and verify the prefab test passes**

Run the two menu commands once in the Unity Editor, save modified assets, then
run the fixture from Step 1. Expected: zero failures and no duplicate children
or components after a second menu-command run.

- [ ] **Step 5: Run all EditMode tests**

Run:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.3.16f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'G:\UnityProjects\My project' -runTests -testPlatform EditMode -testResults 'Temp\all-editmode-bat.xml' -logFile 'Temp\all-editmode-bat.log'
```

Expected: exit code 0 and no failed tests.

- [ ] **Step 6: Perform the Unity Editor handoff**

Open `Assets/Scenes/Test_BatMachine.unity`, enter Play Mode, and verify:

1. patrol stays inside the authored bounds;
2. monitor activation starts natural follow without overlap jitter;
3. fire windup visibly precedes the imperfect lead shot;
4. low-health fire rate and low-poise bursts stack;
5. dodges happen only in Engage and remain punishable by range cards;
6. card poise causes a physical fall, with a visible grounded recovery or death;
7. defensive card deflection reverses the projectile without homing and lets it
   damage the bat/other valid targets as player converted damage.

- [ ] **Step 7: Commit assets and setup tooling**

```powershell
git add Assets/Scrips/Architecture/Editor/BatMachinePrefabSetup.cs Assets/Data/Enemies/Enemy_BatMachine.asset Assets/Data/Damage/Damage_BatProjectile.asset Assets/Prefabs/Enemies/BatMachine.prefab Assets/Scenes/Test_BatMachine.unity Assets/Tests/EditMode/Architecture/Enemy/BatMachinePrefabTests.cs
git commit -m "feat: add bat machine prefab and test arena"
```

## Final Verification Checklist

- [ ] Read `specs/bat-machine-enemy-sdd-20260901-1202.md` and map every behavior to Tasks 1-6.
- [ ] Confirm all new public methods and classes have EditMode coverage.
- [ ] Run the full EditMode command from Task 6 and inspect its XML result for zero failures.
- [ ] Review `git diff --check` and serialized prefab/scene changes for duplicate components or accidental GUID churn.
- [ ] Complete the Play Mode handoff and inspect the saved serialized assets after the user reports the visual results.
