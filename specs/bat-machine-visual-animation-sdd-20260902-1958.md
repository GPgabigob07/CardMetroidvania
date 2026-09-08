# Bat Machine Visual Animation SDD - 20260902-1958

## Contexto

This version adds presentation assets and animation ownership to the Bat Machine
after gameplay, poise, firing, and projectile deflection were implemented. It
uses `specs/bat-machine-enemy-sdd-20260901-1202.md` as its gameplay source and
the second silhouette in `Assets/Art/Enemies/Concepts/flying-variations-concept-sheet.png`
as the selected visual reference: the orange bat with blue eye, crystal horns,
and blue energy tail.

Gameplay remains authoritative. This document only defines deterministic visual
feedback for existing states and projectile travel.

## Asset Contract

Store seven separate Bat sheets under `Assets/Art/Enemies/BatMachine/`:

| State | Sheet name | Motion intent |
| --- | --- | --- |
| PatrolRandom | `BatMachine_PatrolRandom.png` | Broad, relaxed wingbeat and gentle vertical bob. |
| Engage | `BatMachine_Engage.png` | Tighter, faster wingbeat with a forward pitch. |
| WindupFire | `BatMachine_WindupFire.png` | Wing lock and blue-eye/tail energy gathering. |
| Evade | `BatMachine_Evade.png` | Compress, lateral kick, short energy wake, reopen. |
| StunnedFall | `BatMachine_StunnedFall.png` | Asymmetric wing collapse and uncontrolled tumble. |
| GroundedRecovery | `BatMachine_GroundedRecovery.png` | Ground twitch, crystal reassembly, prepared lift. |
| Dead | `BatMachine_Dead.png` | Final collapse; it must not imply flight recovery. |

Every sheet contains exactly seven horizontal frames, each 64 by 64 pixels
(448 by 64 pixels total), transparent background, point filtering, and a
single right-facing base direction. Left travel is a horizontal renderer flip;
do not duplicate left-facing art.

The projectile is `BatMachineProjectile_Spin.png`: five 32 by 32 frames in one
horizontal 160 by 32 sheet. It spins clockwise around a local +X nose, has a
transparent background, uses point filtering, and is horizontally flipped when
its travel direction is negative X.

## Presentation Ownership

`BatMachineVisualController` is a focused presentation component on
`VisualRoot`. It reads the existing `BatMachineBrain.CurrentState` and the
`Rigidbody2D` velocity. It maps each of the seven states to its seven-frame
clip, flips the renderer for signed horizontal velocity (preserving the last
facing direction while near stationary), and varies Patrol/Engage playback
rate within serialized limits based on aerial speed. It must not transition
states, alter steering, spawn attacks, or change collision/poise/damage logic.

`ProjectileSpinVisual` is a focused component on `ProjectileTemplate`. It
loops the five imported frames, reads `EnemyProjectile2D.Direction`, and flips
the renderer for negative X direction. It must not rotate gameplay transforms
or change projectile direction/velocity.

Both components must handle missing clips by retaining the current sprite and
emitting one clear development error, rather than disabling gameplay.

## Unity Setup

Extend the existing idempotent `TIC > Setup > Create Or Update Bat Machine`
tool to configure all art import settings, slice the sheets, attach both visual
components, and assign all clip references. The tool must preserve unrelated
prefab configuration and remain safe to run repeatedly.

The user validates actual visual feel in `Test_BatMachine`: patrol bob,
engage acceleration, evade direction, windup readability, fall/recovery/death
clarity, projectile spin, and reflected projectile facing.

## Test Strategy

EditMode tests cover the complete one-to-one state-sheet mapping, seven frames
per Bat sheet, five frames for the projectile, facing flips for positive and
negative travel, stable facing at near-zero velocity, and the guarantee that
visual controllers do not mutate gameplay state or projectile travel.

## Scope Boundaries

This slice does not add Animator state machines, animation-event gameplay,
new Bat behavior, VFX pooling, sound, or directional art beyond horizontal
mirroring. Runtime timing remains owned by the existing brain and projectile.
