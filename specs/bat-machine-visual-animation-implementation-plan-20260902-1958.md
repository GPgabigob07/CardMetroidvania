# Bat Machine Visual Animation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Give the Bat Machine seven state-specific, motion-readable sprite-sheet loops and its projectile a five-frame spinning visual that faces travel direction.

**Architecture:** `BatMachineVisualController` renders but never owns gameplay, mapping `BatMachineBrain.CurrentState` and body velocity to imported sheet frames. `ProjectileSpinVisual` independently renders the projectile’s five frames from `EnemyProjectile2D.Direction`. The existing idempotent Bat setup command imports/slices assets and wires both components.

**Tech Stack:** Unity 6000.3.16f1, C#, NUnit EditMode, Unity `TextureImporter`, `SpriteRenderer`, built-in image generation.

**Spec:** `specs/bat-machine-visual-animation-sdd-20260902-1958.md`

## Global Constraints

- Use the selected top-right orange/blue flying concept as the Bat reference.
- Seven individual Bat sheets: 448×64, exactly seven 64×64 horizontal frames, transparent, right-facing, point filtered, 64 PPU.
- One projectile sheet: 160×32, exactly five 32×32 horizontal frames, transparent, local +X nose, point filtered, 64 PPU.
- Mirror only with `SpriteRenderer.flipX`; never duplicate left-facing art.
- Visual code must not change combat state, steering, collision, damage, poise, or projectile direction/velocity.
- Use idempotent Editor setup; user performs visual Play Mode confirmation and saves serialized assets.
- Do not commit until the user accepts art direction and the visual/projectile behavior is verified.

---

## File Structure

| File | Responsibility |
| --- | --- |
| `Assets/Art/Enemies/BatMachine/BatMachine_<State>.png` | One authored seven-frame Bat sheet per state. |
| `Assets/Art/Enemies/BatMachine/BatMachineProjectile_Spin.png` | Authored five-frame projectile spin sheet. |
| `Assets/Scrips/Architecture/Enemy/BatMachineVisualController.cs` | Pure Bat state/velocity-to-frame presentation. |
| `Assets/Scrips/Architecture/Enemy/ProjectileSpinVisual.cs` | Pure projectile direction-to-frame presentation. |
| `Assets/Scrips/Architecture/Editor/BatMachinePrefabSetup.cs` | Asset import/slicing and idempotent visual wiring. |
| `Assets/Tests/EditMode/Architecture/Enemy/BatMachineVisualControllerTests.cs` | Runtime controller mapping, speed, and facing tests. |
| `Assets/Tests/EditMode/Architecture/Enemy/ProjectileSpinVisualTests.cs` | Projectile facing and non-mutation tests. |
| `Assets/Tests/EditMode/Architecture/Enemy/BatMachinePrefabTests.cs` | Imported-sheet, slice-count, and prefab-composition tests. |

### Task 1: Define Testable Bat Presentation Mapping

**Files:**
- Create: `Assets/Scrips/Architecture/Enemy/BatMachineVisualController.cs`
- Create: `Assets/Tests/EditMode/Architecture/Enemy/BatMachineVisualControllerTests.cs`

**Consumes:** `BatMachineState`, `BatMachineBrain.CurrentState`, `Rigidbody2D.linearVelocity`, `SpriteRenderer`.

**Produces:** `BatMachineVisualController.Configure(...)`, `Tick(float)`, and an Inspector binding for every `BatMachineState`.

- [ ] **Step 1: Write failing tests**

```csharp
[Test]
public void Tick_EngageStateAndPositiveVelocity_SelectsEngageFramesAndFacesRight()
{
    var rig = CreateRig(BatMachineState.Engage, new Vector2(3f, 0f));
    rig.Controller.Tick(0.2f);
    Assert.AreSame(rig.EngageFrames[1], rig.Renderer.sprite);
    Assert.False(rig.Renderer.flipX);
}

[Test]
public void Tick_NearZeroVelocity_PreservesLastFacingDirection()
{
    var rig = CreateRig(BatMachineState.PatrolRandom, Vector2.left);
    rig.Controller.Tick(0.1f);
    rig.Body.linearVelocity = Vector2.zero;
    rig.Controller.Tick(0.1f);
    Assert.True(rig.Renderer.flipX);
}
```

- [ ] **Step 2: Run the focused fixture and verify RED**

Run: Unity EditMode fixture `BatMachineVisualControllerTests`.

