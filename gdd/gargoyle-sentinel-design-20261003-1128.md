# Gargoyle Sentinel - Encounter Design Draft

## Contexto

Created on 2026-10-03 after the author's elite-enemy brainstorming and sprite
scale review. This is a review draft for the first solo mini-boss. It preserves
the canonical GDD and earlier enemy designs. The author requested a written
specification before coding; proposed mechanics below are not silently promoted
to previously confirmed decisions.

Sources:

- `gdd/gdd-canonico-20260526-2331.md`
- `specs/golem-charger-enemy-sdd-20260806-2146.md`
- `specs/bat-machine-enemy-sdd-20260901-1202.md`
- `specs/neutral-chain-card-baseline-sdd-20260921-1107.md`
- `specs/player-recovery-economy-design-20260929-1516.md`
- Author conversation on 2026-10-03, including accepted visual revisions.
- `gdd/art-references/gargoyle-idle-draft-200x200-20261003.png`

## Confirmed brief

- One elite enemy fighting alone; no adds or summons.
- Difficulty comes from a challenging moveset and combinations.
- Gargoyle appearance follows the project's medieval/arcane-machine cues.
- At least 50% larger than the player in body area.
- Melee and ranged attacks; ranged patterns include 1, 3 and 5 projectiles and
  a laser beam.
- Two gimmicks involving specific card types. One blocks the beam; another
  can stop the nova attack.
- Stunnable, with different attack combinations and shuffled attacks without
  repetition. Feints are desired as an exploration of the moveset.
- Exact sprite frames are 200 x 200 pixels. Ordinary pose artwork fits inside
  a 128 x 128 envelope; padding accommodates extended attacks/wings.
- The compact orange armor, blue crystal horns/core, mechanical folded wings
  and heavy claws were accepted as the appearance baseline.

The author described a default combo and three additional attacks, using both
"interdependent" and "independent." This draft interprets them as independently
selectable attack families that share spacing and recovery rules.

## Proposed encounter

A grounded Gargoyle Sentinel guards a small castle arena. Its wings support
strikes and readable silhouettes; continuous flight is outside this baseline.
Players learn a stable three-hit rhythm, then identify shuffled extensions and
exploit two distinct reactor openings. Motor skill remains central, following
the canonical GDD.

| Move | Behavior | Read and response |
| --- | --- | --- |
| Basic combo | Claw sweep, backhand, delayed overhead slam | Distinct anticipation per strike; move around or away from the committed hit |
| A: Wingbreaker | Short advancing wing strike, then upward claw | Wing opens before advance; upward follow-up punishes an early jump |
| B: Crystal Volley | One aimed shot, narrow three-shot fan, or wide five-shot fan | Crystal count and spread preview identify the variant; aim locks before release |
| C: Throat Beam | Track during early windup, lock aim, emit a straight beam | Throat lights and an aim line appears; evade or use Ward during the opening pulse |
| Nova | Chest shutters open, finite radial blast follows | Reach the exposed core with Poise Damage-enhanced melee or leave the blast radius |

Ordinary uninterrupted repertoire passes are Basic -> shuffled A/B/C -> long
recovery. Short gaps between families permit repositioning and counterattack.
All six A/B/C permutations are possible. A new bag cannot begin with the last
family of the previous bag. Nova is separately scheduled at a recovery boundary,
never inserted into an already committed attack.

An interrupted pass keeps its unspent family bag. After stun, a fresh basic combo
precedes the remaining families, then long recovery; refill occurs only after
that bag is exhausted. This may make the resumed pass shorter than three families.

One optional feint per pass can change the opening claw windup into a delayed
claw strike. The branch has its own readable cue, preserves a response window,
and cannot occur after an active hitbox begins. This first feint does not add
an extra attack family or projectile attack.

## Proposed card gimmicks

1. **Core break:** use the existing Chain Poise Damage card. Only accepted
   primary card-enhanced melee with positive poise payload reduces the exposed
   nova reactor's separate stability budget. Default stability is 4.8: two
   default 2.4-poise hits. Core break cancels the blast and causes stun.
