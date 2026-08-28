# Card Time Session State Machine Refactor SDD - 20260614-2326

## Contexto

This specification plans a future Card Time runtime refactor after debugging a
Finisher defect where a committed interaction appeared to activate a second
time with the remaining duration of the first activation.

The remaining-time symptom is important. It indicates that completed session
data can remain live-looking after commit, rather than proving that a genuinely
new availability window was created.

The current pure runtime stores related session facts in separate mutable
fields:

```text
state
availableCardTime
publishedCardTime
sessionCardTime
activeElapsed
terminalCardTime
inputBufferRemaining
postWindowGraceRemaining
```

This representation permits combinations such as a terminal state retaining
the category or elapsed time of an earlier active session. Cleanup currently
depends on every terminal path resetting every related field correctly.

This document specifies a state-machine model with an explicit active session
object. It is a planning document only; it does not authorize an immediate
large implementation change.

## Historico

This specification refines, but does not replace, the following decisions:

- `specs/card-time-and-training-dummy-review-slice-sdd-20260612-1701.md`
- `specs/card-time-input-leniency-sdd-20260612-1803.md`
- `specs/card-time-chord-and-neutral-handoff-sdd-20260612-1815.md`
- `specs/player-and-global-runtime-responsibility-refactor-sdd-20260614-1013.md`
- `specs/persistent-gameplay-services-and-card-time-ownership-sdd-20260614-1107.md`
- `specs/asset-driven-card-definitions-and-commit-transaction-sdd-20260614-1833.md`

Earlier specifications model `Committed` and `Cancelled` as runtime states.
This document changes their role: they become transition outcomes, while the
durable runtime state after completion contains no active session.

## Goals

- Make it structurally impossible for a non-active state to retain an active
  session timer.
- Make session creation and destruction explicit and atomic.
- Preserve one global authoritative Card Time session.
- Preserve player-source mutation authority.
- Preserve input buffering and post-window grace.
- Preserve globally observable commit, cancel, and timeout outcomes.
- Preserve `GameplayTimeCoordinator` as the only production writer of Unity
  global time.
- Preserve the current HUD and awareness behavior through a staged migration.
- Define the exact blast radius before implementation begins.

## Non-Goals

- Replacing `PlayerCardTimeState`.
- Changing Neutral, Chain, or Finisher timing rules.
- Changing combo ownership or attack animation authoring.
- Making availability fully event-driven in the same refactor.
- Changing card selection, inventory, or equipped-card storage.
- Changing card effect execution.
- Adding multiple simultaneous Card Time sessions.
- Allowing enemies or UI systems to activate Card Time.
- Replacing `GameplayTimeCoordinator`.
- Designing save persistence for an active Card Time session.

## Confirmed Existing Boundaries

### Global Ownership

`CardTimeSessionController` remains the one global authoritative owner.

Only a registered `IPlayerCardTimeSource` may:

- publish availability;
- request activation;
- commit;
- cancel;
- unregister.

Observers receive snapshots and outcomes but no mutation token.

### Time Ownership

`GameplayTimeCoordinator` remains unchanged.

The Card Time controller requests the Card Time modifier only while the
authoritative runtime state is active. Leaving the active state removes that
modifier.

### Card Ownership

`PlayerCardRuntime` continues to own:

- equipped `CardDefinitionSO` resolution;
- card eligibility;
- cost validation;
- payment;
- effect application.

The Card Time state machine knows only the Card Time category and the result
of a synchronous commit transaction.

## Proposed Domain Model

The runtime has three durable states:

```text
Unavailable
Available(opportunity)
Active(session)
```

Commit, explicit cancel, timeout, teardown, and replacement are transition
outcomes. They are not durable states containing session data.

### Opportunity

An opportunity represents an activatable Card Time category:

```csharp
public readonly record struct CardTimeOpportunity(
    PlayerCardTimeState Category);
```

