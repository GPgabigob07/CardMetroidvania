# Player recovery economy — design for review

## Contexto

The user confirmed the playtest build and uGUI work. The next gameplay change prevents energy starvation from blocking traversal and adds health/energy conversion plus a consumable healing card. This is architectural work: it extends card transactions and introduces runtime consumable ownership. This document preserves previous specifications and separates confirmed decisions from proposed tuning. No gameplay implementation is authorized by the existence of this draft.

Sources:
- `gdd/gdd-canonico-20260526-2331.md`
- `specs/card-effects-and-energy-planning-20260614-1239.md`
- `specs/asset-driven-card-definitions-and-commit-transaction-sdd-20260614-1833.md`
- `specs/composable-card-effects-and-gated-ability-bridge-sdd-20260614-1841.md`
- `specs/card-inventory-selection-handshake-sdd-20260622-2309.md`
- `specs/neutral-chain-card-baseline-sdd-20260921-1107.md`
- `specs/player-facing-health-respawn-sdd-20260617-1717.md`
- `specs/unity-editor-collaboration-workflow-20260612-1609.md`
- Current PlayerResourceWallet, PlayerCombatEffects, PlayerController, PlayerCardRuntime, PreparedCardCommit, inventory profile/loadout, snapshot, SimpleHealth, pause/startup/respawn code.

## Confirmed requirements

1. Base energy recovery from hits and time, an HP-to-energy card, an energy-to-health card, and a healing card that consumes one owned copy.
2. Let `C` be the highest energy cost among currently equipped Neutral cards. Passive energy recovery stops at `1.5 * C`.
3. Energy-to-health conversion costs `2 * C` energy per one HP, replacing the previously suggested fixed 50-energy chunk.
4. These values belong in player properties/tuning, and their formulas may be hidden from player-facing text.
5. A valid player always has an equipped Neutral card. Empty Neutral loadouts are not a gameplay state or a proposed new mechanic.
6. If defensive fallback is useful, assume `C = 20`: reserve 30, healing chunk 40. This does not change the valid-loadout invariant.

## Observed baseline

- Player prefab: starting energy 100, maximum 250, maximum HP 5. Existing hit recovery has 0.3 probability and amount 1; existing defeat rewards and energy wells also exist.
- Traversal cards include costs 5 (dash/extra jump) and 15 (jump boost).
- PlayerCardInventoryProfileSO stores owned counts and equipped arrays as an authored asset. Selection uses its equipped IDs. PlayerCardRuntime also has legacy single-card fields; those are not the authoritative multi-slot loadout for calculating C.
- Inventory export currently serializes owned IDs, not consumable quantities. It is not a completed consumable save system.
- SimpleHealth currently provides damage and full initialization, but no bounded healing/nonlethal sacrifice operation.
- Prepared commits check live wallet affordability, but do not yet atomically coordinate health, energy and inventory quantity.

## Proposed baseline, requiring review

| Property | Suggested value | Reason |
| --- | --- | --- |
| HitEnergyAmount | 3 | Predictable combat recovery |
| HitEnergyChance | 1 | Use existing reward path, remove random starvation |
| PassiveEnergyPerSecond | 5 | Recover a small traversal budget promptly |
| PassiveDelayAfterSpendSeconds | 3 | Spending energy postpones passive recovery |
| PassiveReserveMultiplier | 1.5 (confirmed) | Player-owned tuning |
| HealingChunkMultiplier | 2 (confirmed) | Player-owned tuning |
| FallbackNeutralEnergyCost | 20 (confirmed defensive default) | Invalid-data safety only |
| SacrificeHealthCost | 1 | One readable health unit |
| SacrificeEnergyMultiplier | 1 | Gain C energy; avoids profitable reverse conversion |
| ConsumableHealAmount | 2 | Useful emergency recovery at five maximum HP |
| InitialConsumableCopies | 2 for prototype testing | Authored stock, not a permanent balance promise |

Recommended category: all three recovery cards are Neutral, use the current Neutral Card Time availability rules, and have zero fixed energy cost. No extra grounded condition or new cast animation is introduced. Health conversion cards remain reusable; only the dedicated healing card consumes a copy.

Consumable death policy is awaiting user input: recommended default is that spent copies stay spent through death and area reload. A fresh run restores the authored stock; explicit future rewards/pickups can add copies. No replenishment pickup, shop, loot system or checkpoint restock is implemented in this slice.

