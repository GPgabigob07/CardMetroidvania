# Card Time Chord And Neutral Handoff SDD - 20260612-1815

## Contexto

Playtesting found two usability problems:

- Input System modifier composites require shortcut-like timing and make the
  two-button Card Time command unnecessarily strict.
- attack Card Time states return to `Neutral` immediately when the action ends,
  bypassing the intended post-window grace.

This specification complements:

- `specs/card-time-input-leniency-sdd-20260612-1803.md`
- `specs/attack-chain-post-recovery-grace-sdd-20260612-1100.md`

## Rolling Chord

Use two ordinary Input System actions:

```text
CardTimeLeft:
    J
    Mouse 4
    Left Shoulder

CardTimeRight:
    K
    Mouse 5
    Right Shoulder
```

Gameplay combines them through a rolling chord:

- either side may be pressed first;
- the second side may arrive within `chordInputGraceDuration`;
- holding the first side remains valid while pressing the second;
- one chord produces one activation request;
- the chord rearms after both sides are released.

Initial value:

```text
chordInputGraceDuration = 0.20 unscaled seconds
```

## Neutral Handoff

When an animation publishes `Neutral` immediately after `Chain` or `Finisher`:

- preserve the previous attack Card Time state through
  `postWindowGraceDuration`;
- after grace, publish `Neutral`;
- repeated `Neutral` frames must not cancel or restart grace;
- transition from one attack Card Time directly to another remains immediate.

Initial leniency:

```text
inputBufferDuration = 0.25 unscaled seconds
postWindowGraceDuration = 0.50 unscaled seconds
```

These values intentionally align Card Time with the existing attack-chain
forgiveness rather than requiring frame-perfect input.
