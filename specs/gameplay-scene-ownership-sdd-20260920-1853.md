# Gameplay Scene Ownership and Area Streaming

Status: proposed design for review; no runtime or scene changes in this pass.

## Contexto

The directional single-scene loader is implemented and the user reports it works.
The next question is how to separate player machinery from unloadable areas.
The goal is continuous traversal through Blue and Pink, with Pink remaining loaded
while Blue disappears before their geometrical crossing. No mandatory transition
room or separate elevator scene is required. This document extends, rather than
replaces, the existing global-services architecture.

Repository sources reviewed:

- specs/persistent-gameplay-services-and-card-time-ownership-sdd-20260614-1107.md
- specs/player-facing-health-respawn-sdd-20260617-1717.md
- specs/directional-scene-streaming-20260920-1000.md
- specs/unity-editor-collaboration-workflow-20260612-1609.md
- gdd/gdd-canonico-20260526-2331.md
- gdd/blue-area-exploration-draft-20260915-1743.md (proposal, not confirmed progression)
- GameplayServicesBootstrap, GameplayServicesRoot, PlayerDeathRespawn,
  CardTimeTutorialZone, CardMeleeGate, GolemChargerBrain and scene setup tools.

## Observed baseline

- Global services already bootstrap before scene load, survive through
  DontDestroyOnLoad, reject duplicates and bind scene consumers on sceneLoaded.
- Blue contains the player and HUD. Its temporary camera is parented to the
  player. Player prefab overrides and camera hierarchy must survive migration.
- PlayerDeathRespawn immediately teleports to a Transform or fallback coordinate;
  it does not ensure that the destination scene is loaded.
- Card Time unlock lives on PlayerController, but the guide's discovered/visible
  state and UI live on the area-local CardTimeTutorialZone.
- CardMeleeGate.IsOpen is instance state and resets on scene reload.
- Chargers can detect the player through Physics2D when no explicit target is
  assigned; they do not require a new global player singleton.
- SceneStreamingService coordinates requests per scene, but provides neither a
  world-ready contract nor ordering between different target scenes.

## Alternatives

| Approach | Benefit | Cost / suitability |
| --- | --- | --- |
| Always-loaded Gameplay scene, existing persistent services | Inspectable player/HUD wiring; small migration; area lifetime is explicit | Needs startup, respawn and scene ownership rules; recommended |
| Put player, UI and services under DontDestroyOnLoad | Survives even single-mode scene changes | Additional duplicate/lifetime handling; less useful authoring separation; unnecessary here |
| Recreate player in each area | Areas can run independently | Must transfer health, deck, energy, unlocks and transient state; poor fit for seamless overlap |

Recommendation: keep the existing service bootstrap. Add one ordinary Gameplay
scene that stays loaded throughout this prototype's play session. No additional
Boot scene is needed yet. This is a project recommendation, not a requirement
imposed by Unity.

## Ownership contract

| Owner | Contents and lifetime |
| --- | --- |
| Existing GameplayServices root | Global time, hitstop, Card Time session, game state and feedback services; retain current lifetime |
| Gameplay.unity | One configured player, camera/AudioListener, HUD and its input infrastructure if used, tutorial guide presentation, startup/respawn coordinator and in-memory run state |
| Area scenes | Geometry, enemies, local hazards, doors, markers, tutorial discovery triggers, directional streaming triggers and future elevator |
| Assets | Shared configurations, prefabs, cards and event channels; no mutable run progress written into asset defaults |

Player components continue owning their present health, resources, cards and
unlocks. Do not copy these into a second authoritative state store. Run state
holds only facts that must outlive area objects, such as opened gate IDs and
the respawn address. Ending Play Mode/new run clears this state.

Keep Gameplay as the active Unity scene during play. Active scene is not the
same as the player's current area. Area-scoped runtime spawns must be parented
under an area root or explicitly moved into their owning scene; otherwise newly
created objects may incorrectly survive area unload. Player-owned temporary
effects must expire normally and release references to destroyed area targets.
Use the default shared 2D physics world; isolated physics scenes are out of scope.

## Minimal composition and startup

Proposed responsibilities, not mandatory final class names:

- GameplaySceneRoot: explicit references to the player/presentation and a small
  startup coordinator. No combat rules or universal service locator.
- AreaDefinition asset: stable area ID, validated scene path and default spawn
  ID. Store paths for builds through an Editor scene picker; no UnityEditor types
  in runtime code. Initially only Blue and Pink need entries.
- AreaSpawnPoint: stable local ID and authored transform within its area.
- RunProgress: opened gate IDs and current respawn address in session memory.

Gameplay is the build entry scene. Existing service bootstrap runs as today.
The coordinator holds player input and physics until the starting area loads,
service binding and progress restoration finish, and its spawn marker resolves.
Then place the player, zero velocity, initialize camera position, and enable play.
Do not depend on sceneLoaded subscriber ordering: use explicit readiness checks.
This initial startup hold does not introduce a traversal loading zone.

Gameplay references its own player/HUD directly. Area objects use their own local
references, existing trigger contacts or narrow runtime binding. Cross-scene
Transform references are not the persistence format. Consumers must tolerate
services arriving after OnEnable, and unsubscribe when disabled/destroyed.

Provide an explicit Editor command to open Gameplay with a selected area and
start at a selected spawn. It must reuse already-loaded scenes and restore the
editing setup afterward. Do not add a player to every area or silently change all
Play button behavior. Standalone existing test scenes remain usable.

## Streaming integration and overlap

