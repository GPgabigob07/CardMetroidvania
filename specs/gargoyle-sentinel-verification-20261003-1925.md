# Gargoyle Sentinel verification — 20261003-1925

## Contexto

Supersedes the1655 verification state after the single independent review
and its one fix pass. Sources: approved1205 enemy/Ward plans,1128 SDDs,
specs/gargoyle-sentinel-independent-review-20261003-1914.md and both execution
ledgers. The same interrupted reviewer resumed after quota reset; no second
review was commissioned. No merge or build publication is authorized.

## Review resolution

| Finding | Result and evidence |
| --- | --- |
| Per-pass feint RNG replay | RED3 sequence tests fail at a later pass, including authored20%/seed0. Preserve independent RNG across refills; only actual seed changes at refill or explicit encounter reset reseed it. GREEN21 brain checks. |
| Ward missed without health overlap | RED4 total1pass3fail: opening/late guard-only intersections and the bound-brain stagger integration. Discover bound Ward independently; standalone beam discovers same-scene active guards. Existing geometry/terrain/hold/death/counter gates remain authoritative. GREEN48 combined beam/brain/Ward checks. |
| Identical authored melee footprints | RED authored-data distinction failure. Editor applies distinct wider backhand, lower/taller slam and raised upward claw to SO payloads; normal setup preserves live tuning. |
| Rotated cue mismatch | Re-graded Important because new nonzero-angle footprints would otherwise misrepresent damage. RED2 mirrored-corner failures; cue corners now rotate with the same facing and angle as the query. |
| Related point-blank mirroring | During the required cue/query check, RED2 tests showed an offset aim origin could reverse melee away from a nearby player. Bound melee uses the brain's facing, with snapshot-aim fallback retained for standalone payloads. |

Combined melee/setup/presenter GREEN40/40. All important findings are addressed
in this one pass. No deferred minor remains: the sole reviewer minor was promoted
and fixed with regressions. Human input/feel/core-route/art judgments are still
pending, not silently waived or inferred from unit tests.

## Final automated evidence

- Unity6000.3.16f1 batchmode/nographics, native checkout.
- Full final-fixes-editmode.xml:599 total576 passed20 failed3 skipped.
- Exact failed identities equal baseline.xml: no new failures. The20 inherited
  failures remain outside feature scope; the entire EditMode suite is not green.
- Full final-fixes-playmode.xml:9 total9 passed0 failed0 skipped.
- All182 additional EditMode cases versus the original417-case baseline pass;
  the6 additional encounter PlayMode cases pass alongside3 existing regressions.
- Repeated setup serialization and non-mutation checks pass after geometry
  authoring. Native Unity serialization performed asset edits, not manual YAML.
- git diff --check passed, with Unity's existing YAML attribute allowing its
  native empty-value trailing spaces while normal code whitespace checks remain.

## Provisional authored melee data

| Step ID | Size | Offset from foot root | Right-facing angle |
| --- | --- | --- | --- |
| claw-left |1.5x1 |1,1 |0 |
| claw-right |2.1x0.8 |1,1.2 |-10 |
| claw-heavy |1.1x1.8 |0.95,0.7 |-15 |
| wing-advance |2x1.2 |1.05,1.1 |0 |
| wing-slam (upward claw; stable ID retained) |1.2x2 |0.85,1.65 |15 |
| delayed-claw |1.2x0.9 |1,1.05 |0 |

Dimensions/offsets are world units and are owned by the attack SO payloads.
These are baseline distinctions, with final balance subject to player testing.
The explicit TIC/Setup/Apply Initial Gargoyle Melee Footprints action restores
these geometry defaults outside Play Mode; ordinary setup never overwrites them.

## Remaining human acceptance

Use Assets/Scenes/Test_GargoyleSentinel.unity. The text question requesting real
observations is pending: attack/feint/stun readability; two distinct empowered
Chain Poise primary hits breaking nova; Ward opening stagger versus late clip,
facing and20Energy; movement escape at zero Energy; keyboard/gamepad cards/HUD;
live Inspector timing/damage/radius/guard changes. No responses were received
before this checkpoint and no subjective result is claimed.

The exact200x200 import,128x107 alpha bounds, fixed pivot100,36,48PPU and unit
root scale remain verified. The1655 report gives measured whole-silhouette and
collider areas. Artist-defined masks excluding wings/effects and body-only area
>=1.5x player remain required;48PPU is provisional. Single-frame draft remains
visibly labeled; final animations await the artist. These gates keep Task8/
WardTask3 feature acceptance open and the continuation ACTIVE.
