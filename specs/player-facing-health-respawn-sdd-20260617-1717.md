# Player Facing, Health Damage, and Prototype Respawn SDD - 20260617-1717

## Contexto

This specification defines the implementation slice for three connected player
prototype needs:

- flip the visible player sprite when horizontal movement direction changes;
- let the player receive damage through the existing damage system;
- update the existing HUD through player health changes;
- reset the player to world position `(0, 0, 0)` when health reaches zero.

Sources used:

- `AGENTS.md`
- `gdd/gdd-canonico-20260526-2331.md`
- `specs/player-movement-controller-sdd-20260604-2107.md`
- `specs/damage-system-sdd-20260526-0102.md`
- `specs/player-hud-sdd-20260615-0012.md`
- `specs/code-conventions-20260526-0014.md`
- `specs/unity-script-asset-file-layout-20260526-0000.md`
- `specs/unity-editor-collaboration-workflow-20260612-1609.md`
- `Assets/Scrips/Architecture/Player/Runtime/PlayerController.cs`
- `Assets/Scrips/Architecture/Player/Runtime/PlayerMotor2D.cs`
- `Assets/Scrips/Architecture/Runtime/SimpleHealth.cs`
- `Assets/Scrips/Architecture/Hud/PlayerHudUI.cs`
- `Assets/Scrips/Architecture/Editor/PlayerSceneCompositionSetup.cs`
- `Assets/Scrips/Architecture/Editor/PlayerHudSetup.cs`

This version exists because player survivability and visual facing are now part
of the playable prototype loop. Older movement, damage, and HUD specs remain
valid and this document narrows the integration behavior for this slice.

## Goals

- Make horizontal input direction change visibly flip the player presentation.
- Preserve `PlayerContext.FacingDirection` as the gameplay-facing source used by
  dash, attacks, hit detection, and animation snapshots.
- Make the player damageable by using the existing `SimpleHealth`
  implementation.
- Keep `PlayerHudUI` observational: HUD updates must come from
  `SimpleHealth.Changed`, not from direct gameplay commands.
- On death, reset the player to world position `(0, 0, 0)` for the prototype
  respawn loop.
- Restore health after the reset so the prototype remains immediately playable.
- Keep setup deterministic through existing idempotent Unity Editor commands.

## Non-Goals

- No checkpoint, save, room reload, or scene reload system.
- No invulnerability window, death animation, corpse state, or game-over UI.
- No change to the damage formula or `DamageResolver`.
- No direct HUD mutation from damage, death, or respawn code.
- No manual scene YAML editing unless Unity-generated setup proves inadequate.

## Current Baseline

`PlayerContext.RefreshFacingFromInput` already updates
`PlayerContext.FacingDirection` and calls `PlayerMotor2D.SetFacing` when
horizontal input magnitude is at least `0.01`.

`PlayerMotor2D` stores `FacingDirection` but currently does not flip a visual
target.

`SimpleHealth` already implements `IDamageable`, exposes `CurrentHealth`,
`MaximumHealth`, and `IsDead`, raises `Changed`, and raises an optional death
event when health reaches zero.

`PlayerHudUI` already subscribes to `SimpleHealth.Changed` and refreshes five
health chevrons from the current health value.

`PlayerHudSetup` already adds `SimpleHealth` to the player when creating the
sample HUD. `PlayerSceneCompositionSetup` should also ensure health and respawn
dependencies exist so player damage works even before HUD setup is rerun.

## Runtime Design

### Facing Presentation

`PlayerMotor2D` should gain a serialized visual transform reference:

```csharp
[Header("Presentation")]
[SerializeField]
private Transform visualRoot;
```

When `SetFacing` receives a non-zero direction:

1. normalize the gameplay direction to `-1` or `1`;
2. update `FacingDirection`;
3. apply the facing sign to `visualRoot.localScale.x` while preserving the
   original X magnitude.

If `visualRoot` is unassigned, the motor may fall back to a child
`SpriteRenderer` transform when one exists. It should not flip the root physics
object by default because root scale can affect colliders, sensors, and authored
physics values.

The flip convention for the baseline is:

- facing right: positive X scale;
- facing left: negative X scale.

If a particular rig is authored facing left by default, that should be handled
by assigning an appropriate visual root or adding a future explicit
`invertVisualFacing` toggle. The first implementation should avoid extra toggles
unless the current scene proves it needs one.

### Player Damage

