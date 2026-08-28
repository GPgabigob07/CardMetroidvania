# Card Time Selection UI Presentation SDD - 20260707-1027

## Contexto

This specification records the first presentation refinement pass for the
combat Card Time selection UI. It exists because the slot-command baseline is
functionally wired, but the overlay needs stronger player focus and clearer
input affordances while remaining non-pausing and scalable.

Sources used:

- `AGENTS.md`
- `specs/player-hud-sdd-20260615-0012.md`
- `specs/card-inventory-selection-handshake-sdd-20260622-2309.md`
- `specs/card-time-control-scheme-data-layer` decisions from implementation
- current code under `Assets/Scrips/Architecture/Hud`
- current code under `Assets/Scrips/Architecture/Player/CardTime`

## Decision Summary

Card Time selection remains an observer/dispatcher. Presentation changes must
not make the UI own selection truth, pause gameplay, or block raycasts.

The selection view gains:

- a full-screen non-blocking dark backdrop while selection is open;
- larger card slots in the sample HUD setup;
- high-contrast input labels in white rounded/pill-like backgrounds with black
  text;
- a data-driven input display mapper that resolves active control scheme
  bindings into player-facing labels.

The baseline backdrop is a dark translucent uGUI image. A true blur or
post-processing implementation can replace or augment this backdrop later
without changing selection, input, or Card Time session code.

## Public Contracts

`CardTimeInputDisplayMapperSO` maps scheme/action/binding data to display
labels. It is intentionally presentation-only:

- stable action names remain owned by `CardTimeControlSchemeSO`;
- physical bindings remain owned by the Input Actions asset;
- mapper entries can specialize by device family and action name;
- missing mapper entries fall back to scheme display label, default control
  path, action name, then slot number.

`CardTimeSelectionHudUI` consumes the mapper and resolves labels per visible
slot. It still renders from `CardTimeSelectionTransaction` snapshots only.

`CardTimeSelectionSlotUI` owns the visual treatment of a slot command label. It
can initially use a white rounded-looking background image with black text, and
later forward the same data to authored sprites, glyphs, or Animator states.

## Layout Rules

The sample-scene setup should create:

- full-screen dark backdrop under selection cards, with `CanvasGroup` alpha
  driven by the same selection show/hide lifecycle;
- card selection root at bottom center, still non-blocking;
- larger prototype card slots than the first baseline;
- command label badge near the upper portion of each card;
- black command text on a white badge so gamepad/keyboard slot commands remain
  legible during combat.

## Scalability Rules

The mapper must support at least the current prototype schemes:

- keyboard WASD-oriented slot labels;
- keyboard arrows-oriented slot labels;
- gamepad labels for shoulders, triggers, and face buttons.

Future work may replace text labels with sprites or TextMeshPro glyphs. The
selection controller and transaction APIs must not change for that.

## Test Strategy

Build checks:

- `dotnet build TicGame.Architecture.csproj`
- `dotnet build TicGame.Architecture.Editor.csproj`
- `dotnet build TicGame.Architecture.EditModeTests.csproj`

EditMode tests should cover:

- mapper returns gamepad labels for gamepad schemes;
- mapper falls back safely when no entry exists;
- HUD uses mapped labels instead of raw scheme labels when a mapper is
  configured;
- backdrop hides and shows with selection lifecycle;
- slot command badge receives visible high-contrast colors.
