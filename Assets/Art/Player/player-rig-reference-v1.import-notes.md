# Player Rig Reference v1

Source: `Assets/player-reference-middle-crop.png`

Prepared without AI redraw/generation. The original player reference was background-cut and placed on a transparent rigging canvas.

Files:

- `player-rig-reference-v1.png`: padded `512 x 768` transparent canvas, recommended first test asset.
- `player-rig-reference-tight-v1.png`: tight transparent cutout, useful for comparison.

Unity import notes:

- Texture Type: `Sprite (2D and UI)`
- Sprite Mode: `Single`
- Alpha Source: `Input Texture Alpha`
- Mesh Type: `Tight` for deformation tests, or `Full Rect` if the generated mesh clips coat/hair pixels.
- Pivot: `Bottom Center`
- Suggested Pixels Per Unit: match the existing player prefab/config.

2D rigging notes:

- This is a single flattened sprite, so it is best for testing Sprite Skin, bone weights, IK targets, and procedural pose experiments.
- For production-quality rigging, a layered `.psb` with separated torso, upper arms, forearms, hands, coat flaps, legs, feet, scarf, and hair will deform much better.
- The coat and arms overlap in the original pose, so elbow/shoulder deformation will be limited unless the character is later redrawn or separated into parts.
