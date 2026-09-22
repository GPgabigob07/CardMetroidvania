using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TicGame.Architecture.EditorTools
{
    public static class BlueAreaTutorialSetup
    {
        private const string SourcePath = "Assets/Scenes/Blockout_BlueStart_Movement.unity";
        private const string ScenePath = "Assets/Scenes/BlueArea_Tutorial.unity";
        private static Material material;

        [MenuItem("TIC/Setup/Create Or Open Blue Area Tutorial")]
        public static void CreateOrOpen()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
            {
                var existingScene = EditorSceneManager.OpenScene(ScenePath);
                RepairCollisionLayers(existingScene);
                EditorSceneManager.SaveScene(existingScene);
                return;
            }

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/Gameplay.unity") != null)
            {
                Debug.LogError(
                    "Gameplay already owns the player and HUD. The Blue area scene is not recreated; use the Gameplay migration command or open the existing area.");
                return;
            }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/GolemCharger.prefab");
            var energy = AssetDatabase.LoadAssetAtPath<ResourceDefinitionSO>("Assets/Data/Resources/Resource_Energy.asset");
            material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Scenes/BlueStartBlockout.mat");
            var environmentLayer = LayerMask.NameToLayer("Environment");
            var enemyLayer = LayerMask.NameToLayer("Enemy");
            if (prefab == null || energy == null || material == null || environmentLayer < 0 || enemyLayer < 0
                || AssetDatabase.LoadAssetAtPath<SceneAsset>(SourcePath) == null)
            {
                Debug.LogError("Tutorial requires the saved movement scene, blockout material, Charger prefab, Energy resource and Environment/Enemy layers.");
                return;
            }

            var scene = EditorSceneManager.OpenScene(SourcePath);
            var player = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<PlayerController>()).SingleOrDefault();
            if (player == null)
            {
                Debug.LogError("Expected exactly one PlayerController in the saved movement room.");
                return;
            }
            // Save As retains the user's room and all its references in an independent area scene.
            if (!EditorSceneManager.SaveScene(scene, ScenePath, saveAsCopy: true)) return;
            scene = EditorSceneManager.OpenScene(ScenePath);
            player = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<PlayerController>()).Single();

            var existingRoots = scene.GetRootGameObjects();
            var startRoom = new GameObject("Room 01 - Movement").transform;
            foreach (var root in existingRoots)
                if (root != player.gameObject) root.transform.SetParent(startRoom, true);

            var playerData = new SerializedObject(player);
            playerData.FindProperty("cardTimeUnlocked").boolValue = false;
            playerData.FindProperty("cardCatalog").objectReferenceValue = AssetDatabase.LoadAssetAtPath<CardCatalogSO>(
                "Assets/Data/Cards/Inventory/TestCardCatalog.asset");
            playerData.FindProperty("cardInventoryProfile").objectReferenceValue = AssetDatabase.LoadAssetAtPath<PlayerCardInventoryProfileSO>(
                "Assets/Data/Cards/Inventory/TestCardInventory.asset");
            playerData.ApplyModifiedPropertiesWithoutUndo();

            var respawn = new GameObject("Tutorial start respawn").transform;
            respawn.SetParent(startRoom, true);
            respawn.position = player.transform.position;
            player.GetComponent<PlayerDeathRespawn>()?.Configure(player.GetComponent<SimpleHealth>(),
                player, player.GetComponent<PlayerMotor2D>(), respawn);
            var hitDetector = new SerializedObject(player.GetComponent<PlayerAttackHitDetector2D>());
            var targets = hitDetector.FindProperty("targetLayers");
            targets.intValue |= (1 << enemyLayer) | (1 << environmentLayer);
            hitDetector.ApplyModifiedPropertiesWithoutUndo();

            var corridor = new GameObject("Room 02 - Card Time corridor").transform;
            Block(corridor, "Floor", -43, 12.5f, 86, 1, environmentLayer);
            Block(corridor, "Ceiling", -43, 27.5f, 86, 1, environmentLayer);
            Block(corridor, "Next area cap", -86.5f, 20, 1, 14, environmentLayer);

            var first = Group(corridor, "Stage 1 - Basic combat");
            Charger(first, prefab, player, "First Charger", -9, 14, 2);
            Label(first, "Read the threat. Find an opening.", -6, 17);

            var second = Group(corridor, "Stage 2 - Card Time");
            var zone = Trigger(second, "Card Time discovery", -25, 20, 2, 14);
            zone.AddComponent<CardTimeTutorialZone>();
            Block(second, "Approach step", -28, 13.5f, 3, 1, environmentLayer);
            Block(second, "Charger platform", -36, 14.5f, 12, 1, environmentLayer);
            Charger(second, prefab, player, "Platform Charger", -36, 16, 2);
            Refill(second, energy, -45);
            Gate(second, "First card seal", -49, environmentLayer);

            var third = Group(corridor, "Stage 3 - Combined encounter");
            // Adjacent stationary patrol centers give overlapping engagement circles.
            Charger(third, prefab, player, "Combined Charger A", -62, 14, 0);
            Charger(third, prefab, player, "Combined Charger B", -65, 14, 0);
            Block(third, "Approach perch", -57, 14.5f, 4, 1, environmentLayer);
            Block(third, "Recovery perch", -70, 15.5f, 4, 1, environmentLayer);
            Refill(third, energy, -75);
            Gate(third, "Second card seal", -79, environmentLayer);
            Label(third, "Next area\nEnd of this blockout", -82, 17);

            EditorSceneManager.SaveScene(scene);
            PlayerHudSetup.CreateOrUpdateHud(scene);
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(scene);
            Selection.activeGameObject = corridor.gameObject;
            Debug.Log("BlueArea_Tutorial saved. Card Time unlocks in stage 2. Cyan seals require enhanced melee; wells provide repeatable Energy. Source movement scene is preserved.");
        }

        private static Transform Group(Transform parent, string name)
        {
            var root = new GameObject(name).transform;
            root.SetParent(parent, false);
            return root;
        }

        private static GameObject Block(Transform parent, string name, float x, float y, float width, float height, int layer)
        {
            var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = name;
            block.layer = layer;
            block.transform.SetParent(parent, false);
            block.transform.position = new Vector3(x, y, 1);
            block.transform.localScale = new Vector3(width, height, 1);
            Object.DestroyImmediate(block.GetComponent<Collider>());
            block.AddComponent<BoxCollider2D>();
            block.GetComponent<Renderer>().sharedMaterial = material;
            return block;
        }

        private static GameObject Trigger(Transform parent, string name, float x, float y, float width, float height)
        {
            var root = Group(parent, name);
            root.gameObject.layer = LayerMask.NameToLayer("Environment");
            root.position = new Vector3(x, y, 0);
            var collider = root.gameObject.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(width, height);
            collider.isTrigger = true;
            return root.gameObject;
        }

        private static void RepairCollisionLayers(UnityEngine.SceneManagement.Scene scene)
        {
            var environmentLayer = LayerMask.NameToLayer("Environment");
            if (environmentLayer < 0)
                throw new System.InvalidOperationException("The tutorial requires the Environment layer.");

            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var component in root.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (component is CardMeleeGate or CardTimeTutorialZone or TutorialEnergyRefill)
                    {
                        component.gameObject.layer = environmentLayer;
                        EditorUtility.SetDirty(component.gameObject);
                    }
                }
                foreach (var detector in root.GetComponentsInChildren<PlayerAttackHitDetector2D>(true))
                {
                    var data = new SerializedObject(detector);
                    data.FindProperty("targetLayers").intValue |= 1 << environmentLayer;
                    data.ApplyModifiedPropertiesWithoutUndo();
                    PrefabUtility.RecordPrefabInstancePropertyModifications(detector);
                }
            }
            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("Repaired tutorial door/trigger collision layers and melee target masks; layout preserved.");
        }

        private static void Charger(Transform parent, GameObject prefab, PlayerController player,
            string name, float x, float y, float patrolDistance)
        {
            var enemy = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            enemy.name = name;
            enemy.transform.position = new Vector3(x, y, 0);
            var data = new SerializedObject(enemy.GetComponent<GolemChargerBrain>());
            data.FindProperty("target").objectReferenceValue = player.transform;
            data.FindProperty("patrolHalfDistance").floatValue = patrolDistance;
            if (patrolDistance == 0) data.FindProperty("patrolSpeed").floatValue = 0;
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Refill(Transform parent, ResourceDefinitionSO energy, float x)
        {
            Trigger(parent, "Energy well - repeatable", x, 14.5f, 3, 3)
                .AddComponent<TutorialEnergyRefill>().Configure(energy);
            Label(parent, "ENERGY WELL\nRest here to recharge", x, 17);
        }

        private static void Gate(Transform parent, string name, float x, int layer)
        {
            var door = Block(parent, name, x, 20, 1, 14, layer);
            door.AddComponent<CardMeleeGate>().Configure(door.GetComponent<Collider2D>(), door.GetComponent<Renderer>());
            Label(parent, "CARD SEAL\nResponds to enhanced melee", x + 2, 19);
        }

        private static void Label(Transform parent, string text, float x, float y)
        {
            var root = Group(parent, text.Replace('\n', ' '));
            root.position = new Vector3(x, y, -1);
            var label = root.gameObject.AddComponent<TextMesh>();
            label.text = text;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 48;
            label.characterSize = 0.12f;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.color = new Color(0.65f, 0.95f, 1);
            label.GetComponent<Renderer>().sharedMaterial = label.font.material;
        }
    }
}
