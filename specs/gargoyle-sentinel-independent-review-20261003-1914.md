# Final independent review — Gargoyle Sentinel / Ward

Reviewed feature range `2015639c3f09099ff8fe723442acad328fe787b0..0b8b05b07354fcae9662b2d458b5ee7b121c4392`. The later quota checkpoint contains no feature changes. Sources: both approved 1205 plans, both 1128 SDDs, both execution ledgers, review package, 1655 verification report, runtime/Editor source, generated tuning/attack/presentation assets and selected regression/PlayMode fixtures. No Unity runs or code edits were performed. This is the same review resumed after the quota interruption, not a second review.

## Strengths

- Attack snapshots, mandatory first physics sampling, release reservations and canonical target budgets address the large-delta and callback-reentrancy risks explicitly.
- Incoming region selection prevents root health bypass, preserves damage provenance and deduplicates primary strike identity; health-first handling and pending interrupt priority give death precedence over stun/core reactions.
- Ward reserves before deferred payment notifications, checks prepared costs/configuration, locks facing, and shares the existing player lifecycle reset paths. Projectile ownership distinguishes normal completion from interruption cleanup.
- The verification report accurately distinguishes automated evidence (565 passing EditMode tests, 20 exact inherited failures, 3 skips; 9/9 PlayMode) from pending human/art acceptance. Separate inventory authoring and preservation of existing tuning on setup are useful scope controls.

## Critical findings

None identified.

## Important findings

1. **Feint probability is repeated from the same random sample on every full pass.** `Assets/Scrips/Architecture/Enemy/Gargoyle/GargoyleBrain.cs:309` reconstructs `feintRandom` from the unchanged seed whenever an exhausted family bag refills; line 325 then takes its first sample. With the authored seed 0 and probability 0.2 (`Assets/Data/Enemies/GargoyleSentinel/GargoyleTuning.asset:43` and `:54`), uninterrupted passes repeatedly decline the feint; seeds whose first sample qualifies instead feint every pass. This defeats the intended per-pass probability and makes the normal authored encounter omit the feint. Preserve the independent RNG across refills; reseed only for an actual seed change at the permitted boundary or explicit reset. Add a multi-pass test using a nontrivial probability, rather than only probability 0/1.

2. **The beam cannot discover an intersecting Ward unless the player's health collider also intersects the beam.** `Assets/Scrips/Architecture/Enemy/Gargoyle/EnemyBeamAttack2D.cs:71` derives all candidates from the beam capsule's player colliders, then queries guards only on those candidates at line 78. Ward has no query collider. For a concrete supported live edit, enlarge Ward height to 3, put the player's 1x2 body at (4, 1.3), and fire a horizontal 0.3-thick beam from (0, 0), with Ward facing left: the guard crosses the beam while the body misses it, so `TryIntercept` is never called and the opening cannot counter. An angled or endpoint interception can produce the same omission. Discover eligible guards independently of body-hit candidates (for this solo composition, the canonical bound player is sufficient), evaluate the clipped geometry, then resolve health candidates. Cover this through `EnemyBeamAttack2D.Sample`, not only direct Ward geometry tests.

3. **The authored melee steps omit their specified spatial distinctions.** `Assets/Scrips/Architecture/Editor/GargoylePrefabSetup.cs:52` authors different timings but leaves all melee payload shapes at their common defaults. In `Assets/Data/Enemies/GargoyleSentinel/Attack_Basic.asset:24`, `:45`, and `:66`, sweep, wider backhand and downward slam are the same 1.5x1 box at (1,1), angle 0. `Attack_Wingbreaker.asset:24` and `:45` likewise use that same box for wing hit and upward claw; only the first step advances. Consequently the promised wider/upward/downward spatial responses are absent even before artist animations are considered. Author the distinct sizes/offsets/angles required by the approved attack descriptions and retain tests that compare those actual shapes; exact balance numbers can remain subject to human tuning.

## Minor findings

1. **Live melee angle edits make the telegraph disagree with the damage query.** `Assets/Scrips/Architecture/Enemy/Gargoyle/GargoyleAnimationPresenter.cs:73` draws an axis-aligned box, while `EnemyMeleeAttack2D.Sample` rotates the query by `HitboxAngle * facing`. Any nonzero authored/live angle shows a different footprint from the hit area. Rotate cue corners using the same committed facing/angle. The shipped zero-angle assets mask this problem; it becomes relevant immediately when authoring the distinct melee geometry above.

## Focused risk assessment

The review covered all ten package focus conditions in the runtime paths: live timer preservation; lethal/poise priority; first physics sample and single release; region/provenance/cast deduplication; preserved partial bags and owned-offense cleanup; reentrant Ward payment; stale quotes; clipped directional geometry; repeated cast protection; reset/hold integration. Finding 2 is an integration gap beyond the direct geometry tests. Existing test results are evidence reported by the implementer, not fresh runs by this reviewer. The inspected PlayMode tests demonstrate lifecycle/phase execution, not an end-to-end player-input core/Chain or Ward acceptance sequence.

## Declined to judge / remaining acceptance

- Artist animations and body-only occupied-area ratio: explicitly pending, and neither collider dimensions nor the whole-alpha ratio proves the body-mask requirement.
- Keyboard/gamepad selection usability, actual two-primary-hit Chain/core feasibility, zero-energy movement escapes and combat feel: require the agreed genuine PlayMode observations; the inspected automation does not establish them.
- Exact final balance values for distinct melee footprints: the specification establishes the distinctions but leaves final tuning to acceptance. Finding 3 concerns their absence, not a preferred numeric balance.
- The 20 inherited EditMode failures: no new failure identities are reported; repairing the baseline is outside this feature review. Their existence still prevents describing the entire suite as passing.

No other suspected behavior was silently waived as outside scope. This was a focused whole-feature source review, not exhaustive line-by-line certification of every generated serialization field or every test assertion.

## Readiness

**Ready to merge: with fixes; not ready for final feature acceptance.** Resolve the three important findings and the related cue mismatch, verify focused regressions plus the required baseline comparison, and retain the explicit human/art acceptance gate. The architecture provides a sound baseline, but the current authored encounter and guard discovery have concrete behavioral gaps.
