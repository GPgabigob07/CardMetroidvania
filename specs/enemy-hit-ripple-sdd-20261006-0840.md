# Enemy Hit Ripple SDD - 20261006-0840

## Contexto

This design follows `specs/enemy-hit-ripple-assessment-20261006-0832.md`.
The user confirmed the assessment's direction and explicitly selected a full
circle. This version turns the assessment into a reviewable design; it does
not claim approval of the written specification or authorize implementation.
The assessment remains historical memory.

Additional sources: `specs/damage-system-sdd-20260526-0102.md`,
`specs/code-conventions-20260526-0014.md`,
`specs/testing-conventions-20260526-0122.md`, and
`specs/unity-editor-collaboration-workflow-20260612-1609.md`.

## Player-facing behavior

Each eligible resolved hit starts a full circular wave centered on the reported
impact. Its radius increases outward in all directions. The effect brightens
only the enemy sprite's existing visible pixels and preserves transparency.
The impact may be inside the body; no directional clipping is applied.
No particle texture, sound, external art, bloom, or scene light is required.

- Accepted positive health damage: blue ripple.
- Gameplay rejection on a living enemy (such as an armored charge): near-white.
- Accepted positive health damage that kills: blue/red ripple.
- Dead-target, invalid, zero-damage, and duplicate-execution rejection: no ripple.

Use approximately 3-4 source-art pixels of radial thickness, independent of camera
zoom. This does not constrain the horizontal number of highlighted pixels on
every row. Start with a configurable width of 4 source pixels.
Three waves may remain active simultaneously on each enemy, each with independent
origin, radius, lifetime, and palette.

## Proposed defaults for review

These choices are proposals rather than separately confirmed user decisions:

- Fatal palette: blue outer/leading edge and red inner/trailing edge.
- Traversal: target approximately 0.25 seconds to cross the impact-time visual
  bounds; calculate speed once per hit and retain it during animation changes.
- Overflow: a fourth wave replaces the oldest active wave, including for fatal
  hits. Other active waves continue without restart.
- Presentation progresses with unscaled delta time through hitstop and Card Time,
  and freezes during the explicit playtest pause menu.
- One wave per eligible resolved damage transaction. Linked supplemental damage
  remains a separate wave in the baseline; one physical strike may therefore
  produce multiple waves. Its existing proc rules remain unchanged.
- Overlap chooses the strongest highlight, using the newest wave to break ties,
  instead of summing colors into an increasingly white highlight.

## Runtime structure

`EnemyHitRipplePresenter` belongs to the enemy owner and manages an explicitly
configured list of body SpriteRenderers. Exclude telegraphs, projectiles, and
other unrelated child renderers. One bounded runtime state stores three waves
per owner. Each configured renderer displays the same owner-space wave, so an
enemy composed of multiple sprites receives one coherent circle.

`HitRippleProfileSO` contains width, target traversal duration, blue/white/fatal
colors, highlight strength, and optional fade-out tuning. Each concrete
MonoBehaviour/ScriptableObject uses its own matching source filename and the
repository's Inspector conventions. Validate finite positive tuning values.

Use a text-authored URP 2D sprite shader derived from the installed package's
sprite rendering behavior. Preserve ordinary tint, alpha, sorting and existing
lighting behavior outside the highlighted band; determine the current enemy
material's lit/unlit baseline during implementation before selecting the pass.
The highlighted band should remain readable in dark lighting.

Use a shared material with bounded per-renderer wave parameters, initially via
reused MaterialPropertyBlocks. Read and preserve unrelated renderer properties;
do not allocate a new material for every hit or mutate shared wave parameters.
Property blocks have an SRP Batcher tradeoff: profile the actual enemy population.

## Damage integration

Use the existing post-resolution `IDamageListener.OnDamageReceived` callback.
A root target adapter or explicit child-region relay forwards the result to
the owning presenter. Do not subscribe simultaneously to EnemyHealth.Damaged
for the same effect, which would produce duplicate accepted-hit ripples.
Keep shader, material, and palette knowledge outside DamageResolver.

Add backwards-compatible rejection metadata to DamageResult so presentation can
distinguish gameplay blocks from suppressed/non-actionable transactions. Update
the relevant enemy policy return paths explicitly. Existing constructors remain
usable; an unspecified rejection does not automatically produce a white wave.
Accepted fatal classification requires Accepted && AppliedAmount > 0 && Killed.

Extend DamageRequest with optional per-target contact data, keeping the existing
request hit point as fallback. The melee detector computes a contact for each
selected recipient and the resolver uses it when constructing that target's
DamageContext. Retain a single request/report and existing target ordering,
modifier evaluation, consumption, hit confirmation, and hitstop semantics.
Other sources may continue using their existing contact fallback.

Contact is the collider's ClosestPoint approximation, not an opaque-pixel search.
Never interpret a valid world origin (0,0) as missing contact data.

## Coordinates, animation, and lifetime

Store wave origins in enemy-owner local space so they follow translation and
rotation. Supply the visual-to-owner transform for each renderer. Quantize spatial
evaluation to its source sprite pixel grid using pixels-per-unit and pivot/frame
metadata, rather than treating packed atlas UVs as circular geometry.
SpriteRenderer flips change texture appearance without moving the established
physical wave origin; negative Transform scale must be handled separately.

Compute the circular mask from distance to origin and animated radius. At birth,
the leading radius begins at zero; the trailing radius is clamped at zero, giving
a small initial disk before the expanding ring develops. Preserve sampled alpha.
Determine wave travel distance from the furthest impact-time visual bounds corner
plus band thickness; expire once the band has crossed that extent and faded.
Update frame metadata when animation swaps sprites. Newly exposed pixels outside
the captured travel extent need not prolong the wave.

Reset waves on disable, reuse, or explicit enemy reset. Death state alone must
not disable the presenter: fatal waves continue while the death sprite is visible.
Avoid adding a separate corpse lifetime system; if a prefab currently hides too
early, report that integration gap before changing enemy lifetime semantics.

Disable overlapping whole-body hit flashes for configured ripple enemies while
preserving attack/state tints and animations. Existing debug/HUD health feedback
continues operating. Destroying/clearing this presentation must never affect damage.

## Editor setup and verification

Add an idempotent Editor setup command to create/reuse shader material and profile
assets and wire the intended enemy visuals and recipient relays. Follow the existing
Unity collaboration workflow. Use the training dummy for basic color/timing checks,
then golem, bat, and gargoyle for production integration.

Automated checks use explicit initialization and verify outcome classification,
root/child routing, no duplicate notification, three-wave capacity, overflow,
independent progression, pause/reset, and per-target contacts. Regression checks
protect damage totals, multi-target card consumption, supplemental transactions,
region priority, hit confirmation, and hitstop behavior.

Unity must compile the shader with the installed Renderer2D. Play Mode validation
covers opacity, 4-source-pixel radial width, three overlapping circles, per-enemy
origins on a multi-target swing, frames/pivots/flips/scales, existing tint/lighting,
fatal death visibility, hitstop/Card Time/pause, and representative performance.
C# compilation alone cannot verify the shader's appearance.

## Deferred conditions

Preserve the assessment's list: reduced armor damage, weak points, poise break,
interrupts, criticals, card enhancement, gargoyle core counters, and projectile
reflection/ward interception. No additional palette meanings are introduced here.

## Review status

Full-circle geometry is confirmed. The written design, proposed timing/palette/
overlap/transaction defaults, and implementation plan remain to be reviewed.
