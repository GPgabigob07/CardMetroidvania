# Aerial Hit Confirm And Hitstop SDD - 20260612-1753

## Contexto

This specification refines the review combat slice after the first attack
hit-detection integration.

It complements:

- `specs/player-movement-controller-sdd-20260604-2107.md`
- `specs/damage-system-sdd-20260526-0102.md`
- `specs/card-time-and-training-dummy-review-slice-sdd-20260612-1701.md`

## Aerial Attack Movement

Grounded and aerial attacks no longer share forward movement behavior.

Rules:

- grounded Execution may continue using its configured horizontal nudge;
- aerial attacks never add `AirborneExecutionNudge`;
- starting an aerial attack does not change gravity or minimum vertical speed;
- after the current aerial attack confirms at least one accepted damage result,
  Execution may apply:
  - `AirborneExecutionGravityMultiplier`;
  - `AirborneExecutionMinLift`;
- confirmation belongs to the current attack action and resets when the next
  attack begins;
- one confirmed target is sufficient, regardless of the number of colliders or
  targets touched afterward;
- rejected or zero-damage results do not confirm the aerial attack.

The hit detector reports confirmation through a small attack-outcome contract.
`AttackAction` must not query colliders or enemy components.

## Hitstop

`DamageResult` exposes the requested frame-stop duration:

```text
HitStopSeconds
```

Rules:

- accepted damage defaults to `0.1` seconds;
- a `DamageProfileSO` overrides that default;
- `0` explicitly disables hitstop for that profile;
- rejected or zero-damage results request `0`;
- multi-target attacks use the largest requested duration among accepted
  results;
- hitstop duration uses unscaled real time;
- hitstop captures and restores the previous time settings so it can begin
  while gameplay is already slowed by Card Time.

The review implementation may use a global time freeze. A future unified
time-effect service should arbitrate overlapping pause, Card Time, hitstop, and
cinematic effects if those systems begin to overlap frequently.

## Acceptance Criteria

- an aerial attack that misses preserves ordinary airborne gravity and
  horizontal velocity;
- an aerial attack that hits applies configured gravity reduction/minimum lift;
- no aerial attack receives the grounded forward nudge;
- accepted default damage reports `0.1` seconds of hitstop;
- a profile configured to `0` produces no hitstop;
- hitstop restores the time scale that existed before the hit.
