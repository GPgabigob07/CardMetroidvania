# Neutral And Chain Card Baseline Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Ship three Neutral and two Chain cards with real movement and combat effects, including optional enemy poise and a testable Golem Charger response.

**Architecture:** Keep the world-owned Card Time session and player-owned card execution. Extend the existing typed card-operation vocabulary and player runtimes, pass poise through the shared damage transaction, and author cards through idempotent Unity Editor setup tools. Existing demo and Finisher assets remain intact.

**Tech Stack:** Unity 6000.3.16f1, C#, Unity 2D physics, ScriptableObjects, Unity Editor asset/prefab APIs, PowerShell and `dotnet build` for assembly compilation.

**Spec:** `specs/neutral-chain-card-baseline-sdd-20260921-1107.md`

## Global Constraints

- Preserve `PlayerCardTimeState.Neutral`, `Chain`, and `Finisher`; the new deck contains exactly three Neutral and two Chain cards, with existing Finisher choices.
- Costs are Energy 5, 5, 15, 20, and 40 in the spec's card order.
- Ordinary melee poise damage stays zero; the card authors base 2 poise, tunable from 1 to 3, multiplied by 1.2 for five eligible hits.
- Only accepted positive-health-damage primary melee hits on an `EnemyActor` advance new hit effects. Gates and supplemental hits do not count.
- Jump Boost affects the next actual grounded takeoff only; Extra Jump and coyote jumps keep normal velocity.
- New effects clear on death or full run reset, not on `PlayerController.ResetTransientState`, which area streaming also calls.
- Keep concrete Unity `MonoBehaviour` and `ScriptableObject` class names matched to their `.cs` filenames; preserve asset `.meta` GUIDs.
- Use idempotent Editor APIs for prefab and asset setup. Do not hand-edit complex scene or prefab YAML.
- The author chose to skip Unity unit tests for now. Do not add or run unit tests in this plan; use compilation, asset audits, and the specified Play Mode checks. Record that test coverage remains deferred.
- Leave the unrelated untracked `tic/` tree out of commits.
- After creating a `.cs` file, let Unity import it and generate its `.meta`; confirm the regenerated project file lists it before relying on `dotnet build`. A stale generated `.csproj` can report success without compiling the new file.

Validated assembly build commands from the project root:

```powershell
dotnet build TicGame.Architecture.csproj -v:q --no-restore -p:BaseIntermediateOutputPath=Temp/CodexObj/Runtime/ -p:OutputPath=Temp/CodexBin/Runtime/
dotnet build TicGame.Architecture.Editor.csproj -v:q --no-restore -p:BaseIntermediateOutputPath=Temp/CodexObj/Editor/ -p:OutputPath=Temp/CodexBin/Editor/
```

## File Map And Interfaces

| Owner | Files | Contract |
| --- | --- | --- |
| Hit classification | `PlayerCombatEffects.cs` | `EligiblePrimaryHitsResolved(int count)` and `PrimaryAttackMissed`; filters targets by accepted damage, primary provenance, and `EnemyActor`. |
| Card data and execution | `CardEffectKinds.cs`, `CardOperationDefinition.cs`, `CardEffectDefinitionSO.cs`, `PlayerCardRuntime.cs` | Add operation kinds and an authored integer `ChargeCount`; validate and dispatch by operation kind, never by card id. |
| Movement ability state | new `PlayerDashPermissionRuntime.cs`, new `PlayerGroundedJumpBoostRuntime.cs`, `PlayerContext.cs`, `PlayerController.cs`, `GroundedLocomotionState.cs`, `PlayerDeathRespawn.cs` | `Activate`, `CanActivate`, `Clear`, and read-only state; PlayerController gates DashAction; death clears statuses. |
| Damage payload | `DamageInstance.cs`, `DamageTypes.cs`, `DamageResolver.cs`, new `IPoiseDamageSource.cs` | `PoiseDamage` travels separately from health; optional source override provides per-target card poise before damage resolves. |
| Enemy poise | new `EnemyPoise.cs`, new `EnemyPoiseDebugPresentation.cs`, `GolemChargerDamagePolicy.cs`, `GolemChargerBrain.cs`, `GolemChargerPrefabSetup.cs` | Optional capability receives poise only after accepted health damage; depletion enters existing Interrupted state; debug bar makes it observable. |
| Poise and reach state | `PlayerCombatEffects.cs`, `PlayerAttackHitDetector2D.cs` | Five card-poise charges; reach multiplier 1.0–1.25, forward-only hitbox growth, miss reset. |
| Authoring and feedback | `PrototypeCardAssetSetup.cs`, `CardInventoryProfileSetup.cs`, `GolemChargerPrefabSetup.cs`, new `FiveCardPlayerPrefabSetup.cs`, `Player.prefab`, card assets | Stable card ids, catalog/inventory/loadout, status feedback, and narrow idempotent component/reference wiring. |

