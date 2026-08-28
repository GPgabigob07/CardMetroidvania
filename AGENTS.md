# Codex Repository Instructions

## Project Context

This repository is a Unity prototype for a 2D metroidvania focused on melee combat, momentum-based traversal, aerial combat, and a card-driven ability system.

The main knowledge sources currently live in:

- `.docs/`: original extracted source documents and raw review material.
- `gdd/`: versioned GDD and design-memory documents intended for future agents and development planning.
- `specs/`: versioned technical and system design specifications for implementation planning.

## Memory And Documentation Versioning

Prefer creating new timestamped versions of memory documents instead of overwriting existing ones.

Use this pattern for new GDD or design-memory files:

- `gdd/<document-purpose>-YYYYMMDD-HHMM.md`
- `specs/<document-purpose>-YYYYMMDD-HHMM.md`

Examples:

- `gdd/gdd-review-20260525-2143.md`
- `gdd/gdd-canonico-20260525-2200.md`
- `gdd/card-time-spec-20260525-2230.md`
- `specs/prototype-architecture-sdd-20260525-2200.md`

When a design document needs substantial changes:

1. Create a new timestamped file.
2. Preserve the older version as project memory.
3. Mention which prior files were used as sources.
4. Include a short "Contexto" or "Historico" section explaining why the new version exists.

Minor typo fixes may be made in-place when they do not change design meaning. For any semantic change, prefer a new version.

## GDD Workflow

Before implementing gameplay systems, check the latest relevant files in `gdd/`.
Before implementing architecture or subsystem code, check the latest relevant files in `specs/`.

When updating the GDD:

- Keep the canonical GDD focused on current decisions.
- Move risks, rejected ideas, and exploratory analysis to appendices or separate timestamped review files.
- Do not delete old design ideas unless the user explicitly asks for cleanup.
- Preserve original documents in `.docs/` as historical sources.

## Implementation Guidance

Favor small, testable baseline systems before implementing high-complexity mechanics.

For this project, likely early specs should be split into:

- movement controller;
- combat loop;
- Card Time;
- card data/effects;
- enemies;
- HUD/camera/input;
- level design and progression gates;
- Unity technical architecture.

When adding Unity code, prefer data-driven structures where appropriate, especially ScriptableObjects for cards, attacks, enemies, and tuning values.

Unity script-assets must follow Unity file naming expectations: each concrete `MonoBehaviour` or `ScriptableObject` should be a top-level declaration in a `.cs` file with the same name as the class. Do not group multiple concrete MonoBehaviours or ScriptableObjects in one file.

Follow the repository code conventions in `specs/code-conventions-*.md`: apply SOLID pragmatically, document non-concrete methods with XML docs, prefer declarative/idiomatic C# (`var`, `foreach`, native language/library features), and group Unity Inspector fields with editor annotations such as `Header`, `Tooltip`, `Min`, `Range`, or `TextArea` when applicable.

## Unity Editor Collaboration

Before editing scenes, prefabs, serialized assets, or Editor tooling, check the
latest `specs/unity-editor-collaboration-workflow-*.md`.

The current baseline does not require a Unity MCP. Prefer code and data changes,
then idempotent Unity Editor tooling for deterministic scene/prefab setup. Ask
the user for short, explicit Editor actions when visual judgment, live Inspector
assignment, animation authoring, or Play Mode observation is required. After
the user saves those changes, inspect the serialized assets and continue the
implementation.