The first implementation may omit this wrapper and store the category directly
if the wrapper adds no immediate value. The conceptual distinction must still
remain:

```text
published category != active session
```

### Active Session

An active session is a single object created only on successful activation:

```csharp
public sealed class CardTimeActiveSession
{
    public CardTimeActiveSession(
        long id,
        PlayerCardTimeState category,
        float maximumDuration)
    {
        Id = id;
        Category = category;
        MaximumDuration = maximumDuration;
    }

    public long Id { get; }
    public PlayerCardTimeState Category { get; }
    public float MaximumDuration { get; }
    public float Elapsed { get; private set; }
    public float Remaining => Mathf.Max(0f, MaximumDuration - Elapsed);

    public void Tick(float unscaledDeltaTime)
    {
        Elapsed = Mathf.Min(
            MaximumDuration,
            Elapsed + unscaledDeltaTime);
    }
}
```

The concrete type may be an internal class. Observers must never receive the
mutable object directly.

The monotonically increasing `Id` is recommended for diagnostics and tests. It
allows logs to distinguish:

```text
same session published twice
new session for the same category
```

### State Representation

C# does not provide Kotlin-style sealed interfaces with exhaustive `when`
expressions, but nested sealed records or private state classes provide a
similar closed hierarchy:

```csharp
public abstract record CardTimeRuntimeState
{
    private CardTimeRuntimeState() {}

    public sealed record Unavailable : CardTimeRuntimeState;

    public sealed record Available(
        PlayerCardTimeState Category) : CardTimeRuntimeState;

    public sealed record Active(
        CardTimeActiveSession Session) : CardTimeRuntimeState;
}
```

The exact use of records is optional. Unity serialization is not required
because this is a pure runtime model.

The required invariant is:

```text
state is Active  <=>  exactly one active session object exists
state is not Active  =>  no active session object or active elapsed timer exists
```

### Availability and Leniency State

Availability publication and leniency remain separate from the active session:

```text
latestPublishedCategory
offeredCategory
consumedCategory
inputBufferRemaining
postWindowGraceRemaining
```

These values must not contain or reuse an active session object.

`consumedCategory` prevents repeated per-frame publication from recreating the
same opportunity after commit or cancel. It is released only by a meaningful
source transition to a different non-empty category.

This preserves the current protection:

```text
Finisher -> activate -> commit
source publishes None
source republishes Finisher
    => no new opportunity
```

It also allows:

```text
Finisher -> activate -> commit
source advances to Neutral
    => new Neutral opportunity
```

## Transition Outcomes

Observers need to know why an active session ended without retaining the
session as live state.

Use an explicit outcome:

```csharp
public enum CardTimeSessionOutcome
{
    None = 0,
    Committed = 10,
    Cancelled = 20,
    TimedOut = 30,
    SourceRemoved = 40
}
```

The outcome should be carried by the transition event:

```csharp
public readonly struct CardTimeSessionTransition
{
    public CardTimeSessionSnapshot Previous { get; }
    public CardTimeSessionSnapshot Current { get; }
    public CardTimeSessionOutcome Outcome { get; }
    public CardTimeCompletedSessionSnapshot CompletedSession { get; }
}
```

`CompletedSession` is immutable and may include:

```text
SessionId
Category
Elapsed
MaximumDuration
```

It exists only on transitions that end an active session.

The stable `Current` snapshot after completion must report:

```text
IsActive = false
ActiveSessionId = none
SessionCardTime = None
ActiveElapsed = 0
ActiveRemaining = 0
```

## Snapshot Compatibility

The current `CardTimeSessionSnapshot` exposes:

```text
State
AvailableCardTime
SessionCardTime
ActiveElapsed
MaximumActiveDuration
```

Two migration options exist.

### Recommended: State Snapshot Plus Outcome Event

Replace durable `Committed` and `Cancelled` values with:

```text
Unavailable
Available
Active
```

Add:

