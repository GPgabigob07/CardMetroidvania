using System.Linq;
using TicGame.Architecture;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TicGame.Architecture.EditorTools
{
    public static class PlayerPrefabAndGolemTestSceneSetup
    {
        private const string SampleScenePath = "Assets/Scenes/SampleScene.unity";
        private const string TestScenePath = "Assets/Scenes/Test_GolemCharger.unity";
        private const string PlayerPrefabFolder = "Assets/Prefabs/Player";
        private const string PlayerPrefabPath = PlayerPrefabFolder + "/Player.prefab";
        private const string GolemPrefabPath = "Assets/Prefabs/Enemies/GolemCharger.prefab";
        private const string CardImpactProfilePath =
            "Assets/Data/Damage/Damage_CardImpact_Overcharge.asset";
        private const string EnvironmentLayerName = "Environment";

        [MenuItem("TIC/Setup/Create Or Update Player Prefab And Golem Test Scene")]
        public static void CreateOrUpdatePlayerPrefabAndGolemTestScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            EnsureFolder(PlayerPrefabFolder);
            var playerPrefab = ExtractPlayerPrefab();
            if (playerPrefab == null)
            {
                return;
            }

            var golemPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(GolemPrefabPath);
            var cardImpactProfile = AssetDatabase.LoadAssetAtPath<DamageProfileSO>(
                CardImpactProfilePath);
            if (golemPrefab == null || cardImpactProfile == null)
            {
                Debug.LogError(
                    "The Golem test scene requires the Golem Charger prefab and Card Impact profile. "
                    + "Run the Golem Charger setup first.");
                return;
            }

            var environmentLayer = LayerMask.NameToLayer(EnvironmentLayerName);
            if (environmentLayer < 0)
            {
                Debug.LogError($"Missing project layer '{EnvironmentLayerName}'.");
                return;
            }

            CreateOrUpdateTestScene(playerPrefab, golemPrefab, cardImpactProfile, environmentLayer);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Created or updated Player.prefab and Test_GolemCharger.unity.");
        }

        private static GameObject ExtractPlayerPrefab()
        {
            var sampleScene = EditorSceneManager.OpenScene(SampleScenePath, OpenSceneMode.Single);
            var player = sampleScene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<PlayerController>(true))
                .FirstOrDefault();
            if (player == null)
            {
                Debug.LogError($"No {nameof(PlayerController)} exists in {SampleScenePath}.");
                return null;
            }

            if (PrefabUtility.IsPartOfPrefabInstance(player.gameObject))
            {
                PrefabUtility.UnpackPrefabInstance(
                    player.gameObject,
                    PrefabUnpackMode.OutermostRoot,
                    InteractionMode.AutomatedAction);
            }

            var serializedPlayer = new SerializedObject(player);
            var cardSelectionHud = serializedPlayer.FindProperty("cardSelectionHud").objectReferenceValue;
            serializedPlayer.FindProperty("cardSelectionHud").objectReferenceValue = null;
            serializedPlayer.ApplyModifiedPropertiesWithoutUndo();

            var camera = player.GetComponentsInChildren<Camera>(true).FirstOrDefault();
            var cameraTransform = camera != null ? camera.transform : null;
            if (cameraTransform != null)
            {
                cameraTransform.SetParent(parent: null, worldPositionStays: true);
            }

            var prefab = PrefabUtility.SaveAsPrefabAssetAndConnect(
                player.gameObject,
                PlayerPrefabPath,
                InteractionMode.UserAction);

            serializedPlayer = new SerializedObject(player);
            serializedPlayer.FindProperty("cardSelectionHud").objectReferenceValue = cardSelectionHud;
            serializedPlayer.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(sampleScene);
            EditorSceneManager.SaveScene(sampleScene);
            return prefab;
        }

        private static void CreateOrUpdateTestScene(
            GameObject playerPrefab,
            GameObject golemPrefab,
            DamageProfileSO cardImpactProfile,
            int environmentLayer)
        {
            var testScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            testScene.name = "Test_GolemCharger";

            CreateCamera(testScene);
            CreateArena(testScene, environmentLayer);

            var player = PrefabUtility.InstantiatePrefab(playerPrefab, testScene) as GameObject;
            player.name = "Player";
            player.transform.position = new Vector3(x: -2f, y: 0.5f, z: 0f);
            var combatEffects = player.GetComponent<PlayerCombatEffects>();
            combatEffects?.ConfigureSupplementalDamageProfile(cardImpactProfile);

            var golem = PrefabUtility.InstantiatePrefab(golemPrefab, testScene) as GameObject;
            golem.name = "GolemCharger";
            golem.transform.position = new Vector3(x: 3f, y: -0.5f, z: 0f);
            var brain = golem.GetComponent<GolemChargerBrain>();
            if (brain != null)
            {
                var serializedBrain = new SerializedObject(brain);
                serializedBrain.FindProperty("target").objectReferenceValue = player.transform;
                serializedBrain.ApplyModifiedPropertiesWithoutUndo();
            }

            EditorSceneManager.SaveScene(testScene, TestScenePath);
        }

        private static void CreateCamera(Scene scene)
        {
            var cameraObject = new GameObject("Main Camera");
            SceneManager.MoveGameObjectToScene(cameraObject, scene);
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(x: 0f, y: 1.5f, z: -10f);
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            camera.backgroundColor = new Color(0.19f, 0.3f, 0.47f, 1f);
            cameraObject.AddComponent<AudioListener>();
        }

        private static void CreateArena(Scene scene, int environmentLayer)
        {
            CreateArenaBlock(
                scene,
                "Floor",
                new Vector3(x: 0f, y: -1f, z: 1f),
                new Vector3(x: 16f, y: 1f, z: 1f),
                environmentLayer);
            CreateArenaBlock(
                scene,
                "Left Wall",
                new Vector3(x: -8f, y: 2f, z: 1f),
                new Vector3(x: 0.5f, y: 6f, z: 1f),
                environmentLayer);
            CreateArenaBlock(
                scene,
                "Right Wall",
                new Vector3(x: 8f, y: 2f, z: 1f),
                new Vector3(x: 0.5f, y: 6f, z: 1f),
                environmentLayer);
        }

        private static void CreateArenaBlock(
            Scene scene,
            string objectName,
            Vector3 position,
            Vector3 scale,
            int layer)
        {
            var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = objectName;
            block.layer = layer;
            block.transform.position = position;
            block.transform.localScale = scale;
            SceneManager.MoveGameObjectToScene(block, scene);
            Object.DestroyImmediate(block.GetComponent<Collider>());
            block.AddComponent<BoxCollider2D>();
        }

        private static void EnsureFolder(string path)
        {
            var segments = path.Split('/');
            var current = segments[0];
            for (var index = 1; index < segments.Length; index++)
            {
                var next = $"{current}/{segments[index]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, segments[index]);
                }

                current = next;
            }
        }
    }
}
