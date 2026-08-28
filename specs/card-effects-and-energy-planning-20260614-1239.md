# Card Effects And Energy Planning - 20260614-1239

## Contexto

This document plans the planning and implementation order for the first
prototype cards and the player resource state they require.

It exists because Card Time currently identifies and commits a category
(`Neutral`, `Chain`, or `Finisher`), but does not yet identify a selected card,
pay a cost, execute a card effect, or retain card-created runtime state.

Sources used:

- `gdd/gdd-canonico-20260526-2331.md`
- `specs/prototype-architecture-sdd-20260525-2200.md`
- `specs/damage-system-sdd-20260526-0102.md`
- `specs/aerial-hit-confirm-and-hitstop-sdd-20260612-1753.md`
- `specs/enemy-actor-baseline-and-training-dummy-sdd-20260612-1715.md`
- `specs/player-and-global-runtime-responsibility-refactor-sdd-20260614-1013.md`
- `specs/persistent-gameplay-services-and-card-time-ownership-sdd-20260614-1107.md`
- current code under `Assets/Scrips/Architecture`

## Goal Of This Planning Pass

Define the minimum decisions and architectural wires needed before writing a
full implementation SDD.

This pass should answer:

1. What runtime owns player resources?
2. How does a committed Card Time session identify and execute a card?
3. How do cards install state that survives after the commit?
4. What counts as an affirmed hit or a miss?
5. How do enemy defeat and hit outcomes award energy?
6. Which prototype rules are fixed now and which remain authored data?

## Requested Prototype Cards

| Category | Prototype effect | Base energy cost |
| --- | --- | ---: |
| Neutral | Increase outgoing knockback for a limited number of affirmed hits | 5 |
| Chain | Increment outgoing damage after affirmed hits, up to five increments per use; any miss removes the accumulated bonus; repeated uses permit more increments | 15 |
| Chain | Double energy gained from the next five affirmed hits | Pending |
| Finisher | Permit one extra jump while airborne | 5 |
| Finisher | Amplify only the finisher's base damage by spending a base cost plus a variable amount | 15 + X |

## Terminology Baseline

Use `effective hit` in code and specifications instead of `affirmed hit`.

An effective hit is currently represented by:

```text
DamageResult.Accepted
&& DamageResult.AppliedAmount > 0
```

This matches `DamageResolutionReport.EffectiveHitCount`.

A kill is an effective hit whose result also reports `Killed`.

The word `miss` needs an explicit transaction boundary. Recommended prototype
definition:

```text
one completed player attack action that produced zero effective hits
```

This is different from:

- one overlap collider being rejected;
- one target in a multi-target attack rejecting damage;
- failing to activate or commit a card;
- letting Card Time expire;
- moving without attacking.

The attack runtime therefore needs one attack-outcome report at action
completion, not only per-target damage callbacks.

## Recommended Ownership Boundaries

Do not create one broad `PlayerStatus` class that owns health, money, energy,
buffs, cards, and movement rules.

Use a small player-local composition:

```text
Player
|- PlayerResourceWallet
|- PlayerCardRuntime
|- PlayerCombatEffects
`- PlayerExtraJumpRuntime
```

Responsibilities:

### PlayerResourceWallet

- owns current and maximum amounts for spendable player resources;
- validates and performs atomic costs;
- receives gains;
- publishes local change notifications for HUD and debug presentation;
- starts with only `Energy` enabled for the prototype.

It must not know card rules, enemy types, damage formulas, or jump behavior.

### PlayerCardRuntime

- knows the currently equipped or selected cards;
- resolves which card corresponds to the committed Card Time category;
- builds a card execution context;
- requests one atomic payment from the wallet;
- executes the card only after successful payment;
- reports rejection reasons such as no card, wrong category, or insufficient
  resource.

It must not become the owner of health, locomotion, damage resolution, or
enemy rewards.

### PlayerCombatEffects

- exposes active outgoing damage modifiers;
- observes attack outcome reports;
- retains hit-count and miss-reset effects created by cards;
- handles stacking rules for repeated card use;
- exposes outgoing knockback modifiers when knockback is implemented.

This is the likely implementation of `IDamageProvider` for the player, or a
player-local collaborator exposed through that contract.

### PlayerExtraJumpRuntime

- owns temporary extra-jump charges;
- is queried by airborne locomotion when an ordinary coyote/ground jump is not
  available;
- consumes one charge only when the extra jump actually executes;
- clears or preserves charges on landing according to the card rule selected
  below.

## Generic Resource Wire

The prototype should support future resource kinds without making each card
search for concrete player components.

Suggested contracts:

```text
ResourceId
    stable authored identity such as Energy, Money, Life, Heat