The paths above are under `Assets/Scrips/Architecture/` except `Player.prefab` under `Assets/Prefabs/Player/` and data assets under `Assets/Data/Cards/`. The executor must read the approved spec and current code before each task because Unity may reserialize assets between tasks.

## Review Focus

1. A card-enhanced hit opens a blue gate but never extends dash, consumes a poise charge, or grows reach; verify in Tasks 1, 5, and 6.
2. The fifth poise charge spent on the first target of a multi-target swing cannot apply poise to a sixth target; verify in Task 5.
3. A failed card commit due to insufficient Energy or active effect must leave both Energy and state unchanged; verify in Tasks 2, 3, 5, and 6.
4. Dash cannot start when its timer is zero, even if Dash is pressed while a Card Time selection closes; verify in Task 3.
5. Streaming Blue to Pink preserves active card state while death clears it; verify in Task 7.

---

### Task 1: Establish Eligible Enemy-Hit Events

**Files:** Modify `Assets/Scrips/Architecture/Player/Runtime/PlayerCombatEffects.cs`.

**Interfaces:** Produces `event Action<int> EligiblePrimaryHitsResolved` and `event Action PrimaryAttackMissed` for Dash and Reach. Keep the older generic `EffectiveHitCount` for existing demo effects and gate behavior.

- [ ] **Step 1: Add enemy-hit classification and outcome bookkeeping.** In `OnDamageResolved`, count `report.TargetResults` where `report.IsPrimary`, `report.Allows(DamageProcPolicy.ConfirmAttackHit)`, `Result.Accepted`, `Result.AppliedAmount > 0`, and `Context.Target.GetComponentInParent<EnemyActor>() != null`. Add that count to the attack outcome and publish it once per report. In `CompleteAttack`, publish `PrimaryAttackMissed` when the accumulated eligible count is zero, independently of the old generic chain reset.

```csharp
private static bool IsEligibleEnemyHit(in DamageTargetResult targetResult) =>
    targetResult.Result.Accepted
    && targetResult.Result.AppliedAmount > 0f
    && targetResult.Context.Target != null
    && targetResult.Context.Target.GetComponentInParent<EnemyActor>() != null;

public event Action<int> EligiblePrimaryHitsResolved;
public event Action PrimaryAttackMissed;
```

- [ ] **Step 2: Compile the runtime assembly.** From the project root run the command below. Expected: zero errors. This uses `Temp` for intermediate output because the generated root `obj` directory is not writable in this workspace.

```powershell
dotnet build TicGame.Architecture.csproj -v:q --no-restore -p:BaseIntermediateOutputPath=Temp/CodexObj/Runtime/ -p:OutputPath=Temp/CodexBin/Runtime/
```

- [ ] **Step 3: Review the call sites.** Confirm the event is raised after the current damage report has resolved, gates have no `EnemyActor`, and multiple accepted enemy targets contribute their count. Check `git diff --check`, then commit only this task's source and `.meta` changes.

