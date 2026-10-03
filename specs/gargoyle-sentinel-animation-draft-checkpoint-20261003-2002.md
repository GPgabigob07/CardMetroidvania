# Gargoyle Sentinel animation draft checkpoint — 2026-10-03 20:02

## Contexto
The user is playtesting the completed code and requested all required gargoyle animations as draft sheets, at least five frames each, with exact 200×200 frames and a minimum 128×128 gargoyle silhouette. No animation integration was authorized. This supplements, rather than replaces, implementation-checkpoint-20261003-1925.md and verification-20261003-1925.md.

## Completed artwork
- gdd/art-references/gargoyle-animation-drafts-20261003-1938/README.md documents all 25 clips, phase intentions, framing, prompts and artist cleanup needs.
- 125 unique-frame PNGs: each actual PNG header is 200×200, five distinct hashes in every clip.
- Per-frame alpha bounds: width 130–196 pixels, height 129–180 pixels, zero partially transparent pixels. The minimum refers to silhouette bounding boxes including wings, not opaque-pixel count or gameplay body area.
- 25 strips: actual size 1000×200, five 200×200 cells each. Five category atlases: actual size 1000×1000, five-by-five 200×200 cells.
- Pose families: idle/walk/basic combo; Wingbreaker/upward claw/feint/delayed claw/recovery; 1/3/5 volleys and beam charge/fire; beam/nova recoveries, charge/release and counter stagger; hurt/stun/recovery/death/dead hold.
- Built-in imagegen generated the art. Separate replacements fixed crowded attack poses and the too-flat initial corpse pose. Nearest-neighbor whole-pose/frame extraction, binary alpha and exact canvas packing were then validated; see prompts.json, sources.json, overrides.json, pack-drafts.ps1 and pixel-report.json in the draft directory.
- Draft body proportions and pivots still require artist refinement before final playback. All PNGs are outside Assets; no animation bindings or runtime configuration was changed.

## Workspace and next action
Repository/workspace: G:\UnityProjects\My project. Branch: codex/playable-build-menu-20260929; previous code HEAD 34639bdecc268cbc19e17fad20f126c6e54cf32f.

The user's live playtesting changed Assets/Art/Enemies/GargoyleSentinel/GargoyleIdle.png.meta from 48 to 32 PPU while drafting. Preserve this user-owned change and exclude it from the art commit. Do not overwrite, revert, or interpret it as completed body-mask/Play Mode acceptance.

Gameplay acceptance remains pending. The existing continuation heartbeat stays active and quiet until actual human observations or saved assets justify concrete work. Do not regenerate the delivered drafts or connect them automatically. Quota was 26% used in the 300-minute window during the final packaging slice; no reset credits were used.
