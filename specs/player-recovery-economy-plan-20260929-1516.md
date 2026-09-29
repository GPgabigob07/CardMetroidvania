# Player Recovery Economy Implementation Plan — review draft

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans or superpowers:subagent-driven-development to implement this plan task-by-task. Use the checkbox steps to track progress. This draft fulfills the request to plan the full feature; execution waits for review of the paired design and its proposed gameplay defaults.

**Goal:** Prevent energy-starved traversal dead ends and add reusable HP/energy conversions plus correctly consumed healing cards.

**Architecture:** Derive recovery thresholds from the runtime equipped Neutral loadout. Keep recovery tuning and resource effects player-owned, use the existing prepared Card Time commit path, and introduce run-owned inventory counts copied from authored profile data. Apply health, energy and quantity changes atomically.

**Tech Stack:** Unity 6000.3.16f1, C#, ScriptableObject definitions/tuning, existing resource wallet/Card Time/uGUI feedback, Unity Test Framework.

**Spec:** `specs/player-recovery-economy-design-20260929-1516.md`.

## Contexto

New planning version after the working playtest/UI slice. Sources and confirmed versus proposed decisions are recorded in the paired design. Existing specs and source documents are preserved. No gameplay implementation occurs in this planning pass.

## Global Constraints

- Confirmed: C is highest currently equipped Neutral energy cost; passive ceiling 1.5C; heal chunk 2C; defensive C=20; valid gameplay always has an equipped Neutral card.
- Proposed numbers are not user-approved: +3 guaranteed hit energy, 5 energy/sec after 3 seconds, sacrifice1HP -> C energy, consumable heals2HP and starting count2.
- Proposed: retain chunk remainders, do not pay for overheal, consumables survive death as spent, runtime deck fixed for this slice. Resolve these in design review before execution.
- Aggregate fixed Energy entries per equipped card; dynamic conversion amounts do not feed their own basis. Exclude generic discounts from recovery exchanges.
- Keep fractional energy; no silent draining on ceiling changes; no healing while dead; no lethal sacrifice.
- Do not modify shared inventory assets at runtime, implement disk saves, add runtime deck editing, invent checkpoints/loot, or refactor unrelated systems.
- One concrete MonoBehaviour/ScriptableObject per matching .cs file; preserve .meta GUIDs; use Inspector annotations and XML interface docs.
- Read latest GDD/specs, current code conventions, testing conventions and Editor collaboration workflow before implementation.

## Review Focus

1. A dynamic healing cost accidentally becomes the reference Neutral cost and recurses (Task1 tests).
2. Event callbacks or stale prepared selections spend twice, heal for free or lose a consumable (Tasks3/4 tests).
3. Death/area reload duplicates consumables or writes depletion back into assets (Tasks2/7 tests).
4. Pause/Card Time/startup changes regeneration speed or grants catch-up energy (Tasks5/7 tests).
5. Swapping expensive/cheap loadouts or changing cost modifiers undermines the no-wait-healing claim (Tasks1/4 and scope validation in Task7).

## File structure and dependencies

Use `Assets/Scrips/Architecture/Player/Recovery/` for new recovery logic:
- `PlayerRecoveryTuningSO.cs`: authored tuning defaults and validation, not runtime balances.
- `PlayerRecoveryEconomy.cs`: pure cost basis, ceilings and quote arithmetic.
- `RecoveryCardQuote.cs`: immutable amounts/basis/revision needed for an atomic recovery commit.
- `PlayerRecoveryController.cs`: player references, derived properties, passive tick and recovery transaction coordination.

Use existing `Player/Cards/`:
- New `PlayerCardInventoryRuntime.cs`: run-owned count/loadout provider, initialized from profile once.
- New `CardConsumptionPolicy.cs`: reusable versus consume one on success.
- Extend `CardDefinitionSO.cs`, `CardEffectKinds.cs`, `CardOperationDefinition.cs`, `PlayerCardRuntime.cs`, `PreparedCardCommit.cs`, `PlayerCardCommitSnapshot.cs`, `PlayerCardCommitSnapshotSource.cs`, and `CardCommitFailure.cs` only as needed for recovery quotes/policy/readiness.
- `PlayerCardInventoryProfileSO.cs` remains authored data and editor API; preserve its existing export compatibility without claiming new save support.

