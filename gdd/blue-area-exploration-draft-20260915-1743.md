# Blue Area First Exploration Section

## Contexto

Design draft requested after the player validated the movement room, staged
combat corridor, Card Time unlock, energy wells and enhanced-melee gates.
This proposes the next section of the blue area after the second gate, not
a new biome. All new dimensions, encounters and connections below are proposals.

Sources:
- gdd/gdd-canonico-20260526-2331.md
- gdd/cronograma-build-novembro-20260828-1401.md
- specs/blue-area-tutorial-corridor-20260911-1500.md
- specs/blue-start-room-movement-revision-20260911-1300.md
- Original four-area diagram in the GDD Word snapshot
- User's playtest feedback and decisions in this conversation

## Purpose

Move from instructed combat to a short exploration loop. Give the player a safe
checkpoint, a visible destination and a choice of how to approach one encounter.
Retain movement, melee and the current Card Time deck. Neither dash nor a new
card is required. The green area's puzzle mechanic remains separate.

## Proposed flow

Second tutorial gate -> Rest chamber -> Split-level gallery -> Upper landing
-> next blue-area section. The upper landing opens a shortcut back to the rest
chamber. This is a connection graph, not final world coordinates. Fold the route
into the blue footprint when positioning rooms; do not assume that continuing
through the west exit requires expanding indefinitely westward.

### Rest chamber

Provisional interior: 14 wide x 10 high.

No enemies. A checkpoint restores health and Energy and becomes the respawn
location for this section. Its initial implementation can be scene-local;
cross-session save/load is separate work. Previously opened tutorial gates and
Card Time remain unlocked during death retries.

Show a barred doorway into the future shortcut so the player can recognize it
from the far side later. An ordinary open exit leads into the gallery. Avoid
another mandatory card seal immediately after the tutorial's two seals.

### Split-level gallery

Provisional interior: 30 wide x 18 high.

Show a lit upper exit on entry. A lower, broad route holds one Charger with
enough retreat space for reading its attack. An upper platform route passes
above its engagement range, allowing the player to choose combat or traversal.
Both routes converge on the landing. Neither is gated by defeating the enemy.

Use approximately 1.5-2 unit platform rises and conservative gaps, then tune
against actual movement. Upper platforms must account for the Charger's circular
4-unit detection radius; merely placing the enemy below them is insufficient.
Ensure the platform route cannot collapse into an unavoidable drop onto the
enemy. Mistakes land on a recoverable floor; no lethal pits in this first loop.

Provide one optional alcove off the upper route with a small lore/environmental
discovery. It does not contain an essential ability or a required deck card.
The purpose is to demonstrate that looking away from the main route pays off.

### Upper landing

Provisional interior: 12 wide x 10 high.

Give the player room to see the onward exit and operate a nearby shortcut
release. Opening it permanently connects back to the rest chamber for this run.
The shortcut is opened from the landing side; it is not a third card-damage test.
Use a simple physical connection if spatial packing permits; a nonlinear link
is consistent with the GDD's dotted connections if needed.

The onward exit remains the section endpoint until the following blue rooms
are designed. No boss, elite or dash pickup is committed by this draft.

## Experience target

Approximately 2-4 minutes on first exploration, excluding repeated deaths.
This is a pacing target to test, not a measured duration. The player should be
able to state where they are going, choose combat or traversal, recognize the
checkpoint when the shortcut opens and return without repeating the tutorial.

## Review and playtest checks

- The checkpoint can be found before the first new threat.
- Both gallery routes work with basic jump and movement, without dash.
- Combat and traversal are both viable choices and converge clearly.
- Falling does not trap the player or force a lethal recovery.
- The optional alcove is distinguishable from the onward route.
- The shortcut creates a useful return route rather than a redundant doorway.
- Room camera bounds reveal the next landing without exposing distracting voids.

Checkpoint activation, shortcut behavior and camera bounds require implementation;
this document proposes them and does not claim they already exist.