2. **Beam choke:** a new Neutral Ward card grants a short directional guard.
   Intercept the opening beam pulse from the correct side to stop the beam and
   expose the head during a shorter stagger. Card activation alone does not
   counter a beam that never intersects the guard.

Both are proposed optional counters. Movement can avoid the beam and finite
nova radius, so missing a card or energy never makes victory impossible. There
is no invulnerable shell requiring the two cards to deal health damage.

The nova opening accepts ordinary positive-health-damage hits, allowing the
player to establish Chain Card Time before selecting Poise Damage. Ordinary
hits do not reduce reactor stability. Global poise can also interrupt nova;
that is a valid alternative route, not a third required card gimmick.

## Mandatory tuning ownership

All timings, health/poise/damage values, speeds, ranges, hitbox geometry, attack
patterns, probabilities, counters, card costs and presentation tuning are
ScriptableObject-authored. Components own instance state only. Values listed in
this document are initial asset defaults. Editing valid simulation values on
those assets during Play Mode updates active instances at the safe boundaries
defined in the technical spec; no enemy restart is required. Asset import/frame
metadata is also SO-authored but applied through Editor tooling outside Play Mode.

## Stun, fairness and tuning

Global poise depletion causes a punishable stun. Nova core break uses that same
stun response. Beam choke causes a shorter stagger. Death takes precedence over
either. Attacks and owned projectiles are cleared before stun or death completes.

Initial tuning is a proposal: 12 global poise, no passive poise regeneration,
1.25-second stun, 0.65-second beam stagger and 0.75-second post-stun poise
resistance. Restore poise when leaving stun/stagger. Resistance does not prevent
health damage or the specific reactor/beam counters, and is visibly communicated.

Regular hits deal 1 player HP; nova deals 2. Each volley, beam cast and nova can
damage a given player only once. Each basic-combo strike is a distinct hit.
No passive contact damage is added to this enemy.

## Art and world scale

The exact artist reference is a 200 x 200 transparent PNG; its visible idle
bounds are 128 x 107 pixels. It is an AI draft resized with nearest-neighbor
sampling and binary alpha; manual pixel cleanup and animation authoring remain
artist work. A 128 x 128 envelope is a limit, not a requirement to fill every
pixel. All frames share a fixed foot pivot. Beam/nova effects use separate assets.

Pixel dimensions, visible body area, collider size and Unity world size are
different measurements. At 64 PPU and unit scale, a 128 x 128 envelope covers
2 x 2 units, while the 200 x 200 canvas covers 3.125 x 3.125 units. Transparent
padding never defines collision or the 50% body-area requirement.

The current Player prefab is not yet a 32 x 64 production sprite: its selected
sprite rect is 141 x 299 at 128 PPU, with a 1 x 2 physical collider. Do not resize
the player as a side effect of this encounter. Relative scale must be measured
against the actual player in the test scene; the technical spec defines a
provisional gargoyle import scale and the acceptance measurement.

## Review decisions

The spec package proposes optional counters, the Poise Damage/Ward pairing,
Basic plus three family sequencing, grounded movement and bounded feints. These
choices, initial timings, Ward's cost and its effect on the recovery economy
need review before an implementation plan is approved. The accepted sprite
canvas and visual identity remain constraints throughout that review.

Technical contracts: `specs/gargoyle-sentinel-enemy-sdd-20261003-1128.md` and
`specs/ward-card-and-beam-interception-sdd-20261003-1128.md`.
## Historico - 2026-10-03 tuning revision

Supersedes `gdd/gargoyle-sentinel-design-20261003-1035.md`, which remains preserved as project memory.
The author accepted the existing design and added a mandatory requirement:
all timings, damage and other tuning values must be authored in ScriptableObjects
for live Play Mode testing. This version makes data ownership, live-edit behavior
and verification explicit. Other encounter decisions are retained.
