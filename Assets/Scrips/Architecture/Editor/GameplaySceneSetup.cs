using System;
using System.Collections.Generic;
using System.Linq;
using TicGame.Architecture;
using UnityEditor;
using UnityEditor.Build.Profile;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TicGame.Architecture.EditorTools
{
    /// <summary>
    /// Migrates the authored player and presentation composition into Gameplay.
    /// </summary>
    public static class GameplaySceneSetup
    {
        private const string GameplayScenePath = "Assets/Scenes/Gameplay.unity";
        private const string BlueScenePath = "Assets/Scenes/BlueArea_Tutorial.unity";
        private const string PinkScenePath = "Assets/Scenes/PinkArea_Perimeters.unity";
        private const string AreaFolder = "Assets/Data/Areas";
        private const string BlueDefinitionPath = AreaFolder + "/BlueArea.asset";
        private const string PinkDefinitionPath = AreaFolder + "/PinkArea.asset";
        private const string GameplayRootName = "[Gameplay Composition]";
        private const string HudRootName = "[Player HUD]";
        private const string BlueAreaId = "blue";
        private const string PinkAreaId = "pink";
        private const string BlueSpawnId = "blue-start";
        private const string PinkSpawnId = "pink-start";

        [MenuItem("TIC/Setup/Create Or Update Gameplay Scene")]
        public static void CreateOrUpdateGameplayScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("Gameplay migration cannot run while entering or in Play Mode.");
                return;
            }

            if (HasUnsavedGameplayScene())
            {
                Debug.LogError("An unsaved Gameplay scene from an earlier failed migration is still loaded. "
                    + "Close it without saving (or restart Unity) before rerunning this command.");
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.Log("Gameplay migration cancelled because modified scenes were not saved.");
                return;
            }

            var inventory = InventoryScenes();
            if (!inventory.TryPrepare(out var error))
            {
                Debug.LogError($"Gameplay migration aborted before saving: {error}");
                return;
            }

            var undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Migrate Gameplay Scene Ownership");
            var createdAssetPaths = new List<string>();
            var savedScenePaths = new List<string>();
            var originalBuildScenes = EditorBuildSettings.scenes.ToArray();
            var originalPlayerScene = inventory.Player.gameObject.scene;
            var originalHudScene = inventory.HudRoot.scene;
            var playerRoot = inventory.Player.transform.root.gameObject;
            var cameraRoot = inventory.GameplayCamera.transform.root.gameObject;
            var cameraMovesIndependently = cameraRoot != playerRoot;
            var originalCameraScene = cameraMovesIndependently
                ? inventory.GameplayCamera.gameObject.scene
                : default;
            try
            {
                var gameplayScene = inventory.GameplayScene;
                var blueScene = inventory.BlueScene;
                var pinkScene = inventory.PinkScene;
                var player = inventory.Player;
                var hud = inventory.HudRoot;

                MoveToGameplay(playerRoot, gameplayScene);
                if (hud != null && hud.scene != gameplayScene)
                {
                    MoveToGameplay(hud, gameplayScene);
                }
                if (cameraMovesIndependently
                    && inventory.GameplayCamera.scene != gameplayScene)
                {
                    MoveToGameplay(cameraRoot, gameplayScene);
                }

                var blueSpawn = EnsureSpawn(blueScene, BlueSpawnId, FindBlueSpawnTransform(blueScene));
                var pinkSpawn = EnsureSpawn(pinkScene, PinkSpawnId, FindPinkSpawnTransform(pinkScene));
                var blueDefinition = CreateOrUpdateAreaDefinition(
                    BlueDefinitionPath, BlueAreaId, BlueScenePath, BlueSpawnId, createdAssetPaths);
                var pinkDefinition = CreateOrUpdateAreaDefinition(
                    PinkDefinitionPath, PinkAreaId, PinkScenePath, PinkSpawnId, createdAssetPaths);
                ConfigureGates(blueScene, BlueAreaId);
                ConfigureGates(pinkScene, PinkAreaId);
                ClearExplicitPlayerTargets(blueScene, player.transform);
                ClearExplicitPlayerTargets(pinkScene, player.transform);
                ClearRespawnTarget(player.gameObject);

                var composition = GetOrCreateComposition(gameplayScene);
                var playerHold = player.GetComponent<PlayerWorldHold>()
                    ?? Undo.AddComponent<PlayerWorldHold>(player.gameObject);
                var guide = composition.GetComponent<CardTimeGuideUI>()
                    ?? Undo.AddComponent<CardTimeGuideUI>(composition);
                var coordinator = composition.GetComponent<GameplayAreaCoordinator>()
                    ?? Undo.AddComponent<GameplayAreaCoordinator>(composition);
                var root = composition.GetComponent<GameplaySceneRoot>()
                    ?? Undo.AddComponent<GameplaySceneRoot>(composition);
                WireComposition(root, player, playerHold, guide, coordinator, blueDefinition, pinkDefinition);

                RemoveOverviewCameras(blueScene, inventory.GameplayCamera);
                RemoveOverviewCameras(pinkScene, inventory.GameplayCamera);
                RemoveOverviewCameras(gameplayScene, inventory.GameplayCamera);
                if (!TryValidateSceneReferences(gameplayScene, blueScene, pinkScene, out error))
                {
                    throw new InvalidOperationException(error);
                }

                EditorUtility.SetDirty(root);
                EditorUtility.SetDirty(composition);
                EditorSceneManager.MarkSceneDirty(gameplayScene);
                EditorSceneManager.MarkSceneDirty(blueScene);
                EditorSceneManager.MarkSceneDirty(pinkScene);
                if (!SaveScene(gameplayScene, inventory.CreatedGameplayScene))
                {
                    throw new InvalidOperationException($"Could not save Gameplay scene at {GameplayScenePath}.");
                }
                savedScenePaths.Add(GameplayScenePath);
                if (!EditorSceneManager.SaveScene(blueScene))
                {
                    throw new InvalidOperationException($"Could not save area scene at {BlueScenePath}.");
                }
                savedScenePaths.Add(BlueScenePath);
                if (!EditorSceneManager.SaveScene(pinkScene))
                {
                    throw new InvalidOperationException($"Could not save area scene at {PinkScenePath}.");
                }
                savedScenePaths.Add(PinkScenePath);
                AssetDatabase.Refresh();
                UpdateBuildSceneLists();
                AssetDatabase.SaveAssets();
                Undo.CollapseUndoOperations(undoGroup);
                Debug.Log("Gameplay scene migration completed. Gameplay owns one player, camera, listener and HUD.");
            }
            catch (Exception exception)
            {
                if (savedScenePaths.Count > 0)
                {
                    Debug.LogError($"Gameplay migration failed after saving {string.Join(", ", savedScenePaths)}. "
                        + "The migration was not rolled back because restoring in-memory state would diverge from saved scenes. "
                        + $"Resolve the save failure, inspect those scenes, then rerun the command.\n{exception}");
                    return;
                }

                foreach (var path in createdAssetPaths)
                {
                    if (AssetDatabase.LoadMainAssetAtPath(path) != null)
                    {
                        AssetDatabase.DeleteAsset(path);
                    }
                }

                EditorBuildSettings.scenes = originalBuildScenes;

                var rollbackErrors = new List<string>();
                TryMoveRootWithoutUndo(playerRoot, originalPlayerScene, rollbackErrors);
                TryMoveRootWithoutUndo(inventory.HudRoot, originalHudScene, rollbackErrors);
                if (cameraMovesIndependently)
                {
                    TryMoveRootWithoutUndo(cameraRoot, originalCameraScene, rollbackErrors);
                }
                TryRevertOrdinaryUndoOperations(undoGroup, rollbackErrors);

                if (inventory.CreatedGameplayScene)
                {
                    TryCloseUnsavedGameplayScene(inventory.GameplayScene, rollbackErrors);
                    if (AssetDatabase.LoadMainAssetAtPath(GameplayScenePath) != null)
                    {
                        AssetDatabase.DeleteAsset(GameplayScenePath);
                    }
                }

                var rollbackSummary = rollbackErrors.Count == 0
                    ? "Scene-root ownership was restored without saving."
                    : $"Rollback could not restore: {string.Join("; ", rollbackErrors)}. No scenes were saved.";
                Debug.LogError($"Gameplay migration failed. {rollbackSummary}\n{exception}");
            }
        }

        private static SceneInventory InventoryScenes()
        {
            var blue = SceneManager.GetSceneByPath(BlueScenePath);
            var pink = SceneManager.GetSceneByPath(PinkScenePath);
            var gameplay = SceneManager.GetSceneByPath(GameplayScenePath);
            return new SceneInventory(blue, pink, gameplay);
        }

        private static bool HasUnsavedGameplayScene()
        {
            return Enumerable.Range(0, SceneManager.sceneCount)
                .Select(SceneManager.GetSceneAt)
                .Any(scene => scene.isLoaded
                    && string.IsNullOrEmpty(scene.path)
                    && string.Equals(scene.name, "Gameplay", StringComparison.Ordinal));
        }

        private static GameObject GetOrCreateComposition(Scene gameplayScene)
        {
            var existing = gameplayScene.GetRootGameObjects()
                .FirstOrDefault(root => root.GetComponent<GameplaySceneRoot>() != null);
            if (existing != null)
            {
                return existing;
            }

            existing = new GameObject(GameplayRootName);
            SceneManager.MoveGameObjectToScene(existing, gameplayScene);
            Undo.RegisterCreatedObjectUndo(existing, "Create Gameplay Composition");
            return existing;
        }

        private static void WireComposition(
            GameplaySceneRoot root,
            PlayerController player,
            PlayerWorldHold playerHold,
            CardTimeGuideUI guide,
            GameplayAreaCoordinator coordinator,
            AreaDefinition blue,
            AreaDefinition pink)
        {
            var serialized = new SerializedObject(root);
            serialized.FindProperty("player").objectReferenceValue = player;
            serialized.FindProperty("playerHold").objectReferenceValue = playerHold;
            serialized.FindProperty("guide").objectReferenceValue = guide;
            serialized.FindProperty("coordinator").objectReferenceValue = coordinator;
            serialized.FindProperty("areaDefinitions").arraySize = 2;
            serialized.FindProperty("areaDefinitions").GetArrayElementAtIndex(0).objectReferenceValue = blue;
            serialized.FindProperty("areaDefinitions").GetArrayElementAtIndex(1).objectReferenceValue = pink;
            serialized.FindProperty("initialAreaId").stringValue = BlueAreaId;
            serialized.FindProperty("initialSpawnId").stringValue = BlueSpawnId;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void MoveToGameplay(GameObject target, Scene gameplayScene)
        {
            if (target == null || target.transform.parent != null)
            {
                throw new InvalidOperationException("Only a scene root can be moved into Gameplay.");
            }

            SceneManager.MoveGameObjectToScene(target, gameplayScene);
        }

        private static void TryMoveRootWithoutUndo(GameObject target, Scene destination, ICollection<string> errors)
        {
            if (target == null || !destination.IsValid() || target.scene == destination)
            {
                return;
            }

            if (target.transform.parent != null)
            {
                errors.Add($"{target.name} is a child of {target.transform.parent.name}");
                return;
            }

            try
            {
                SceneManager.MoveGameObjectToScene(target, destination);
            }
            catch (Exception exception)
            {
                errors.Add($"{target.name}: {exception.Message}");
            }
        }

        private static void TryCloseUnsavedGameplayScene(Scene gameplayScene, ICollection<string> errors)
        {
            if (!gameplayScene.IsValid() || !gameplayScene.isLoaded)
            {
                return;
            }

            try
            {
                if (!EditorSceneManager.CloseScene(gameplayScene, true))
                {
                    errors.Add("the temporary Gameplay scene could not be closed");
                }
            }
            catch (Exception exception)
            {
                errors.Add($"temporary Gameplay scene: {exception.Message}");
            }
        }

        private static void TryRevertOrdinaryUndoOperations(int undoGroup, ICollection<string> errors)
        {
            try
            {
                Undo.RevertAllDownToGroup(undoGroup);
            }
            catch (Exception exception)
            {
                errors.Add($"ordinary migration edits: {exception.Message}");
            }
        }

        private static bool SaveScene(Scene scene, bool saveAs)
        {
            return saveAs
                ? EditorSceneManager.SaveScene(scene, GameplayScenePath)
                : EditorSceneManager.SaveScene(scene);
        }

        private static Transform FindBlueSpawnTransform(Scene scene)
        {
            return FindNamedTransform(scene, "Start and respawn")
                ?? FindNamedTransform(scene, "Blue Start - 20 x 20 movement room")
                ?? scene.GetRootGameObjects().FirstOrDefault()?.transform;
        }

        private static Transform FindPinkSpawnTransform(Scene scene)
        {
            return FindNamedTransform(scene, "01 - Arrival chamber")
                ?? FindNamedTransform(scene, "Pink Area - Perimeters Only")
                ?? scene.GetRootGameObjects().FirstOrDefault()?.transform;
        }

        private static Transform FindNamedTransform(Scene scene, string name)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                var candidate = root.GetComponentsInChildren<Transform>(true)
                    .FirstOrDefault(transform => transform.name == name);
                if (candidate != null)
                {
                    return candidate;
                }
            }

            return null;
        }

        private static AreaSpawnPoint EnsureSpawn(Scene scene, string spawnId, Transform authoredTransform)
        {
            if (authoredTransform == null)
            {
                throw new InvalidOperationException($"Scene '{scene.path}' has no transform suitable for spawn '{spawnId}'.");
            }

            var points = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<AreaSpawnPoint>(true))
                .Where(point => point.SpawnId == spawnId)
                .ToArray();
            if (points.Length > 1)
            {
                throw new InvalidOperationException($"Scene '{scene.path}' has duplicate spawn markers named '{spawnId}'.");
            }

            var point = points.SingleOrDefault();
            if (point == null)
            {
                point = authoredTransform.gameObject.AddComponent<AreaSpawnPoint>();
                Undo.RegisterCreatedObjectUndo(point, "Create area spawn marker");
            }

            Undo.RecordObject(point, "Configure area spawn marker");
            point.Configure(spawnId);
            EditorUtility.SetDirty(point);
            return point;
        }

        private static AreaDefinition CreateOrUpdateAreaDefinition(
            string path,
            string areaId,
            string scenePath,
            string spawnId,
            ICollection<string> createdAssetPaths)
        {
            EnsureFolder(AreaFolder);
            var definition = AssetDatabase.LoadAssetAtPath<AreaDefinition>(path);
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<AreaDefinition>();
                definition.name = System.IO.Path.GetFileNameWithoutExtension(path);
                AssetDatabase.CreateAsset(definition, path);
                createdAssetPaths.Add(path);
            }

            Undo.RecordObject(definition, "Configure area definition");
            definition.Configure(areaId, scenePath, spawnId);
            EditorUtility.SetDirty(definition);
            return definition;
        }

        private static void ConfigureGates(Scene scene, string areaId)
        {
            var gates = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<CardMeleeGate>(true))
                .OrderBy(gate => GetHierarchyPath(gate.transform), StringComparer.Ordinal)
                .ToArray();
            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < gates.Length; index++)
            {
                var serialized = new SerializedObject(gates[index]);
                var property = serialized.FindProperty("gateId");
                var desired = property != null && !string.IsNullOrWhiteSpace(property.stringValue)
                    ? property.stringValue
                    : $"{areaId}-gate-{index + 1:00}";
                if (!ids.Add(desired))
                {
                    desired = $"{areaId}-gate-{index + 1:00}";
                    while (!ids.Add(desired)) desired += "-copy";
                }

                if (property != null)
                {
                    Undo.RecordObject(gates[index], "Configure gate identity");
                    property.stringValue = desired;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                }
            }
        }

        private static void ClearExplicitPlayerTargets(Scene scene, Transform player)
        {
            foreach (var brain in scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<GolemChargerBrain>(true)))
            {
                var serialized = new SerializedObject(brain);
                var target = serialized.FindProperty("target");
                if (target?.objectReferenceValue is Transform transform
                    && (transform == player || transform.IsChildOf(player)))
                {
                    Undo.RecordObject(brain, "Clear charger player target");
                    target.objectReferenceValue = null;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                }
            }
        }

        private static void ClearRespawnTarget(GameObject player)
        {
            var respawn = player.GetComponent<PlayerDeathRespawn>();
            if (respawn == null) return;
            var serialized = new SerializedObject(respawn);
            var target = serialized.FindProperty("respawnTarget");
            if (target != null)
            {
                Undo.RecordObject(respawn, "Clear cross-scene respawn target");
                target.objectReferenceValue = null;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void RemoveOverviewCameras(Scene gameplayScene, Camera gameplayCamera)
        {
            foreach (var camera in gameplayScene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Camera>(true)))
            {
                if (camera == gameplayCamera || camera.transform.IsChildOf(gameplayCamera.transform)) continue;
                if (!camera.name.Contains("overview", StringComparison.OrdinalIgnoreCase)) continue;
                Undo.DestroyObjectImmediate(camera.gameObject);
            }
        }

        private static bool IsOverviewCamera(Camera camera)
        {
            return camera != null && camera.name.Contains("overview", StringComparison.OrdinalIgnoreCase);
        }

        private static bool TryValidateSceneReferences(
            Scene gameplay,
            Scene blue,
            Scene pink,
            out string error)
        {
            var errors = new List<string>();
            var scenes = new[] { gameplay, blue, pink };
            foreach (var scene in scenes)
            {
                foreach (var root in scene.GetRootGameObjects())
                {
                    foreach (var component in root.GetComponentsInChildren<Component>(true))
                    {
                        if (component == null) continue;
                        var serialized = new SerializedObject(component);
                        var iterator = serialized.GetIterator();
                        while (iterator.NextVisible(true))
                        {
                            if (iterator.propertyType != SerializedPropertyType.ObjectReference
                                || iterator.objectReferenceValue == null
                                || EditorUtility.IsPersistent(iterator.objectReferenceValue)) continue;
                            var referencedScene = GetObjectScene(iterator.objectReferenceValue);
                            if (referencedScene.IsValid() && referencedScene != scene)
                            {
                                errors.Add($"{scene.path}:{component.GetType().Name}.{iterator.propertyPath} references {referencedScene.path}.");
                            }
                        }
                    }
                }
            }

            var gameplayCameras = FindSceneObjects<Camera>(gameplay);
            var listeners = FindSceneObjects<AudioListener>(gameplay);
            if (gameplayCameras.Length != 1) errors.Add($"Gameplay must contain exactly one Camera (found {gameplayCameras.Length}).");
            if (listeners.Length != 1) errors.Add($"Gameplay must contain exactly one AudioListener (found {listeners.Length}).");
            var players = FindSceneObjects<PlayerController>(gameplay);
            if (players.Length != 1) errors.Add($"Gameplay must contain exactly one PlayerController (found {players.Length}).");
            error = errors.Count == 0 ? null : string.Join(Environment.NewLine, errors.Distinct());
            return errors.Count == 0;
        }

        private static T[] FindSceneObjects<T>(Scene scene) where T : Component
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return Array.Empty<T>();
            }

            return scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<T>(true))
                .ToArray();
        }

        private static GameObject[] GetRootObjects(Scene scene)
        {
            return !scene.IsValid() || !scene.isLoaded
                ? Array.Empty<GameObject>()
                : scene.GetRootGameObjects();
        }

        private static Scene GetObjectScene(UnityEngine.Object target)
        {
            if (target is GameObject gameObject) return gameObject.scene;
            if (target is Component component) return component.gameObject.scene;
            return default;
        }

        private static string GetHierarchyPath(Transform transform)
        {
            var names = new Stack<string>();
            while (transform != null)
            {
                names.Push(transform.name);
                transform = transform.parent;
            }

            return string.Join("/", names);
        }

        private static void UpdateBuildSceneLists()
        {
            var required = new[] { GameplayScenePath, BlueScenePath, PinkScenePath };
            EditorBuildSettings.scenes = RebuildRequiredBuildScenes(EditorBuildSettings.scenes).ToArray();
            UpdateAndValidateActiveBuildProfile(required);
        }

        private static void UpdateAndValidateActiveBuildProfile(IEnumerable<string> requiredPaths)
        {
            var profile = BuildProfile.GetActiveBuildProfile();
            if (profile == null || !profile.overrideGlobalScenes)
            {
                return;
            }

            Undo.RecordObject(profile, "Configure active Build Profile scenes");
            profile.scenes = RebuildRequiredBuildScenes(profile.scenes).ToArray();
            EditorUtility.SetDirty(profile);

            var configured = new HashSet<string>(profile.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path), StringComparer.Ordinal);
            var missing = requiredPaths.Where(path => !configured.Contains(path)).ToArray();
            if (missing.Length > 0)
            {
                throw new InvalidOperationException(
                    $"Active Build Profile '{profile.name}' omits or disables required scenes: {string.Join(", ", missing)}.");
            }
        }

        private static List<EditorBuildSettingsScene> RebuildRequiredBuildScenes(
            IEnumerable<EditorBuildSettingsScene> existingScenes)
        {
            var requiredPaths = new HashSet<string>(new[]
            {
                GameplayScenePath,
                BlueScenePath,
                PinkScenePath
            }, StringComparer.Ordinal);
            var rebuilt = existingScenes
                .Where(scene => !requiredPaths.Contains(scene.path))
                .ToList();
            rebuilt.Insert(0, CreateGuidBackedBuildScene(GameplayScenePath));
            rebuilt.Add(CreateGuidBackedBuildScene(BlueScenePath));
            rebuilt.Add(CreateGuidBackedBuildScene(PinkScenePath));
            return rebuilt;
        }

        private static EditorBuildSettingsScene CreateGuidBackedBuildScene(string scenePath)
        {
            var guid = AssetDatabase.AssetPathToGUID(scenePath);
            if (string.IsNullOrWhiteSpace(guid)
                || string.Equals(guid, "00000000000000000000000000000000", StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Cannot add '{scenePath}' to Build Settings because Unity has not assigned it a valid asset GUID.");
            }

            return new EditorBuildSettingsScene(new GUID(guid), true);
        }

        private static void EnsureFolder(string path)
        {
            var segments = path.Split('/');
            var current = segments[0];
            for (var index = 1; index < segments.Length; index++)
            {
                var next = $"{current}/{segments[index]}";
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, segments[index]);
                current = next;
            }
        }

        private sealed class SceneInventory
        {
            public SceneInventory(Scene blue, Scene pink, Scene gameplay)
            {
                BlueScene = blue;
                PinkScene = pink;
                GameplayScene = gameplay;
            }

            public Scene BlueScene { get; private set; }
            public Scene PinkScene { get; private set; }
            public Scene GameplayScene { get; private set; }
            public bool CreatedGameplayScene { get; private set; }
            public PlayerController Player { get; private set; }
            public GameObject HudRoot { get; private set; }
            public Camera GameplayCamera { get; private set; }

            public bool TryPrepare(out string error)
            {
                if (!BlueScene.IsValid() || !BlueScene.isLoaded)
                {
                    BlueScene = EditorSceneManager.OpenScene(BlueScenePath, OpenSceneMode.Additive);
                }
                if (!BlueScene.IsValid() || !BlueScene.isLoaded)
                {
                    error = $"Required area scene is not available: {BlueScenePath}.";
                    return false;
                }
                if (!PinkScene.IsValid() || !PinkScene.isLoaded)
                {
                    PinkScene = EditorSceneManager.OpenScene(PinkScenePath, OpenSceneMode.Additive);
                }
                if (!PinkScene.IsValid() || !PinkScene.isLoaded)
                {
                    error = $"Required area scene is not available: {PinkScenePath}.";
                    return false;
                }

                if (!GameplayScene.IsValid() || !GameplayScene.isLoaded)
                {
                    var unsavedGameplayScenes = Enumerable.Range(0, SceneManager.sceneCount)
                        .Select(SceneManager.GetSceneAt)
                        .Where(scene => scene.isLoaded
                            && string.IsNullOrEmpty(scene.path)
                            && string.Equals(scene.name, "Gameplay", StringComparison.Ordinal))
                        .ToArray();
                    if (unsavedGameplayScenes.Length > 0)
                    {
                        error = "An unsaved Gameplay scene from an earlier failed migration is still loaded. "
                            + "Close it without saving (or restart Unity) before rerunning this command.";
                        return false;
                    }

                    if (AssetDatabase.LoadAssetAtPath<SceneAsset>(GameplayScenePath) != null)
                    {
                        GameplayScene = EditorSceneManager.OpenScene(GameplayScenePath, OpenSceneMode.Additive);
                        if (!GameplayScene.IsValid() || !GameplayScene.isLoaded)
                        {
                            error = $"Existing Gameplay scene could not be opened: {GameplayScenePath}.";
                            return false;
                        }
                    }
                }

                var bluePlayers = FindSceneObjects<PlayerController>(BlueScene);
                var pinkPlayers = FindSceneObjects<PlayerController>(PinkScene);
                var gameplayPlayers = FindSceneObjects<PlayerController>(GameplayScene);
                if (bluePlayers.Length + pinkPlayers.Length + gameplayPlayers.Length != 1)
                {
                    error = $"Expected one configured player across Blue, Pink and Gameplay; found {bluePlayers.Length} in Blue, {pinkPlayers.Length} in Pink and {gameplayPlayers.Length} in Gameplay.";
                    return false;
                }

                Player = bluePlayers.SingleOrDefault() ?? pinkPlayers.SingleOrDefault() ?? gameplayPlayers.Single();
                var blueHud = GetRootObjects(BlueScene).SingleOrDefault(root => root.name == HudRootName);
                var pinkHud = GetRootObjects(PinkScene).SingleOrDefault(root => root.name == HudRootName);
                var gameplayHud = GetRootObjects(GameplayScene).SingleOrDefault(root => root.name == HudRootName);
                if (new[] { blueHud, pinkHud, gameplayHud }.Count(hud => hud != null) > 1)
                {
                    error = "HUD ownership is ambiguous: more than one area contains [Player HUD].";
                    return false;
                }

                HudRoot = blueHud ?? pinkHud ?? gameplayHud;
                if (HudRoot == null)
                {
                    error = "Expected one configured [Player HUD] root in Blue or Gameplay.";
                    return false;
                }

                var cameras = FindSceneObjects<Camera>(BlueScene)
                    .Concat(FindSceneObjects<Camera>(PinkScene))
                    .Concat(FindSceneObjects<Camera>(GameplayScene))
                    .Where(camera => !IsOverviewCamera(camera))
                    .ToArray();
                var listeners = FindSceneObjects<AudioListener>(BlueScene)
                    .Concat(FindSceneObjects<AudioListener>(PinkScene))
                    .Concat(FindSceneObjects<AudioListener>(GameplayScene))
                    .Where(listener => !IsOverviewCamera(listener.GetComponent<Camera>()))
                    .ToArray();
                if (cameras.Length != 1 || listeners.Length != 1)
                {
                    error = $"Expected one gameplay Camera and AudioListener; found {cameras.Length} cameras and {listeners.Length} listeners.";
                    return false;
                }

                GameplayCamera = cameras[0];
                if (!GameplayScene.IsValid() || !GameplayScene.isLoaded)
                {
                    GameplayScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
                    var newGameplayScene = GameplayScene;
                    newGameplayScene.name = "Gameplay";
                    CreatedGameplayScene = true;
                }

                error = null;
                return true;
            }
        }
    }
}