### Task 2: Grounded Extra Jump And One-Use Jump Boost

**Files:** Create `Assets/Scrips/Architecture/Player/Runtime/PlayerGroundedJumpBoostRuntime.cs` and matching `.meta`; modify `PlayerContext.cs`, `GroundedLocomotionState.cs`, `PlayerController.cs`, `PlayerCardRuntime.cs`, `CardEffectKinds.cs`, `CardOperationDefinition.cs`, and `CardEffectDefinitionSO.cs`.

**Interfaces:** `PlayerGroundedJumpBoostRuntime.CanArm`, `Arm(float multiplier, CardDefinitionSO card)`, `ConsumeGroundedLaunch(float baseVelocity)`, `Clear()`, `IsArmed`. Existing `PlayerExtraJumpRuntime.Invoke` grants Grounded Double Jump. `PlayerCardRuntime.ClearNewCardEffects()` will be completed as other cards arrive.

- [ ] **Step 1: Add the small jump-boost runtime and grounded consumption.** Reject `Arm` while already armed. Return `baseVelocity * multiplier` and clear the charge only when `GroundedLocomotionState` starts its jump. Pass the runtime through `PlayerContext`; do not alter coyote or `AirborneLocomotionState` extra-jump paths.

```csharp
if (context.Locomotion.HasBufferedJump)
{
    velocity.y = context.GroundedJumpBoostRuntime != null
        ? context.GroundedJumpBoostRuntime.ConsumeGroundedLaunch(config.JumpVelocity)
        : config.JumpVelocity;
    context.Locomotion.ConsumeJumpBuffer();
    context.Locomotion.ForceState(context, PlayerLocomotionState.Airborne);
}
```

- [ ] **Step 2: Extend operation validation and execution.** Add `CardOperationKind.ArmGroundedJumpBoost` with positive multiplier. Extend `PlayerCardRuntime.CanExecuteOperations`, `HasSupportedEffectShape`, and `ApplyOperations` to dispatch this reusable operation. Grounded Double Jump uses the existing `InvokeAbility` operation plus `IsGrounded` activation condition; reject it when the one extra-jump charge is already present. Recheck live operation availability in `TryApplyPreparedCommit` immediately before `wallet.TrySpend`, so an effect activated after preparation cannot spend Energy and silently fail.

```csharp
case CardOperationKind.ArmGroundedJumpBoost:
    if (!groundedJumpBoost.CanArm) return false;
    break;
// Commit dispatch: groundedJumpBoost.Arm(operation.Multiplier, card);
```

- [ ] **Step 3: Compile runtime and inspect two manual cases.** Import the new script in Unity, confirm its inclusion in the regenerated runtime project file, and run the runtime build command in Global Constraints. In Unity Play Mode after Task 7 authors the assets, check that airborne activation of Grounded Double Jump spends no Energy, that a grounded activation grants exactly one midair jump, and that Jump Boost changes one grounded jump but not an extra jump. Record the observation for final verification.

- [ ] **Step 4: Review and commit the movement slice.** Run `git diff --check`; commit the files above without `tic/`.

### Task 3: Time-Limited Dash Permission

**Files:** Create `Assets/Scrips/Architecture/Player/Runtime/PlayerDashPermissionRuntime.cs` and `.meta`; modify `PlayerController.cs`, `PlayerCardRuntime.cs`, `CardEffectKinds.cs`, `CardOperationDefinition.cs`, and `CardEffectDefinitionSO.cs`.

**Interfaces:** `CanActivate`, `IsEnabled`, `RemainingSeconds`, `Activate(float durationSeconds, float extensionPerHit, CardDefinitionSO card)`, `Tick(float deltaTime)`, `Clear()`. Consume Task 1's `EligiblePrimaryHitsResolved(int)` event. The existing `DashAction` is unchanged.

