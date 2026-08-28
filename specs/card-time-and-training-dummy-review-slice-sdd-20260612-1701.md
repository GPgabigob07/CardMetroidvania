# Card Time And Training Dummy Review Slice SDD - 20260612-1701

## Contexto

This specification defines the gameplay slice required for the review scheduled
shortly after `21:00 UTC-3` on June 12, 2026.

It converts the existing Card Time projection into a visible, interactive
system and adds stationary targets for grounded and aerial combo testing. The
scope intentionally avoids final card selection UI, dedicated aerial attack
animations, enemy AI, and a fourth attack animation.

Sources:

- `.docs/GDD-TIC.md`
- `gdd/gdd-canonico-20260526-2331.md`
- `specs/damage-system-sdd-20260526-0102.md`
- `specs/player-movement-controller-sdd-20260604-2107.md`
- `specs/player-animation-state-projection-sdd-20260609-0009.md`
- `specs/attack-chain-buffer-and-speed-sdd-20260612-1022.md`
- `specs/attack-chain-post-recovery-grace-sdd-20260612-1100.md`
- `specs/unity-editor-collaboration-workflow-20260612-1609.md`

The original GDD proposes slowing gameplay to `10%` during Card Time. This
review slice keeps that value as a configurable starting point.

## Goals

The review build must demonstrate:

1. recognizable Card Time availability during the three-hit combo;
2. deliberate Card Time activation through a dedicated input;
3. slowed gameplay while the current attack animation continues;
4. Attack changing meaning from basic attack to Card Time commit;
5. grounded and aerial attacks connecting with visible targets;
6. a minimal card-like damage benefit with visible state;
7. stationary targets that regenerate instead of dying permanently.

## Non-Goals

The review slice does not include:

- deck building or hand management;
- choosing among multiple cards;
- final card art or card animation;
- a fourth basic attack or Zenith animation;
- hit-confirmed Card Time requirements;
- distinct aerial attack clips;
- enemy AI, attacks, navigation, stagger, or knockback;
- permanent balance values;
- pause-menu integration;
- stacking multiple independent time-scale effects.

## Terminology

### Availability Window

A period authored by the current attack animation during which a specific
`PlayerCardTimeState` may be activated.

### Active Session

The period after the player activates an available Card Time. During this
session the game is slowed, a placeholder card is selected, and Attack acts as
Commit.

### Commit

The player confirms the currently selected card by pressing Attack during an
active Card Time session. Commit consumes the Card Time interaction and returns
Attack to its normal combat meaning.

## Card Time States

The prototype continues using the canonical simplified states:

```text
None
Neutral
Chain
Finisher
```

Initial authoring intent:

| State | Initial availability |
| --- | --- |
| Neutral | Outside an attack action |
| Chain | Animation-authored window between or during Attack1 and Attack2 |
| Finisher | Animation-authored window associated with Attack3 recovery |

Attack animation states remain the authority for attack-time availability
through `PlayerActionAnimationStateBehaviour.cardTimeState`. Each animation
state may expose a different Card Time state and therefore a different window
length.

The implementation must not hardcode one shared duration for all Card Time
states. Window duration comes from how long the Animator publishes that state.

Hit confirmation is not required to open or preserve a window in this slice.
The window model must remain independent from hit reporting so a future policy
can require a confirmed hit without replacing the Card Time runtime.

## Runtime State Model

Use a small runtime with the following states:

```text
Unavailable
Available
Active
Committed
Cancelled
```

Expected flow:

```text
Animator publishes Card Time state
    -> Available

Card Time input while Available
    -> Active
    -> apply slowdown
    -> preselect placeholder card

Attack while Active
    -> Commit selected card
    -> restore normal time
    -> Committed

Active duration reaches limit
    -> discard selection
    -> restore normal time
    -> Cancelled

Window closes before activation
    -> Unavailable
```

Once Active, the session is allowed to outlive the original availability
window. This prevents the slowed attack animation from closing the interaction
before the player can commit.