Expected: compilation/test failure because `BatMachineVisualController` is absent.

- [ ] **Step 3: Implement the minimal presentation component**

```csharp
public void Tick(float deltaTime)
{
    var frames = ResolveFrames(brain.CurrentState);
    if (frames == null || frames.Length != 7) return;
    elapsed += Mathf.Max(0f, deltaTime) * ResolvePlaybackRate(brain.CurrentState);
    renderer.sprite = frames[(int)(elapsed * framesPerSecond) % frames.Length];
    UpdateFacing(body.linearVelocity.x);
}
```

Create a serializable binding containing one `BatMachineState` and exactly seven `Sprite` references. Only PatrolRandom and Engage use velocity-adjusted playback; all other states use their serialized base rate. `UpdateFacing` changes only `renderer.flipX` when `abs(x)` exceeds a serialized dead-zone.

- [ ] **Step 4: Run the focused fixture and verify GREEN**

Run: Unity EditMode fixture `BatMachineVisualControllerTests`.

Expected: all mapping, speed, and preserved-facing assertions pass.

- [ ] **Step 5: Review for presentation isolation**

Confirm the component contains no `TryChangeState`, force/velocity assignment, collision, damage, or poise calls.

### Task 2: Define Testable Projectile Spin Presentation

**Files:**
- Create: `Assets/Scrips/Architecture/Enemy/ProjectileSpinVisual.cs`
- Create: `Assets/Tests/EditMode/Architecture/Enemy/ProjectileSpinVisualTests.cs`

**Consumes:** `EnemyProjectile2D.Direction`, `SpriteRenderer`.

**Produces:** `ProjectileSpinVisual.Configure(...)` and `Tick(float)`.

- [ ] **Step 1: Write failing tests**

```csharp
[Test]
public void Tick_NegativeProjectileDirection_FlipsRendererWithoutChangingDirection()
{
    var rig = CreateLaunchedProjectileRig(Vector2.left);
    rig.Visual.Tick(0.2f);
    Assert.True(rig.Renderer.flipX);
    Assert.AreEqual(Vector2.left, rig.Projectile.Direction);
}

[Test]
public void Tick_LoopsAcrossFiveFrames()
{
    var rig = CreateLaunchedProjectileRig(Vector2.right);
    rig.Visual.Tick(1.1f);
    Assert.AreSame(rig.Frames[0], rig.Renderer.sprite);
}
```

- [ ] **Step 2: Run the focused fixture and verify RED**

Run: Unity EditMode fixture `ProjectileSpinVisualTests`.

Expected: compilation/test failure because `ProjectileSpinVisual` is absent.

- [ ] **Step 3: Implement the minimal spin component**

```csharp
public void Tick(float deltaTime)
{
    if (frames == null || frames.Length != 5 || projectile == null) return;
    elapsed += Mathf.Max(0f, deltaTime);
    renderer.sprite = frames[(int)(elapsed * framesPerSecond) % frames.Length];
    renderer.flipX = projectile.Direction.x < 0f;
}
```

The component owns no transform rotation and never assigns `EnemyProjectile2D.Direction` or `Rigidbody2D.linearVelocity`.

- [ ] **Step 4: Run the focused fixture and verify GREEN**

Run: Unity EditMode fixture `ProjectileSpinVisualTests`.

Expected: five-frame looping, both facings, and non-mutation assertions pass.

### Task 3: Author and Normalize Art Sheets

**Files:**
- Create: seven named Bat state sheets in `Assets/Art/Enemies/BatMachine/`
- Create: `Assets/Art/Enemies/BatMachine/BatMachineProjectile_Spin.png`

**Consumes:** selected top-right concept, asset contract from the SDD.

**Produces:** transparent, right-facing raster sheets ready for Unity slicing.

- [ ] **Step 1: Generate sheets one at a time**

Use built-in image generation. Prompt every Bat sheet with: "single horizontal seven-frame 448×64 pixel-art spritesheet, transparent background, top-right orange mechanical bat concept with blue eye/crystal horns/energy tail, right facing, consistent silhouette and palette, no text, no borders." Add the state-specific motion intent from the SDD. Generate the projectile as a horizontal five-frame spin sheet with local +X nose.

- [ ] **Step 2: Normalize source dimensions without changing art ownership**

Crop transparent borders and nearest-neighbor resize each Bat sheet to exactly 448×64 and the projectile sheet to exactly 160×32. Preserve alpha; place each frame on its stated cell with no bleed between cells.

