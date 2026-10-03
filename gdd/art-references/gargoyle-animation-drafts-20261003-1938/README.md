# Gargoyle Sentinel animation drafts — 2026-10-03

## Contexto
Pose-reference sheets for the approved Gargoyle Sentinel, requested while the user playtests the coded encounter. Sources: gdd/gargoyle-sentinel-design-20261003-1128.md, specs/gargoyle-sentinel-enemy-sdd-20261003-1128.md, and gdd/art-references/gargoyle-idle-draft-200x200-20261003.png.
Latest human instructions: at least five frames per animation, every frame EXACTLY 200×200 pixels, gargoyle silhouette at least 128×128, draft sheets only. This draft uses **silhouette bounding-box dimensions**, including wings, as the 128×128 measurement. That is separate from opaque-pixel area or the gameplay body-mask comparison.

## Deliverables
- **25 clips, five distinct frames each: 125 individual transparent PNGs.**
- Every file in frames/ is exactly **200×200** actual PNG pixels.
- Every file in strips/ is **1000×200**, one row of five **200×200** cells.
- Every file in atlases/ is **1000×1000**, five rows of five **200×200** cells.
- No borders, text or gutters inside the exported sheets. Read cells left to right, rows top to bottom.
- Binary alpha: each pixel is fully opaque or fully transparent.
- Measured silhouettes meet both minimum dimensions of 128 pixels. See pixel-report.json for each frame.
- Generated with the built-in imagegen tool; the actual prompts are in prompts.json. Its larger native outputs were extracted and packed with nearest-neighbor sampling into the verified final files. Native generation size is **not** the delivered frame size.

## Artist brief
Keep the approved orange/ochre armor, charcoal joints, ivory skull/claws, two blue crystal horns, cyan rib reactor and mechanical folded wings. All poses face right in a fixed three-quarter view. Grounded movement only.

These are animation **pose drafts**. Refine anatomy consistency, armor registration, timing and the attack arcs during final pixel animation authoring. The attack-strip replacements use compact rising/bent-arm gestures to keep the complete pose in the frame; align final sweep/backhand/delayed-claw contact with the existing forward melee hitboxes. Keep the Wingbreaker follow-up an upward claw; the existing asset ID wing-slam is historical, not a request for another downward slam.

Draft frames share a bottom foot baseline at PNG y=184 (top-left coordinates), approximate bottom-left pivot (100,16). This is draft framing, **not** a changed Unity import setting; the original idle reference uses (100,36). Resolve the final shared pivot and character-body scale with the artist before importing. Minimum silhouette normalization is per pose; final animation should standardize body proportions rather than use these drafts as finished playback.

Keep detached projectile, beam and nova VFX separate from the gargoyle. These sheets cover the character poses; the current encounter already draws beam/nova/projectile presentation in code. Its 1/3/5 counts and damage/telegraph geometry remain controlled by existing ScriptableObjects. No flight, adds or contact-damage animation is needed.

