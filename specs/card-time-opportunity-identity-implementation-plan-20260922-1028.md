# Card Time Opportunity Identity Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Let a grounded player use consecutive Neutral cards without attacking, defer airborne Neutral rearm until landing, and allow distinct Attack1 and Attack2 Chain card opportunities.

**Architecture:** The player owns monotonically increasing opportunity IDs and publishes an immutable category/ID pair through its registered source. The world Card Time service consumes the ID attached to each session, so a new window may have the same category without reopening an old window. The controller coordinates successful Neutral rearm using live grounded state and preserves a pending airborne rearm through area streaming.

**Tech Stack:** Unity 6000.3.16f1, C# 9, existing `TicGame.Architecture` assembly, Unity Input System, existing Gameplay scene Card Time service.

**Spec:** `specs/card-time-opportunity-identity-and-neutral-rearm-sdd-20260922-1010.md`

## Global Constraints

- Keep one authoritative active Card Time session and one card commit per opportunity.
- Keep card definitions, Energy payment, effects, attack animation categories, HUD category rendering, and release-before-retrigger chord unchanged.
- Treat transient area resets as action cleanup, not a landing or full run reset; do not reuse IDs while a source remains registered.
- Preserve the user's current `Assets/Scenes/BlueArea_Tutorial.unity`, `Assets/Scenes/PinkArea_Perimeters.unity`, and untracked `tic/` work.
- The user chose to skip Unity unit-test execution for now. Validate compilation and a focused manual Play Mode matrix; do not report EditMode tests as run.
- Before any scene, prefab, serialized asset, or Editor-tool change, reread `specs/unity-editor-collaboration-workflow-20260612-1609.md`. No such asset change is planned.

## Review Focus

1. A stale opportunity republished after `None` must stay consumed; verify in Task 1's runtime check and Task 3's manual pass.
2. Attack2 must receive a new Chain ID even though Attack1 was also Chain; verify in Task 2's code inspection and Task 3's Play Mode sequence.
3. An airborne Neutral use followed by area streaming must not bypass the landing requirement; verify in Task 2's state review and Task 3's Play Mode pass.
4. A grounded Neutral commit must not reactivate from held chord buttons; verify in Task 3's Play Mode pass.
5. Unregistering and registering a new player source must not inherit an old source's consumed ID; verify in Task 1's source-lifecycle review and Task 3's Gameplay-scene reload pass.

---

## File Map

- Create `Assets/Scrips/Architecture/Player/CardTime/CardTimeOpportunity.cs`: immutable category/ID value with `None`; Unity generates its `.meta` on import.
- Modify `Assets/Scrips/Architecture/Player/CardTime/CardTimeActiveSession.cs`: retain the opportunity ID separately from the existing session ID.
- Modify `Assets/Scrips/Architecture/Player/CardTime/PlayerCardTimeRuntime.cs`: offer, activate, consume, and reject by opportunity ID while preserving category timing rules.
- Modify `Assets/Scrips/Architecture/Player/PlayerAttackComboRuntime.cs`: mint IDs for attack starts and Neutral rearm; keep pending airborne Neutral state through transient clear.
- Modify `Assets/Scrips/Architecture/Player/CardTime/IPlayerCardTimeSource.cs` and `Assets/Scrips/Architecture/Runtime/CardTimeSessionController.cs`: pass the pair through the registered player source and clear source-scoped state on unregister.
- Modify `Assets/Scrips/Architecture/Player/Runtime/PlayerController.cs`: publish the pair and coordinate commit, landing, timeout, reset, and selection cleanup.
- Modify `Assets/Tests/EditMode/Architecture/Player/GameplayStartupTests.cs`: update its source fake signature so the test assembly still compiles; do not execute unit tests in this slice.
- Modify `Assets/Scrips/Architecture/Player/Runtime/PlayerDeathRespawn.cs`: call the controller's explicit full-run Card Time reset on death, before either respawn route. Do not make `ResetTransientState` clear the airborne pending rearm.

### Task 1: Give the World Session an Opportunity Identity

**Files:** Create `Assets/Scrips/Architecture/Player/CardTime/CardTimeOpportunity.cs`; modify `CardTimeActiveSession.cs`, `PlayerCardTimeRuntime.cs`, `IPlayerCardTimeSource.cs`, and `CardTimeSessionController.cs` at the paths in File Map.