ResourceAmount
    ResourceId + numeric amount

IResourceWallet
    GetCurrent(resource)
    CanSpend(costs)
    TrySpend(costs)
    Gain(resource, amount, source)

CardCost
    one or more authored ResourceAmount entries
```

Recommended data choice:

- use a `ResourceDefinitionSO` stable asset identity;
- create `Resource_Energy.asset` first;
- allow a card to contain a list of fixed costs;
- keep variable costs as an execution-time addition to the fixed list.

This permits future cards to spend:

- energy;
- money;
- life, through a wallet adapter or explicit health-backed resource policy;
- heat or card-specific meters;
- combinations of resources.

Do not treat every future status or buff as a resource. A value is a resource
when it can be measured, gained, and atomically spent. A temporary modifier
with stacks may remain an effect state unless card rules actually spend it.

## Card Definition And Execution Wire

Suggested definition:

```text
CardDefinitionSO
- stable id
- display name and description
- Card Time category
- fixed costs
- effect definition/reference
- prototype tuning values
- presentation metadata later
```

Suggested commit path:

```text
Card Time category becomes available
    -> player activates Card Time
    -> player selects or already has one card selected for that category
    -> player commits
    -> PlayerCardRuntime validates category and affordability
    -> wallet atomically pays fixed and variable costs
    -> card effect executes
    -> Card Time session transitions to Committed
```

The current `TryCommit()` returns only `bool` and Card Time does not know the
selected card. The implementation SDD must decide whether:

1. card validation happens before calling the existing Card Time commit; or
2. Card Time accepts a commit callback/command that can reject the transaction.

Recommended baseline: keep Card Time responsible only for timing and
authority. `PlayerCardRuntime` validates and executes the card transaction,
then calls `TryCommit()` only when the transaction can succeed.

The payment and effect installation should still be atomic from the player's
perspective. If session commit unexpectedly fails, no cost may remain spent.

## Prototype Energy Rules

Initial player values should be authored:

```text
starting energy
maximum energy
hit gain amount
base hit-gain chance = 30%
```

Recommended baseline values to validate in play:

```text
starting energy = 30
maximum energy = 100
hit gain amount = 1
enemy default defeat gain = 1
```

Only the 30% base chance is currently specified by design. The other numbers
are planning defaults, not canonical balance decisions.

### Energy On Enemy Defeat

Add authored reward data on the enemy side, defaulting to one:

```text
EnemyDefinitionSO
`- defeatRewards
   `- Energy: 1
```

The enemy should describe the reward. A player reward receiver should apply
it after a confirmed player-caused defeat.

Do not put `player.GainEnergy(1)` inside `EnemyHealth`.

### Energy On Hit

Recommended resolution:

```text
effective player hit
    -> evaluate one energy-gain roll per completed damage request
    -> base chance 30%
    -> active modifiers alter chance and/or amount
    -> successful roll grants authored hit gain amount
```

Use one roll per attack request, not one roll per collider. Multi-target
behavior remains an explicit later decision.

Chance and amount must be separate values:

```text
chance modifier
gain amount modifier
```

The requested chain card doubles the amount gained; it does not raise the
30% chance unless its final text says so.

For deterministic tests, randomness must enter through an injectable roll
source rather than direct `UnityEngine.Random` calls inside the rule.

### Direct Energy From Neutral Cards

This becomes a simple card effect:

```text
wallet.Gain(Energy, authoredAmount, cardSource)
```

It is supported by the wire but is not one of the first five required cards.

### Energy Particles

Energy particles are a delivery and presentation method, not a player status.

Future flow:

```text
enemy hit or defeat
    -> reward roll/result
    -> optional pickup particle is spawned
    -> pickup reaches player
    -> wallet receives the energy gain
