# Environmental Hazard And Checkpoint Recovery SDD — 20260922-1616

## Contexto

Pink now has authored hazard geometry. The player should take an authored amount
of damage on contact, then return to the most recently activated area spawn.
The return must be covered by a black screen, with movement disabled for at
least two seconds so area loading and teleporting are not visible.

This spec extends `specs/gameplay-scene-ownership-sdd-20260920-1853.md` and
`specs/player-facing-health-respawn-sdd-20260617-1717.md`. The former already
defines persistent Gameplay ownership, `RunProgress.Respawn`, `SpawnAddress`,
`AreaSpawnPoint`, and the area-loading respawn sequence. The latter defines
player damage and health restoration on death. Pink currently has `pink-start`
and `pink-corridor-begin` spawn markers. The player prefab has one root
`CapsuleCollider2D` and `SimpleHealth`; it does not yet have a separate player
hurtbox component. These source files and the current scene serialization were
inspected on 2026-09-22.

## Intended Behavior

- Entering a checkpoint volume makes its authored `AreaSpawnPoint` the current
  respawn address for this run. It does not teleport, heal, or reload the area.
- Entering a hazard trigger through the player collider applies the hazard's
  configured positive damage once for that contact, then starts covered
  recovery to `RunProgress.Respawn`.
- If damage is nonlethal, the player returns with reduced health. If damage is
  lethal, the existing death policy restores health after respawn. Both use
  the same covered movement and area-loading sequence.
- The player's input and physics are held immediately on accepted hazard
  contact. A full-screen black cover appears, the destination area is made
  ready, and the player/camera move under cover. The cover clears only after
  readiness and a minimum two seconds from contact. Controls return when the
  cover is fully clear. If loading exceeds two seconds, the cover remains.
- An error loading the area or resolving the spawn leaves the player held and
  covered, reports the existing coordinator error, and remains retryable.

The author approved retaining nonlethal damage and interpreting two seconds
as the minimum total lock from contact through cover removal on 2026-09-22.

## Approach Decision

Extend the existing `GameplayAreaCoordinator` with a covered hazard-recovery
mode. It already reconciles loaded areas, resolves stable spawn addresses,
and handles retryable failures. A player-local teleport would be smaller but
could land in an unloaded area; treating every hazard as lethal would reuse
death directly but discard nonlethal health loss. The shared coordinator path
keeps one authority for movement between areas and changes health restoration
only where the hazard requires it.

## Existing Pieces And Missing Pieces

| Existing | Needed |
| --- | --- |
| `RunProgress.SetRespawn(SpawnAddress)` | Area checkpoint trigger and validation/binding |
| `AreaSpawnPoint` and Blue/Pink `AreaDefinition` assets | Author checkpoint volumes and safe marker references |
| `SimpleHealth` and `DamageResolver` | Hazard contact source with Inspector damage amount |
| `GameplayAreaCoordinator.RespawnAsync()` | Recovery mode that preserves health after nonlethal damage |
| `PlayerWorldHold` lease | Hold across cover, loading, minimum delay, and fade-out |
| Gameplay-owned HUD canvas and player-follow camera | Persistent full-screen black cover and presenter |

The Gameplay camera is a child of the player, so moving the player also moves
the camera. A separate camera teleport API is unnecessary for this baseline.

## Checkpoint Ownership

`AreaRespawnCheckpoint` is an area-scoped `MonoBehaviour` on an authored trigger
volume. It references one `AreaSpawnPoint` in the same area scene. During area
binding, `GameplayAreaCoordinator` supplies that area's ID and the one
`RunProgress` instance. The component forms a `SpawnAddress(areaId,
spawnPoint.SpawnId)` only after validating a nonempty ID, a same-scene marker,
and the player contact. The coordinator's existing unique-marker check remains
authoritative on recovery. Repeated contacts with the same checkpoint are
idempotent. A checkpoint may be placed before a hazard or at the start of a
new room; it need not share the marker's physical location.

The checkpoint records the last touched location across area unloads. It does
not store a `Transform` in persistent state. Scene-authored spawn locations
must be outside hazards and solid geometry. `pink-corridor-begin` can serve as
the first Pink checkpoint destination; additional markers can be authored as
the level layout develops.

## Hazard Contact And Damage

`EnvironmentalHazard2D` is an area-scoped component on each hazard trigger.
Its Inspector exposes `damageAmount` with a positive validated value. It
accepts only the registered player collider (the existing root capsule counts
as the player hurtbox for this baseline). A contact resolves damage through
the existing damage pipeline against the player's `SimpleHealth`, so the HUD
receives the normal health event. The hazard owns no health or respawn state.

