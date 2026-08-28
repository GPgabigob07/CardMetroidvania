# Card Effect Feedback SDD - 20260625-0929

## Contexto

This specification records the baseline presentation layer for card effect
feedback. It exists because Card Time selection can now commit a specific card,
but the player still has no clear way to see when a card activates, triggers,
fails to trigger, or expires.

Sources used:

- `AGENTS.md`
- `specs/card-inventory-selection-handshake-sdd-20260622-2309.md`
- `specs/composable-card-effects-and-gated-ability-bridge-sdd-20260614-1841.md`
- `specs/player-hud-sdd-20260615-0012.md`
- current code under `Assets/Scrips/Architecture/Player`
- current code under `Assets/Scrips/Architecture/Hud`
- current event-channel and gameplay-service patterns

## Decision Summary

Card effect feedback is runtime-only presentation state. It is not persisted and
it is not authoritative gameplay state.

Cards provide presentation identity, starting with an optional icon on
`CardDefinitionSO`. Effect owners provide formatted HUD values. HUD and world
presenters render view models and events; they do not read player stacks,
charge counters, or effect internals.

The first implementation wires existing player-owned effect systems:

- `PlayerCombatEffects` for chain, knockback, energy-on-hit, and supplemental
  damage feedback;
- `PlayerExtraJumpRuntime` for extra-jump grant, consume, and clear feedback;
- card commit/readiness paths for card-level activation or failure feedback.

The contracts are entity-ready. A future `CardRunner` can become the owner of
card execution and feedback reporting without replacing HUD or world
presenters.

## Public Contracts

### Card Presentation

`CardDefinitionSO` gains optional presentation data:

- `Icon`: card art used by selection, HUD indicators, and world feedback.

Missing icons must be safe. Presenters may render a fallback sprite, fallback
color, or hide only the image portion, but missing art must not break gameplay.

### HUD View Model

`CardHudEffectViewModel` represents one card-related HUD indicator:

- stable effect key;
- source object;
- card id and card definition;
- icon;
- display text already formatted by the effect owner;
- optional lifetime progress;
- visual state, such as inactive, active, expiring, or expired;
- blink/fade preference for timed effects.

The HUD model intentionally does not expose generic stack names or raw internal
counters. A chain effect may show `x3`; a knockback effect may show `2`; a
future timed aura may show no text and only blink. The effect owner decides.

### World View Model

`CardWorldFeedbackViewModel` represents one transient world-space feedback
event:

- card id and card definition;
- icon;
- source object;
- optional world position;
- anchor policy;
- feedback kind, such as activated, triggered, failed, expired, or cleared;
- optional display seconds.

The baseline presenter shows only the card icon. Particle systems made of small
cards are deferred and should consume the same event later.

### Feedback Service

`ICardFeedbackService` is exposed through gameplay services and provides:

- a world feedback event stream;
- registration or replacement of active HUD models by stable key;
- removal of active HUD models by key, source, or card;
- read-only snapshots for HUD presenters.

Missing feedback service references must be tolerated by gameplay code.
Feedback must never be required for payment, damage, movement, or card effect
execution.

## Feedback Lifecycle

Card commit and effect owners emit feedback at the point where they already know
the truth:

- activation: after payment succeeds and an effect is armed or applied;
- trigger: when a reactive effect actually contributes to a hit, resource gain,
  extra jump, or supplemental damage;
- failure: when player intent happened but the card/effect cannot apply;
- expiry or clear: when an armed effect is consumed, times out, misses, or is
  cleared.

World anchor policy is contextual:

- successful on-hit feedback uses the hit/contact point when available;
- activation, failure, expiry, and missing hit-point feedback fall back to the
  source entity head;
- the player head anchor is the default source fallback for current player
  cards.

## Current Player Wiring

`PlayerCombatEffects` remains the effect owner for current combat card behavior.
It reports feedback when:

- chain capacity is granted, advances, resets, or contributes damage;
- energy-on-hit charges are granted, roll, consume, or fail to produce energy;
- knockback charges are granted and consumed on an effective hit;
- supplemental damage is armed, resolved, or cleared because the attack ends
  without a valid trigger.

`PlayerExtraJumpRuntime` remains the effect owner for extra jump behavior. It
reports feedback when temporary charges are granted, consumed, or cleared.

`PlayerCardRuntime` remains the player-side card reader for now. It reports
card-level failure only when preparation or final application rejects the card.
Successful card-level activation feedback may be emitted after final payment
and before owner-specific effect reporting.

## HUD Rules

The card HUD indicator lives under the existing player HUD, visually below or
near the health indicator.

The HUD presenter:

- reads snapshots from `ICardFeedbackService`;
- renders icon plus optional text;
- supports timed blink/fade without requiring an Animator;
- removes expired models when the service says they are gone;
- never reads `PlayerCombatEffects` counters directly.

## World Rules

The world presenter listens to card world feedback events and spawns a short
icon popup.

Baseline behavior:

- use hit/contact position when the event provides one;
- otherwise use a configured entity-head offset from the source transform;
- color or alpha distinguishes activated, triggered, failed, and expired;
- do not block gameplay, input, raycasts, Card Time, or combat.

## Future CardRunner Migration

The feedback service and view models should not assume the player is the only
source. A future `CardRunner` can:

- own active card effect lifetimes for players, enemies, or spawned entities;
- produce `CardHudEffectViewModel` instances from runner-controlled effects;
- emit the same `CardWorldFeedbackViewModel` events;
- command entity-local effect owners instead of letting each player component
  interpret card data directly.

This implementation deliberately does not migrate gameplay execution into a
full runner yet. It adds the presentation contracts and wires the current
owners.

## Test Strategy

Build checks after each checkpoint:

- `dotnet build TicGame.Architecture.csproj`
- `dotnet build TicGame.Architecture.Editor.csproj`
- `dotnet build TicGame.Architecture.EditModeTests.csproj`

EditMode tests should cover:

- card icon fallback is safe;
- HUD view models support custom text, counts, and timed/blinking state;
- world feedback resolves hit-point and source-head anchors correctly;
- HUD presenters render from view models, not player counters;
- `PlayerCombatEffects` emits feedback for current charge, capacity, and
  supplemental flows;
- `PlayerExtraJumpRuntime` emits grant, consume, and clear feedback;
- missing feedback service does not break gameplay.
