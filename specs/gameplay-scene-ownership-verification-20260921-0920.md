# Gameplay Scene Ownership — Verification Note 20260921-0920

## Contexto

This verification records the pre-Unity-Editor state of the implementation described by
`specs/gameplay-scene-ownership-sdd-20260920-1853.md` and planned in
`specs/gameplay-scene-ownership-implementation-plan-20260920-1911.md`.
It follows the execution ledger in
`.superpowers/sdd/gameplay-scene-ownership-implementation-plan-20260920-1911/progress.md`.

The repository workflow assigns scene migration, serialized asset creation, Play Mode,
physics, camera, input, and build verification to the Unity Editor handoff. Unity was
not launched for this audit and no scene YAML was edited.

## Fresh command-line evidence

At 2026-09-21 09:20 America/Sao_Paulo:

- `.superpowers/sdd/gameplay-scene-ownership-implementation-plan-20260920-1911/compile.ps1 -Tests`
  completed with 0 warnings and 0 errors.
- `.superpowers/sdd/gameplay-scene-ownership-implementation-plan-20260920-1911/compile.ps1`
  completed with 0 warnings and 0 errors.
- `git diff --check` reported no whitespace errors. Git emitted only existing LF-to-CRLF
  conversion warnings for unrelated modified files.

These are C# compilation checks, not Unity Test Runner or Play Mode results. No direct
pure fixture was rerun: the only existing reflection runner covers the already-reviewed
streaming fixture, and constructing a general runner would not execute the remaining
fixtures correctly because they create Unity objects and require Unity lifecycle support.

## Static and serialized audit

- The Task 1–6 runtime, Editor, and fixture source files are present. Every inspected
  feature C# file has a corresponding `.meta` file.
- Existing Blue/Pink scene components point to the matching script GUIDs for
  `CardMeleeGate`, `CardTimeTutorialZone`, `DirectionalSceneTrigger`, and
  `SceneTriggerVolume`. Their existing local object references are intact.
- `Assets/Scenes/Gameplay.unity`, `Assets/Data/Areas/BlueArea.asset`, and
  `Assets/Data/Areas/PinkArea.asset` are absent. `Assets/Data/Areas` does not yet
  exist. This is expected before the migration command, but it prevents composition
  validation and the area-play command from operating.
- `ProjectSettings/EditorBuildSettings.asset` currently lists only
  `Assets/Scenes/SampleScene.unity`; Gameplay, Blue, and Pink are not verified build
  entries.
- The current Blue gate components have no serialized `gateId` values, and the areas
  have no `AreaSpawnPoint` components. The migration command is responsible for adding
  these serialized facts. Until it is run, gate persistence and address-based startup
  cannot be tested.
- The checked-in streaming/coordinator code provides protected gameplay-scene unload
  rejection, request results, directional suspension, readiness binding, respawn
  reconciliation, crossing/contact reset, and generation fencing. Static inspection
  cannot prove Unity callback ordering, object lifetimes, or shared-physics behavior.

## Task 7 acceptance status

| Required check | Status before Editor handoff | Evidence or remaining action |
| --- | --- | --- |
| Run named EditMode fixtures and existing regressions | Pending | Fixtures compile; Unity Test Runner has not run them. |
| Run idempotent migration and inspect saved serialization | Pending | Required generated scene/assets are absent. |
| Build/startup and area-play command; domain reload off | Pending | Requires generated composition, enabled build scenes, and Play Mode. |
| Repeated Pink/Blue streaming preserves player/session state | Pending | Requires Play Mode observations and instance/resource records. |
| Death while Blue is unloaded or a request is pending | Pending | Requires additive scene timing and Play Mode. |
| Fast bidirectional traversal and deliberately delayed load | Pending | Requires timing measurements; overlap-collider constraint remains unproven. |
| Area-owned temporary object is destroyed on unload | Pending | Requires Play Mode. |
| Missing build entry/marker and protected-unload diagnostic/retry | Partial | Static code paths exist; Unity Console and retry behavior remain unrun. |
| Compile and whitespace check | Complete for this audit | Fresh 0-warning/0-error compiles; `git diff --check` clean. |

## Required Unity handoff

Use Unity 6000.3.16f1 with no unsaved unrelated work in the affected scenes.

1. Let Unity import and compile the current files, then run **TIC > Setup > Create Or
   Update Gameplay Scene** once. Accept only the intended scene/asset saves. The command
   must create `Gameplay.unity`, `Assets/Data/Areas/BlueArea.asset`, and
   `Assets/Data/Areas/PinkArea.asset`, migrate the one player/HUD/camera/listener,
   create/reuse spawn markers and gate IDs, and add Gameplay/Blue/Pink to the active
   build scene list. If it stops on an active Build Profile diagnostic, add those exact
   three scenes to that profile and rerun the same command.
2. Save `Gameplay`, `BlueArea_Tutorial`, `PinkArea_Perimeters`, and both AreaDefinition
   assets. Rerun the setup menu once; it must report/leave one player, HUD, camera,
   listener, root, markers, and gate IDs without duplicates. Return the saved diffs and
   the first full Console error if any.
3. In the Unity Test Runner, run `AreaRunProgressTests`, `DirectionalSceneStreamingTests`,
   `CardMeleeGateTests`, `AreaProgressBindingTests`, `GameplayStartupTests`,
   `GameplayRespawnTests`, and `GameplaySceneSetupTests`, plus the existing gate and
   streaming regressions. Record the exact passed/failed/skipped counts.
4. Run **TIC > Play > Area With Gameplay** for Blue and Pink. Verify one player,
   gameplay camera, AudioListener, services root, HUD and input. Repeat after disabling
   domain reload in Enter Play Mode Options.
5. In Play Mode, record player instance ID, health, energy, deck, Card Time source and
   unlock before/after repeated Blue unload/reload from Pink. Open gates and discover
   the guide before one reload; verify opened gates persist and the guide remains
   accessible.
6. Kill the player with Blue unloaded and during a pending Blue request. Verify one
   respawn at a unique valid marker, no resumed stale crossing, no conflicting retained
   area, and actionable retry output for a deliberately missing marker/build entry or
   protected unload.
7. Measure Blue load/unload completion and maximum-speed crossings in both directions,
   including one deliberately delayed load. In the shared Blue/Pink footprint, stop and
   report any duplicate geometry collision, missing floor, or forced stop; this audit
   does not authorize a travel freeze or layout change.
8. Parent a temporary object to the loaded area, unload that area, and confirm the
   object is destroyed. Also check player effects contain no retained reference to its
   destroyed target.

## Remaining risks

The main unresolved risk is geometry reappearing from Blue while the player occupies
Pink's overlapping shaft. The runtime request contract cannot establish safe physical
lead distance. Scene migration, active Build Profile behavior, Unity Editor callbacks,
domain-reload-disabled static reset, and serialized cross-scene reference validation
also remain unverified until the handoff above is completed.