- [ ] **Step 1: Implement the timed permission.** Subscribe once to eligible-hit events, unsubscribe on disable, clamp remaining time at zero, and add `count * 0.3f` when active. Tick with gameplay `Time.deltaTime`; publish remaining-time HUD feedback on activation/extension/expiry.

```csharp
public void NotifyEligibleHits(int count)
{
    if (IsEnabled && count > 0)
        RemainingSeconds += count * extensionPerHit;
}

public void Tick(float deltaTime) =>
    RemainingSeconds = Mathf.Max(0f, RemainingSeconds - Mathf.Max(0f, deltaTime));
```

- [ ] **Step 2: Gate controller input and add card dispatch.** Tick permission at the start of `PlayerController.Update` with `Time.deltaTime`, then guard the current `new DashAction()` path with `dashPermission.IsEnabled`. Add `CardOperationKind.GrantTimedDash` using `Amount = 5f` and `Multiplier = 0.3f`; reject while active before Energy payment. Keep Card Time selection cancellation and dash-start order explicit so a closing selection cannot bypass the guard.

```csharp
if (input.DashPressed && dashPermission != null && dashPermission.IsEnabled)
{
    if (ActionRunner.TryStartAction(Context, new DashAction(), replaceCurrent: false))
        attackCombo.Clear();
}
```

- [ ] **Step 3: Compile and review boundary behavior.** Import the new script in Unity, confirm its inclusion in the regenerated runtime project file, and run the runtime build command in Global Constraints. In Play Mode after Task 7, confirm no dash before commit, dash for five gameplay seconds, +0.3 seconds per eligible enemy hit, no extension from a gate, and no dash at zero even on the frame selection closes. Confirm active re-use fails without Energy loss.

- [ ] **Step 4: Review and commit.** Run `git diff --check`; commit this slice.

### Task 4: Shared Poise Payload And Golem Capability

**Files:** Create `Assets/Scrips/Architecture/Damage/IPoiseDamageSource.cs`, `Assets/Scrips/Architecture/Enemy/EnemyPoise.cs`, and `Assets/Scrips/Architecture/Enemy/EnemyPoiseDebugPresentation.cs` with `.meta`; modify `DamageInstance.cs`, `DamageTypes.cs`, `DamageResolver.cs`, `GolemChargerDamagePolicy.cs`, `GolemChargerBrain.cs`, and `Assets/Scrips/Architecture/Editor/GolemChargerPrefabSetup.cs`.

**Interfaces:** `DamageInstance.PoiseDamage`, `DamageContext.PoiseDamage`; `IPoiseDamageSource.GetPoiseDamage(in DamageInstance instance, GameObject target)`; `EnemyPoise.Initialize(float maximum, float regenerationPerSecond)`, `Tick(float deltaTime)`, `ApplyPoiseDamage(float amount)`, `RestoreToFull()`, and `event Action Depleted`.

- [ ] **Step 1: Carry non-negative poise through damage resolution.** Add optional `poiseDamage = 0f` to existing constructors to preserve callers. In `DamageResolver`, look for an optional source `IPoiseDamageSource` and use its per-target value, or `DamageInstance.PoiseDamage` otherwise. Clamp before constructing each target's `DamageContext`. The source override must be queried anew for each target.

```csharp
var poiseSource = FindFirst<IPoiseDamageSource>(instance.SourceObject);
var poiseDamage = Mathf.Max(0f,
    poiseSource != null
        ? poiseSource.GetPoiseDamage(instance, target)
        : instance.PoiseDamage);
// Pass poiseDamage to DamageContext for this target.
```

- [ ] **Step 2: Add reusable enemy storage.** `EnemyPoise` clamps maximum/current values, regenerates only while above zero and below maximum, raises depletion once on the transition to zero, and resets its latch on explicit restoration. It does not select AI states. On Golem, configure initial maximum 10 and regeneration 0.33 per gameplay second; retain fields for later tuning.