Retain the two-volume mechanism and independent per-direction actions. The
Gameplay scene is protected from streaming unload even if its player is temporarily
inactive or absent. Keep the current player-containing-scene guard as well.

Ordinary traversal never repositions or recreates the player. Area load/unload
must not reset its cards, energy, health, unlocks or active Card Time source.
Use the existing coordinator for per-scene serialization; extend it with a
completion/result contract so startup and respawn can await a known outcome.
Already-loaded targets still require binding/state restoration before ready.

Asynchronous completion time is not guaranteed. Place and measure trigger lead
distance at the fastest supported movement speed. In the shared Blue/Pink
footprint, there is an additional constraint: reloading Blue too early can
reintroduce its colliders while the player is still crossing them in Pink.
Unload must finish before entering the overlap; reload must start after clearing
the conflicting footprint but early enough before Blue's floor is required.

The first baseline uses the existing route geometry and measured timing. If those
constraints cannot both hold, pure load/unload triggers are insufficient: a later
content-activation boundary or revised route is needed. Do not claim that merely
making Gameplay persistent solves overlapping collision or guarantees seamless
performance. Do not introduce an unapproved forced safe room or hidden traversal
pause. Failed traversal requests report a clear error; a release-ready recovery
policy is separate work.

## Respawn is necessary for the split

Replace persistent references to area spawn Transforms with an address consisting
of area ID and spawn ID. Resolve the Transform only while the area is loaded.
The first baseline respawn address is the configured starting marker; a checkpoint
activation system is not required for this migration.

On death: interrupt transient combat/Card Time through existing reset behavior,
hold player simulation, temporarily ignore directional requests, and settle
in-flight operations. Load the respawn area, restore its progress and resolve
its marker. Move the player, zero velocity, restore health according to current
rules, reset camera position, clear directional crossing histories, then resume.
Do not change energy/deck/death balancing as part of this migration.

To avoid reintroducing overlapping area colliders at the destination, respawn
reconciles the loaded area set to the destination area only, plus Gameplay and
existing services. This deliberate reset occurs on death, not during traversal.
Failed loads or missing/duplicate markers leave the player held with a clear
retryable error; never fall back to teleporting into an unloaded world at (0,0).

## Smallest useful area persistence

Proposed default awaiting user preference: opened gates stay open for this run;
enemies respawn/reset when an area reloads. Preserve Card Time unlock through the
existing persistent player. Split tutorial discovery from guide presentation so
the guide remains accessible outside Blue and is not automatically redisplayed
on every reload. Gate IDs are stable and unique within an area; restore opened
gates before declaring that area ready. No generic object snapshot framework.

Deferred: disk saves, enemy persistence, pickup persistence for future pickups,
checkpoint interaction design, elevator ride recovery, menus/session switching,
Addressables, automatic world streaming graphs and advanced camera zones.
Keep the existing temporary follow camera for the first slice.

## Migration boundary and validation

Use an idempotent Editor setup/migration command. Move the configured player root
(including camera) and HUD together using Unity APIs, preserve prefab overrides
and internal references, replace the area spawn reference, and audit all remaining
scene references. Remove area-only overview cameras from the gameplay composition
if present. Validate exactly one player, gameplay camera, AudioListener and required
UI input system. Do not overwrite hand-edited geometry or duplicate trigger objects.
Update area setup tools so rerunning them does not recreate local players/HUDs.
Include Gameplay and area scenes in the enabled Build Profile list.

Implementation can be delivered in two reviewable slices: ownership/startup and
Editor composition first; respawn/progress/streaming integration second. The first
slice alone is not acceptance for a fully playable unloadable-area loop.

Acceptance checks:

1. Start from Gameplay and via the Editor area test command; one player and one
   services root exist, with correct input, HUD and camera.
2. Unload/reload Blue from Pink repeatedly, including request reversals. Player
   identity and health/energy/deck/unlocks remain unchanged.
3. Cross the shared footprint both ways at maximum speed: no duplicate geometry
   collision, missing floors or forced stop. Record load/unload timing.
4. Die with the spawn area unloaded, including during an in-flight operation.
   Return to valid geometry once, with no stale trigger action or duplicate player.
5. Open both tutorial gates, unlock Card Time, unload/reload Blue. Verify the
   selected gate policy, retained unlock and accessible guide.
6. Spawn area-owned content while Gameplay is active; unloading its area removes it.
7. Missing scenes/markers and duplicate IDs give actionable errors; protected scene
   unload fails; repeat Play Mode with domain reload enabled and disabled.
8. Run migration twice; inspect saved scene references and prefab overrides.

Use unit tests for progress keys, startup/respawn sequencing, request arbitration
and failure paths. Use Unity integration/Play Mode checks for actual scene lifetime,
physics, camera, service rebinding and reference integrity. This spec pass runs no
gameplay tests because it changes no executable behavior.

## Unity references checked for 6000.3

- [SetActiveScene](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/SceneManagement.SceneManager.SetActiveScene.html): active scene controls default new-object ownership and lighting, not which loaded scenes render.
- [UnloadSceneAsync](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/SceneManagement.SceneManager.UnloadSceneAsync.html): destroys scene objects; completion time is unspecified; asset memory is a separate concern. Do not add UnloadUnusedAssets to every trigger crossing without profiling.
- [sceneLoaded](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/SceneManagement.SceneManager-sceneLoaded.html): notification occurs after OnEnable and before Start, relevant to dependency binding.

These API contracts support the proposal; Unity does not prescribe this exact
Gameplay/services split. Written-design review precedes implementation planning.