The hazard requests recovery only if damage was accepted. Contact is latched
per player while recovery is in progress; extra collider callbacks cannot
apply repeated damage or start a second operation. Once recovery completes,
the latch clears. The checkpoint marker must be positioned clear of all
hazards to prevent immediate retrigger on release. If a lethal hit raises the
existing death notification synchronously, the hazard must not issue a second
recovery request. A single coordinator operation owns the return for either
case.

Nonlethal recovery cancels transient movement/combat and Card Time, while
retaining Energy, cards, unlocks, and temporary card effects that normally
survive ordinary area streaming. Lethal recovery keeps the existing death
cleanup policy.

Enemy attacks continue using their existing damage and death behavior. No
general invulnerability system, knockback, or damage-over-time hazard is added
in this slice.

## Covered Recovery Sequence

The persistent Gameplay scene owns `RespawnCoverUI`, a black, full-screen UI
surface above the existing HUD. It can be a `CanvasGroup`/`Image` on a
dedicated high-sorting canvas. Its fade uses unscaled time so Card Time or
hitstop cannot strand the player. A short fade duration (initial default
0.2 seconds each direction) is configurable for visual tuning. It begins
fully transparent and does not intercept gameplay input outside recovery.

The coordinator exposes one recovery entry point for a hazard hit and reuses
the existing death respawn path internally. The sequence is:

1. Accept one request, acquire `PlayerWorldHold`, clear transient player
   action/Card Time state, and start the cover fade to black.
2. Wait until fully opaque before any area unload or player teleport.
3. Suspend directional streaming requests, settle in-flight requests, unload
   other areas, load and bind the destination area, and resolve exactly one
   `AreaSpawnPoint` by the saved address.
4. Move the player with zero velocity, reset crossing history, and retain or
   restore health according to whether the hit was nonlethal or lethal.
5. Keep the player held and black screen visible until the destination is
   ready. Start the fade-out late enough that it finishes no earlier than two
   unscaled seconds after contact. Release the hold only after the cover is
   fully clear.

The minimum duration includes both fades. If loading takes longer, the hold
and black cover last longer. Death from another source can use the same cover
path; its health restoration remains unchanged. `RespawnAsync()` and the
existing retry entry point should converge on the same single-flight
operation, rather than racing two teleports. A failed operation retains the
hold and opaque cover; retry completes the same destination procedure.
The existing `RespawnSequence` releases its hold internally; its ownership
must move to the outer covered sequence so the player remains held through
the fade-out.

## Authoring And Integration

Use code plus a small idempotent Unity Editor setup for the persistent cover
and deterministic references in `Gameplay.unity`. Do not hand-edit broad
scene YAML. Pink checkpoint and hazard placement remain visual authoring: the
user chooses volume bounds and safe marker positions in the Editor, then saves
the scene. Setup should reuse current Blue/Pink area definitions and preserve
the user's current unsaved/uncommitted scene, prefab, and TagManager changes.

Because the Pink scene currently has no serialized environmental hazard
component, an implementation cannot assume an existing hazard GameObject name
or modify every collider. The Editor handoff must identify each chosen object,
the component to add, its damage amount, checkpoint marker reference, and
which scene to save. A separate player hurtbox is deferred unless playtesting
shows that the root capsule is insufficient.

## Failure And Acceptance Cases

- Checkpoint A then B changes the saved address to B; a Pink hazard returns to
  B even if Pink was unloaded and reloaded.
- A nonlethal 1-damage hazard hit from 5 health returns at 4 health. A lethal
  hit uses the existing health restoration policy and starts only one return.
- Several collider callbacks in one contact apply one damage and one return.
- Input and physics stop at contact; the player never sees unload/teleport;
  cover removal and input restoration occur no earlier than two seconds.
- An area load lasting longer than two seconds keeps the player covered and
  held until it succeeds. A missing/duplicate marker produces a clear error
  and retry without teleporting to an invalid location.
- A spawn marker outside hazards does not immediately retrigger the same
  hazard after control returns. Ordinary enemy damage and Card Time remain
  functional after recovery.

Validation should compile the runtime and EditMode assemblies, run focused
pure-state tests for checkpoint selection and recovery sequencing where
practical, and obtain one Unity Play Mode report for trigger contacts, visual
cover timing, repeated contacts, loading, health, and input restoration.
