# Neutral And Chain Card Baseline SDD - 20260921-1107

## Contexto

This specification records the first five intended player cards after the
persistent Gameplay scene and streamed area setup. The prototype currently has
five demonstration cards and a working Card Time selection flow, but those
effects do not implement this intended set. The author confirmed that
"stage 1" means Neutral Card Time and "stage 2" means Chain Card Time;
Finisher remains unchanged for this slice.

Sources used:

- `gdd/gdd-canonico-20260526-2331.md`
- `specs/card-effects-and-energy-planning-20260614-1239.md`
- `specs/composable-card-effects-and-gated-ability-bridge-sdd-20260614-1841.md`
- `specs/card-inventory-selection-handshake-sdd-20260622-2309.md`
- `specs/bat-machine-enemy-sdd-20260901-1202.md`
- `specs/unity-editor-collaboration-workflow-20260612-1609.md`
- current card, movement, combat, damage, enemy, and Editor code under
  `Assets/Scrips/Architecture`
- author decisions in the card-design conversation on 2026-09-21

This document defines the new card slice. It preserves the older specs and
demonstration assets as project history; it does not adopt the bat enemy's
unimplemented movement, projectile, or fall mechanics.

## Goal And Scope

The player can choose three Neutral cards and two Chain cards through the
existing Card Time selection flow. Each card spends Energy atomically when
committed, produces visible state feedback, and changes actual movement or
combat behavior. The card system keeps the world-owned Card Time session and
player-owned resource/effect execution split. Existing Finisher cards continue
to work.

Implement the minimum reusable effect vocabulary and small player ability
runtimes needed for these five cards. Do not add one effect class or a branch
in the player controller for each card. Do not build a general visual-scripting
or arbitrary event-rule engine.

## Card Rules

| Card | Card Time | Energy | Commit and effect |
| --- | --- | ---: | --- |
| Grounded Double Jump | Neutral | 5 | Can be committed only while grounded. Grants one midair jump using the existing Extra Jump ability. Landing clears an unused charge. |
| Dash Enabler | Neutral | 5 | Enables dash input for five seconds of gameplay time. Each effective primary melee hit on an enemy adds 0.3 seconds while active. Outside this permission, dash input cannot start a dash. |
| Jump Boost | Neutral | 15 | Arms one grounded takeoff at twice normal jump launch velocity. Only an actual grounded jump consumes it; coyote and extra jumps do not. |
| Poise Damage | Chain | 20 | Arms five effective primary melee hits. Each eligible hit deals an authored base poise amount in the 1–3 range, multiplied by 1.2. The starting authored value is 2, producing 2.4 poise damage per hit. |
| Growing Reach | Chain | 40 | Each eligible primary melee hit raises forward attack reach by 5% of its unmodified value, up to five increments (25%). A completed attack action with no eligible enemy hit clears the bonus. |

An effective hit is an accepted damage result with positive applied health
damage, as defined in `specs/card-effects-and-energy-planning-20260614-1239.md`.
For these cards, an eligible hit must also target an `EnemyActor`, including
the training dummy. A tutorial gate can report an effective hit for its door
logic, but it is not an eligible enemy hit and does not extend dash, spend a
poise charge, or grow reach. An attack that hits two eligible enemies counts
as two hits for charge consumption and reach growth. A miss is one completed
player attack action with zero eligible enemy hits. Supplemental damage,
reflected projectiles, damage over time, and repeated resolution of an
already-hit target do not advance these cards.

Card effects begin only after the commit transaction succeeds. Invalid
conditions or an insufficient Energy balance reject without spending. An
active one-shot or charged effect rejects a second activation rather than
silently consuming Energy. Dash likewise rejects reactivation while its timer
is active; Growing Reach rejects reactivation until it clears. These effects
clear on player death or a full run reset. The controller's existing
`ResetTransientState` is also used by area streaming and must not clear card
effects: the persistent player owns their state, and timers keep running in
gameplay time. A completed Card Time session does not itself consume a jump,
hit charge, or range increment.

## Movement Integration

Use the existing `PlayerExtraJumpRuntime` for Grounded Double Jump. The card
has a grounded activation condition, while the granted jump is spent in the
air. The existing airborne Finisher Extra Jump card remains available; the
one-charge capacity means another card cannot grant a second stored charge.