```

The prototype should first grant energy directly. Particle drops should be a
later replaceable delivery adapter so card and wallet rules do not depend on
VFX, pooling, or pickup movement.

## Card Rule Proposals

### Neutral: Knockback Charges

Recommended behavior:

- pay 5 energy on commit;
- install an outgoing knockback multiplier;
- consume one charge per completed damage request with at least one effective
  hit;
- multi-target hits consume one charge total;
- misses do not consume charges;
- repeated use adds charges up to an authored cap;
- no timer.

Required prerequisite: outgoing knockback is only authored in
`DamageProfileSO` today and is not applied by enemy runtime. Implementing this
card requires a knockback request/result path and an enemy knockback receiver
or motor policy.

Open tuning:

- multiplier;
- charges per use;
- maximum stored charges;
- whether bosses or immovable enemies still consume a charge.

### Chain: Escalating Damage

Recommended behavior:

- pay 15 energy on commit;
- each use adds five to the effect's `maximum increments`;
- current increments are preserved when the card is reused;
- each completed attack with at least one effective hit adds one increment,
  capped by the accumulated maximum;
- each increment adds an authored percentage to
  `DamageFormulaValues.FinalDamagePercent`;
- one completed attack with zero effective hits clears current increments;
- the increased maximum remains for the current encounter until an explicit
  reset rule occurs;
- no timer.

This interpretation makes repeated uses meaningful without granting five
damage stacks immediately.

Open rules:

- percentage per increment;
- absolute maximum increment capacity;
- whether a miss clears only current increments or also additional capacity;
- reset on room change, rest, death, unequip, or encounter end;
- whether card reuse while already active can be refused at maximum capacity.

Recommended prototype reset: clear both increments and added capacity on player
death or scene transition; a miss clears increments but not capacity.

### Chain: Double Hit Energy

Recommended behavior:

- cost remains pending;
- install five effective-hit charges;
- when a hit-energy roll succeeds, multiply the granted amount by `2`;
- consume one card charge on every effective attack hit opportunity, even when
  the 30% roll fails;
- one completed multi-target request consumes one charge;
- misses do not consume charges;
- repeated use adds five charges up to an authored cap;
- no timer.

This makes "next five affirmed hits" literal. An alternative is to consume only
when energy is successfully gained, but that would mean "next five successful
energy procs" and is substantially stronger.

### Finisher: Air Jump

Recommended behavior:

- pay 5 energy on commit;
- the card is valid only while airborne;
- grant one extra-jump charge;
- the next jump input executes a normal authored jump and consumes the charge;
- landing clears unused card-granted charges;
- repeated use while airborne may add another charge only up to an authored
  cap, recommended `1` for the prototype;
- committing the card does not automatically jump.

This preserves player input and makes the card an extension of movement rather
than an automatic displacement.

### Finisher: Base Damage Overcharge

Recommended behavior:

- fixed cost is 15 energy;
- variable cost `X` is selected at commit time and added to the atomic payment;
- the effect applies only to the finisher attack created by that commit;
- it amplifies the attack-scaled base portion before flat damage and final
  damage modifiers;
- unused variable budget is never taken;
- no partial payment;
- enforce an authored maximum `X`.

The current damage formula calls the relevant component:

```text
(StrikePercent + StrikeBonusPercent) * Attack
```

Recommended implementation target: add an overcharge contribution to
`StrikeBonusPercent`, leaving `FlatDamage` and `FinalDamagePercent` unchanged.

Open tuning:

- conversion from each energy point in `X` to base-damage percentage;
- maximum `X`;
- how the player chooses `X` during the current Card Time UI;
- whether `X = 0` is legal, making the card cost only 15;
- what happens when energy is below the desired `15 + X`.

Recommended affordability behavior: clamp the offered `X` in UI to spendable
energy, but execute only the explicitly confirmed value.

## Required Outcome Signals

The current code has per-target damage reports and a boolean hit confirmation
on the active attack. These cards need a player-local outcome stream:

```text
AttackStarted
AttackResolved
    attack id/state
    effective hit count
    killed target count
    was miss
    damage report(s)