```text
ActiveSessionId
IsAvailable
IsActive
```

Commit and cancel meaning moves to `CardTimeSessionTransition.Outcome`.

This is the cleanest long-term model.

### Compatibility Stage

If removing enum members immediately creates excessive churn:

- retain `Committed` and `Cancelled` enum values temporarily;
- publish them only as a single transition snapshot;
- immediately settle the runtime to `Unavailable`;
- never store an active session in either terminal snapshot;
- mark terminal enum members obsolete after observers migrate to outcomes.

This stage is acceptable only as a migration bridge. Production code must not
start timers or create opportunities from terminal snapshots.

## State Transitions

### Availability

```text
Unavailable + publish valid category
    -> Available(category)

Available(A) + publish A
    -> no change

Available(A) + publish B
    -> Available(B)

Available(A) + publish None
    -> grace(A), then Unavailable

Available(Chain or Finisher) + publish Neutral
    -> grace(previous), then Available(Neutral)
```

### Activation

```text
Available(category) + activation request
    -> create new CardTimeActiveSession
    -> Active(session)
    -> emit transition
    -> CardTimeSessionController requests slowdown
```

Activation while active is rejected and does not replace the session.

### Commit

Required final API:

```csharp
bool TryCommit(Func<bool> transaction);
```

Flow:

```text
Active(session)
    -> invoke transaction exactly once

transaction false
    -> remain Active(same session object and elapsed time)
    -> no terminal outcome

transaction true
    -> capture immutable completed-session snapshot
    -> detach active session
    -> latch consumed category
    -> settle to Unavailable or currently valid different opportunity
    -> emit one Committed outcome
    -> controller removes slowdown
```

The active session must be detached before external observers process the
committed transition.

No observer callback may see:

```text
Outcome = Committed
IsActive = true
```

### Cancel and Timeout

Cancel and timeout follow the same detach-first rule:

```text
capture completed data
detach session
clear active timer ownership
latch consumed category
emit outcome
remove slowdown
```

Timeout must be distinguishable from explicit cancellation for diagnostics,
even if presentation initially treats both the same.

### Source Removal

Unregistering the active player source:

- ends any active session with `SourceRemoved`;
- removes the Card Time modifier;
- clears availability and input buffering;
- invalidates the source token;
- leaves no consumed latch owned by the removed source.

## Gameplay Time Integration

`CardTimeSessionController.HandleRuntimeChanged` should depend only on durable
state edges:

```text
not Active -> Active
    => SetModifier(CardTime)

Active -> not Active
    => RemoveModifier(CardTime)
```

It must not infer slowdown ownership from:

- outcome;
- category;
- elapsed time;
- terminal enum value.

This makes time ownership follow the existence of an active session exactly.

## Availability Publication Policy

The current player publishes `attackCombo.AvailableCardTime` every frame.

This refactor must remain correct under repeated publication. Idempotence is a
hard requirement:

```text
same published category + same machine context
    => no state mutation
    => no transition event
    => no timer reset
    => no new session id
```

Changing the player to event-driven availability is a separate optimization.
It may follow later without changing this state model.

## Blast Radius

### Directly Changed

#### `PlayerCardTimeRuntime`

Largest change.

- replace flat active-session fields with the closed state model;
- create/detach `CardTimeActiveSession`;
- preserve leniency and consumed-category logic;
- emit outcomes;
- implement atomic transaction commit;
- enforce idempotent publication.

#### `CardTimeSessionSnapshot`

- represent only durable current state;
- optionally add active session id;
- remove or deprecate terminal-state dependence;
- guarantee zero/None active data outside Active.

#### `CardTimeSessionTransition`

- add outcome;
- add immutable completed-session data where applicable.

#### `CardTimeSessionState`

- recommended final values: `Unavailable`, `Available`, `Active`;
- compatibility stage may temporarily retain terminal values.

#### `IPlayerCardTimeSource`