Dash permission is a small player runtime with remaining time and an
`IsEnabled` query. `PlayerController` checks that query before starting the
existing `DashAction`. Tick with gameplay delta time, and extend only from
effective primary melee hits, not from individual damage callbacks that can
fire twice for the same hit. Dash duration and speed remain owned by the
existing dash definition.

Jump Boost is a one-charge player runtime read by the grounded jump launch
path. Multiply the configured jump launch velocity by 2 when that grounded
jump actually begins, then consume the charge. Keep extra-jump and coyote
launch paths at their normal velocity. Do not mutate the movement config
asset or consume the boost merely because the player pressed jump.

## Combat And Poise Integration

Keep ordinary melee poise damage at zero. Add a non-negative poise amount to
the damage transaction, separate from health damage and knockback. The active
Poise Damage card authors the amount at the player damage construction site.
Resolve it per target so a single attack striking multiple enemies cannot
spend five charges yet apply a sixth poise hit. A charge is consumed only when
that target's primary melee damage is accepted as an eligible hit. Rejected
armor hits neither spend a charge nor apply poise. Card-related poise must not
turn an otherwise rejected Golem charge hit into an accepted hit.

Add the previously specified optional `EnemyPoise` capability, with maximum,
current, regeneration, restoration, and a single depletion event. Enemies
without it ignore poise damage while still taking health damage. Enemy damage
policies forward accepted poise to the component they own; the component does
not decide enemy AI behavior. Poise is not a player resource and does not
change the existing health damage formula.

Give the current Golem Charger `EnemyPoise` and route depletion to its existing
Interrupted state from any living, interruptible state. Keep its present
Card/Impact tag rules for charge armor and ordinary interrupts. While
Interrupted, poise stays depleted; entering Recovery restores it so a later
card use can produce another interruption. Initial Golem maximum poise and
regeneration are tuning data, selected so five timely hits from one default
card use can deplete it. Show depletion and restoration through existing or
small additional debug/HUD feedback so the effect can be judged in Play Mode.
This baseline does not implement the bat enemy or its special fall response.

Growing Reach changes only the forward extent of the primary melee detection
box. Keep its rear edge and vertical height fixed, and draw the actual active
shape in its selected Gizmo. Store the base shape as authored data and compute
the modified shape at query time; do not mutate serialized dimensions.
Apply a newly earned 5% increment to subsequent queries, not retroactively
to other targets in the current query. Clear the bonus when the existing
attack-completion bookkeeping identifies a miss.

## Card Data, Selection, And Feedback

Extend typed card operations, validation, and runtime dispatch only with the
small reusable concepts required above: timed ability permission, one-use
jump modifier, primary-hit charges with poise payload, and hit-built reach.
The existing grounded condition and ability operation can be reused for
Grounded Double Jump. Retain definition assets as immutable data and mutable
effect state on the player.

Create definition, effect, and status assets with stable ids and the five
specified Energy costs. Add all five to the prototype catalog and inventory.
The prototype equipped loadout presents exactly these three Neutral and two
Chain cards; preserve older demonstration assets in the repository and keep
Finisher's current selection. Update the existing idempotent Editor asset and
inventory setup tools so rerunning them neither duplicates assets nor
re-equips old demonstration cards over this curated loadout. Use an idempotent
Editor prefab update for Golem Poise rather than hand-editing complex YAML.

Present remaining dash time, pending jump charge, remaining poise hits, and
reach increments through the existing card-feedback service. Feedback clears
when each effect ends. A rejected card commit should show the existing failure
feedback and leave resources and state unchanged. A primary melee attack with
an active card enhancement may still open a tutorial gate under its existing
rule; opening the gate does not count as an enemy hit for the new effects.

## Verification And Limits

Compile runtime and Editor assemblies. Check serialized asset references,
stable ids, Energy costs, inventory categories, Golem component wiring, and
idempotent setup output. The user will perform short Play Mode checks for
grounded activation, dash gating and extension, one grounded boosted jump,
five poise hits and Golem interruption, growing reach and miss reset, Energy
payment, HUD feedback, and survival of an area stream. Unity unit tests are
deferred at the author's request; compilation and Play Mode observation are
the validation baseline for this slice.

Exact enemy poise tuning and visual polish remain adjustable after Play Mode
observation. No new Finisher cards, deck-building progression, bat enemy,
general posture HUD, or card-effect damage type is part of this slice.