AttackCompleted
```

The implementation should avoid global event channels for this synchronous,
player-local combat state. A narrow listener or coordinator under the player
is sufficient.

Enemy defeat rewards may use a broader damage/defeat notification because the
reward originates on the enemy side and must identify the credited source.

## Planning Sequence

### Planning Slice 1: Canonical Rules

Confirm:

- meanings of effective hit and miss;
- energy starting/max/hit values;
- chain stack and reset rules;
- energy-chain card cost and charge-consumption rule;
- extra-jump grant and landing behavior;
- overcharge conversion, cap, and selection UX;
- whether the prototype uses one pre-equipped card per category or a selectable
  hand/deck.

Deliverable: a short timestamped GDD decision update if these choices change
or extend canonical design.

### Planning Slice 2: Card Transaction SDD

Specify:

- `CardDefinitionSO`;
- selected-card ownership;
- commit validation order;
- cost transaction and rollback behavior;
- card execution context;
- effect lifetime and reset ownership;
- debug presentation needed before final HUD.

### Planning Slice 3: Resource And Reward SDD

Specify:

- resource identity and wallet API;
- player energy configuration;
- deterministic hit-gain rolls;
- modifiers to gain chance and amount;
- enemy-authored defeat rewards;
- source credit for kills;
- direct gain first, particle delivery later.

### Planning Slice 4: Combat Outcome And Effect SDD

Specify:

- attack-level hit/miss reporting;
- player implementation of `IDamageProvider`;
- active damage modifiers;
- chain stacking;
- limited-hit charge consumption;
- outgoing knockback transaction and enemy response.

### Planning Slice 5: Movement Effect SDD

Specify:

- temporary extra-jump charge API;
- airborne validity checks;
- input buffering interaction;
- landing and scene reset rules;
- animation and debug feedback.

### Planning Slice 6: Vertical Prototype Delivery

Implement in dependency order:

1. card selection stub and commit transaction;
2. energy wallet with debug display;
3. fixed card costs;
4. enemy defeat energy;
5. hit energy and deterministic tests;
6. extra-jump finisher;
7. escalating-damage chain;
8. doubled hit-energy chain;
9. base-damage overcharge finisher;
10. knockback runtime and knockback neutral card.

Knockback is last because the current enemy baseline has no knockback response.

## Test Matrix For The Future SDDs

Minimum pure/EditMode coverage:

- an unaffordable card does not commit or spend;
- a successful commit spends exactly once;
- a failed session commit rolls back or avoids payment;
- resource gains clamp to maximum;
- enemy reward values come from enemy data and default to one;
- hit gain uses the configured chance and injectable roll;
- gain amount and gain chance modifiers are independent;
- one multi-target request does not duplicate limited-hit charge consumption;
- a completed missed attack resets chain increments;
- rejected target results alone do not count as effective hits;
- repeated chain use extends capacity according to the confirmed rule;
- extra jump cannot execute while grounded and consumes once while airborne;
- landing clears the temporary extra-jump charge;
- overcharge modifies the base-scaled damage component only;
- variable cost cannot exceed energy or authored cap;
- knockback charges are consumed only by effective hit requests.

Play Mode validation:

- energy costs and gains are legible in the debug HUD;
- cards cannot be committed in the wrong Card Time category;
- miss reset is understandable to the player;
- energy gain feels observable despite the 30% chance;
- extra jump preserves existing jump buffering and air control;
- overcharge selection is usable within Card Time;
- enemy knockback remains stable and does not break patrol physics.

## Main Risks

### Card Time Has No Card Selection Yet

Adding effects directly to `TryCommit()` would hardcode category behavior and
make the second card in a category difficult. Selection and transaction
identity must come first.

### `PlayerStatus` Can Become A New Blob

Resources, card effects, combat modifiers, and movement charges have different
lifetimes and dependencies. Keep them behind small contracts and compose them
on the player.

### Miss Detection Is Easy To Miscount

Per-collider and per-target events cannot reliably define a missed attack.
Miss rules must use the completed attack action as their boundary.

### Knockback Is Not Implemented

The neutral knockback card cannot be honestly prototyped by changing only
damage data. Enemy response and immovable-target rules are prerequisites.

### Random Energy Can Hide Validation

Use deterministic debug controls or a seeded/injectable roll source so the
30% rule can be tested and demonstrated without waiting for favorable rolls.

### Variable Cost Needs UI

The overcharge finisher is not only a damage modifier. It requires a fast,
legible way to choose `X` inside Card Time. Until that interaction is chosen,
the implementation can expose a fixed prototype `X`, but that would validate
damage and payment rather than the final card UX.

## Decisions Recommended For The First Prototype

- Energy is the only spendable card resource.
- Resources are data-identified and wallet-driven.
- Energy particles are deferred; gains apply directly.
- One pre-equipped card slot exists per category before hand/deck selection.
- An effective hit is accepted positive damage.
- A miss is a completed attack action with zero effective hits.
- Multi-target requests count once for charges, stacks, and energy rolls.
- Temporary card effects have no timers unless the card explicitly defines
  one.
- The extra-jump card grants a charge and does not auto-jump.
- Base-damage overcharge writes to the strike/base portion, not final damage.
- Knockback is implemented after the resource, card transaction, and combat
  outcome foundations.

## Open Decisions Requiring Design Input

1. What percentage does each escalating-damage increment add?
2. Does a miss remove only current increments or also the extra capacity added
   by repeated card uses?
3. What is the absolute cap after repeated escalating-card uses?
4. What does the doubled-energy chain card cost?
5. Should its five charges be consumed by effective hits or only successful
   30% energy procs?
6. How many knockback-boosted hits does one neutral-card use grant, and by what
   multiplier?
7. Can the air-jump finisher be committed while grounded for later use, or only
   while already airborne?
8. What is the energy-to-base-damage conversion and maximum `X`?
9. How is `X` selected during Card Time?
10. For this prototype, is there one fixed card per category, multiple equipped
    slots, or a selectable hand?

