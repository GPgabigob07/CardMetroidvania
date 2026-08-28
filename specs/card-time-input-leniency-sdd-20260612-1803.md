# Card Time Input Leniency SDD - 20260612-1803

## Contexto

This specification adds forgiving input around animation-authored Card Time
windows and centralizes prototype tuning.

It complements:

- `specs/card-time-and-training-dummy-review-slice-sdd-20260612-1701.md`
- `specs/attack-chain-buffer-and-speed-sdd-20260612-1022.md`
- `specs/attack-chain-post-recovery-grace-sdd-20260612-1100.md`

## Decision

Animation states remain the authority for the exact Card Time state and window
length. A shared `PlayerCardTimeConfigSO` defines interaction policy:

```text
maximumActiveDuration = 5.0 unscaled seconds
activeTimeScale = 0.10
inputBufferDuration = 0.15 unscaled seconds
postWindowGraceDuration = 0.15 unscaled seconds
```

## Early Input Buffer

If Card Time input is pressed shortly before an animation publishes a valid
window:

- store one pending activation request;
- consume it automatically when a window opens within
  `inputBufferDuration`;
- do not show invalid-input feedback while the request is buffered;
- expire it if no window opens in time;
- additional presses replace, but do not stack, the buffered request.

## Post-Window Grace

When an animation-authored window closes:

- preserve the last published Card Time state for
  `postWindowGraceDuration`;
- allow activation during that grace;
- do not reopen a window after its grace expires;
- do not apply grace to an already active session;
- committing or cancelling still requires the window to close before the same
  animation window can activate again.

## Input Result

Activation requests return one of:

```text
Activated
Buffered
Rejected
```

Only `Rejected` produces invalid-input feedback.

## Acceptance Criteria

- input immediately before a valid animation window activates when it opens;
- input too early expires without activation;
- input immediately after a window closes still activates the previous Card
  Time state;
- input after grace is rejected;
- animation-authored windows may have different lengths without duplicating
  leniency values;
- slowdown and active-session duration come from the same configuration asset.