**Interfaces:** Produce `CardTimeOpportunity(PlayerCardTimeState category, long opportunityId)`, `CardTimeOpportunity.None`, `IPlayerCardTimeSource.PublishAvailability(CardTimeOpportunity opportunity)`, and `PlayerCardTimeRuntime.PublishAvailability(CardTimeOpportunity opportunity)`. Existing `CardTimeSessionSnapshot.ActiveSessionId` remains a distinct activation ID.

- [ ] **Step 1: Add the immutable value and thread it through the source.** Use a `readonly struct` with `Category`, `OpportunityId`, and `None`, validate that a non-`None` category has a positive ID, and add equality so repeat publication is identifiable. Add the pair overload to the source/token/controller while retaining the existing category overload as a migration bridge. `PlayerController` can keep using the old overload until Task 3, so the runtime assembly compiles after this task.

```csharp
public readonly struct CardTimeOpportunity : IEquatable<CardTimeOpportunity>
{
    public static CardTimeOpportunity None => default;
    public CardTimeOpportunity(PlayerCardTimeState category, long opportunityId);
    public PlayerCardTimeState Category { get; }
    public long OpportunityId { get; }
    public bool IsValid => Category != PlayerCardTimeState.None && OpportunityId > 0;
}

void IPlayerCardTimeSource.PublishAvailability(CardTimeOpportunity opportunity);
// Existing PublishAvailability(PlayerCardTimeState) remains until Task 3.
```

- [ ] **Step 2: Replace the consumed-category latch with a consumed-ID latch.** Keep `publishedOpportunity`, `availableOpportunity`, and `consumedOpportunityId`. `None` does not release consumption. The same consumed ID cannot be offered again, even after `None`; a new positive ID can offer the same category. Store the offered ID on activation, then consume that exact ID on commit, explicit cancel, and timeout. Keep category-based post-window grace and input-buffer timing. A failed `TryCommit` transaction leaves the active session and its ID untouched.

```csharp
// Key transition, inside PublishAvailability(CardTimeOpportunity next):
if (state == CardTimeSessionState.Active)
{
    publishedOpportunity = next;
    return;
}
if (next.IsValid && next.OpportunityId == consumedOpportunityId)
{
    availableOpportunity = CardTimeOpportunity.None;
    state = CardTimeSessionState.Unavailable;
    return;
}
// Otherwise preserve the existing category-based grace/buffer behavior,
// but assign availableOpportunity = next when the new offer takes effect.
```

The implementation must also set `availableOpportunity = None` when `next` is `None` after grace, and `RequestActivation` must reject while only a consumed ID is published. Avoid using a category change as a substitute for a new ID in production. Retain the old category-only `PlayerCardTimeRuntime.PublishAvailability(PlayerCardTimeState)` overload solely to keep existing EditMode source calls compiling: its compatibility adapter assigns ID 1 to the first nonempty category, increments only on a different nonempty category, and keeps the same ID across `None -> same category`. Mark that overload obsolete in its XML documentation and do not call it from gameplay code.

- [ ] **Step 3: Scope consumed IDs to the registered source.** Add `PlayerCardTimeRuntime.ClearSource()` that clears published/available/consumed opportunity and buffered input without resetting `nextSessionId`. In `CardTimeSessionController.Unregister`, cancel any active session, call `ClearSource()`, then invalidate the token. A newly registered source beginning at ID 1 must be accepted.

```csharp
private void Unregister(PlayerSourceToken source)
{
    if (!IsAuthorized(source)) return;
    runtime.Cancel();
    runtime.ClearSource();
    source.Invalidate();
    activeSource = null;
}
```

- [ ] **Step 4: Compile and inspect the boundary.** After Unity imports the new `.cs` file and regenerates project files, run `dotnet build "TicGame.Architecture.csproj" --no-restore -v:q`. Check `rg -n 'PublishAvailability\(' Assets/Scrips -g '*.cs'` to confirm both overloads route to the runtime while migration is in progress. Do not run EditMode tests. Commit only Task 1 files once compilation succeeds.

### Task 2: Mint New Player Opportunities at Real Boundaries

**Files:** Modify `Assets/Scrips/Architecture/Player/PlayerAttackComboRuntime.cs`; adjust only the Task 1 controller adapter needed to compile. Keep existing card selection, effect, and damage files unchanged.