The player should receive damage through `SimpleHealth` as an `IDamageable`.
Enemy attacks, hazards, and test damage sources can target the player
GameObject, and `DamageResolver` will discover the health component through the
existing damage contract.

No HUD or player controller code should call `DamageResolver` merely to reduce
player health. Damage application remains owned by the attacking source or
hazard.

### HUD Health Updates

The HUD remains bound to `SimpleHealth`:

```text
DamageResolver
-> SimpleHealth.ApplyDamage
-> SimpleHealth.Changed
-> PlayerHudUI.HandleHealthChanged
-> health chevron colors refresh
```

`PlayerHudUI` should not know about respawn, death, damage profiles, or damage
sources.

### Death Reset and Health Restore

Add a small dedicated runtime component:

```text
PlayerDeathRespawn
```

Responsibilities:

- subscribe to a configured `SimpleHealth.Changed` source;
- detect the transition into dead health;
- clear player transient action state when a `PlayerController` is assigned;
- zero player velocity through `PlayerMotor2D`;
- move the player transform to a configured respawn position, default
  `(0, 0, 0)`;
- restore health through `SimpleHealth.Initialize`.

Recommended serialized fields:

- `SimpleHealth health`;
- `PlayerController playerController`;
- `PlayerMotor2D motor`;
- `Transform respawnTarget`;
- `Vector3 fallbackRespawnPosition`, default `(0, 0, 0)`;
- `bool restoreHealthOnRespawn`, default `true`.

`respawnTarget` is optional. If unassigned, use `fallbackRespawnPosition`.

The component should guard against recursive health events while it is restoring
health.

For the current prototype, respawn should be immediate. If death animation or
invulnerability is added later, this component can become the single place where
delay and temporary lockout are introduced.

## PlayerController Integration

`PlayerController` should expose a narrow method for death reset support rather
than letting `PlayerDeathRespawn` reach into private internals.

Suggested method:

```csharp
public void ResetTransientState()
```

Responsibilities:

- complete any active attack bookkeeping;
- clear current action;
- clear combo/Card Time opportunity state if needed;
- publish Card Time availability as `None`;
- reset the current input snapshot to `PlayerInputSnapshot.None`.

`PlayerDeathRespawn` should call this method before moving the player and
restoring health.

## Editor Setup

Update `PlayerSceneCompositionSetup` so
`TIC/Setup/Update Sample Scene Player Composition` ensures the player has:

- `SimpleHealth`;
- `PlayerDeathRespawn`;
- the respawn component wired to the player, health, and motor;
- the motor visual root assigned when a deterministic child visual can be found.

Update `PlayerHudSetup` only if necessary to keep using the same `SimpleHealth`
instance. It should not create a second health component.

If visual root assignment cannot be inferred safely, the setup command should
leave the field empty and log a clear Editor warning with the exact Inspector
field to assign.

## Tests

Add focused EditMode coverage:

- `PlayerMotor2D.SetFacing` stores normalized facing and flips an assigned
  visual root to negative X when facing left.
- `PlayerMotor2D.SetFacing(0)` preserves both facing and visual scale.
- `PlayerDeathRespawn` moves its owner to `(0, 0, 0)` when health reaches zero.
- `PlayerDeathRespawn` restores health after immediate prototype respawn.
- `SimpleHealth.Changed` remains the health UI signal; existing HUD value math
  tests remain valid.

## Manual Validation

After implementation, run the relevant setup command in Unity:

`TIC/Setup/Update Sample Scene Player Composition`

Then run the HUD setup if the HUD is not already present:

`TIC/Setup/Create Or Update Sample Scene HUD`

Play Mode validation:

1. Move right and confirm the player visual faces right.
2. Move left and confirm the player visual flips left.
3. Apply damage to the player through an existing damage source or temporary
   test hazard.
4. Confirm health chevrons decrease.
5. Reduce health to zero.
6. Confirm the player returns to world position `(0, 0, 0)`.
7. Confirm health chevrons refill after respawn.

## Acceptance Criteria

- Horizontal movement direction changes the visible player facing.
- Root physics scale is not required to change for sprite flipping.
- Player GameObject has a `SimpleHealth` damage target.
- Damage to the player updates HUD health chevrons through
  `SimpleHealth.Changed`.
- Death resets the player to `(0, 0, 0)`.
- Prototype respawn restores health immediately.
- Runtime code does not require HUD references.
- Damage resolver remains unchanged unless an existing bug is discovered.
- Editor setup is idempotent and does not duplicate health, HUD, or respawn
  components.
