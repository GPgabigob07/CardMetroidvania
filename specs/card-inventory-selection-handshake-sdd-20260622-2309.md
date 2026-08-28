# Card Inventory Selection Handshake SDD - 20260622-2309

## Contexto

This specification records the next architectural direction for card
inventory, loadouts, Card Time selection UI, and player-side card execution.
It exists because the current prototype already has:

- one world-scoped authoritative Card Time session;
- asset-backed `CardDefinitionSO` cards;
- player-local card payment and effect application;
- a first test/profile inventory asset;
- a visual UI prototype for separated Card Time pickers.

However, inventory, loadouts, selected-card UI, and commit execution must not
be wired as a direct UI-to-player shortcut. The user proposed a handshake-style
flow similar to a peer-to-peer negotiation:

```text
player asks for Card Time
world answers if a session can open
world/UI asks for card selection
player commits a selection
selected card is sent back to the player
player CardReader executes or rejects from current player status
```

Sources used:

- `AGENTS.md`
- `gdd/gdd-canonico-20260526-2331.md`
- `specs/persistent-gameplay-services-and-card-time-ownership-sdd-20260614-1107.md`
- `specs/card-time-session-state-machine-refactor-sdd-20260614-2326.md`
- `specs/card-effects-and-energy-planning-20260614-1239.md`
- `specs/asset-driven-card-definitions-and-commit-transaction-sdd-20260614-1833.md`
- `output/card-ui-prototypes/README.md`
- current code under `Assets/Scrips/Architecture`

## Decision Summary

Card Time remains world-scoped.

Card selection negotiation becomes world/session-scoped.

Card reading and effect arming remain player-scoped.

Inventory and loadouts are player or game-session progression data, persisted
as stable ids rather than scene references.

The UI is a participant in selection presentation only. It must never be the
source of gameplay truth. If the UI disappears, rebuilds, reloads, or misses a
frame, the authoritative selection session still exists outside the UI.

## Why Not Keep Everything On The Player?

Keeping all card state on the player is simple while Card Time is only one
button and one equipped slot. It becomes fragile when Card Time has:

- non-pausing UI overlays;
- menus and selection state;
- timeout behavior;
- input negotiation;
- observers such as HUD, camera, VFX, audio, and enemies;
- future enemy reactions to Card Time;
- possible scene or UI reconstruction while gameplay continues.

The player should not own menu/session state that other world systems observe
and react to. The player should own the consequences of the selected card on
the player's body, resources, combat output, and movement state.

## Ownership Model

### World Scoped

#### CardTimeSessionController

Owns:

- one authoritative Card Time session;
- player-source authorization;
- active session lifetime;
- timeout, cancellation, and commit outcome;
- global slowdown request through gameplay time services.

It does not:

- know card effect rules;
- own player inventory;
- spend player resources;
- mutate player combat or movement state.

#### CardSelectionSession

New world/session concept.

Owns:

- active Card Time category;
- candidate card ids for that session;
- selected candidate index or selected card id;
- whether a selection exists;
- selection timeout/cancel state coupled to the active Card Time session;
- non-paused UI presentation state.

It does not:

- decide whether a card can execute;
- spend resources;
- install player effects;
- persist permanent inventory.

This may initially live beside `CardTimeSessionController`, but should be a
focused collaborator rather than being folded into a broad service blob.

#### Card Catalog

World or project-level read-only resolver.

Owns:

- stable card id to `CardDefinitionSO` resolution;
- lookup of authored cards available in the build;
- validation diagnostics for missing ids.

It does not:

- own which cards the player has;
- own equipped loadouts;
- mutate card assets.

### Player Or Game Session Scoped

#### CardInventory

Owns:

- owned card ids;
- counts only if duplicate rewards become relevant;
- progression unlock state for cards.

It should persist as stable ids:

```text
ownedCardIds[]
```

For the prototype, this can be represented by
`PlayerCardInventoryProfileSO` in Editor/testing and by plain save data in the
future save file.

#### CardLoadout

Owns:

- equipped card ids per Card Time type;
- per-type capacity limits;
- player-authored order inside that Card Time picker.

Prototype caps:

```text
Neutral  -> 8
Chain    -> 6
Finisher -> 4
```

Future five-type Card Time should extend the same structure instead of adding
parallel fields for each type.