## Clip index
| Atlas | Row | Clip | Intended use |
|---|---:|---|---|
| [01-movement-basic](atlases/01-movement-basic.png) | 1 | [idle](strips/idle.png) | five distinct subtle breathing poses: neutral, chest rise, horn/core pulse, wing hinge settle, return close to neutral |
| [01-movement-basic](atlases/01-movement-basic.png) | 2 | [walk](strips/walk.png) | five grounded walking poses, alternating claw-foot plants with weight shifting; no translation across cell |
| [01-movement-basic](atlases/01-movement-basic.png) | 3 | [claw-sweep](strips/claw-sweep.png) | five attack poses: front claw draws back, crouched anticipation, forward horizontal sweep, full reach, follow-through |
| [01-movement-basic](atlases/01-movement-basic.png) | 4 | [claw-backhand](strips/claw-backhand.png) | five attack poses: opposite claw crosses chest, torso coils, wide horizontal backhand, extended arc, recoil |
| [01-movement-basic](atlases/01-movement-basic.png) | 5 | [claw-heavy](strips/claw-heavy.png) | five attack poses: claw rises, overhead heavy anticipation, downward slam, ground-contact compressed pose, recovery |
| [02-wing-feint](atlases/02-wing-feint.png) | 1 | [wing-advance](strips/wing-advance.png) | five attack poses: wing opens slightly, shoulder loads, grounded forward wing strike, braced follow-through, wing folds |
| [02-wing-feint](atlases/02-wing-feint.png) | 2 | [upward-claw](strips/upward-claw.png) | five attack poses: low claw load, crouch, powerful upward claw, high reach, recoil |
| [02-wing-feint](atlases/02-wing-feint.png) | 3 | [feint-cue](strips/feint-cue.png) | five fake attack poses: sweep anticipation, pause, abrupt recoil, conspicuous wing tuck, delayed loaded stance; do not execute swing |
| [02-wing-feint](atlases/02-wing-feint.png) | 4 | [delayed-claw](strips/delayed-claw.png) | five attack poses: held claw coil, deeper anticipation, sudden forward strike, extended reach, follow-through |
| [02-wing-feint](atlases/02-wing-feint.png) | 5 | [melee-recovery](strips/melee-recovery.png) | five poses smoothly relaxing an attack stance back into the idle silhouette |
| [03-ranged](atlases/03-ranged.png) | 1 | [volley-one](strips/volley-one.png) | five cast poses: single horn glows, head rises, chest-to-mouth charge, one firing recoil, settle; no detached projectile |
| [03-ranged](atlases/03-ranged.png) | 2 | [volley-three](strips/volley-three.png) | five cast poses: both horns glow, chest rises, throat charge, medium firing recoil, settle; no detached projectile |
| [03-ranged](atlases/03-ranged.png) | 3 | [volley-five](strips/volley-five.png) | five cast poses: wings brace, reactor intensifies, bright throat charge, strong firing recoil, settle; no detached projectile |
| [03-ranged](atlases/03-ranged.png) | 4 | [beam-charge](strips/beam-charge.png) | five poses tracking then aiming straight right: throat begins lighting, jaw opens, neck extends, horns flare, fully charged locked aim |
| [03-ranged](atlases/03-ranged.png) | 5 | [beam-fire](strips/beam-fire.png) | five sustained beam-emission poses: mouth open, neck extended, feet firmly braced, small distinct recoil cycles, tiny bright cyan muzzle glow only; no actual beam |
| [04-reactor-counters](atlases/04-reactor-counters.png) | 1 | [beam-recovery](strips/beam-recovery.png) | five poses closing mouth and lowering a heated throat back to idle |
| [04-reactor-counters](atlases/04-reactor-counters.png) | 2 | [nova-charge](strips/nova-charge.png) | five poses opening rib plates to expose the blue reactor, wings spread slightly, crouched braced charge, growing attached core light, fully exposed core; no huge surrounding aura |
| [04-reactor-counters](atlases/04-reactor-counters.png) | 3 | [nova-release](strips/nova-release.png) | five poses: reactor at peak, explosive bodily extension, chest plates recoil, body braces, energy spent; no detached blast effect |
| [04-reactor-counters](atlases/04-reactor-counters.png) | 4 | [nova-recovery](strips/nova-recovery.png) | five poses dimming exposed core and closing rib plates back to idle |
| [04-reactor-counters](atlases/04-reactor-counters.png) | 5 | [beam-counter-stagger](strips/beam-counter-stagger.png) | five poses choking beam: jaw snaps, head jolts backward, throat dims, head droops exposed, unsteady hunched punishable stance; distinct from global stun |
| [05-hit-stun-death](atlases/05-hit-stun-death.png) | 1 | [hurt](strips/hurt.png) | five brief nonlethal impact-recoil poses, core remains lit |
| [05-hit-stun-death](atlases/05-hit-stun-death.png) | 2 | [global-stun](strips/global-stun.png) | five dazed hunched poses: knees dip, claws slump, head droops, wings sag, slight dazed sway; keep wings raised enough for silhouette 128 pixels tall |
| [05-hit-stun-death](atlases/05-hit-stun-death.png) | 3 | [stun-recovery](strips/stun-recovery.png) | five poses regaining balance from hunch to alert idle |
| [05-hit-stun-death](atlases/05-hit-stun-death.png) | 4 | [death](strips/death.png) | five fatal poses: reactor flickers, knees buckle, claws collapse, head sinks, heavy folded-wing corpse; corpse wing peaks retain 128-pixel silhouette height, no disappearance |
| [05-hit-stun-death](atlases/05-hit-stun-death.png) | 5 | [dead-hold](strips/dead-hold.png) | five settled dead-body poses with diminishing residual cyan core flicker, final dark core; hunched grounded corpse with wing peaks raised, same full size |

## Playback guidance for future authoring
- Idle, walk, beam-fire, global-stun and dead-hold may loop or hold selected poses.
- Basic and Wingbreaker attack strips include anticipation, contact and follow-through. They are references for separate windup/active/recovery playback, not five equally timed gameplay events.
- Volley one/three/five have separate charge/recoil draft variants. The projectile count is emitted by code.
- Beam-charge contains readable throat lighting and locked aim; beam-fire contains only the emission stance/muzzle glow. Beam-counter-stagger exposes the head and differs from global stun.
- Nova-charge exposes the reactor during the card-interaction window; nova-release is body recoil; nova-recovery closes the rib plates. An interrupted nova uses the global stun poses.
- Feint-cue visibly recoils/tucks the wing before delayed-claw. Do not hide the player's response cue.
- Hurt is a short nonlethal reaction. Death ends as a kneeling, bowed statue with upright folded wings; dead-hold settles with a dark reactor.
- No gameplay timings were baked into these PNGs or changed. Final timing stays in the runtime ScriptableObjects.

## Integration status
Stored entirely under gdd/art-references/, outside Assets/. No scenes, prefabs, ScriptableObjects, runtime code, animation bindings or import settings were changed for these drafts.

