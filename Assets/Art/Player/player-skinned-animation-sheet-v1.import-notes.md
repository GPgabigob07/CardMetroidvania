# Player Skinned Animation Sheet v1

Source reference: `Assets/player-reference-middle-crop.png`

Generated sheet: `Assets/Art/Player/player-skinned-animation-sheet-v1.png`

Debug keyed source: `Assets/Art/Player/player-skinned-animation-sheet-keyed-v1.png`

Unity import notes:

- Texture Type: `Sprite (2D and UI)`
- Sprite Mode: `Multiple`
- Pixels Per Unit: choose the same value used by the current player setup.
- Mesh Type: `Tight` for normal sprite animation, or `Full Rect` if preparing 2D Animation skinning/bones.
- Sprite Editor slicing:
  - Type: `Grid By Cell Size`
  - Cell Size: `362 x 362`
  - Columns: `4`
  - Rows: `3`
  - Pivot: `Bottom Center` for character animation, then adjust per frame if needed.

Frame order, left to right, top to bottom:

1. Idle neutral
2. Idle breathing / coat lift
3. Run contact
4. Run passing
5. Run airborne
6. Run recovery
7. Jump takeoff
8. Falling
9. Dash forward
10. Melee slash windup
11. Melee slash active
12. Landing crouch
