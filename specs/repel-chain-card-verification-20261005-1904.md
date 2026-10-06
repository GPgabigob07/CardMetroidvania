# Repel Chain Card Verification — 20261005-1904

## Contexto

Implementation of the user-approved
`specs/repel-chain-card-sdd-20261005-1804.md` and
`specs/repel-chain-card-implementation-plan-20261005-1835.md`.
Completed in the shared Unity checkout on branch `codex/repel-chain-card`,
preserving the earlier uncommitted bat-detection changes. No commit, push or merge
was performed. The original specs remain historical design and planning records.

## Implemented

- Reusable Chain card `repel`: 15 Energy, 3 gameplay seconds, multiple projectiles.
- Player-local timed capability with pause/hold suppression, active-effect rejection,
  death/reset/transition clearing, remaining-duration HUD and expiry feedback.
- Typed card operation, prepared quote and atomic payment/arming with reentrancy,
  stale cost/effect/session, affordability and category checks.
- Reflection only through active melee execution overlap. Separate projectile
  discovery covers both Enemy-layer bat and Default-layer Gargoyle shots, without
  widening ordinary melee damage filtering. Projectile-only strikes do not
  confirm an enemy hit or run primary melee damage/procs.
- Radial outward direction, reverse-incoming fallback at coincident positions,
  one conversion per shot, unchanged health damage/speed/remaining lifetime,
  player ownership/Converted provenance, exactly 10 poise, suppressed procs.
- Authored card/effect assets, catalog addition, player-prefab capability and a
  separate Repel test inventory/scene. Normal loadouts remain unchanged.

## Integration findings and rulings

Real physics tests found that the global collision matrix disables Enemy-to-player
and Enemy-to-enemy contacts. Projectiles now include their configured target
layers through collider-local overrides at launch, leaving the matrix unchanged.
Unconverted shots cannot damage EnemyActor targets; source-descendant colliders
cannot receive their own projectile. Converted direction updates velocity
immediately, so the next physics step uses the reflected motion.

Bat physical-body hits could bypass the damage policy through the root's
EnemyHealth component. BatMachineHurtbox now implements the existing
IEnemyDamageRegion contract, and projectile actor contacts use the shared
EnemyDamageRegionSelection path. Physical body contacts cannot bypass typed
hurtboxes. This also lets normal melee consistently select the bat's policy.
Gargoyle typed regions retain their established selection rules.

Temporary PlayerWorldHold previously calls ResetTransientState. That method now
accepts an optional clearTransientCardDefenses argument; holds preserve Repel
while disabling interception/timer progress. Actual transition/reset/respawn
callers keep the default clear. Ward's existing clearing behavior is preserved.

## Verification evidence

Unity 6000.3.16f1, isolated validation project under `Temp/FlyingEnemyValidation`:

- Missing capability, unsupported operation, absent melee interception, lifecycle
  clearing, Default-layer coverage and damage-routing regressions observed red
  before their respective fixes.
- Final focused EditMode fixtures: **70 passed, zero failed**.
- Final actual projectile-contact PlayMode fixture: **7 passed, zero failed**.
- Broad EditMode suite: **638 total, 615 passed, 20 failed, 3 skipped**.
- Clean committed HEAD baseline in `Temp/RepelBaselineValidation`: **599 total,
  576 passed, the same 20 failed, 3 skipped**. Failed-test identity comparison
  found no added or removed failures. These baseline failures are not claimed fixed.
- Fresh independent code review found the Default-layer coverage issue; it was
  fixed and checked with a regression. Follow-up review found no remaining
  Important/Critical issues.
- `git diff --check` passes. Generated assets/prefab changes were inspected before
  copying only the intended outputs back into the shared project with their GUIDs.

Results/logs: `Temp/RepelWork/editmode-final.xml`, `physics-final.xml`,
`editmode-baseline.xml` and matching `.log` files. Progress ledger:
`Temp/RepelWork/progress.md`.

## Playtest handoff

Wait for Unity to recompile, then open `Assets/Scenes/Test_BatRepel.unity` and
enter Play Mode. This is a copy of the existing Bat arena with a HUD and
`RepelTestCardInventory`; Repel occupies the former Growing Reach Chain slot,
while Poise Damage and the other category loadouts are preserved.

Land a melee hit to obtain a Chain opportunity, select Repel, then strike incoming
shots during its 3-second window. Check grounded/aerial interception, several
shots, expiry, damage from missed shots and reflected enemy health/poise damage.
The displayed timer follows gameplay time. Duration is authored in
`Effect_Repel.asset`; cost is authored in `Card_Chain_Repel.asset`.

Visual readability, timing feel and final tuning remain for the user's playtest.
Idempotent setup menu: `TIC/Setup/Create Or Update Repel Playtest`. Existing card
cost/duration edits are preserved when rerunning setup.