Persist shape:

```text
loadouts[]
    category
    equippedCardIds[]
```

If final persistence wants a compact int matrix, this save data can be mapped
to it:

```text
cardCatalogIndex[card id] -> int
loadoutMatrix[cardTimeIndex][slotIndex] -> cardCatalogIndex or -1
ownedCardBits/cardCounts -> owned state
```

The gameplay API should still speak stable ids and card definitions at the
boundary, so catalog reorder does not corrupt saves.

#### PlayerStatus

The system needs a status query boundary, but not necessarily one giant
`PlayerStatus` component.

Use a small read facade or context assembled from existing player-local
systems:

```text
PlayerResourceWallet
SimpleHealth / future health service
PlayerCombatEffects
PlayerExtraJumpRuntime
future status modifiers
```

The CardReader asks this boundary for facts that affect card validation:

- current Energy;
- maximum Energy;
- health or life-spend availability;
- cost deltas;
- resource gain deltas;
- status locks;
- airborne/grounded state;
- ability availability;
- active card-related modifiers.

#### PlayerCardReader

New player-scoped execution boundary.

Owns:

- turning a selected `CardDefinitionSO` into a player-local prepared command;
- querying current player status;
- applying cost deltas and context deltas;
- atomically spending resources;
- arming status watchers and effect runtimes;
- reporting rejection reasons.

It should replace the current role of `PlayerCardRuntime` over time, or
`PlayerCardRuntime` should be refactored until it effectively becomes this
CardReader.

It does not:

- own Card Time session state;
- own card selection UI state;
- decide which cards are equipped;
- own global slowdown.

## Handshake Flow

### 1. Availability Publication

Player combat state publishes Card Time opportunities to the world service:

```text
PlayerCombo/Animation
    -> PublishAvailability(category, optional opportunity id)
```

The service may ignore publication when:

- category is `None`;
- the same consumed opportunity is being republished;
- another active session already exists;
- source token is invalid.

### 2. Activation Request

Player input asks for Card Time:

```text
PlayerCardTimeInput
    -> RequestActivation()
```

If unavailable:

```text
world answers Rejected
no UI opens
gameplay continues
```

If available:

```text
world creates CardTimeSession
world creates CardSelectionSession
world asks loadout provider for candidates
UI observes session and renders candidates
gameplay continues under Card Time rules
```

### 3. Candidate Resolution

Candidate cards come from player or game-session loadout data:

```text
CardSelectionSession(category)
    -> CardLoadoutProvider.GetEquippedCards(category)
    -> card ids
    -> CardCatalog resolves definitions for UI display
```

Rules:

- only equipped cards for the active Card Time type are candidates;
- missing card ids are skipped and logged;
- invalid category mismatches are skipped and surfaced in diagnostics;
- empty candidates keep Card Time active only if design wants a visible
  "no card" failure; first prototype may cancel immediately with feedback.

### 4. Selection Navigation

UI and input update world selection state:

```text
SelectNext()
SelectPrevious()
SelectIndex(index)
```

Selection state is authoritative outside the visual UI. The UI is rebuilt from
the selection snapshot:

```text
category
candidates
selectedIndex
energy readout
remaining time
disabled/rejection preview
```

If the UI is destroyed and recreated, it binds to the active selection session
and resumes display.

### 5. Commit Request

Player presses the commit action:

```text
PlayerCardTimeInput
    -> CardSelectionSession.TryGetSelectedCard()
```

If no card is selected:

```text
ignored or rejected feedback
Card Time remains active
no resource is spent
```

If a card is selected:

```text
selected card id/definition is sent to PlayerCardReader
```

The CardReader prepares execution:

```text
PlayerCardReader.TryPrepare(selectedCard, context)
    -> queries PlayerStatus/resources
    -> applies cost/status deltas
    -> validates category and conditions
    -> creates PreparedCardCommand
```

If preparation fails:

```text
selection session remains active
UI shows reason
Card Time remains active unless timeout/cancel occurs
```

If preparation succeeds:

```text
CardTimeSource.TryCommit(prepared.TryApply)
```

The world Card Time service invokes the transaction exactly once.

Transaction false:

```text
Card Time remains active
selection remains active
no outcome committed
```

Transaction true:

```text
resources spent
effects/status watchers armed
Card Time emits Committed outcome
selection session closes
player combo opportunity is consumed
```

## Ordering

The following operations do not need strict visual order, but the authoritative
transaction order must be deterministic:

```text
1. selected card exists
2. PlayerCardReader prepares from current player status
3. Card Time commit transaction is requested
4. Card Time service invokes prepared transaction exactly once
5. transaction applies cost/effects
6. Card Time commits only if transaction succeeds
7. UI closes from committed outcome
```

UI animation, sound, camera, and VFX may react in any order after the outcome
event, but they cannot change the outcome.

## UI Rules

Card Time UI is non-blocking.

It must:

- render from world selection/session snapshots;
- show current Energy prominently while open;
- show remaining Card Time;
- show current selected card;
- show disabled cards when known;
- tolerate missing or delayed preview reasons;
- close from session outcomes, not from local button assumptions.

It must not:

- pause gameplay;
- hold authoritative selected-card state only in view objects;
- spend resources;
- call card effects directly;
- activate Card Time without a player source token.

Recommended presentation:

```text
Neutral  -> lower-third fan, up to 8
Chain    -> grid, up to 6
Finisher -> lower-third horizontal list, up to 4
```

The Finisher visual design can change later without affecting this flow.

## Inventory And Equip Screen Rules

The equip screen edits inventory/loadout data, not active Card Time session
data.

It should:

- read owned card ids;
- resolve definitions through catalog;
- write equipped card ids by Card Time type;
- enforce per-type capacity;
- preserve slot order;
- validate ownership before equipping;
- filter by card type, owned/unowned, equipped/unequipped, affordability if a
  preview player status exists.

It should not:

- require an active Card Time session;
- directly mutate the in-combat selection session;
- apply card effects;
- depend on combat UI objects.

In a running game, changing loadout while Card Time is active should be
explicitly disallowed for the prototype. Later, if allowed, the active
selection session should keep the candidate snapshot it opened with.

## Persistence

The durable save can be compact, but the authoring/runtime boundary should
stay readable.

Recommended conceptual save:

```text
PlayerProgressionSave
    progressionKeys[]
    cardInventory
        ownedCardIds[]
        loadouts[]
            category
            equippedCardIds[]
```

Possible compact save:

```text
ownedCards: bitset or int count array indexed by card catalog index
loadoutMatrix: int[cardTimeCount, maxSlots]
progressionKeys: int/string keys
```

Rules:

- save card identity by stable card id or by a catalog index that is generated
  from stable ids;
- never persist Unity instance ids;
- do not persist active Card Time sessions;
- do not persist active selection UI state;
- do persist unlocked/owned cards and equipped loadout order;
- active card buffs/statuses need their own policy later, depending on whether
  saves can happen mid-combat.

For the prototype, `PlayerCardInventoryProfileSO` is an Editor/test profile,
not the final save system.

## Required Interfaces

Names are provisional.

### `ICardLoadoutProvider`

```csharp
public interface ICardLoadoutProvider
{
    /// <summary>
    /// Gets the equipped card ids for a Card Time type in presentation order.
    /// </summary>
    IReadOnlyList<string> GetEquippedCardIds(PlayerCardTimeState category);
}
```

### `ICardCatalog`

```csharp
public interface ICardCatalog
{
    /// <summary>
    /// Resolves a stable card id to its authored definition.
    /// </summary>
    bool TryGetCard(string id, out CardDefinitionSO card);
}
```

### `ICardSelectionSession`

```csharp
public interface ICardSelectionSession
{
    /// <summary>
    /// Gets the latest selection snapshot for UI and diagnostics.
    /// </summary>
    CardSelectionSnapshot Current { get; }

    /// <summary>
    /// Moves the selected candidate by a signed offset.
    /// </summary>
    bool MoveSelection(int direction);

    /// <summary>
    /// Selects a candidate by index.
    /// </summary>
    bool SelectIndex(int index);

    /// <summary>
    /// Gets the currently selected card, if any.
    /// </summary>
    bool TryGetSelectedCard(out CardDefinitionSO card);
}
```

### `IPlayerCardReader`