**Interfaces:** Produce `PlayerAttackComboRuntime.AvailableOpportunity`, `NotifyCardCommitted(bool isGrounded)`, `NotifyCardCancelled()`, `NotifyGrounded(bool isGrounded)`, `RequestNeutralRearmAfterTimeout()`, and `ResetForFullRun()`. Preserve `AvailableCardTime` as a projection of `AvailableOpportunity.Category` for existing consumers and test compilation.

- [ ] **Step 1: Move ID generation into the combo runtime.** Initialize one Neutral opportunity. Increment a private `nextOpportunityId` only when a genuinely new opportunity starts: successful Attack1/2/3 start, transition from an attack category back to Neutral, a grounded Neutral rearm after commit, or fulfillment of a pending rearm on landing. Repeated `Tick` or frame publication returns the same ID. `NotifyAttackStarted(Attack2)` creates a fresh Chain ID even when Attack1's current category was Chain.

```csharp
private long nextOpportunityId = 1;
private CardTimeOpportunity currentOpportunity =
    new(PlayerCardTimeState.Neutral, 1);
private long consumedOpportunityId;
private bool neutralRearmPendingGrounding;

public CardTimeOpportunity AvailableOpportunity =>
    currentOpportunity.OpportunityId == consumedOpportunityId
        || neutralRearmPendingGrounding &&
           currentOpportunity.Category == PlayerCardTimeState.Neutral
        ? CardTimeOpportunity.None
        : currentOpportunity;

private void BeginOpportunity(PlayerCardTimeState category) =>
    currentOpportunity = new CardTimeOpportunity(category, ++nextOpportunityId);
```

- [ ] **Step 2: Apply the agreed Neutral commit rule.** On successful Neutral commit, consume the current ID. If grounded and still Neutral, begin a fresh Neutral opportunity after the active selection is disposed; if airborne, set `neutralRearmPendingGrounding`. An attack may begin and mint Chain IDs while this flag remains set. `NotifyGrounded(true)` may fulfill the pending rearm only when the current category is Neutral; landing during Chain leaves the pending flag until the return to Neutral. A new Neutral ID remains valid if the player later jumps without using it. On Chain/Finisher commit, consume only that attack window's ID.

```csharp
public void NotifyCardCommitted(bool isGrounded)
{
    consumedOpportunityId = currentOpportunity.OpportunityId;
    if (currentOpportunity.Category != PlayerCardTimeState.Neutral) return;
    neutralRearmPendingGrounding = !isGrounded;
    if (isGrounded) BeginOpportunity(PlayerCardTimeState.Neutral);
}
```

`NotifyCardCancelled()` consumes the current ID without creating a new one, retaining the existing one-attempt cancellation behavior. `RequestNeutralRearmAfterTimeout()` marks a pending grounded Neutral rearm; `NotifyGrounded(true)` fulfills it only when the current category is Neutral. The world runtime consumes the timed-out ID, so the old ID cannot reopen before landing. A failed commit transaction does not invoke either method.

- [ ] **Step 3: Preserve the airborne wait across transient reset.** `Clear()` resets action/combo timing but does not reset `nextOpportunityId` or `neutralRearmPendingGrounding`. It returns to Neutral with a new ID only when leaving a different category without a pending airborne rearm; clearing an already consumed Neutral opportunity keeps that ID consumed. If the pending flag is set, it leaves Neutral unavailable even though the action state is cleared. Add a separate `ResetForFullRun()` that clears the pending flag and starts a fresh Neutral ID for death/run restart; use it only from the actual death/reset path, not area streaming. Never initialize the next ID back to 1 while the source remains registered.

- [ ] **Step 4: Compile and inspect state transitions.** Run `dotnet build "TicGame.Architecture.csproj" --no-restore -v:q` after Unity refresh. Inspect `NotifyAttackStarted`, `Tick`, `Clear`, and `NotifyGrounded` against this sequence: airborne Neutral commit → Attack1 Chain ID A → Attack2 Chain ID B → return to Neutral while airborne remains unavailable → grounded contact yields one Neutral ID. Commit only Task 2 files when the runtime assembly compiles.

### Task 3: Wire Commit, Landing, And Manual Play Mode Verification

**Files:** Modify `Assets/Scrips/Architecture/Player/Runtime/PlayerController.cs` and `Assets/Scrips/Architecture/Player/Runtime/PlayerDeathRespawn.cs`; update `Assets/Tests/EditMode/Architecture/Player/GameplayStartupTests.cs` fake signature. No scene or prefab edits.