```csharp
public float ApplyPoiseDamage(float amount)
{
    var applied = Mathf.Min(CurrentPoise, Mathf.Max(0f, amount));
    CurrentPoise -= applied;
    if (CurrentPoise <= 0f && !depletionRaised)
    {
        depletionRaised = true;
        Depleted?.Invoke();
    }
    return applied;
}
```

- [ ] **Step 3: Forward accepted Golem hits and connect its state.** Preserve both `context.PoiseDamage` and `context.IsCardEnhancedMelee` when the damage policy creates its adjusted context. Apply poise only after `health.ApplyDamage` reports an accepted positive health hit. Subscribe the brain to `EnemyPoise.Depleted`; call `poise.Tick(deltaTime)` during living non-Interrupted state updates, enter `Interrupted` on depletion, and leave the current Card/Impact armor rejection unchanged. Restore poise when entering Recovery or when the actor is reset. Add `EnemyPoiseDebugPresentation` to draw a small poise bar using `NormalizedPoise`; update the idempotent Golem prefab setup to add/configure both components and references. Do not edit prefab YAML directly.

```csharp
var result = health.ApplyDamage(adjustedContext);
if (result.Accepted && result.AppliedAmount > 0f)
    poise?.ApplyPoiseDamage(adjustedContext.PoiseDamage);
return result;
```

- [ ] **Step 4: Compile both assemblies and inspect the prefab.** Import the new scripts in Unity, confirm the regenerated runtime and Editor project files include them, then run both build commands in Global Constraints. In Unity, run `TIC > Setup > Create Or Update Golem Charger`, save the prefab, then inspect its new component and serialized references. Check that an existing tag-based interrupt still works; card-poise behavior is exercised in Task 5.

- [ ] **Step 5: Review and commit.** Run `git diff --check`, inspect prefab YAML churn and GUIDs, and commit this capability slice.

### Task 5: Five-Hit Poise Card

**Files:** Modify `PlayerCombatEffects.cs`, `PlayerCardRuntime.cs`, `CardEffectKinds.cs`, `CardOperationDefinition.cs`, and `CardEffectDefinitionSO.cs`.

**Interfaces:** `PlayerCombatEffects` implements `IPoiseDamageSource`; add `CanArmPoiseHits`, `ArmPoiseHits(int hits, float basePoise, float multiplier, CardDefinitionSO card)`, `RemainingPoiseHits`, and clear/feedback behavior. Author operation with `Amount = 2f`, `Multiplier = 1.2f`, `ChargeCount = 5`.

- [ ] **Step 1: Add per-target card poise.** The source returns zero for non-primary damage, non-enemy targets, or zero remaining charges; otherwise it returns `basePoise * multiplier`. In existing `OnDamageDealt`, consume one charge only when `context.PoiseDamage > 0`, the health result is accepted with positive amount, and the target is an `EnemyActor`. This callback occurs before the resolver queries the next target, enforcing the five-hit cap within one swing.

```csharp
public float GetPoiseDamage(in DamageInstance instance, GameObject target) =>
    remainingPoiseHits > 0
    && instance.Provenance.OriginKind == DamageOriginKind.Primary
    && (instance.ProcPolicy & DamageProcPolicy.ConfirmAttackHit) != 0
    && target != null && target.GetComponentInParent<EnemyActor>() != null
        ? basePoiseDamage * poiseMultiplier
        : 0f;
```

- [ ] **Step 2: Add generic operation dispatch and feedback.** Add `CardOperationKind.ArmPoiseHits` plus integer `ChargeCount` validation to `CardOperationDefinition`. Extend `PlayerCardRuntime` readiness and commit switches by operation kind and reject an already-active poise effect before spending. Mark primary melee as card-enhanced while the poise status applies so the blue gate keeps its existing behavior, but never count the gate as an enemy hit. Show remaining charges in the card HUD.

```csharp
case CardOperationKind.ArmPoiseHits:
    combatEffects.ArmPoiseHits(
        operation.ChargeCount, operation.Amount, operation.Multiplier, card);
    break;
```