```csharp
public interface IPlayerCardReader
{
    /// <summary>
    /// Validates the selected card against the current player state and
    /// returns a single-use command that can be committed by Card Time.
    /// </summary>
    CardReadinessResult TryPrepare(
        CardDefinitionSO card,
        in PlayerCardReadContext context);
}
```

### `IPlayerCardStatusView`

```csharp
public interface IPlayerCardStatusView
{
    /// <summary>
    /// Gets current resource and status facts that can affect card costs,
    /// availability, or execution.
    /// </summary>
    PlayerCardStatusSnapshot Current { get; }
}
```

## Implementation Phases

### Phase 1: Data Boundary

1. Keep `PlayerCardInventoryProfileSO` as the Editor/test authoring profile.
2. Add `ICardLoadoutProvider`.
3. Add a catalog resolver for card definition assets.
4. Add tests for id resolution, missing ids, and loadout order.

### Phase 2: Selection Session

1. Add pure `CardSelectionSession` runtime.
2. Feed it category plus candidate ids.
3. Add movement/select/selected-card tests.
4. Make it independent from UI objects.

### Phase 3: CardReader Preparation

1. Refactor `PlayerCardRuntime` toward `PlayerCardReader`.
2. Add selected-card preparation.
3. Add structured rejection reasons.
4. Query resource/status facts before commit.

### Phase 4: Atomic Card Time Commit

1. Change `IPlayerCardTimeSource.TryCommit()` to accept a synchronous
   transaction.
2. Commit only prepared card commands.
3. Consume combo opportunity only after success.
4. Keep session active on transaction failure.

### Phase 5: Non-Blocking Combat UI

1. Add Card Time selection UI as an observer/controller of selection session.
2. Bind Energy/current resources through status snapshots.
3. Keep selection state authoritative outside the visual objects.
4. Test UI teardown/rebind conceptually with selection-session tests.

### Phase 6: Equip Screen

1. Build an Editor/runtime screen that edits inventory/loadout data.
2. Use the same loadout provider shape as combat selection.
3. Add filters by card type and equipped/owned state.
4. Defer final persistence integration until the broader save system exists.

## Test Matrix

### Inventory

- owned cards persist by stable id;
- duplicate owned card ids are rejected or merged;
- equipped cards must be owned;
- equipped cards must match Card Time type;
- loadout capacity is enforced;
- loadout order round-trips through save data.

### Selection Session

- activation creates candidates from the active category;
- empty candidates produce a clear no-card state;
- selection survives UI rebuild;
- selection wraps or clamps according to chosen input rule;
- selected card id resolves through catalog;
- missing catalog entries do not crash.

### Handshake

- activation rejected means no selection session opens;
- no selected card means commit is ignored/rejected without spending;
- preparation failure leaves Card Time active;
- transaction failure leaves Card Time active;
- transaction success closes selection and commits Card Time;
- player combo opportunity is consumed only on success.

### Player CardReader

- cost deltas affect affordability;
- status locks can reject card preparation;
- health/energy deltas are honored;
- effect watchers are armed only after payment;
- prepared command is single-use.

### UI

- UI renders from snapshot after creation;
- UI can be destroyed/recreated without losing selection;
- Energy readout updates while Card Time is active;
- timeout closes UI from world outcome;
- UI does not pause gameplay;
- UI cannot activate Card Time without player source authority.

## Open Decisions

- Final names for the five Card Time types if Alpha/Beta/Epsilon/Omega/Lambda
  return as player-visible states.
- Whether active selection should default to the first card, the last used
  card, or no card.
- Whether selection navigation wraps.
- Whether no-card activation should open an empty UI, flash a rejection, or
  cancel immediately.
- Whether loadout edits are allowed during active gameplay outside safe rooms.
- Whether final save uses stable strings directly or a generated int catalog.
- Whether temporary card buffs can be saved mid-run or are treated as combat
  volatile state.

## Acceptance Criteria

- Card Time and selection session are world-scoped and observable.
- UI is stateless/rebuildable relative to authoritative selection state.
- Player owns card reading, payment, status queries, and effect arming.
- Inventory and loadouts persist independently of active Card Time.
- Combat commit follows ask-answer-execute semantics.
- Failed selection or failed preparation does not consume Card Time.
- Successful prepared transaction spends and applies exactly once.
- The architecture scales from three prototype Card Times to five without
  replacing persistence shape.
