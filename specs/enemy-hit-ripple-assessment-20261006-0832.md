# Enemy Hit Ripple Assessment - 20261006-0832

## Contexto

Repository assessment requested before implementing enemy hit feedback. There
are no authored SFX/VFX assets for this feature. This document preserves the
requested behavior, implementation gaps, and questions for later design work.
It is an assessment and proposal, not an approved implementation specification.
No gameplay, shaders, materials, scenes, or prefabs were changed.

Sources inspected:

- `gdd/gdd-canonico-20260526-2331.md`
- `gdd/acompanhamento-build-novembro-20261004-1134.md`
- `specs/damage-system-sdd-20260526-0102.md`
- `specs/enemy-actor-baseline-and-training-dummy-sdd-20260612-1715.md`
- `specs/card-effect-feedback-20260625-0929.md`
- `specs/persistent-gameplay-services-and-card-time-ownership-sdd-20260614-1107.md`
- `specs/unity-editor-collaboration-workflow-20260612-1609.md`
- Current damage, enemy, melee hit detection, render settings, and enemy prefabs.

## Requested outcome

An expanding circular highlight begins at the hit point and crosses the enemy
sprite toward its opposite side. Only existing visible sprite pixels light up.
The requested band is approximately 3-4 pixels wide, with 2-3 simultaneous waves
on the same enemy. Damage uses blue, rejection uses near-white, and a fatal hit
uses blue/red. No new image assets are necessary for a procedural band.

Interpretation still to confirm: expanding ring versus a directional curved
front. A ring centered on a boundary naturally crosses the body; an interior
origin expands in every direction unless clipped by hit direction.

## Existing support

- Unity 6000.3.16f1 and URP 17.3.0 are installed. All serialized quality tiers
  reference UniversalRP, which references Renderer2D.
- Enemy visuals use SpriteRenderers. No custom shader or shader graph was found
  under Assets during this assessment.
- DamageContext already contains hit point, direction, source, target, tags,
  attack execution identity, and provenance.
- DamageResult exposes Accepted, Killed, AppliedAmount, and RemainingHealth.
- DamageResolver notifies target-local IDamageListener implementations for
  accepted and rejected transactions, and supports an optional IDamageEventSink.
- EnemyHealth and EnemyActor publish accepted damage and defeat events.
- EnemyDamageRegionSelection groups hurtboxes by owner and prioritizes regions.
- Existing Editor setup tools provide a pattern for deterministic prefab wiring.

## Integration gaps

### Rejected hits and child recipients

EnemyHealth returns early for rejected damage, so health events cannot drive
all three colors. The resolver's listener lookup checks only the target object,
not its parent. Gargoyle and golem region recipients can be children of the enemy.
A root presenter implementing IDamageListener alone therefore misses these hits.
Use an explicit region relay to the owner's presenter, or a shared post-resolution
notification routed to the owner. Keep shader knowledge out of DamageResolver.
The optional event sink is an alternative, but existing melee calls omit it;
every relevant caller would need consistent wiring.

### Per-target impact location

PlayerAttackHitDetector2D computes ClosestPoint only for the first new target;
DamageRequest then shares that point across all targets. Multi-enemy strikes
need per-target contact data to anchor each ripple correctly. Preserve one
damage request's existing modifier/proc/consumption semantics; splitting it into
independent requests just for VFX could change combat behavior.
Collider contact is an approximation, not a guaranteed opaque sprite pixel.

### Outcome classification

Fatal feedback requires Accepted && AppliedAmount > 0 && Killed. Some rejected
transactions against defeated enemies return Killed=true, which must not trigger
another fatal ripple. Dead targets, duplicate execution suppression, invalid
targets, and zero-damage transactions should not automatically look like armor
blocks. DamageResult currently has no rejection-reason field.

### Existing presentation and timing

EnemyDebugPresentation and GargoyleAnimationPresenter already use whole-sprite
hit flashes; EnemyContactAttack2D also changes tint for attack cues. Replace or
disable overlapping hit flashes while preserving state/attack cues. Ripples need
to survive sprite-frame changes, flips, movement, child offsets, and death state
transitions. Verify that fatal presentation remains visible for the wave duration.
Clear active waves on disable, reuse, and reset.