- [ ] **Step 3: Compile and manually exercise poise.** Run the runtime build command in Global Constraints. After Task 7 creates the card asset, verify normal melee causes zero poise loss; each of the next five accepted enemy hits causes 2.4 poise loss and consumes one charge; rejected armor hits, gates, and supplemental damage consume none; a timely set can interrupt Golem. At one remaining charge, strike two enemies in one attack and verify only the first receives poise. Re-use while active must fail without spending Energy.

- [ ] **Step 4: Review and commit.** Run `git diff --check`; commit the poise-card slice.

### Task 6: Growing Attack Reach

**Files:** Modify `PlayerCombatEffects.cs`, `PlayerAttackHitDetector2D.cs`, `PlayerCardRuntime.cs`, `CardEffectKinds.cs`, `CardOperationDefinition.cs`, and `CardEffectDefinitionSO.cs`.

**Interfaces:** `PlayerCombatEffects.CanArmGrowingReach`, `ArmGrowingReach(float percentPerHit, int maxIncrements, CardDefinitionSO card)`, `PrimaryReachMultiplier`, `ClearGrowingReach()`. Author `Amount = 0.05f`, `ChargeCount = 5` and cost 40 Energy.

- [ ] **Step 1: Maintain hit-built reach.** On Task 1's eligible-hit event, add at most the remaining increments, capping at five. On `PrimaryAttackMissed`, clear the increment count and active status. Reject a second activation while active. A gate hit is a miss for this card if no enemy was also hit.

```csharp
reachIncrements = Mathf.Min(reachLimit, reachIncrements + eligibleHitCount);
public float PrimaryReachMultiplier => 1f + reachIncrements * reachPercentPerHit;
```

- [ ] **Step 2: Compute a forward-only hitbox.** In `PlayerAttackHitDetector2D`, compute the unmodified rear edge and forward edge from `localOffset.x` and `size.x`. Multiply only forward reach by `PrimaryReachMultiplier`, then derive query center and width; preserve Y center and height. Use the same calculation in `OverlapBoxAll` and `OnDrawGizmosSelected`. Do not mutate the serialized `size` or `localOffset`.

```csharp
var rear = localOffset.x - size.x * 0.5f;
var front = (localOffset.x + size.x * 0.5f) * reachMultiplier;
var width = front - rear;
var centerX = (front + rear) * 0.5f * facing;
```

- [ ] **Step 3: Add the typed operation and compile.** Add `CardOperationKind.ArmGrowingReach`, readiness rejection, and dispatch. Include active Growing Reach in `BuildPrimaryDamageInstance`'s `IsCardEnhancedMelee` calculation without counting gate hits as enemies. Run the runtime build command in Global Constraints. After Task 7 creates its asset, use Play Mode and selected Gizmos to check 0%, 5%, 10%, 15%, 20%, 25%, a sixth hit remaining at 25%, miss reset, gate not growing reach, and active re-use failing without Energy loss.

- [ ] **Step 4: Review and commit.** Run `git diff --check`; commit the reach slice.

### Task 7: Author Assets, Wire Prefabs, And Verify The Five-Card Slice

**Files:** Modify `Assets/Scrips/Architecture/Editor/PrototypeCardAssetSetup.cs` and `CardInventoryProfileSetup.cs`; create `Assets/Scrips/Architecture/Editor/FiveCardPlayerPrefabSetup.cs` and `.meta`; modify `Assets/Scrips/Architecture/Player/Runtime/PlayerDeathRespawn.cs`; update/create `Assets/Data/Cards/{Definitions,Effects,Statuses,Inventory}/*.asset` and `.meta`, `Assets/Prefabs/Player/Player.prefab`, and `Assets/Prefabs/Enemies/GolemCharger.prefab` through Unity Editor serialization.