## Cost basis and player properties

For each equipped Neutral definition, sum its non-negative, finite fixed costs referencing the player's Energy resource; C is the maximum of those sums. Sum duplicate resource entries before comparing cards. Exclude other resource types and transient discounts/surcharges. In particular, do not include the healing card's derived exchange amount in its own cost basis: that would create the recursive equation C = 2C.

Zero-energy utility cards remain equipped and selectable but do not raise C. If no usable positive basis can be resolved, use defensive C=20 and report an authoring validation issue. Invalid references/NaN/negative costs do not propagate into arithmetic. Valid content must always supply the guaranteed Neutral loadout; the fallback is not shown as a gameplay option.

PlayerRecoveryTuningSO stores authored defaults; a player-local recovery component exposes read-only `NeutralCostBasis`, `PassiveEnergyCeiling`, `EnergyPerHealth`, and `SacrificeEnergyGain`. No new universal PlayerStatus class. Recompute from the runtime equipped definitions on loadout changes, not from the whole catalog, owned-but-unequipped cards, or one legacy selected slot.

`PassiveEnergyCeiling = min(wallet maximum, 1.5 * C)`; `EnergyPerHealth = 2 * C`. Retain fractional energy (C=15 -> reserve 22.5, chunk 30); round presentation only. If C or the healing chunk exceeds wallet capacity, flag content tuning: do not silently lower the exchange price. A required traversal sequence must fit the reserve between safe recovery locations; these formulas alone do not prove the level is escapable.

The cap applies only to passive gain. Hits, defeat rewards, wells and cards can fill above it up to maximum energy. Lowering the cap never deletes existing energy. Proposed hit reward is once per effective primary attack resolution against a living enemy, including a killing hit; no reward per collider, supplemental damage, self-sacrifice, miss, gate or empty swing. Preserve existing kill rewards and double-hit-energy card behavior without adding a second grant path.

## Time recovery

Track successful energy expenditure through the wallet's authoritative change path; reset the delay only when energy actually decreases. Rejected payments do not reset it. Use gameplay simulation delta time, so pause yields no gain and Card Time slowdown does not provide accelerated regeneration. Start the delay on new-run initialization.

Do not tick during death, startup/world hold, recovery, menus or transitions; do not accumulate deferred catch-up time. For a large delta, subtract the delay first and apply gain only for the remaining eligible time. Once at/above the cap, grant zero. Wallet maximum remains authoritative. Preserve existing respawn energy policy; recovery does not silently refill the wallet.

## The three cards

### Health sacrifice — working name: Blood Charge

Proposed effect: spend 1 HP and gain C energy. Require enough health to leave at least 1 HP and enough wallet headroom for the entire gain; otherwise reject without payment. The sacrifice is a health cost, not incoming damage: no death, invulnerability frame, knockback, hitstop, enemy hit reward or damage-taken proc. Publish ordinary health/energy change notifications.

The earlier fixed proposal of 40 energy is superseded as a recommendation, not a user decision: when C=15, the new healing chunk is 30 and fixed 40 would make the exchange profitable. Require configured sacrifice gain to be strictly less than the energy required to heal its HP cost. Apply no generic energy-cost discount to either conversion exchange in this slice.

### Energy conversion heal — working name: Reconstitute

Proposed remainder/overheal policy: `chunks = min(floor(E / (2*C)), ceil(missing HP))`. Spend `chunks * 2*C`, restore up to `chunks` HP, clamp at maximum HP, and retain all unspent energy. One final chunk may restore less than 1 HP if the damage system leaves a fractional deficit. Reject when dead, at full HP, or below one complete chunk; no payment on rejection.

Examples with C=15: E=29 -> unavailable; E=30 -> 1 HP; E=95 and missing 3 HP -> spend90/heal3/retain5; E=95 and missing1 HP -> spend30/heal1/retain65. With fallback C=20, E=95 and missing3 -> spend80/heal2/retain15.

This implements the earlier recommendation to avoid wasting energy on overflow/overheal; user has confirmed chunk scaling, but has not explicitly confirmed remainder handling.

### Consumable heal — working name: Mend

Proposed effect: restore up to 2 HP, consume one copy, fixed energy cost zero. Reject at full HP, dead, or quantity zero. Cancellation, timeout, failed preparation, lost readiness and duplicate commit consume nothing. Successful partial healing consumes one full copy.