- [ ] **Step 3: Inspect dimensions and alpha**

Run read-only image inspection. Expected: seven Bat images exactly 448×64; projectile exactly 160×32; transparent backgrounds; no text/watermark; each sheet’s subject is readable at its target frame size.

- [ ] **Step 4: Record the selected outputs**

Keep only the selected project-bound PNGs under `Assets/Art/Enemies/BatMachine/`; retain the existing `BatMachineProjectile-v2.png` until prefab setup switches to the spin sheet and user confirms the replacement is correct.

### Task 4: Wire Deterministic Import and Prefab Composition

**Files:**
- Modify: `Assets/Scrips/Architecture/Editor/BatMachinePrefabSetup.cs`
- Modify: `Assets/Tests/EditMode/Architecture/Enemy/BatMachinePrefabTests.cs`

**Consumes:** Tasks 1–3 components and PNG paths.

**Produces:** idempotent setup that slices/assigns all visual assets to the Bat prefab and projectile template.

- [ ] **Step 1: Write failing prefab/import tests**

```csharp
[Test]
public void BatMachinePrefab_VisualControllerHasSevenFramesForEveryState()
{
    var controller = LoadPrefab().transform.Find("VisualRoot")
        .GetComponent<BatMachineVisualController>();
    Assert.NotNull(controller);
    foreach (var state in Enum.GetValues(typeof(BatMachineState)).Cast<BatMachineState>())
        Assert.AreEqual(7, controller.GetFrames(state).Count);
}

[Test]
public void ProjectileSpinSheet_ImportsExactlyFiveSprites()
{
    Assert.AreEqual(5, AssetDatabase.LoadAllAssetsAtPath(ProjectileSpinPath)
        .OfType<Sprite>().Count());
}
```

- [ ] **Step 2: Run focused prefab fixture and verify RED**

Run: Unity EditMode fixture `BatMachinePrefabTests`.

Expected: failures because visual assets/controllers are not yet assigned.

- [ ] **Step 3: Extend the setup tool minimally**

Add a `ConfigureSpriteSheet(path, frameWidth, frameHeight, pixelsPerUnit)` helper that imports a Sprite Multiple sheet, Point filtering, no mipmaps, uncompressed texture, and exactly the prescribed horizontal rects. Load the generated `Sprite` assets in frame order. Attach/configure `BatMachineVisualController` on `VisualRoot`; attach/configure `ProjectileSpinVisual` on `ProjectileTemplate`; replace the template renderer’s static v2 sprite with the spin sheet’s first frame.

- [ ] **Step 4: Run setup in Unity and save**

User action: run `TIC > Setup > Create Or Update Bat Machine`; wait for imports; save `BatMachine.prefab` and project. Do not regenerate the test scene unless its instance needs refresh.

- [ ] **Step 5: Verify GREEN**

Run: focused `BatMachinePrefabTests`, `BatMachineVisualControllerTests`, and `ProjectileSpinVisualTests`, then all EditMode tests when Unity is not project-locked.

Expected: every fixture passes, all referenced sprites are in their expected asset paths, and `git diff --check` is clean.

### Task 5: Visual Play Mode Acceptance and Final Review

**Files:**
- Verify: `Assets/Scenes/Test_BatMachine.unity`
- Verify: `Assets/Prefabs/Enemies/BatMachine.prefab`

- [ ] **Step 1: User visual validation**

Open `Assets/Scenes/Test_BatMachine.unity` and enter Play Mode. Confirm:

1. Patrol bobs and broad-flaps; Engage visibly pitches and speeds up.
2. Evade visibly commits sideways, and Windup concentrates blue energy before launch.
3. Stun fall, landing recovery, and dead frame sequence communicate their gameplay state.
4. Projectile spins in flight, points right while traveling right, and flips after deflection without steering/homing.

- [ ] **Step 2: Inspect saved serialized changes**

Check only expected PNG/meta, prefab, editor setup, tests, and visual scripts changed. Ensure no unrelated scene/prefab churn.

- [ ] **Step 3: Run completion verification**

Use `superpowers:verification-before-completion`; run all relevant EditMode tests and `git diff --check`. Do not state completion without captured passing output.

- [ ] **Step 4: Request review and commit only with user authorization**

Use `superpowers:requesting-code-review`. Present the reviewed result to the user; commit only after they explicitly lift the current no-commit constraint.
