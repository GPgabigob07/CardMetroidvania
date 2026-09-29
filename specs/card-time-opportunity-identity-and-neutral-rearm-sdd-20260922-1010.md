# Card Time Opportunity Identity And Neutral Rearm SDD - 20260922-1010

## Contexto

Playtesting the first Neutral and Chain cards showed that a committed Neutral
card makes Card Time unavailable until the player attacks. The same
category-based consumed latch also treats Attack1 and Attack2, both `Chain`, as
one opportunity. This conflicts with Neutral's role as a repeatable traversal
and preparation category and with the intended sequence of using a Chain card
on each distinct attack window.

The author confirmed the desired rule on 2026-09-22: after a Neutral card is
used, Neutral should rearm as soon as possible while grounded; if airborne, it
must wait until landing. An airborne player may still attack and use a Chain
opportunity. Each new attack window may offer its own Chain card, even when the
previous attack window was also Chain.

This specification refines, without overwriting, these sources:

- `gdd/gdd-canonico-20260526-2331.md` (Neutral, Chain, Finisher roles);
- `specs/card-time-and-training-dummy-review-slice-sdd-20260612-1701.md`
  (initial category availability);
- `specs/card-time-session-state-machine-refactor-sdd-20260614-2326.md`
  (session ownership and the documented category-identity limitation);
- `specs/card-inventory-selection-handshake-sdd-20260622-2309.md`
  (selection and successful-commit handshake);
- `specs/neutral-chain-card-baseline-sdd-20260921-1107.md` (current cards);
- the current player combo, Card Time runtime, controller, and chord code under
  `Assets/Scrips/Architecture/Player` and `Assets/Scrips/Architecture/Runtime`.

## Goal And Scope

Make consecutive legitimate Card Time opportunities distinguishable even when
they share a category. After committing a Neutral card, permit another Neutral
selection without forcing a melee attack when the player is grounded. While
airborne, defer that Neutral rearm until landing, but keep attack-authored
Chain and Finisher opportunities available.

Keep one active Card Time session, one successful card commit per opportunity,
existing card costs and effects, and the existing release-before-retrigger
input chord. Do not change the timing or category of attack animations, add a
card cooldown, or make Neutral globally grounded-only.

## Opportunity Contract

The player source publishes an immutable pair:

```text
CardTimeOpportunity(Category, OpportunityId)
```

`OpportunityId` is positive and increases whenever that source creates a
genuinely new opportunity. `None` carries no ID. An ID is never recycled while
the same player source is registered, including across transient action resets
and streamed-area changes. The source token continues to establish authority;
the ID distinguishes opportunities within that source, not across players or
gameplay runs.

The Card Time service stores the last consumed opportunity ID. Repeated
publication of the same ID, including `opportunity -> None -> opportunity`,
cannot reopen it. A different ID may offer the same category. Publication
while a session is active does not replace that session or change the category
of the selected card. The service associates the active session with the ID
that created it and consumes that ID on terminal paths that currently consume
the opportunity. The session's own ID remains separate and continues to
identify each activation for selection and diagnostics.

The published category still controls input buffering and post-window grace.
An ID does not extend a closed animation window or bypass its authored timing.
The source creates IDs on real window starts or explicit Neutral rearm events,
never on every frame or simply because the UI refreshed.

## Source Rules

### Neutral

Before any Neutral card use, Neutral remains available under its existing
outside-combo rule, including while airborne. A successful Neutral commit
consumes its current opportunity and closes the selection session.

- If the player is grounded and the combat state is Neutral, create and publish
  a new Neutral opportunity as soon as the commit and selection cleanup finish.
- If the player is airborne, remember a pending Neutral rearm. Publish no new
  Neutral opportunity until grounded contact occurs and the combat state is
  Neutral. Landing creates one new ID; subsequent grounded frames reuse it.