Ownership quantity belongs to the run, not CardDefinitionSO or the inventory profile asset. Definitions contain a reusable/consume-one-on-success policy. Slot placement stays stable when quantity reaches zero: display depleted/unavailable with count zero; do not shift other input slots mid-selection. Availability uses the runtime quantity. Consuming a card does not unequip its slot or change C solely because count reaches zero. The always-equipped Neutral invariant remains true.

## Atomicity and integration

Keep world-owned Card Time and player-owned effects. Preparation creates an immutable quote containing definition, category/session, cost basis/loadout revision, energy spent/gained, HP spent/healed, consumable count, and quantities/health needed to validate it.

Immediately before applying, revalidate live category/session, loadout revision/basis, health, energy and quantity. If state differs enough to change the quoted effect/cost, reject and let a later selection re-quote; never silently charge a different conversion price. Ordinary existing cards retain their existing affordability semantics.

Commit all involved state exactly once. Add a reentrancy guard before writes, prevalidate all mutations, defer health/wallet/inventory notifications until the coherent state is committed, and release the guard after publication. No failure may leave a paid cost without effect, lost copy, or free heal. Event callbacks cannot reapply the same prepared commit. Avoid faking healing through negative DamageContext or treating Initialize as a heal.

Inventory runtime is initialized once from the authored profile when the player/run is created, survives streamed areas and ordinary respawn, and is discarded on new run. Selection resolves owned/equipped IDs and quantities through that runtime provider. Existing editor inventory tools remain profile authoring tools. Disk saves and save-format migration are out of scope; do not claim current ID-only export preserves consumables.

## Loadout-swap exploit and scope

For a fixed C, passive cap 1.5C is below the healing price 2C. That does not by itself prevent waiting-based healing across changing loadouts. Example: regenerate to60 with C=40, then equip C=10 cards and heal3 for60.

No player-facing runtime deck editor was found in the current slice. Recommended scope: initialize the runtime equipped deck from its authored profile and keep it fixed for that run; no new runtime equip UI/API is introduced. This keeps the confirmed currently-equipped formula honest for the available gameplay. If the user intends in-run equipping, choose its rules before implementing this economy; merely restricting it to a checkpoint does not mathematically remove the exploit unless checkpoint policy makes it irrelevant. Do not silently clamp energy, add an undocumented cost high-water mark, or alter the agreed formula as a workaround.

## UI wording and feedback

No need to expose the multipliers or C in ordinary HUD text. Proposed copy:
- Passive: “Energy slowly recovers to a reserve.”
- Blood Charge: “Sacrifice health to restore energy.”
- Reconstitute: “Convert stored energy into health.”
- Mend: “Restore health. Consumed on use.”

Show meaningful current results/readiness on the card: “1 HP -> 15 energy”, “90 energy -> 3 HP”, or “Restores up to 2 HP · 2 remaining”, generated from the quote. Hidden formulas must not conceal actual payment. Explain unavailable states (not enough energy, health too low, full health, energy full, no copies) using existing feedback channels. Preserve HUD health/energy updates and input labels.

## Verification and completion

Unit tests: cost aggregation, multiple Neutral slots, non-equipped exclusion, valid/invalid fallback, fractional ceiling, capacity clamp without draining, timer delay splitting, hit reward de-duplication, frozen pause/death/hold, all three recovery effects and boundaries, no conversion profit at representative C values, quantity persistence, no asset mutation, stale quote rejection and notification reentrancy.

Integration: existing Card Time selection/prepared commit path, 0-energy passive recovery into a traversal card, health sacrifice at low HP, over-cap hit recovery, energy healing remainder, last consumable use, cancellation, death/area reload/new-run stock, title/pause transitions, HUD readiness and unchanged slot bindings. Play Mode route traversal verifies reserve size is actually sufficient.

Known baseline: the preceding full suite had 20 inherited failures and 3 skipped tests, reproduced at 4beafe6. Save and compare exact failing test identities, not just totals. Do not call that suite green. Follow explicit EditMode initialization and idempotent Unity setup tooling.

Review still needed: proposed numbers/card categories; sacrifice scaling; remainder/overheal policy; consumable death/stock policy; whether any in-run loadout changes are required. The implementation plan is a dependent review draft, not approval to choose unresolved gameplay rules silently.
