using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TicGame.Architecture.EditorTools
{
    public static class BlueStartRoomSetup
    {
        private const string ScenePath = "Assets/Scenes/Blockout_BlueStart_Movement.unity";

        [MenuItem("TIC/Setup/Create Or Open Blue Starting Room Blockout")]
        public static void CreateOrOpen()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Exit Play Mode before creating the blockout.");
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
            {
                EditorSceneManager.OpenScene(ScenePath);
                return;
            }

            var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefabs/Player/Player.prefab");
            var layer = LayerMask.NameToLayer("Environment");
            if (playerPrefab == null || layer < 0)
            {
                Debug.LogError("Blockout requires the Player prefab and the Environment layer.");
                return;
            }

            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
            {
                Debug.LogError("Blockout requires the URP Unlit shader for visible geometry.");
                return;
            }
            const string materialPath = "Assets/Scenes/BlueStartBlockout.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                material = new Material(shader);
                material.SetColor("_BaseColor", new Color(0.32f, 0.48f, 0.72f));
                AssetDatabase.CreateAsset(material, materialPath);
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var geometry = new GameObject("Blue Start - 20 x 20 movement room").transform;
            Block(geometry, "Catch floor - safe retry", 10, -0.5f, 22, 1, layer);
            Block(geometry, "West boundary cap", -0.5f, 10, 1, 20, layer);
            Block(geometry, "East boundary cap", 20.5f, 10, 1, 20, layer);
            Block(geometry, "Ceiling", 10, 20.5f, 22, 1, layer);

            // Platform coordinates specify their top surface, making rise tuning explicit.
            Platform(geometry, "Safe spawn shelf - walk left", 16, 2, 6, layer);
            Platform(geometry, "Main route - jump left", 10, 4, 4, layer);
            Platform(geometry, "Main route - corridor approach", 4, 6, 6, layer);
            Platform(geometry, "Optional practice - turn right", 10, 8, 4, layer);
            Platform(geometry, "Optional practice - upper landing", 16, 10, 4, layer);
            Platform(geometry, "Optional practice - overlook", 10, 12, 4, layer);

            foreach (var renderer in geometry.GetComponentsInChildren<MeshRenderer>())
            {
                renderer.sharedMaterial = material;
            }

            var markers = new GameObject("Connection reservations - not active transitions").transform;
            var spawn = Marker(markers, "Start and respawn", new Vector3(17, 4, 0));
            Marker(markers, "West - combat tutorial corridor (capped)", new Vector3(1, 7, 0));
            Marker(markers, "East - future blue connection (capped)", new Vector3(19, 3, 0));
            Marker(markers, "Nonlinear arrival reservation - confirm destination later", new Vector3(16, 3, 0));

            var player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab, scene);
            player.transform.position = spawn.position;
            player.GetComponent<PlayerDeathRespawn>()?.Configure(
                player.GetComponent<SimpleHealth>(), player.GetComponent<PlayerController>(),
                player.GetComponent<PlayerMotor2D>(), spawn);

            var cameraObject = new GameObject("Main Camera - temporary player follow");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(player.transform, false);
            cameraObject.transform.localPosition = new Vector3(0, 2, -10);
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.06f, 0.09f, 0.16f);
            cameraObject.AddComponent<AudioListener>();

            EditorSceneManager.SaveScene(scene, ScenePath);
            Selection.activeGameObject = geometry.gameObject;
            SceneView.lastActiveSceneView?.Frame(new Bounds(new Vector3(10, 10), new Vector3(22, 22, 1)), false);
            Debug.Log("Movement-only starting room saved. Test the leftward route and optional upper loop without dash. The west connection is capped until the combat corridor exists. Existing rooms are opened without regeneration.");
        }

        private static void Platform(Transform parent, string name, float x, float top, float width, int layer)
        {
            Block(parent, name, x, top - 0.5f, width, 1, layer);
        }

        private static void Block(Transform parent, string name, float x, float y, float width, float height, int layer)
        {
            var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = name;
            block.layer = layer;
            block.transform.SetParent(parent, false);
            block.transform.localPosition = new Vector3(x, y, 1);
            block.transform.localScale = new Vector3(width, height, 1);
            Object.DestroyImmediate(block.GetComponent<Collider>());
            block.AddComponent<BoxCollider2D>();
        }

        private static Transform Marker(Transform parent, string name, Vector3 position)
        {
            var marker = new GameObject(name).transform;
            marker.SetParent(parent, false);
            marker.position = position;
            return marker;
        }
    }
}
