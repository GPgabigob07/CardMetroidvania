# Blue Starting Room Blockout

## Contexto

The user approved a 50 x 50 Unity-unit starting room at the middle square of
the blue area in the original GDD diagram. Dotted connections represent nonlinear
shortcuts/teleporters. The green puzzle area and its future mechanic are outside
this pass. Sources: the GDD snapshot in output/gdd-final-audit-20260615,
gdd/gdd-canonico-20260526-2331.md, gdd/cronograma-build-novembro-20260828-1401.md,
specs/unity-editor-collaboration-workflow-20260612-1609.md and this conversation.

## Approved baseline and implementation plan

Goal: a safe spawn, catch floor, introductory platforms, one encounter and
reserved blue-area connections inside a 50 x 50 interior. Room-local interior
bounds are x=0..50, y=0..50. These are not final world-map coordinates.

- Create BlueStartRoomSetup.cs in the existing Architecture/Editor assembly.
- Reuse Player.prefab and GolemCharger.prefab without modifying either.
- Generate Blockout_BlueStart.unity through Unity APIs. If it already exists,
  open it unchanged to preserve manual refinement and avoid duplicate objects.
- Use solid 2D platforms on the Environment layer, starting with 2-unit rises.
  The current jumpVelocity=14 and riseGravityScale=3 imply approximately 3.33
  units maximum rise with default gravity; this is an estimate, not a playtest.
- Keep the upper volume open. The initial playable route occupies the lower
  12 units; a 50-unit room does not require filling its entire height now.
- Put the golem on a separate broad shelf, away from spawn. Let the lower floor
  provide a bypass and recovery route. Configure respawn back to spawn.
- Mark provisional west/east connections and nonlinear arrival with named
  scene objects. Boundary caps remain solid until adjacent rooms are built.
- Use a camera parented to the player for this spatial prototype. Camera
  confinement, smoothing, HUD integration and functional room transitions are
  subsequent work, not implemented gameplay promises.

## Verification and Editor handoff

Compile the Editor assembly including the new file and inspect the diff.
In Unity run TIC > Setup > Create Or Open Blue Starting Room Blockout.
The command saves the generated scene. Run it twice to check it does not
duplicate objects or overwrite edits. Enter Play Mode, traverse the platforms,
fall to the catch floor, approach/fight the golem, and verify death returns to
spawn. Check collider contact, camera readability and enemy aggro distance.
Save any adjustments. Runtime traversal and visual quality require this test.
