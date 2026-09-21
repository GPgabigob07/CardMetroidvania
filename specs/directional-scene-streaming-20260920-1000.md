# Directional Single Scene Streaming

## Contexto

The user approved a reusable two-trigger mechanism controlling one target scene
independently. Pink stays loaded while Blue is unloaded before the elevator
crossing, and Blue can load again on the return route. No dedicated safe room,
area swap or split elevator scene is required.

Sources: .docs/TIC.md (continuous dynamic map loading),
specs/pink-area-perimeters-20260918-1728.md,
specs/persistent-gameplay-services-and-card-time-ownership-sdd-20260614-1107.md,
specs/unity-editor-collaboration-workflow-20260612-1609.md and this conversation.

## Implementation

DirectionalSceneTrigger owns the target scene path, A/B volume references and
independent A-to-B/B-to-A actions (None, Load, Unload). First contact establishes
the incoming side. Contact with the other side performs that direction's action.
Duplicate collider entries are filtered per player and per volume. Facing does
not determine direction. Keep volumes non-overlapping; teleporting a player
across them is not treated specially by this flow-based mechanism.

SceneStreamingService serializes additive load/unload operations per target path.
Latest request wins after in-flight operations finish. Already-satisfied requests
do nothing. Failures expose an Inspector error and do not retry indefinitely.
The service does not depend on a coroutine attached to a disposable trigger.
No new persistent GameObject or duplicate gameplay services root is introduced.

The trigger cannot target its own scene. Unloading a scene containing a
PlayerController or unloading the last loaded scene is rejected. This is a
generic content-scene mechanism, not automatic extraction of gameplay roots.

## Authoring

Use GameObject > TIC > Directional Scene Trigger in the scene that stays loaded.
Assign Target Scene using the asset picker. Add that scene to the enabled Build
Profile scene list. The generated A/B children use Environment and trigger
BoxCollider2D components, matching the current player collision matrix.

Default: A below B, A-to-B Unload and B-to-A Load. For the pink lower shaft,
placing the parent at (-44, 6) gives A at y=4 and B at y=8, before the schematic
blue crossing begins at y=13. These are a proposed placement, not authored scene
changes. Resize the volumes to catch the player and space them for maximum speed.
Load sufficiently early on a return route; no travel barrier, pause, guaranteed
loading deadline or automatic missing-floor fallback is provided.

In Play Mode the parent Inspector reports Target Loaded, Operation In Progress
and the last error. Set either direction to None for a one-way operation.

## Integration still required for current area scenes

BlueArea_Tutorial currently contains the player and HUD. Before using it as
unloadable content, move the player/camera/HUD and their references to a separate
loaded gameplay scene and remove duplicate gameplay/overview cameras from area
content. Do not merely mark the player persistent while allowing reloads to
instantiate duplicate players. Area gate/checkpoint state restoration is also
separate from this mechanism: unloading currently resets scene-local objects.
No existing area scene is modified by this feature.

## Verification

Four NUnit contract tests passed using the actual pure C# coordinator and
crossing tracker: first/repeated contact, both directions, None actions,
redundant requests, reversal during an in-flight load, and failure/retry.
Runtime, Editor and EditMode test compilation passed with zero warnings/errors
using temporary compile-check projects against the installed Unity assemblies.
These checks do not execute Unity physics or SceneManager. Play Mode validation
remains: cross both volumes using an unloadable content scene, check actual
asynchronous operations, and confirm the owner remains loaded.