**Interfaces:** Five stable card ids; `TestCardCatalog` resolves them; `TestCardInventory` owns and equips exactly three Neutral and two Chain new cards while preserving the existing Finisher choices. Player prefab has the new movement runtimes and assigned `PlayerCardRuntime`/`PlayerController` references.

- [ ] **Step 1: Extend idempotent card authoring.** In `PrototypeCardAssetSetup`, create or update five definitions, effects, and required statuses with the spec's categories/costs and operation values. Use stable ids such as `card.neutral.grounded-double-jump`, `card.neutral.dash-enabler`, `card.neutral.jump-boost`, `card.chain.poise-damage`, and `card.chain.growing-reach`. Keep old demo assets untouched.

```csharp
// Data values to author through the existing CreateCard/CreateOrLoad helpers:
// Neutral: ExtraJump + IsGrounded (5); GrantTimedDash(5, 0.3) (5);
//          ArmGroundedJumpBoost(multiplier: 2) (15).
// Chain:   ArmPoiseHits(chargeCount: 5, amount: 2, multiplier: 1.2) (20);
//          ArmGrowingReach(chargeCount: 5, amount: 0.05) (40).
```

- [ ] **Step 2: Curate selection and wire Player.** Update the catalog to include the new ids. Change `CardInventoryProfileSetup` so rerunning it owns but does not auto-equip every demo asset: iterate a snapshot of each loadout's currently equipped cards, call `profile.TryUnequip(card)`, then call `profile.TryEquip(card)` for the three new Neutral, two new Chain, and existing Finisher choices in stable order. Create a narrow idempotent `TIC/Setup/Create Or Update Five Card Player Prefab` command that loads only the existing Player prefab contents, gets or adds dash permission and grounded jump boost, serializes their references into `PlayerController` and `PlayerCardRuntime`, saves the prefab, and unloads contents. Do not rerun the older Player-and-Golem-test-scene command for this change. In `PlayerDeathRespawn.HandleHealthChanged`, call `PlayerCardRuntime.ClearNewCardEffects()` once when health reaches zero before either respawn route; that method clears extra jump, dash, jump boost, poise, and reach. `ResetTransientState` remains action-only and does not call it.

```csharp
// Preserve current Finisher choices; this order is the prototype picker order.
var neutral = new[] { groundedDoubleJump, dashEnabler, jumpBoost };
var chain = new[] { poiseDamage, growingReach };
```

- [ ] **Step 3: Compile both assemblies and run Editor setup.** Import the new Player Editor script in Unity and confirm the regenerated Editor project includes it. Run both build commands in Global Constraints. In Unity, run `TIC > Setup > Update Prototype Card Assets`, `TIC > Setup > Create Or Update Test Card Inventory`, `TIC > Setup > Create Or Update Five Card Player Prefab`, and `TIC > Setup > Create Or Update Golem Charger` in that order. Save the resulting assets and prefabs. Rerun the setup sequence once and confirm it does not add duplicate components or cards. Review every modified `.asset`, `.prefab`, and `.meta`: no duplicate GUIDs, no missing script references, exact costs/categories, and no broad scene reserialization.

- [ ] **Step 4: Perform the Play Mode matrix.** Ask the user for one focused pass in Gameplay/Blue: each card can be selected and pays exactly its cost; Grounded Double Jump only activates on ground; Jump Boost only changes one grounded jump; dash is gated and extends on enemy hits; normal melee has zero poise and the card's five hits can interrupt the Charger; Growing Reach caps at 25% and resets on miss; the blue gate never advances enemy-hit effects; death clears effects; a Blue/Pink area stream preserves active effects. The user reports the first Console error or observed mismatch. Inspect saved serialized assets after the Editor run and fix any discrepancy before claiming completion.

- [ ] **Step 5: Final review and commit.** Run `git diff --check`, both compile commands, and a static GUID/category/cost audit. Record manual Play Mode results and the deferred-unit-test limit in a new timestamped `specs/` verification note. Commit only the implementation and verification assets; leave `tic/` untracked. Request a whole-branch code review before integration.
