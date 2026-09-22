# Gameplay Scene Ownership — Verification Update 20260921-1044

## Contexto

This update follows `specs/gameplay-scene-ownership-verification-20260921-0920.md` and the implementation in `specs/gameplay-scene-ownership-sdd-20260920-1853.md`. It records the first successful Unity Editor migration after repairs to child-camera movement, rollback, and unloaded scene handling. The older pre-Editor verification note remains as history.

## Observed state

- The user ran **TIC > Setup > Create Or Update Gameplay Scene** and reported completion. `Assets/Scenes/Gameplay.unity` and both `Assets/Data/Areas/*.asset` definitions are now saved.
- Serialized inspection finds one `PlayerController`, one camera, one `AudioListener`, one `GameplaySceneRoot`, one `GameplayAreaCoordinator`, and one `[Player HUD]` in Gameplay. Blue and Pink each have one spawn marker. Blue has two stable gate IDs.
- The initial Editor command wrote a zero GUID for Gameplay in `ProjectSettings/EditorBuildSettings.asset` even though `Gameplay.unity.meta` contains `45074cca45fc77443a88969b3a25ef58`. The setup command now constructs GUID-backed build entries. The known Gameplay Build Settings GUID was corrected directly from its `.meta` file; the scene path and GUID match.
- The runtime, Editor, and test C# sources have compiled through the local Unity-assembly harness. This checks compilation only, not Unity lifecycle behavior.

## Validation still pending

The user chose to skip Unity unit testing for now. Gameplay Play Mode startup, Blue/Pink streaming, Card Time gating, persistent gate state, respawn, and overlapping-area physics have not been observed in this verification. Those checks remain necessary before treating the scene-ownership behavior as playtested.