Only one Card Time session may be active at a time.

## Slowdown Rules

Initial values:

```text
activeTimeScale = 0.10
maximumActiveDuration = 5.0 unscaled seconds
```

Rules:

- slowdown begins only after valid Card Time activation;
- availability alone never changes game speed;
- the current attack animation continues at the slowed game speed;
- physics and gameplay use the slowed time scale;
- HUD feedback and the Card Time countdown use unscaled time;
- Commit restores the previous game time scale immediately;
- timeout restores the previous game time scale and cancels the selection;
- disabling or destroying the runtime must restore the captured time scale;
- entering a second session while one is active is rejected;
- `Time.fixedDeltaTime` should scale proportionally while Card Time owns the
  slowdown and be restored afterward.

The runtime must capture and restore prior values instead of assuming normal
time is always `1.0`.

## Input

Add a dedicated `CardTime` button action to the existing Player action map.

Preferred bindings:

```text
Gamepad: Left Shoulder + Right Shoulder
Keyboard: J + K
Mouse: Mouse 4 + Mouse 5
```

Use Input System modifier composite bindings. Add mirrored composites where
needed so either member of the pair may be pressed first.

Fallback, only if composite behavior blocks the review build:

```text
Keyboard: K
Gamepad: Right Shoulder
```

Fallback bindings must remain on the dedicated `CardTime` action. Card Time
must not be read directly from device APIs inside gameplay code.

### Input Priority

During an active Card Time session:

1. Attack means Commit.
2. Attack must not buffer or start a basic attack.
3. Card Time input must not start another session.
4. Movement and the current action continue under slowdown.

Outside an active session, Attack keeps its existing combo behavior.

## Placeholder Card Effect

Card effects are the lowest implementation priority for the review.

If included, the review card is preselected automatically when Card Time
activates. No selection cursor or card list is required.

Effect:

```text
On Commit:
    enable Consecutive Hit Bonus

For each successful attack after activation:
    increment bonus by +1 damage

On an attack that hits no valid target:
    reset bonus to 0
```

Additional rules:

- one attack may damage the same target only once;
- multiple targets hit by one attack count as one successful attack for bonus
  progression;
- the current bonus applies once per target hit;
- the UI shows the active bonus value;
- timeout before Commit applies no effect;
- activation and Commit themselves do not require hit confirmation.

The bonus runtime should consume attack outcome events rather than inspect
colliders directly. This preserves the option to add hit-confirmed Card Time
rules later.

## Attack Hit Detection

Add a player attack hit detector that:

- is active only during the attack Execution phase;
- uses a configurable 2D overlap shape and target layer mask;
- positions the shape relative to player facing;
- supports grounded and airborne use without separate attack logic;
- tracks unique `IDamageable` targets for the current attack;
- creates damage transactions through the existing damage architecture;
- reports whether the completed attack hit at least one valid target;
- clears per-attack target memory when a new attack begins.

The first implementation may use the same hitbox shape and base damage for all
three attacks. Per-attack shapes and values remain future data work.

Provide a scene-view gizmo for the attack shape. Runtime hitbox visibility is a
debug option, not required final presentation.

## Training Dummy

The shared enemy structure and dummy specialization are defined by:

- `specs/enemy-actor-baseline-and-training-dummy-sdd-20260612-1715.md`

Create a reusable stationary training-dummy component/prefab with:

- a visible placeholder body;
- a `Collider2D` hurtbox;
- `SimpleHealth` or equivalent `IDamageable` behavior;
- configurable maximum health;
- no AI;
- no outgoing damage;
- no hit knockback;
- no gravity-driven movement;
- visible hit feedback;
- visible health feedback;
- automatic regeneration.

Regeneration rules:

```text
regenerationDelay = configurable unscaled or gameplay seconds
regenerationRate = configurable health per second
```

For the review, the dummy must not remain dead. Reaching zero health should
start or accelerate regeneration and keep the target available for continued
testing.

Place two instances in the review scene:

- one grounded dummy reachable by the normal combo;
- one immovable aerial dummy positioned for jump and aerial combo testing.

Both instances use the same runtime behavior; only transform and optional
presentation differ.

## Visual Feedback

Visual indication is the highest-priority Card Time deliverable.

The review HUD must communicate:

- current available state: `Neutral`, `Chain`, or `Finisher`;
- whether Card Time is merely available or currently active;
- active-session countdown;
- Commit prompt while active;
- timeout/cancel feedback;
- invalid activation feedback;
- consecutive-hit damage bonus when enabled;
- dummy health or damage response.

Suggested minimum presentation:

```text
Available:
    colored border or banner + state label

Active:
    stronger overlay/tint + countdown + "Attack: Commit"

Invalid:
    short red flash or "No Card Time" label

Committed:
    short confirmation pulse
```

Use unscaled time for HUD transitions so feedback remains readable at `10%`
game speed.

Color alone must not carry the entire meaning; state text or a distinct icon is
required.

## Integration Boundaries

Recommended responsibilities:

- `PlayerActionAnimationStateBehaviour`: publishes animation-authored Card Time
  availability.
- `AttackAction`: exposes the current published state without owning slowdown
  or card selection.
- `PlayerCardTimeRuntime`: owns availability, activation, session timeout,
  commit, cancel, and state-change notifications.
- `PlayerController`: reads the dedicated action and routes Attack according to
  Card Time input priority.
- `CardTimeSlowdownController`: captures and restores time settings.
- `PlayerAttackHitDetector2D`: detects targets and reports attack outcomes.
- placeholder card runtime: owns the consecutive-hit bonus.
- HUD presenter: observes runtime state and renders feedback.
- training dummy: owns health regeneration and hit presentation.

Exact class names may vary to follow surrounding code, but time-scale,
hit-detection, card-effect, and UI responsibilities should not be collapsed
into `PlayerController`.

## Review Acceptance Criteria

### Card Time

- Card Time availability is visible during authored attack windows.
- Different attack states can expose windows with different lengths.
- Activating outside a valid window gives immediate feedback and does not slow
  the game.
- Valid activation slows gameplay to the configured value.
- The current attack animation continues while slowed.
- The session lasts no longer than five unscaled seconds.
- Pressing Attack while active commits and exits slowdown.
- The same Attack press does not start or buffer another attack.
- Timeout cancels and restores normal speed.

### Combat Targets

- Every basic attack can damage the grounded dummy.
- The same attack cannot damage one dummy multiple times.
- Aerial attacks can damage the floating dummy.
- Dummies remain stationary.
- Damage is visibly communicated.
- Health regenerates so testing can continue without scene reload.

### Placeholder Effect

- Commit visibly enables the consecutive-hit bonus.
- Successful attacks increase the bonus.
- An attack that hits no valid target resets the bonus.
- Current bonus is visible.

## Tests

### EditMode

- activating from `Unavailable` is rejected;
- activating from each available state starts one session;
- active session times out using unscaled duration;
- Commit completes the session;
- Commit has priority over basic Attack routing;
- disabling slowdown ownership restores captured time settings;
- one attack outcome increments the bonus once;
- a missed attack resets the bonus;
- duplicate target hits in one attack are ignored;
- hit-confirm policy remains optional and disabled.

### PlayMode Or Manual

- Animator-authored windows reach the runtime;
- HUD updates remain responsive during slowdown;
- both input devices available for review can activate Card Time;
- attack animation continues at slowed speed;
- grounded and aerial dummy placement supports the intended combos;
- health regeneration and feedback remain visible after repeated hits.

## Delivery Priority

If time becomes constrained, complete in this order:

1. attack hit detection;
2. grounded and aerial regenerating dummies;
3. Card Time availability visuals;
4. Card Time activation and slowdown;
5. Attack-as-Commit routing;
6. chord input polish and mirrored bindings;
7. placeholder consecutive-hit damage bonus;
8. additional visual polish and automated coverage.
