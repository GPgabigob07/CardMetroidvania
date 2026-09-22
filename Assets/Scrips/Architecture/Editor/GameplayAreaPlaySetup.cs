using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using TicGame.Architecture;

namespace TicGame.Architecture.EditorTools
{
    /// <summary>Opens Gameplay with one authored area for a focused play session.</summary>
    [InitializeOnLoad]
    public sealed class GameplayAreaPlaySetup : EditorWindow
    {
        private const string SessionKey = "TicGame.GameplayAreaPlaySetup";
        private const string SelectedAreaKey = SessionKey + ".area";
        private const string SelectedSpawnKey = SessionKey + ".spawn";
        private const string SceneSetupKey = SessionKey + ".sceneSetup";
        private const string GameplayScenePath = "Assets/Scenes/Gameplay.unity";
        private const string DefaultSpawnId = "blue-start";

        private AreaDefinition[] areas = Array.Empty<AreaDefinition>();
        private int selectedArea;
        private string spawnId = DefaultSpawnId;

        static GameplayAreaPlaySetup()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        [MenuItem("TIC/Play/Area With Gameplay")]
        private static void OpenWindow()
        {
            GetWindow<GameplayAreaPlaySetup>(true, "Play Area With Gameplay", true).ShowUtility();
        }

        private void OnEnable()
        {
            areas = LoadAreas();
            selectedArea = Mathf.Clamp(selectedArea, 0, Math.Max(0, areas.Length - 1));
            if (areas.Length > 0) spawnId = areas[selectedArea].DefaultSpawnId;
        }

        private void OnGUI()
        {
            if (areas.Length == 0)
            {
                EditorGUILayout.HelpBox("No AreaDefinition assets were found. Run the Gameplay migration first.", MessageType.Warning);
                if (GUILayout.Button("Close")) Close();
                return;
            }

            var names = areas.Select(area => area.AreaId).ToArray();
            selectedArea = EditorGUILayout.Popup("Area", selectedArea, names);
            spawnId = EditorGUILayout.TextField("Spawn ID", spawnId);
            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
            {
                if (GUILayout.Button("Play Area")) StartPlay();
            }
            if (GUILayout.Button("Cancel")) Close();
        }

        private void StartPlay()
        {
            var area = areas[selectedArea];
            if (string.IsNullOrWhiteSpace(spawnId)) spawnId = area.DefaultSpawnId;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.Log("Area play cancelled because modified scenes were not saved.");
                return;
            }

            var priorSetup = EditorSceneManager.GetSceneManagerSetup();
            SessionState.SetString(SceneSetupKey, SerializeSceneSetup(priorSetup));
            SessionState.SetString(SelectedAreaKey, area.AreaId);
            SessionState.SetString(SelectedSpawnKey, spawnId);
            try
            {
                var gameplay = EditorSceneManager.OpenScene(GameplayScenePath, OpenSceneMode.Single);
                if (!gameplay.IsValid()) throw new InvalidOperationException($"Could not open {GameplayScenePath}.");
                var areaScene = EditorSceneManager.OpenScene(area.ScenePath, OpenSceneMode.Additive);
                if (!areaScene.IsValid()) throw new InvalidOperationException($"Could not open {area.ScenePath}.");
                SceneManager.SetActiveScene(gameplay);
                Close();
                EditorApplication.isPlaying = true;
            }
            catch (Exception exception)
            {
                RestorePendingSetup();
                ClearSessionState();
                GameplayStartupOverride.Clear();
                Debug.LogError($"Area play setup failed: {exception.Message}");
            }
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode)
            {
                GameplayStartupOverride.Clear();
                return;
            }

            if (state != PlayModeStateChange.EnteredEditMode || !HasPendingRestore()) return;
            RestorePendingSetup();
            ClearSessionState();
            GameplayStartupOverride.Clear();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ConsumeSelectedStartupOverride()
        {
            var areaId = SessionState.GetString(SelectedAreaKey, string.Empty);
            var spawnId = SessionState.GetString(SelectedSpawnKey, string.Empty);
            if (string.IsNullOrWhiteSpace(areaId) || string.IsNullOrWhiteSpace(spawnId)) return;
            GameplayStartupOverride.Set(areaId, spawnId);
            SessionState.EraseString(SelectedAreaKey);
            SessionState.EraseString(SelectedSpawnKey);
        }

        private static void RestorePendingSetup()
        {
            var setup = DeserializeSceneSetup(SessionState.GetString(SceneSetupKey, string.Empty));
            if (setup.Length == 0) return;
            EditorSceneManager.RestoreSceneManagerSetup(setup);
            var active = setup.FirstOrDefault(item => item.isActive);
            if (active != null)
            {
                var scene = SceneManager.GetSceneByPath(active.path);
                if (scene.IsValid()) SceneManager.SetActiveScene(scene);
            }
        }

        public static string SerializeSceneSetup(SceneSetup[] setup)
        {
            return GameplayScenePlayState.Serialize(setup.Select(item => new GameplaySceneSetupState(item.path, item.isLoaded, item.isActive)));
        }

        public static SceneSetup[] DeserializeSceneSetup(string value)
        {
            return GameplayScenePlayState.Deserialize(value)
                .Select(item => new SceneSetup { path = item.Path, isLoaded = item.IsLoaded, isActive = item.IsActive }).ToArray();
        }

        private static AreaDefinition[] LoadAreas()
        {
            return AssetDatabase.FindAssets("t:AreaDefinition")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<AreaDefinition>)
                .Where(area => area != null && !string.IsNullOrEmpty(area.ScenePath))
                .OrderBy(area => area.AreaId, StringComparer.Ordinal)
                .ToArray();
        }

        private static bool HasPendingRestore() => !string.IsNullOrEmpty(SessionState.GetString(SceneSetupKey, string.Empty));

        private static void ClearSessionState()
        {
            SessionState.EraseString(SceneSetupKey);
            SessionState.EraseString(SelectedAreaKey);
            SessionState.EraseString(SelectedSpawnKey);
        }

    }
}
