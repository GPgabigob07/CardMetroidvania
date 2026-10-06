# Hit Ripple Lab and Contact Fix - 20261006-1232

## Contexto

Follow-up to `specs/enemy-hit-ripple-verification-20261006-0917.md`,
`specs/enemy-hit-ripple-sdd-20261006-0840.md` and
`specs/enemy-hit-ripple-implementation-plan-20261006-0850.md`.
The user requested a new scene with single-sprite objects and hit detection,
and reported that the ripple was too subtle and appeared centered on the sprite
origin. Prior documents are preserved; this note records the changed tuning,
contact calculation and standalone diagnostic scene.

## Investigation and changes

An actual rendered regression through DamageResolver, recipient listener and
EnemyHitRipplePresenter demonstrated that a supplied off-center world contact
already renders around that contact rather than the sprite pivot. Earlier GPU
tests set shader parameters directly and did not verify this full path.

Melee supplied Collider2D.ClosestPoint from the attack overlap-box center. When
that center is inside an enemy, ClosestPoint returns an interior point, which
can appear close to its origin. A regression showed the old point on the wrong
side of the first collider: approximately (0.61,-0.19) instead of the
attacker-facing (0.39,-0.19). Melee now measures the per-recipient contact from
the attacker's position at the attack's vertical offset. It still uses one
request/report, with unchanged target selection, damage/procs and hit confirmation.

The default profile now uses 0.65-second traversal, full highlight strength,
brighter blue/fatal leading colors, and an 8% final fade instead of 0.25 seconds,
95% strength and a 15% fade. Width stays at four source pixels. The longer
duration makes the thin band easier to follow and lets rapid hits stack visibly.

## Standalone scene

`Assets/Scenes/Test_HitRipple.unity` contains three stationary targets, each with
exactly one SpriteRenderer and one hit collider:

- small centered-pivot grid square;
- large grid rectangle for stacking;
- circle with an offset pivot and a flipped sprite.

No player, enemy AI, animation controller or combat arena is required. Screen
clicks perform real collider queries and send exact clicked world points through
DamageResolver and the normal ripple recipient listener. The target deliberately
simulates result categories without death hiding the sprite. A manual diagnostic
clock lets the controller freeze waves without changing global gameplay time.

Orange crosses show sprite pivots; pink crosses show recorded hit points. Labels
show active wave count, width, duration and last hit coordinates/outcome.

Controls in Play Mode:

- Left click: selected damage/rejected/fatal mode; hold for repeated hits.
- 1 / 2 / 3 or the buttons: choose mode.
- Right click: rejected; middle click: fatal.
- B or the burst button: three successive hits at the last selected hit point.
- P or Freeze: pause/resume wave progression; R: clear waves.

Tuning remains in `Assets/Data/Feedback/HitRippleProfile.asset`.
Rebuild menu: `TicGame > Feedback > Create or Update Hit Ripple Test Scene`.
The creator reuses assets and the scene GUID, replaces only its owned lab root,
and preserves the previously active scene. It refuses an unsaved untitled base
scene or a dirty loaded lab instead of discarding changes. To regenerate, save
or open a saved scene first. The delivered scene is already authored and saved.

## Verification

Unity 6000.3.16f1 in isolated `local-artifacts/HitRippleLabValidation`:

- Missing standalone setup and attacker-facing contact regression observed red.
- Actual pre-fix presenter off-center render regression passed, narrowing the
  problem to supplied melee contact/visibility rather than hard-coded shader origin.
- Final focused EditMode: **27 passed, zero failed**.
- Final graphics/physics PlayMode: **13 passed, zero failed**, including actual
  screen/collider hits, distinct damage/rejected/fatal origins, pause, three live
  sprite renderers sharing the material, offset/flipped pivot, lit shader colors
  and transparency, plus existing projectile collision regressions.
- Broad EditMode: **658 total, 635 passed, 20 failed, 3 skipped**; failure names
  match the previously recorded baseline set. No new failures.
- All changed runtime, Editor and test code compiled through Unity.
- `git diff --check` passes. Generated scene/art assets copied back with matching
  script and asset GUIDs; no metadata conflicts.

The new lit tests use Light2D from the installed
`Unity.RenderPipelines.Universal.2D.Runtime` assembly. The earlier test's lookup
used the main URP assembly and silently omitted that light. The revised lookup
asserts it exists; fixtures unload their global light between tests.

Rendered capture: `local-artifacts/HitRippleLabWork/lab-preview.png`.
Results/logs and local ledger: `local-artifacts/HitRippleLabWork/`.
These local artifact folders are ignored by Git and avoid Unity's Temp cleanup.

Independent read-only review reported no Critical/Important findings. One minor
diagnostic edge case remains: switching the selected target before a queued burst
finishes can drop its remaining hits, because the burst keeps the first hit point
but uses the newly selected target. Normal held clicks and bursts on one target
work; deferred polish would capture the burst recipient too.

No commits, pushes, Unity upgrade, new dependency, or changes to unrelated scenes
were made. The user's `Assets/_Recovery` content was preserved.