- change commit from `TryCommit()` to
  `TryCommit(Func<bool> transaction)`;
- document exactly-once callback semantics.

#### `CardTimeSessionController`

- forward transaction commit;
- map only active-state edges to `GameplayTimeCoordinator`;
- relay richer transition outcomes;
- preserve source-token authority.

#### `PlayerController`

- prepare the card transaction before asking Card Time to commit;
- pass the synchronous transaction into `TryCommit`;
- consume combo Card Time only after successful commit;
- stop issuing a separate card commit after the session already transitioned.

The intended flow is:

```text
prepare card
Card Time TryCommit(prepared.TryApply)
on success: consume combo opportunity
```

### Observer Changes

#### `CardTimeHudUI`

- render from `Current.IsAvailable` and `Current.IsActive`;
- use outcome only for optional one-shot committed/cancelled feedback;
- never treat a completed-session snapshot as active.

#### `PlayerCardTimeDebugPresenter`

- display durable state separately from latest outcome;
- optionally show session id for debugging.

#### `CardTimeAwareness`

- active awareness derives only from the current durable state;
- category is `None` outside Active;
- outcome may be retained separately if consumers need one-frame reactions.

#### `PlayerControllerEditor`

- display current state, active session id, category, and remaining duration;
- display latest outcome as diagnostics rather than machine state.

### Tests Changed

- `PlayerCardTimeRuntimeTests`
- `CardTimeSessionControllerTests`
- `CardTimeAwarenessTests`
- `GameplayServicesRootTests`
- HUD Card Time tests
- player/card commit tests

### Verified Outside Blast Radius

No intended production changes:

- `GameplayTimeCoordinator`;
- `GameplayTimeModifierResolver`;
- `PlayerAttackComboRuntime`;
- `PlayerAttackSequence`;
- `CardDefinitionSO`;
- card effect definitions;
- `PlayerCombatEffects`;
- attack hit detection;
- enemy locomotion and patrol logic;
- animation-authored Card Time category mapping.

Tests for these systems may still run as regression coverage.

## Migration Plan

### Phase 0: Characterization

Before changing production behavior, add tests for the current required rules:

- repeated availability publication is idempotent;
- active session outlives the source animation window;
- successful commit emits one outcome;
- commit transaction failure keeps the same active session and elapsed time;
- commit detaches the session and clears active data;
- `None -> same consumed category` does not reopen;
- a different category releases the consumed latch;
- timeout and cancel remove slowdown exactly once;
- a newly activated session receives a new session id;
- no observer sees active data after a terminal outcome.

Add a regression reproducing:

```text
publish Finisher
activate
tick partially
commit
publish None
publish Finisher
request activation
```

Expected:

- activation is rejected;
- no second active transition occurs;
- no session with the old id remains;
- remaining time is zero in current state;
- no additional Card Time modifier is installed.

### Phase 1: Internal Session Object

Keep public snapshots temporarily compatible.

- introduce the internal active session object;
- derive active category and elapsed time only from that object;
- detach it atomically on all terminal paths;
- preserve existing external event shape.

This phase addresses the remaining-time defect with the smallest public API
change.

### Phase 2: Outcome-Based Transitions

- add `CardTimeSessionOutcome`;
- add completed-session snapshot;
- migrate observers from terminal state checks to outcome checks;
- ensure durable current state is never terminal.

### Phase 3: Atomic Card Transaction

- add prepared card commit;
- change source commit to accept the synchronous transaction;
- remove the split `Card Time commit -> card commit` flow;
- test transaction failure and exactly-once application.

This phase completes the previously specified atomic commit behavior.

### Phase 4: Remove Compatibility States

- remove production dependence on `Committed` and `Cancelled`;
- remove obsolete enum values if no serialized asset relies on them;
- update debug tooling and tests;
- retain migration notes in the specification history.

### Phase 5: Optional Event-Driven Availability

Separate future task:

