# Ward Card And Beam Interception SDD - Review Draft

## Contexto

Created 2026-10-03 as the player-side dependency of the proposed Gargoyle beam
gimmick. The author requires a card interaction that blocks the laser. Ward is
the proposed realization, not an existing confirmed card design. Source context:

- `gdd/gargoyle-sentinel-design-20261003-1035.md`
- `specs/gargoyle-sentinel-enemy-sdd-20261003-1035.md`
- `specs/asset-driven-card-definitions-and-commit-transaction-sdd-20260614-1833.md`
- `specs/neutral-chain-card-baseline-sdd-20260921-1107.md`
- `specs/player-recovery-economy-design-20260929-1516.md`
- `specs/player-recovery-economy-verification-20260930-0014.md`
- `specs/unity-editor-collaboration-workflow-20260612-1609.md`
- Current PlayerCardRuntime, PreparedCardCommit, CardOperationDefinition,
  CardTimeSessionController, PlayerWorldHold and SimpleHealth at `13e0138`.

## Proposed card behavior

| Property | Draft value |
| --- | --- |
| Stable ID / name | `card.neutral.ward` / Ward |
| Card Time | Neutral |
| Fixed Energy cost | 20 |
| Consumption | Reusable; no inventory copy consumed |
| Guard lifetime | 0.60 gameplay seconds after successful commit |
| Guard geometry | Vertical segment, 1.5 units tall, 0.6 units forward of player root |
| Facing | Locked from player facing at commit; movement remains enabled |
| Stack policy | Reject while active |
| Successful counter | First intersecting Gargoyle beam opening pulse consumes guard, cancels beam, causes 0.65-second stagger |

Ward is directional. The emitter must be in front of the player: positive dot
product with the committed facing. The thick clipped beam must intersect the
live guard segment before reaching the player's damage bounds, with no terrain
blocking that path. Tangential near misses are determined by the beam's actual
thickness, not an arbitrary distance from the enemy.

The opening pulse is the first 0.20 gameplay seconds of beam Active. A guard
interception during that interval cancels the entire beam before health damage
is resolved. During the remaining beam interval, Ward still clips the beam at
the guard while active, but causes no stagger and is not consumed by repeated
contact. When it expires, the beam can reach/damage the player if the cast has
not already applied an accepted hit. Only an opening counter consumes Ward.

Ward does not block ordinary melee, nova or projectile volleys in this first
dependency slice. It grants no global invulnerability and reflects no damage.
Show shield orientation, remaining lifetime, successful counter and expiry.
Player-facing copy: "Briefly guard against beams. Catch the opening pulse to
interrupt the enemy." Keep readiness visible through current card feedback.

## Components and integration

- `PlayerWardRuntime.cs`: player-local MonoBehaviour owns facing, segment,
  duration, CanArm, Arm, Tick, TryIntercept and Clear. No enemy selection logic.
- `WardDefinitionSO.cs`: immutable lifetime/geometry settings, referenced by
  the typed card operation. Concrete asset class in its own same-named file.
- Append an explicit `ArmDirectionalWard` CardOperationKind without renumbering
  existing values. Extend operation data with its WardDefinitionSO reference;
  validate finite positive lifetime/height and a finite non-negative offset.
- Extend PlayerCardRuntime readiness, supported-shape validation and operation
  dispatch. No card-name/string check in PlayerController or GargoyleBrain.
- Beam queries PlayerWardRuntime on the canonical player root before resolving
  health. No second competing IDamageable is added to the player root and no
  component-order assumption is used to intercept SimpleHealth.
- TryIntercept receives a cast token, clipped beam segment/thickness, emitter
  position and whether its opening is still live. Return a typed result: None,
  Clipped or Countered, including clip position. Only Countered consumes the
  one-use opening counter and requests the Gargoyle stagger.
- The beam owner processes one Countered result per token and rechecks death
  priority. Multiple colliders, callbacks or queries cannot trigger repeated
  staggers or consume another Ward on the same cast.

Readiness validates current Neutral availability, dependency, live player,
inactive guard and full Energy affordability. Recheck before payment/effect.
Prevent event reentrancy from spending Energy while failing to arm Ward: reserve
the guard/commit under a guard flag, coordinate deferred wallet notification
using the existing transaction pattern, and publish after the coherent state
is committed. Cancellation, stale session, active guard and insufficient Energy
leave both resource and guard unchanged. Never spend first then discover Arm
failed. Retain existing session ID/category validation and AlreadyApplied guard.

Gameplay-scaled duration advances at Card Time's slowed simulation rate and
freezes during pause or player world hold. Those holds also suppress
interception/damage queries. Death, title/new-run
reset and area transition clear Ward; ordinary completed Card Time selection
does not. Unlike long-lived card buffs, this transient combat guard intentionally
does not carry into another area. It may be armed in air through normal Neutral
availability; no new Card Time category or guaranteed enemy-triggered window.

## Loadout and economy boundary

The existing prototype control scheme has authored slot limits. Add Ward to a
Gargoyle test inventory/profile and reuse one Neutral slot formerly occupied by
Jump Boost; preserve Grounded Double Jump, Dash Enabler and the recovery slots.
Do not append a seventh slot, change input chords or overwrite the normal run
profile. Equip Poise Damage in Chain. Catalog addition is idempotent and runtime
effects do not mutate definition/profile assets.

20 Energy makes the proposed highest positive equipped Neutral cost C=20 in
that test profile. Existing recovery formulas then yield passive reserve 30,
energy-to-health price 40 per HP and Blood Charge gain 20. This is an observable
balance change from a C=15 profile and must be reviewed with Ward's cost. Preserve
the recovery formulas; do not exempt Ward from C or create a recursive price.

The basic movement route remains valid without Ward. Do not auto-equip, auto-cast
or grant free counter energy at every beam telegraph. Author initial test stock
and test zero-energy escape separately. A current movement/attack opportunity
may prevent Neutral activation; this is a player timing choice, not grounds for
bypassing Card Time's existing opportunity identity/rearm rules.

## Verification and acceptance

Focused tests cover exact one-time payment/arm, transaction notification
reentrancy, rejected/stale/cancelled commits, facing lock, moving/airborne segment,
front/back hits, terrain clipping, opening boundary, late guard clipping/expiry,
overlapping player colliders, repeated cast tokens and death precedence. Freeze
timer and queries during pause/hold; clear on transition/reset. Assert recovery
cost basis changes from 15 to 20 in the test loadout while normal loadout remains
unchanged. Validate slot bounds and repeated setup idempotency.

Human Play Mode acceptance: obtain Neutral availability, select Ward while
reading the actual 0.8-second beam telegraph, orient the guard, intercept the
opening and punish stagger. Also test late Ward, wrong facing, no energy,
movement-only avoidance, expiry, keyboard/gamepad and Card Time pause/slowdown.
If successful interception requires invisible knowledge or impossible timing,
retune beam windup/guard lifetime; do not silently change card categories.

This dependency and the enemy spec require review together before a detailed
implementation plan is approved. No Ward code or data assets exist yet.
