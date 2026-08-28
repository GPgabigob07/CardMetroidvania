# Unity Editor Collaboration Workflow - 20260612-1609

## Contexto

This specification defines how scene, prefab, and Inspector work should be split
between the user and Codex while the project remains on Unity `6000.3.16f1`
(Unity 6.3 LTS) and the available Unity MCP requires Unity 6.6 or newer.

The project will not upgrade Unity or add an MCP only to support agent access.
This workflow should be reviewed when the project intentionally upgrades to a
compatible Unity version.

Sources used:

- `AGENTS.md`
- `specs/prototype-architecture-sdd-20260525-2200.md`
- `specs/code-conventions-20260526-0014.md`
- `specs/testing-conventions-20260526-0122.md`

## Current Baseline

- Unity asset serialization is already configured as `Force Text`
  (`m_SerializationMode: 2`).
- Scenes, prefabs, ScriptableObjects, materials, and project settings can be
  reviewed as serialized repository assets.
- Codex does not have direct access to the live Scene view, Hierarchy,
  Inspector, animation windows, or Play Mode visuals.
- Complex scene or prefab YAML should not be edited manually when Unity can
  perform the operation more safely through its serialization APIs.

## Responsibilities

### Codex owns

- C# runtime and editor code;
- ScriptableObject types and data structures;
- tests and command-line validation where available;
- small, clearly understood text-serialized asset changes;
- reusable or one-shot Unity Editor tools for complex scene/prefab changes;
- exact setup and validation instructions for actions that require the Editor;
- review of saved `.unity`, `.prefab`, `.asset`, `.meta`, and settings changes;
- continuation of implementation after the user completes an Editor handoff.

### The user owns

- visual composition and subjective placement in the Scene view;
- dragging references into Inspector fields when no safe automated setup exists;
- animation timeline and visual transition tuning;
- importing or configuring assets through vendor-specific Unity importers;
- entering Play Mode and reporting visual, input, physics, or timing behavior;
- saving scenes and prefabs after Editor-side work;
- providing screenshots or Console output when the serialized files do not
  explain the observed result.

## Preferred Implementation Order

For scene or prefab work, use the first safe option that fits:

1. Implement behavior in code and data without requiring scene changes.
2. Use existing prefab, scene, or ScriptableObject structures.
3. Add a small idempotent Editor command that creates or wires deterministic
   objects, components, assets, and references through Unity APIs.
4. Ask the user to perform a short, explicit Inspector or Scene-view action.
5. Manually edit serialized YAML only for narrow, well-understood changes with
   stable references.

Editor automation should be idempotent where practical: running it twice should
update or reuse its targets instead of creating duplicates.

## User Handoff Format

When Editor interaction is needed, Codex should provide:

- the scene or prefab to open;
- the exact GameObject or asset to select;
- component and field names using their Inspector labels;
- values, object references, tags, layers, or sorting settings to assign;
- whether to save a scene, prefab, asset, or project setting;
- a short Play Mode validation procedure;
- the evidence needed afterward, usually saved repository changes plus any
  relevant Console errors or screenshot.

The user should not need to infer architecture or invent missing setup. Keep
each handoff small enough to validate before moving to the next one.

## Verification Loop

After the user completes an Editor handoff:

1. Codex inspects the serialized changes and checks for unexpected churn.
2. Codex verifies scripts compile and runs relevant tests when possible.
3. Codex checks that references, layers, tags, prefab overrides, and asset GUIDs
   match the intended setup.
4. The user performs visual or Play Mode verification when the result depends
   on rendering, physics feel, animation, input, or scene composition.
5. Codex continues implementation or fixes the reported issue.

## Scene And Prefab Safety Rules

- Preserve `.meta` files and GUIDs when moving or renaming assets.
- Avoid broad scene reserialization for a narrow change.
- Do not hand-edit `fileID` or GUID references unless their ownership and target
  are known.
- Prefer prefab variants or data assets over duplicating configured objects.
- Keep generated Editor tools under an `Editor` folder so they are excluded
  from runtime builds.
- Remove one-shot Editor tooling only when it has no continuing project value
  and removal will not erase useful project history.
- Never assume a scene is saved; explicitly include saving in user handoffs.
- Review prefab overrides before applying them to a source prefab.

## Information Worth Providing To Codex

For efficient debugging, provide only the evidence relevant to the problem:

- the first Console error and its full stack trace;
- a screenshot showing Hierarchy, selected object, and Inspector;
- a short description of expected versus observed Play Mode behavior;
- whether the issue occurs in Edit Mode, Play Mode, or after reopening Unity;
- any unsaved scene or prefab state that is intentionally not in the repository.

## MCP Upgrade Decision

Reconsider a Unity MCP after an intentional Unity upgrade only if it supports:

- hierarchy and component inspection;
- safe scene/prefab edits through Unity APIs;
- Console retrieval and log filtering;
- Play Mode control;
- screenshots of Scene and Game views;
- test execution;
- undo support and clear reporting of changed assets.

Until then, MCP-specific assumptions must not be introduced into implementation
plans or required for project maintenance.
