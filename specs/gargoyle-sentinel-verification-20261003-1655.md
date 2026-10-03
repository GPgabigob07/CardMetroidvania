# Gargoyle Sentinel verification — 20261003-1655

## Contexto

Approved enemy/Ward plans dated20261003-1205 and SDDs dated20261003-1128 are
the sources. Native checkout execution with one final independent review was
approved. This records authored encounter verification before that review.

## Automated evidence

- Unity6000.3.16f1, batchmode/nographics, current checkout.
- Task8 setup RED6 missing-asset failures, then GREEN6.
- Presentation RED2; GREEN after explicit EditMode health initialization.
- Composition RED2 missing presenter bindings; GREEN9 setup/presenter checks.
- Geometry RED2: missing authored projectile radius and initial volley aim lock.
  Added attack-owned radius and explicitly authored the initial0.15 aim lock.
- Actual PlayMode RED exposed a missing reset binding and fixture assumptions
  about frame-count/time and launcher readiness. Corrected fixtures to await
  scaled deadlines/CanEmit, then GREEN6/6 encounter lifecycle checks.
- Full EditMode task8-regression-editmode2.xml:588 total565 passed20 failed3
  skipped. Exact failed identities equal baseline.xml; no new failure identities.
  An earlier extra Golem failure was scene leakage from the authoring fixture;
  closing its arena at teardown removed that regression.
- Full PlayMode task8-regression-playmode.xml:9 total9 passed0 failed; prior
  recovery baseline3/3 remains green.
- Repeated setup preserves generated data/prefab/scene bytes and live tuning.
- Real Update/FixedUpdate progresses through Active; pause/hold, death-owned
  projectile cleanup, scene unload, encounter reset and asset non-mutation pass.
- git diff --check passed. No master merge or build publication.

## Pixel and body-area report

The source PNG and imported sprite are exactly200x200. Nonzero-alpha bounds
are128x107, pivot100,36 and48PPU; enemy root scale is1,1,1. There are8975
nonzero-alpha pixels: the whole wing-inclusive silhouette occupies3.895399
world-square units. Current player first frame uses141x299 at128PPU; its27539
nonzero-alpha pixels occupy1.680847 world-square units. The whole-silhouette
ratio is2.317521. These are measured image values, not a body-mask acceptance.

The provisional enemy body collider is1.8x2.1=3.78 world-square units versus
the player collider1x2=2, ratio1.89. This also is not proof of occupied body area.
The artist/human must define masks excluding wings/effects for both actors and
confirm body-mask world area>=1.5 times player.48PPU remains provisional until
that acceptance. The player has not been resized.

## Test arena and live tuning

Open Assets/Scenes/Test_GargoyleSentinel.unity; use TIC/Setup/Create Or Update
Gargoyle Sentinel outside Play Mode if composition is missing. Setup preserves
existing simulation tuning. Assets/Data/Enemies/GargoyleSentinel owns phases,
geometry, damage, poise, cadence, feint and presentation. WardDefinition and
Neutral_Ward under Assets/Data/Cards own guard geometry/duration and Energy cost.
The distinct GargoyleCardInventory replaces Jump Boost with Ward and keeps
movement, Chain Poise and recovery. Ordinary equipped inventory is unchanged;
the shared catalog gains Ward and preserves recovery entries.

The debug overlay displays state/step/bag/HP/poise and an encounter reset. Reset
restores actor HP/poise and player HP/Energy, cancels offense/card transients and
repositions both. It intentionally preserves spent Mend stock; a fresh scene
reload starts a fresh inventory. Draft animations are visibly marked pending.

## Required human observations — pending

In Play Mode confirm: readable Basic/family orders and feint/stun cues; Chain
Poise two-distinct-primary-hit core counter during nova; front-facing Ward
opening stagger versus late clip, Energy20 and back-facing rejection; movement
escape from beam/nova at zero Energy; keyboard/gamepad card selection and HUD;
Inspector changes to current timing/damage/radius/guard duration without reset;
pixel scale/body masks and artist animation handoff. No observations are invented.

Final independent review remains pending at this report version. Feature delivery
and automation shutdown remain gated on review/fixes and actual human acceptance.