Other existing modifications: `Runtime/SimpleHealth.cs`, `Player/Resources/PlayerResourceWallet.cs`, `Player/Runtime/PlayerController.cs`, `Player/Runtime/PlayerCombatEffects.cs`, `Hud/CardTimeSelectionSlotUI.cs` and the existing feedback presenters where quote/count text belongs.

Editor: new `Editor/PlayerRecoverySetup.cs`; create recovery tuning/card/effect assets via Unity APIs and wire `Assets/Prefabs/Player/Player.prefab`. Resolve and extend its currently assigned inventory profile; do not create a disconnected second loadout.

Tests: new `Assets/Tests/EditMode/Architecture/Recovery/` fixtures and `Assets/Tests/PlayMode/Architecture/Recovery/PlayerRecoveryIntegrationTests.cs`; extend current wallet/card/health fixtures when the contract already belongs there.

Order: Tasks1 and2 provide arithmetic/inventory; Task3 provides safe mutation; Task4 joins them into card commits; Task5 adds ordinary recovery; Task6 exposes/wires the feature; Task7 validates the resulting gameplay loop.

## Task 1: Derive player recovery properties

**Files:** tuning/economy/quote files; `PlayerRecoveryEconomyTests.cs`.

**Interfaces:** `PlayerRecoveryEconomy.ResolveNeutralCost(IEnumerable<CardDefinitionSO> equipped, ResourceDefinitionSO energy, float fallback) -> float`; `GetPassiveCeiling(float basis, float maximumEnergy, float multiplier) -> float`; `QuoteEnergyHealing(float energy, float health, float maxHealth, float basis, float multiplier) -> RecoveryCardQuote`. Quote exposes Basis, EnergySpent, EnergyGained, HealthSpent, HealthRestored, CopiesConsumed, and InventoryRevision; no mutable balances.

- [ ] Resolve proposed rules with the user and record a new approved design version if semantics change.
- [ ] Write tests using literal fixtures: equipped costs5/15 -> C15; owned-but-unequipped100 ignored; one card entries5+10 -> C15; no valid positive data -> fallback20; C15/max250 -> cap22.5; max10 -> cap10 without wallet mutation.
- [ ] Test C15 healing boundaries: energy29 -> no quote; energy30/missing1 -> spend30/heal1; energy95/missing3 -> spend90/heal3/retain5; energy95/missing1 -> spend30/heal1/retain65. Test fractions, NaN/invalid authoring and health already full/dead.
- [ ] Run the fixture and observe meaningful failures, then implement pure arithmetic and tuning validation. Recovery cards' zero fixed energy costs must not recursively reference their derived quotes.
- [ ] Run again: all fixture assertions pass. Commit this arithmetic/tuning slice.

## Task 2: Introduce run-owned inventory quantities

**Files:** inventory runtime/policy, CardDefinitionSO, PlayerController selection wiring; `PlayerCardInventoryRuntimeTests.cs`.

**Interfaces:** `PlayerCardInventoryRuntime.Initialize(PlayerCardInventoryProfileSO profile)`; `GetCount(string cardId) -> int`; `GetEquippedCards(PlayerCardTimeState category) -> IReadOnlyList<CardDefinitionSO>`; `GetEquippedCardIds(PlayerCardTimeState category) -> IReadOnlyList<string>`; `int Revision`; `event Action Changed`. Consumption is available only to the commit path, not directly to buttons.

- [ ] Write tests: two runtimes copied from one profile are independent; consuming one copy changes only that runtime; zero count retains the slot but is unavailable; reusable cards do not lose count; copying a profile does not mutate source entries.
- [ ] Test area rebinding and player disable/enable do not reinitialize inventory. A newly constructed run resets to profile stock; ordinary respawn follows the chosen consumption policy.
- [ ] Observe failures; implement an idempotent initialization contract and source all normal multi-slot selection IDs from this runtime. Preserve legacy standalone test composition where no profile exists through explicit initialization, not an empty-loadout gameplay branch.
- [ ] Validate authored Neutral loadout invariant. Add no runtime equipment mutator in this slice; count changes do not auto-unequip or reorder.
- [ ] Pass inventory and existing card-selection tests; commit.