Presentation time is a deliberate choice: propose unscaled progression during
hitstop/Card Time, with an explicit pause-menu freeze. Avoid global shader time
as the sole clock if individual pause behavior is needed.

## Proposed baseline

1. One reusable sprite shader and material, preserving sprite alpha, tint,
   sorting, and the chosen lit/unlit behavior.
2. One EnemyHitRipplePresenter per enemy owner, controlling its visual renderers.
3. One HitRippleProfileSO with outcome colors, speed/duration, source-pixel width,
   intensity, and blend policy. Concrete Unity classes each get a matching file.
4. Three bounded wave slots per enemy: independent origin, age/radius, color,
   and lifetime. Proposed overflow: replace the oldest; fatal hits always get a slot.
5. A small explicit damage-result routing adapter for child hurtbox recipients.
6. An idempotent Editor command for material/profile creation and prefab setup.

Evaluate each fragment's distance from each wave origin and highlight fragments
within the moving band. Quantize spatial evaluation to the source sprite pixel
grid and preserve alpha. Use renderer-local coordinates rather than raw atlas UV
distance, with pixels-per-unit conversion, to avoid frame packing and aspect
ratio distortion. Account explicitly for SpriteRenderer flips and animated
frames with different pivots or pixel densities.

Width distinction: 3-4 pixels of radial thickness does not guarantee 3-4 lit
pixels on every horizontal row. Circle tangencies create longer horizontal
segments. Literal per-row width requires a different mask/curve definition.
Source-art pixels and screen pixels are also different at varying zoom/scales.

Use bounded blending so overlapping waves do not wash the entire enemy white.
Proposed fatal palette: blue leading edge with red trailing edge; this is not
yet a confirmed interpretation of blue/red.

MaterialPropertyBlock is a candidate for per-renderer wave parameters, avoiding
shared-material mutation and per-hit material creation. Unity documents that
property blocks are incompatible with the SRP Batcher; profile actual rendering
rather than assuming batching or performance benefits.
Reference: https://docs.unity3d.com/6000.3/Documentation/ScriptReference/MaterialPropertyBlock.html

## Alternatives

- Recommended: procedural band integrated into the enemy sprite shader. No
  additional sprite overlay; requires careful preservation of base rendering.
- Separate overlay SpriteRenderer: preserves the existing base material but
  duplicates drawing and must synchronize frames, flips, sorting, and masking.
- Shader Graph with a custom function: feasible and visually editable, but adds
  graph serialization/authoring overhead for bounded multi-wave logic. A text
  shader is easier to review and maintain in this repository's current workflow.

## Conditions to revisit later

- Reduced armor damage: golem idle multiplier versus a fully rejected charge hit.
- Weak-point damage: interrupted golem head and exposed gargoyle head.
- Poise depletion/stagger and attack interruption, distinct from health damage.
- Card-enhanced primary hits and linked supplemental damage: decide whether one
  physical hit should create one wave or multiple mechanical-damage waves.
- Gargoyle core counter success, distinct from ordinary body damage.
- Critical hits: formula support exists, but presentation should follow confirmed
  runtime outcomes rather than assume a critical gameplay feature is active.
- Block/invulnerability versus dead-target, invalid, zero, or duplicate rejection.
- Projectile reflection/ward interception: contact feedback without enemy damage.

Do not add colors for these until their player-facing meaning is agreed.

## Verification needed when implemented

Pure/runtime checks: classification, one wave per intended hit, region routing,
three-wave capacity, overflow, independent expiration, reset, per-target contacts,
and no changes to damage/proc/consumption behavior.

Unity checks: shader compilation with the installed Renderer2D; blue/white/fatal
appearance; transparency and any active SpriteMasks; animation/flips/scales;
3-4 source-pixel width; three stacked waves; lighting/tint behavior; hitstop,
Card Time and pause; death visibility; target-device profiling.

No shader feasibility probe or Play Mode visual validation was performed in this
assessment. Next design question: source-pixel radial band versus literal row
width, and full ring versus directional front.