**Interfaces:** Consume `AvailableOpportunity`, `NotifyCardCommitted(bool)`, `NotifyCardCancelled()`, `NotifyGrounded(bool)`, and the registered `PublishAvailability(CardTimeOpportunity)` source contract.

- [ ] **Step 1: Publish the player-owned opportunity.** Change `PublishCardTimeAvailability()` to publish `attackCombo.AvailableOpportunity` while unlocked, otherwise `CardTimeOpportunity.None`. In `FixedUpdate`, use refreshed `sensors.IsGrounded` to notify the combo runtime and publish if landing fulfilled a pending rearm. Keep the active session unchanged during landing. All disable, unregister, locked, and transient-clear paths publish `None`, never a fabricated ID. Remove the migration category overload from `IPlayerCardTimeSource` and `CardTimeSessionController` after updating every production call and the test fake; retain only the old pure-runtime overload used by existing tests.

```csharp
private void PublishCardTimeAvailability()
{
    cardTimeSource?.PublishAvailability(cardTimeUnlocked
        ? attackCombo.AvailableOpportunity
        : CardTimeOpportunity.None);
}
```

- [ ] **Step 2: Sequence successful commit after selection cleanup.** After `TryCommit(readiness.Commit)` succeeds, close the selection, tell the combo runtime the card committed using live `sensors.IsGrounded`, then publish its next opportunity. The world service has already consumed the old ID, so a new grounded Neutral ID can be offered immediately. Failure in card preparation/payment must spend no Energy and must not call `NotifyCardCommitted`. Explicit cancel calls `NotifyCardCancelled`, publishes `None`, and retains its existing single-attempt behavior.

```csharp
if (cardTimeSource?.TryCommit(readiness.Commit) == true)
{
    cardSelectionHud?.PlaySlotAnimation(
        feedbackSlotIndex, CardTimeSelectionSlotAnimation.Committed);
    DisposeActiveCardSelection();
    attackCombo.NotifyCardCommitted(sensors?.IsGrounded == true);
    PublishCardTimeAvailability();
}
```

- [ ] **Step 3: Keep timeout, streaming, and death distinct.** On a Neutral timeout transition, call `attackCombo.RequestNeutralRearmAfterTimeout()`; let `NotifyGrounded(true)` generate the next ID when the combat category is Neutral. Keep `ResetTransientState()` pending-rearm-safe for `GameplayAreaCoordinator` and `PlayerWorldHold`. Add `PlayerController.ResetCardTimeForFullRun()` to cancel an active session, call `attackCombo.ResetForFullRun()`, and publish `None` while death handling proceeds; ordinary live-player publication offers the fresh Neutral ID after respawn. Call it from `PlayerDeathRespawn.HandleHealthChanged` before choosing either respawn route. Update the `GameplayStartupTests` fake's publication signature to accept `CardTimeOpportunity` so the test assembly compiles, but do not execute tests.

- [ ] **Step 4: Compile the runtime and test assemblies without running tests.** After Unity refresh, run `dotnet build "TicGame.Architecture.csproj" --no-restore -v:q` and `dotnet build "TicGame.Architecture.EditModeTests.csproj" --no-restore -v:q`. Inspect the Console for compiler errors. Check `git diff --check` and ensure only intended C# and generated `.meta` files changed before committing Task 3.

- [ ] **Step 5: Ask for one focused Play Mode report.** In the current Gameplay scene, have the user check: (a) grounded Neutral card → release chord → Neutral is available again without attack; holding chord does not reopen; (b) airborne Neutral card → `NO CARD TIME` while still airborne → landing restores Neutral; (c) airborne Neutral card → Attack1 Chain commit → Attack2 Chain commit; (d) a `None` transition, area stream, respawn, and Gameplay-scene reload do not duplicate or permanently lose opportunities; (e) Finisher and Card Time slowdown end normally. Record observed results in a new timestamped verification note under `specs/`, distinguishing confirmed Play Mode behavior from unrun unit tests.

## Plan Self-Review Checklist

- [ ] Spec coverage: ID ownership, same-category Chain, grounded/airborne Neutral, chord, active-session stability, grace/buffer, cancel/timeout, streaming/death, and HUD category projection each have an owning task.
- [ ] A scan for unfinished-work markers finds no placeholder instructions in this plan.
- [ ] Interface names used in Tasks 2-3 match those produced in Tasks 1-2.
- [ ] Each Review Focus case has a named inspection or manual check in its owning task.
- [ ] Do not start product-code changes until the user reviews this plan and chooses the execution approach.