## Task 3: Safe healing and nonlethal health payment

**Files:** SimpleHealth, wallet, recovery controller; `PlayerRecoveryMutationTests.cs`.

**Interfaces:** public `SimpleHealth.Heal(float amount) -> float` returns actual restored health; `TrySpendNonlethal(float amount, float minimumRemainingHealth = 1f) -> bool`. Add narrowly scoped internal deferred-notification mutation support for health, wallet and inventory, used by `PlayerRecoveryController.TryApply(RecoveryCardQuote quote) -> bool`.

- [ ] Test healing5HP maximum:3->5 for amount2;4->5 for amount2 returns1; dead0 stays0; negative/NaN amounts cannot corrupt health. Test spending1HP at2 succeeds to1 and at1 fails unchanged.
- [ ] Test sacrifice raises health Changed but no damageTaken/death/hitstop event. Do not implement it by sending negative damage or calling full initialization.
- [ ] Test atomic mutation: insufficient live energy/health/copies leaves every value unchanged. Event observers see the final coherent state; a callback attempting to reapply the same transaction cannot mutate twice.
- [ ] Observe failures, then implement validation-before-mutation and deferred notification publication. Resolve all dependencies before payment; no fallible operation remains between payment and effect.
- [ ] Run wallet/health/transaction tests and inspect damage/respawn regressions; commit.

## Task 4: Execute three recovery card effects through prepared commits

**Files:** recovery quote/controller, card effect kinds/operation definition, PlayerCardRuntime, PreparedCardCommit, snapshot/source and failure enum; `RecoveryCardCommitTests.cs`.

**Interfaces:** `PlayerRecoveryController.TryQuote(CardDefinitionSO card, PlayerCardCommitSnapshot snapshot, out RecoveryCardQuote quote) -> bool`; attach quote to PreparedCardCommit without replacing existing fixed Costs. `TryApply` revalidates the immutable quote and guards reentry. Add explicit failure cases for full health, insufficient health, capacity, depleted stock and stale recovery quote.

- [ ] Test sacrifice C15: health3/energy0 -> health2/energy15; health1 rejects; less than15 headroom rejects. Assert no profitable 1HP conversion cycle at C5,15,20,40 with proposed gainC and healing2C.
- [ ] Test conversion numbers from Task1 through the actual selection -> prepare -> apply route, not just the arithmetic helper.
- [ ] Test Mend: health2/count2 -> health4/count1; full health/count2 remains unchanged; cancelled/timed-out selection consumes0; repeated apply heals/spends once; depleted copy cannot prepare; partial heal uses one copy.
- [ ] Test between prepare/apply: damage, energy spend/gain, changed basis/revision, and lost stock. Reject changes that alter the quoted payment/result, publish an actionable failure and allow a fresh quote.
- [ ] Observe failures. Add reusable recovery operation vocabulary without branching on card IDs. Keep HP cost outside the wallet; do not double-pay a dynamic heal cost via both fixed Costs and quote.
- [ ] Preserve existing ordinary-card cost adjustment behavior, but explicitly reject unsupported discounts on conversion exchanges. Route legacy commit entry points through the same recovery validation if they can receive these cards.
- [ ] Pass new and existing commit/selection/snapshot tests; commit.

## Task 5: Passive and reliable hit recovery

**Files:** recovery controller, PlayerCombatEffects, player wiring; `PlayerEnergyRecoveryTests.cs`.

**Interfaces:** `PlayerRecoveryController.Tick(float gameplayDeltaTime, bool canRecover)`; read-only `NeutralCostBasis`, `PassiveEnergyCeiling`, `EnergyPerHealth`, `SacrificeEnergyGain`; wallet change subscription detects actual expenditure. Existing ResolveHitEnergy remains the single hit-grant path.