- If an attack starts before landing, its Chain or Finisher opportunity can be
  offered normally. It does not clear the pending Neutral rearm. Returning to
  Neutral while still airborne does not satisfy the pending rearm.
- Landing during an active attack or Card Time session does not replace that
  session. The pending Neutral rearm is fulfilled when the player is grounded
  and returns to Neutral.

This is a restriction **after Neutral use**, not a general ban on airborne
Neutral Card Time. Once a new grounded Neutral opportunity is created, ordinary
movement away from the ground does not invalidate it; using that opportunity
in the air starts the same wait-for-landing cycle again.

The new opportunity is available immediately in gameplay state. A held Card
Time chord cannot reactivate it: both chord buttons must be released and a new
chord entered. No arbitrary time cooldown or requirement to spend the card's
effect is added. Card-level validation still rejects an unavailable effect or
insufficient Energy without spending it.

### Chain And Finisher

Each distinct attack-authored Card Time window creates a new ID when that
window begins. Attack1 and Attack2 both remain `Chain`, but their IDs differ.
For example:

```text
Airborne Neutral commit -> Neutral waits for landing
Attack1 -> Chain opportunity A -> commit
Attack2 -> Chain opportunity B -> commit
Attack3 -> Finisher opportunity C
```

Each window can commit at most one card. Repeated animation/frame publication
inside one window retains the same ID. Finisher keeps its current category and
authored availability; it gains the same explicit identity protection. A new
attack after the combo genuinely restarts receives new IDs.

### Cancellation, Timeout, And Reset

This change does not redefine which terminal outcomes consume an attempt.
Explicit cancellation retains its existing one-attempt behavior; a failed
prepared transaction remains active under the existing commit contract. Neutral
timeout retains its existing grounded recovery policy. These outcomes must not
accidentally generate repeated IDs from per-frame publication.

Area streaming or another transient action reset is not a landing event and
must not clear an airborne pending Neutral rearm. Player death or full run reset
may discard the pending rearm and begin a fresh source opportunity. Source
unregistration invalidates all of that source's IDs and active selection.

## Ownership And Data Flow

- `PlayerAttackComboRuntime` or a narrowly scoped player-owned opportunity
  runtime owns the monotonic ID and decides when attack windows or Neutral
  rearm produce a new opportunity. It does not decide card eligibility.
- `PlayerController` supplies current grounded state, coordinates successful
  commit and selection cleanup, and publishes the resulting opportunity
  through `IPlayerCardTimeSource`. It does not spoof a category transition to
  clear the service's latch.
- `IPlayerCardTimeSource` and `CardTimeSessionController` forward the immutable
  opportunity from the registered player source.
- `PlayerCardTimeRuntime` tracks available and consumed opportunity identity,
  while its active session retains its own session ID and fixed category.
- HUD and selection continue to read the existing category/session snapshot.
  Opportunity ID may be exposed in diagnostics but need not appear in the
  player-facing UI.

## Behavioral Acceptance Cases

1. Commit a Neutral card while grounded and outside an attack. The next
   Neutral opportunity appears without attacking; holding the same chord does
   not activate it, while releasing and pressing the chord does.
2. Commit a Neutral card in the air. Neutral stays unavailable through airborne
   idle frames. Landing and returning to Neutral offer exactly one new Neutral
   opportunity.
3. After an airborne Neutral commit, Attack1 offers Chain. Commit there;
   Attack2 offers a different Chain opportunity and can also commit a card.
4. Repeated publication, a transient `None`, HUD rebuild, or area-streaming
   reset cannot duplicate a consumed opportunity or bypass the airborne
   landing requirement.
5. One attack window still accepts at most one commit. Finisher, input buffer,
   post-window grace, slowdown cleanup, atomic Energy payment, and card effects
   retain their established behavior.

These are implementation acceptance cases, not a claim of completed Play Mode
validation. The user has chosen to skip Unity unit-test execution for now;
the implementation plan should include a focused manual Play Mode pass.
