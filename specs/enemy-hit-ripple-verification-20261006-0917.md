# Enemy Hit Ripple Verification - 20261006-0917

## Contexto

Implementation of the user-approved design and plan:

- `specs/enemy-hit-ripple-assessment-20261006-0832.md`
- `specs/enemy-hit-ripple-sdd-20261006-0840.md`
- `specs/enemy-hit-ripple-implementation-plan-20261006-0850.md`

Native execution in the shared checkout on `codex/next-iteration-20261006`.
No commit, push, merge, Unity upgrade or additional dependency was performed.
Earlier design/planning versions are preserved as historical records.

## Implemented behavior

- Full-circle ripple centered on each target's own reported hit point, clipped
  to existing sprite alpha; source-pixel grid evaluation with 4-pixel radial width.
- Blue accepted damage, near-white gameplay rejection, fatal blue leading/red
  trailing edges; dead/invalid/zero/duplicate transactions produce no ripple.
- Three independent wave slots per enemy; a fourth replaces the oldest.
- Default 0.25-second traversal across captured visual bounds; independent waves
  and bounded overlap blending, newest winning equal-strength ties.
- Unscaled progression through hitstop and Card Time; explicit menu-pause freeze.
- Owner-local origins, child visual transforms, flips/pivots/frame metadata,
  explicit reset and disable clearing; defeat alone does not clear the wave.
- Shared URP 2D lit material with per-renderer property blocks; no per-hit materials.
- Root and child-recipient adapters notify once after damage resolution without
  duplicating EnemyHealth.Damaged. One request/report remains the unit of combat
  resolution; modifiers, card consumption, supplemental damage, hitstop and hit
  confirmation retain their existing semantics.
- Golem, bat and gargoyle prefabs are wired. Existing whole-body hit flashes are
  suppressed where ripple feedback takes over; state/attack tints remain.
- SampleScene's generated training dummies are wired through two serialized
  bootstrap references. Their unit-square geometry is preserved with a procedural
  32-pixel source texture suitable for reviewing a four-pixel band.
- Idempotent menu: `TicGame > Feedback > Setup Enemy Hit Ripples`.

## Verification evidence

Unity 6000.3.16f1 with the installed URP 17.3.0. Tests ran in
`Temp/HitRippleValidation` while the user's Editor stayed open on the main project.
Unity needed elevated access to its cache/licensing services; the sandboxed attempt
failed before any tests ran. Validation did not compete for the open project.

- Missing outcome/contact/runtime behavior observed red before implementation.
- Missing presenter observed red in three routing/lifecycle/property tests.
- Shader and setup existence checks observed red before their implementation.
- Initial focused integration: 47 passed, zero failed.
- Final full EditMode: **657 total, 634 passed, 20 failed, 3 skipped**.
- Fresh committed HEAD baseline in `Temp/HitRippleBaseline`: **638 total,
  615 passed, 20 failed, 3 skipped**. Exact failed-test identity comparison has
  **zero differences**; no new failures. All **17 ripple EditMode tests** pass,
  along with the new actual melee-contact and zero-damage golem regressions.
- Final graphics-enabled PlayMode: **16 passed, zero failed**, including three
  actual GPU ripple tests plus existing gargoyle encounter/projectile physics.
- GPU checks read rendered pixels: full circle in horizontal/vertical directions,
  ordinary blue and rejected white, fatal edge colors, untouched center pixels,
  transparent holes inside the band, bounded three-wave overlap and newest ties.
- All runtime/Editor/test assemblies compiled in Unity. Shader imported and
  executed without shader errors in graphics-enabled tests.
- `git diff --check` passes. Copied prefab/material/profile GUID references resolve
  to project assets or the retained URP/built-in resources.

Independent review found one Important setup issue: the inactive bat projectile
template was included by a default inactive-excluding ancestor query. A regression
reproduced two bound visuals where only VisualRoot belongs to the enemy. The fix
uses inactive-aware ancestor queries, excludes the template, and restores its
original material. The regression then passed, followed by the full EditMode and
16-test PlayMode runs above. No other Critical/Important findings were reported.

Results and logs: `Temp/HitRipple/`; execution ledger: `Temp/HitRipple/progress.md`.
Only the intended Unity-authored assets/prefabs and the two SampleScene bootstrap
references were copied back, with matching new .meta files. Unrelated test-generated
assets were excluded.

## Implementation rulings

- Existing feature checkout retained to keep the result in the user's active
  Unity project. Cost: changes share this checkout rather than a new worktree.
- Windows-native validation/ledger replaces bash-only skill helpers. Cost: scratch
  logs are local; this versioned note preserves the result.
- Shader receives an inverse visual transform and visual-local band width to
  compensate radial pixel thickness for child scaling. Cost if incorrect: visual
  width distortion; matrix/PPU behavior is covered by tests.
- Dynamic batching disabled for this shader because object-local pixel geometry
  must remain intact; property blocks also have an SRP Batcher tradeoff. Cost:
  extra render submissions, including idle enemies, pending representative profiling.
- Dummy source texture uses 32 pixels rather than one while keeping its unit
  geometry. Cost: a small owned runtime texture/sprite allocation, released on teardown.
- Existing baseline failures preserved rather than expanding this feature into
  unrelated combat/UI/respawn fixes. Cost: the repository-wide suite remains non-green.

## Playtest handoff and deferred validation

1. Focus Unity and let it import/recompile the changes.
2. Open `Assets/Scenes/Test_GolemCharger.unity` and enter Play Mode. Hit its body
   for blue, hit during an uninterruptible charge for white, and kill it for
   blue/red. Rapid hits should retain up to three visible rings.
3. Repeat in `Test_BatMachine.unity` and `Test_GargoyleSentinel.unity` for animated
   enemies; use `SampleScene.unity` for the generated training dummies.
4. Tune `Assets/Data/Feedback/HitRippleProfile.asset` in the Inspector:
   Width Pixels, Traversal Seconds, outcome colors, Strength and Fade Out Fraction.
5. If feedback is not wired after another enemy setup command rebuilds a prefab,
   rerun `TicGame > Feedback > Setup Enemy Hit Ripples`, then save the intended assets.

Visual timing/readability acceptance on the actual enemy art remains the user's
Play Mode check. Uploaded flip/pivot/negative-scale parameters are tested, but
expanded GPU coverage for asymmetric pivots, frame swaps and two independently
rendered enemies sharing a material is deferred as a minor review suggestion.
Representative target-device draw-call/performance measurements are also deferred.

Additional outcome ideas remain in the original assessment; no extra palette
meanings were added in this implementation.
