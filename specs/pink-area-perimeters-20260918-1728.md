# Pink Area Perimeter Blockout

## Contexto

The user requested the pink area's general perimeters from the original GDD
diagram. The tall central channel is reserved for an elevator-with-enemies
encounter, ending at the top junction with salmon. No platforming is requested.
The user explicitly confirmed that the pink shaft and blue corridor remain
separate routes, with no doorway at their apparent map crossing.

Sources: original GDD snapshot (rendered page 55), saved BlueArea_Tutorial scene,
specs/unity-editor-collaboration-workflow-20260612-1609.md and this conversation.
The unapproved blue exploration draft is not implemented by this work.

## Scope and provisional dimensions

Create PinkArea_Perimeters.unity as a separate area authoring scene. Coordinates
are schematic and based on the existing 86-unit blue tutorial corridor as scale
reference; they do not specify simultaneous loading of overlapping area scenes.
In particular, do not additively overlay the pink shaft onto the existing blue
corridor without a later spatial/loading design: these represent separate spaces.

| Shell | Interior width x height | Lower-left position |
| --- | --- | --- |
| Arrival chamber | 30 x 36 | -116, 13 |
| Lower connector | 8 x 14 | -104, -1 |
| Lower return passage | 65 x 10 | -104, -11 |
| Elevator shaft | 10 x 137 | -49, -1 |
| Upper-left passage | 32 x 10 | -81, 79 |
| Salmon side approach | 12 x 6 | -93, 83 |
| Right chamber | 26 x 21 | -39, 45 |
| Nonlinear connection approach | 14 x 4 | -13, 62 |

Generate only exterior walls, floors and ceilings of the connected rectangles;
remove shared boundaries to keep internal junctions open. Open the arrival
chamber's east connection to blue at y=13..27, the shaft's top at y=136, and
the upper-left side connection to salmon. The nonlinear return is a named marker
at the capped end of the right approach, not a functioning teleporter.

The elevator shaft does not extend beyond its top salmon junction. Its walls
remain continuous at y=13..27 despite the blue corridor crossing in the diagram.
The elevator, combat waves, interior platforms, checkpoints, player spawn,
transitions and functional scene loading are outside this perimeter-only pass.

## Implementation and validation

PinkAreaPerimeterSetup creates eight named shell groups with pink URP unlit
walls and BoxCollider2D components on Environment. A fixed overview camera
supports shape review. Repeated setup opens the saved scene unchanged, preserving
manual edits. Existing blue scenes are not modified.

Run TIC > Setup > Create Or Open Pink Area Perimeters. Review the whole outline
in Scene view, especially the lower return, shaft top, salmon side passage and
right annex. Save dimensional adjustments. Playable traversal is not a completion
criterion while elevator and platforming remain intentionally absent.
