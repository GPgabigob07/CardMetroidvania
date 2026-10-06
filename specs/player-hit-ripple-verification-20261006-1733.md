# Player damage ripple — 2026-10-06

## Context
Extends the enemy ripple and the diagnostic work recorded in `hit-ripple-lab-and-contact-fix-20261006-1232.md` to player damage, as requested. Player body uses the existing circular sprite shader with a separate pure-red profile.

## Implementation
- `PlayerHitRipplePresenter` subscribes to accepted positive finite `SimpleHealth.Damaged` notifications. Direct and resolver damage produce one ripple; healing, nonlethal health costs, zero damage, and blocked hits do not.
- Shared `HitRippleVisualRuntime` retains contact-point coordinates, four-source-pixel width, 0.65-second travel, three concurrent slots, alpha clipping, and unscaled progression with explicit pause support.
- `PlayerHitRippleProfile.asset` uses red for ordinary and fatal bands. Player prefab Animation sprite uses the ripple material; accessory sprites are excluded.
- Reset/disable clears active waves. Damage is published before existing health/death callbacks so immediate respawn cannot leave a ripple at the new position. Existing immediate respawn also means the lethal band may be cleared before it becomes visible.
- Idempotent Editor command: `TicGame/Feedback/Setup Player Hit Ripples`.

## Verification
Isolated Unity 6000.3.16f1 project, current source/assets copied before testing.
- Initial four player tests failed because the presenter was missing.
- Initial focused EditMode suite: 22/22 passed.
- Final expanded EditMode suite: 109/110 passed, including five player checks, prefab wiring, enemy ripple regressions, health, recovery and Ward checks.
- Remaining failure: `PlayerMovementControllerTests.PlayerDeathRespawn_ResetsPositionVelocityAndHealth_WhenHealthReachesZero`. Reproduced with the new damage/reset notifications disabled; unrelated to this extension.
- Graphics PlayMode suite: 5/5 passed, including pure-red player band on a blue sprite and existing enemy color/alpha/off-center checks.
- Independent review found no important regressions; removed unused extraction field.

Detailed test XML/logs are in ignored `local-artifacts/PlayerRippleWork/`.