- [ ] Test C15/energy0, proposed delay3/rate5: tick2 ->0; next tick2 ->5; large tick10 ->22.5. Existing energy100 stays100. A failed spend does not restart delay; a successful one does. Hit gains may exceed22.5 up to max250.
- [ ] Test canRecover=false grants no energy and accumulates no later catch-up; pause time0; Card Time scaled delta; startup/recovery/death blocked. Unsubscribe cleanly on destruction and avoid double wallet subscriptions after rebinding.
- [ ] Test qualifying primary enemy hit gives3 once; multi-target once; misses, supplemental damage, gates and self-sacrifice give0. Existing double-energy effect gives6 while consuming one charge; kill reward still applies independently once.
- [ ] Observe failures, implement and set the existing hit chance/amount tuning to the approved values. Do not add another reward subscriber that duplicates ResolveHitEnergy.
- [ ] Pass recovery/hit/feedback tests; commit.

## Task 6: Author assets and communicate actual costs

**Files:** PlayerRecoverySetup.cs; recovery data assets; player/profile wiring; relevant existing HUD slot/feedback presenters; `RecoveryCardPresentationTests.cs`.

**Interfaces:** `PlayerRecoverySetup.CreateOrUpdate()` at `TIC/Setup/Create Or Update Player Recovery`. Proposed asset IDs: `blood-charge`, `reconstitute`, `mend`; resolve names before creation if user changes them. Show quote amounts/counts without hard-coding C or the old50 price.

- [ ] Test unavailable labels for full HP, low HP, low energy, insufficient headroom and zero copies. Confirm zero copies keep stable slot/input labels and ordinary cards show no consumable badge.
- [ ] Implement authored recovery operations and policies, tuning properties, initial stock, and controller references through Unity APIs. Extend the existing equipped Neutral profile without exceeding its capacity or removing traversal cards.
- [ ] Run setup twice; confirm unchanged GUIDs, no duplicate component/card/inventory entries, zero runtime modification of ScriptableObject assets, and proper references in Gameplay's player composition.
- [ ] Inspect UI on keyboard/gamepad: actual current payment/result is readable, hidden multipliers stay out of default copy, HP/energy/count update together, rejection feedback is useful.
- [ ] Pass presentation tests and inspect serialized diff; commit.

## Task 7: End-to-end recovery and regression validation

**Files:** PlayerRecoveryIntegrationTests.cs; new timestamped verification record in specs; existing Windows playtest builder used only if a refreshed build is requested.

- [ ] PlayMode: drain to0 with a valid traversal loadout, wait for reserve, execute traversal, verify no regen while paused/held/dead. Check Card Time activation at0 energy remains possible for zero-energy utility cards.
- [ ] Use each recovery card through actual Card Time selection; verify successful/failed/cancelled use, whole chunk remainder, no death from sacrifice, no energy for self-payment and no reentrant duplicate effects.
- [ ] Consume stock, cross Blue/Pink, die/respawn, return to title/new run; assert the approved lifetime policy and unchanged source profile. Check stable slot placement at zero quantity.
- [ ] Validate C against the entire equipped Neutral list, not currently selected card; confirm no in-run equip interface slipped into scope. If in-run equip is requested, stop and resolve the exploit policy instead of claiming 1.5C<2C alone prevents it.
- [ ] Run relevant fixtures, then the full EditMode suite once and compare named failures against the recorded inherited baseline. Report all new failures and do not claim a green suite while inherited failures remain.
- [ ] User verifies actual platforming route with minimum reserve, HP/energy tradeoff clarity, and consumable value. If geometry demands more than the reserve, document the route finding and propose a targeted content/tuning change; do not silently change confirmed multipliers.
- [ ] Save verified values, test outputs, user observations and remaining risks in a timestamped spec. Review the whole change before integration; do not upload or send builds automatically.

## Planning self-review

The plan covers recovery, three cards, player properties, costs, transaction safety, runtime counts, pause/respawn/scene lifetime and presentation. Tests use agreed literal formulas and distinguish proposed tuning. Shared interfaces and responsibilities are defined above; a transaction must publish notifications after writes rather than relying on method call order. Review focus cases map to explicit steps. Gameplay choices remain visibly pending rather than silently treated as approved.

## Execution handoff

Recommended: native execution in this chat, because inventory, resource mutation and prepared commits share state and benefit from one implementer. The independent review required by the execution workflow remains necessary; if unavailable, report that limitation explicitly. Review the paired design and proposed defaults, then this plan, before code changes.
