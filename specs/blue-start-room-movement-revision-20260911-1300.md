# Blue Starting Room Movement Revision

## Contexto

Supersedes the playable layout proposed in
specs/blue-start-room-blockout-20260911-1200.md after the user's first playtest.
The 50 x 50 room felt empty. The starting room should introduce basic movement;
combat belongs in the long corridor to its left. Dash will be discovered later,
but can remain enabled during this blockout iteration.

Additional sources: the original GDD room diagram, current PlayerMovementConfig,
specs/player-movement-controller-sdd-20260604-2107.md and
specs/unity-editor-collaboration-workflow-20260612-1609.md.

## Layout

20 x 20 interior is a provisional tuning choice, preserving the square shape.
The original 50 x 50 scene remains available for comparison. The setup command
now creates/opens Assets/Scenes/Blockout_BlueStart_Movement.unity.

| Surface | Center x | Top y | Width | Purpose |
| --- | --- | --- | --- | --- |
| Spawn shelf | 16 | 2 | 6 | Safe walking space; begin facing the leftward route |
| First jump | 10 | 4 | 4 | One-unit edge gap, two-unit rise |
| West approach | 4 | 6 | 6 | Repeat jump and reach the future combat corridor |
| Upper practice 1 | 10 | 8 | 4 | Change direction and practice air correction |
| Upper practice 2 | 16 | 10 | 4 | Repeat basic jump |
| Overlook | 10 | 12 | 4 | Turn back and descend onto previous surfaces |

Spawn is (17,4). The full-width floor at y=0 catches falls without damage.
Return to the right shelf to retry; it is reachable from the floor with a basic
jump. Consecutive intended jumps rise two units with one- or two-unit edge gaps.
No jump requires dash, extra jumps, attacks or cards. Upper practice is optional.
The capped west connection is at (1,7), above the corridor-approach platform.
The corridor itself is not built by this revision. No enemies are instantiated.
Dash/input/prefab abilities are unchanged. The temporary follow camera uses
orthographic size 5; confinement and final framing remain future work.

## Implementation and validation

Update only the existing generator and create this versioned design record.
Use a distinct scene path to preserve the saved first iteration. Repeated setup
opens the movement scene unchanged, preserving user adjustments.

Compile current runtime and Editor sources. In Unity run the same setup command:
TIC > Setup > Create Or Open Blue Starting Room Blockout. Confirm the active scene
is Blockout_BlueStart_Movement, then test using only movement and jump:

- Reach the left corridor marker from spawn.
- Follow the optional platforms to the overlook and descend safely.
- Fall to the floor from each route and recover to spawn without dash.
- Check overhead clearance and visibility of upcoming landings.
- Confirm no enemy is present. Save any Scene-view adjustments.

Success means basic movement alone navigates both routes without getting stuck,
and the player can identify the leftward continuation. These require Play Mode
observation; calculated distances do not establish game feel.
