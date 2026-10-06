# Repel Chain Card — Design for Review

## Contexto

The user approved a reusable Chain card costing 15 Energy, enabling melee
projectile reflection for an initial 3 gameplay seconds. The purpose is to let
the player prepare for a projectile enemy during an available Chain Card Time,
then intercept shots through ordinary melee combat. Card activation must not
require a second precisely timed defensive input.

Sources:

- `gdd/gdd-canonico-20260526-2331.md`
- `specs/bat-machine-enemy-sdd-20260901-1202.md`
- `specs/ward-card-and-beam-interception-sdd-20261003-1128.md`
- `specs/asset-driven-card-definitions-and-commit-transaction-sdd-20260614-1833.md`
- Current `PlayerCardRuntime`, `PlayerAttackHitDetector2D`, `EnemyProjectile2D`,
  card-operation definitions and prepared-commit flow.

This is a new design version. The prior Bat specification remains historical;
its reflected damage rules remain authoritative. This card changes the reflected
direction to the radial direction approved in this conversation.

## Approved player behavior

| Property | Value |
| --- | --- |
| Display name | Repel |
| Category | Chain |
| Fixed cost | 15 Energy |
| Inventory consumption | Reusable |
| Initial duration | 3 gameplay seconds after successful commit |
| Trigger | Active melee hitbox intersects a launched enemy projectile |
| Direction | Normalize(projectile position minus player position) |
| Number of reflections | Multiple projectiles during the active duration |
| Repeated reflection | Each projectile can convert to player ownership once |

Card copy: "For 3 seconds, melee strikes repel enemy projectiles."
The displayed duration should follow the authored tuning if it is edited.

Windup, recovery, idle body contact and an inactive attack do not reflect shots.
The buff does not grant immunity: a projectile that reaches the player's damage
collider without being intercepted still follows ordinary damage resolution.
The player can move, jump and attack normally while the buff is active.

The direction uses the player's root and the projectile's position at interception,
not the enemy's position or the projectile's original travel direction. It does
not home. If both positions coincide, use the reverse incoming direction so the
launch direction remains valid.

## Existing damage contract

Use the "Projectile Deflection And Converted Damage" section of
`specs/bat-machine-enemy-sdd-20260901-1202.md` unchanged:

- Preserve projectile health damage.
- Transfer source and Converted provenance to the player, retaining projectile
  parent/root identity and explicit proc suppression.
- Set converted projectile poise damage to exactly 10.
- Resolve damage through the shared resolver and expire after the first accepted
  hit or the existing remaining lifetime.

Preserve current projectile speed. Reflection neither resets its lifetime nor
applies the melee attack's damage formula or card modifiers to it. Detach enemy
launcher ownership/hit budget using the existing conversion contract.

## Proposed supporting policies

These close implementation edge cases and are included for written-spec review:

- Reject activation while Repel is active; do not spend Energy, stack or refresh.
- Count duration in scaled gameplay time: pause and world hold freeze it; Card
  Time advances it at its normal slowed gameplay rate.
- Clear on death, new-run/reset and area transition; ending Card Time or the
  activating combo alone does not clear it.
- A reflection alone does not confirm an enemy melee hit, earn hit Energy,
  advance attack chains, consume on-hit effects, or request enemy-hit hitstop.
- Multiple projectile colliders/queries cannot convert one shot repeatedly.
  Already converted projectiles are excluded from Repel eligibility.
- Simultaneous damage and interception are ordered by the existing physics/query
  processing; no retroactive health refund. Tests must establish interception
  before damage when the melee overlap is resolved first. If playtests expose
  ordering unfairness, investigate collision scheduling as a separate change.

## Integration design

Use an asset-backed typed card operation to arm a player-local timed Repel effect.
Author duration in effect data and cost/category in `CardDefinitionSO`; no checks
of display names or stable IDs in gameplay logic. Append operation enum values
without renumbering serialized values. Follow existing supported-shape validation,
readiness, prepared-commit revision checks and atomic payment/effect handling.
Invalid configuration, stale quotes, insufficient Energy or an already active
effect must leave both wallet and effect unchanged.

Extend the active melee hit-detection flow to process eligible projectile overlaps
before its early return for no damageable enemy targets. Reuse actual melee box
geometry, execution phase and range tuning; projectile-only strikes must work.
Detect the canonical projectile component through child colliders, handle each
projectile once, and preserve ordinary enemy damage in the same attack query.
Ensure projectile-layer coverage without broadening ordinary damage recipients.

Extend projectile conversion with an explicit direction-capable entry point,
preserving existing reverse-direction callers and their tests. Centralize
ownership, damage and provenance conversion so both paths share one contract.
Separate Repel's once-only eligibility from unrelated future conversion policies.

Show active duration/expiry through existing effect feedback where supported.
Add the card to the catalog and a focused playtest loadout with an existing Chain
slot; preserve production loadouts and input slot limits unless separately asked.
Concrete MonoBehaviours and ScriptableObjects use same-named files. Scene/prefab
setup follows the repository Editor collaboration workflow.

## Verification and playtest

Regression coverage must verify:

- Chain-only availability, 15 Energy payment, no inventory consumption and atomic
  rejection; authored duration validation and stale preparation rejection.
- Expiry, pause/world hold, lifecycle clearing and active-effect reactivation.
- Reflection during melee execution only; no reflection without the buff,
  during windup/recovery, or from body contact alone.
- Projectile-only overlaps, child/multiple colliders, multiple different shots,
  no repeat conversion, and normal enemy damage alongside reflection.
- Radial direction, coincident-position fallback, unchanged speed/remaining
  lifetime and all documented converted-damage/proc rules.

Playtest against Bat Machine: activate during a Chain opportunity, intercept a
shot with grounded and aerial attacks, intercept several within the duration,
then verify attacks no longer reflect after expiry. Also verify a missed
interception can still damage the player and converted shots can hit enemies.

## Status

Design written from approved card behavior; supporting policies await review.
No Repel runtime code or assets have been implemented. After written-spec
approval, prepare the implementation plan before starting code.