- emit availability changes from `PlayerAttackComboRuntime`;
- forward only meaningful changes from `PlayerController`;
- remove per-frame availability publication after characterization tests pass.

The state machine must remain idempotent even after this optimization.

## Test Matrix

### State Integrity

- Unavailable contains no session.
- Available contains category but no session.
- Active contains exactly one session.
- Every non-active snapshot reports zero active timing.
- Session ids never repeat during one runtime lifetime.

### Activation

- valid opportunity creates one session;
- buffered activation creates one session when opportunity arrives;
- repeated activation request while active is rejected;
- repeated source publication does not replace the session.

### Commit

- successful transaction is invoked exactly once;
- failed transaction leaves the same session active;
- successful commit emits one committed outcome;
- successful commit clears active data before listeners run;
- successful commit removes slowdown once;
- stale same-category publication cannot reactivate.

### Cancel and Timeout

- explicit cancel emits Cancelled;
- timeout emits TimedOut;
- source removal emits SourceRemoved;
- all three detach the session;
- all three remove slowdown once;
- none retain remaining active time.

### Leniency

- early input buffering remains unchanged;
- post-window grace remains unchanged;
- repeated `None` does not restart grace;
- Neutral handoff remains unchanged;
- different-category handoff remains immediate.

### Observation

- HUD displays only durable current state;
- awareness reports inactive after commit;
- completed outcome includes the former category and elapsed duration;
- observers cannot mutate the runtime.

### Card Transaction

- invalid card never invokes session commit transaction;
- transaction failure spends nothing and leaves session active;
- success spends and applies exactly once;
- combo opportunity is consumed only after success.

## Risks

### Event Ordering

Observers may currently assume the transition's `Current` snapshot retains the
committed category. They must read `CompletedSession.Category` for terminal
feedback after migration.

### Double Notifications

A compatibility implementation that emits both `Committed` and
`Unavailable` may produce two events. Prefer one outcome-bearing transition
directly to the durable destination.

### Atomic Commit Scope

Changing commit and session representation simultaneously increases debugging
surface. The phased plan intentionally introduces the session object before
changing the public commit protocol.

### Consumed Latch Semantics

Nulling the session does not by itself prevent stale source publications from
opening another opportunity. The consumed-category latch remains required
until availability publication becomes edge-based and carries stronger window
identity.

### Category Identity Is Not Window Identity

Two consecutive legitimate windows can share the same category, especially
Chain. A category-only consumed latch may eventually be insufficient.

Recommended future extension:

```text
CardTimeOpportunityId
```

The source increments this id only when a genuinely new animation/combo
opportunity begins. The runtime then consumes an opportunity id rather than
guessing from category changes.

This is not required for the first session-object migration, but the state
model should not prevent it.

## Diagnostics

During implementation, log or expose in the custom Inspector:

```text
runtime state
latest published category
offered category
consumed category
active session id
active session category
elapsed / maximum duration
latest outcome
completed session id
```

For the reported Finisher defect, the decisive signal is:

```text
Did the second Active transition reuse the original session id?
```

If yes, session teardown failed.

If no, availability created a genuinely new session and the opportunity latch
or source publication is responsible.

## Acceptance Criteria

- a completed session object is unreachable from the runtime's durable state;
- non-active snapshots contain no category or remaining active duration from a
  completed session;
- commit, cancel, timeout, and source removal detach sessions atomically;
- repeated stale publication cannot recreate a consumed opportunity;
- time slowdown exists exactly while the durable state is Active;
- transaction failure leaves the same active session unchanged;
- transaction success applies the card exactly once and emits one outcome;
- observers receive terminal context through immutable completed-session data;
- player-source authority and one-global-session ownership remain unchanged;
- existing input buffer, post-window grace, and Neutral handoff behavior remain
  covered by tests;
- the migration can be implemented in phases without changing card assets or
  gameplay-time arbitration.
