using System.Linq;
using TicGame.Architecture;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TicGame.Architecture.EditorTools
{
    public static class GolemChargerSceneSetup
    {
        private const string ScenePath = "Assets/Scenes/SampleScene.unity";
        private const string PrefabPath = "Assets/Prefabs/Enemies/GolemCharger.prefab";
        private const string CardImpactProfilePath = "Assets/Data/Damage/Damage_CardImpact_Overcharge.asset";
        private const string InstanceName = "GolemCharger Training";

        [MenuItem("TIC/Setup/Place Golem Charger In Sample Scene")]
        public static void PlaceGolemChargerInSampleScene()
        {
            var scene = OpenTargetScene();
            if (!scene.IsValid())
            {
                return;
            }

            var player = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<PlayerController>(includeInactive: true))
                .FirstOrDefault();
            if (player == null)
            {
                Debug.LogError($"No {nameof(PlayerController)} exists in {ScenePath}.");
                return;
            }

            var combatEffects = player.GetComponent<PlayerCombatEffects>();
            var cardImpactProfile = AssetDatabase.LoadAssetAtPath<DamageProfileSO>(CardImpactProfilePath);
            if (combatEffects == null || cardImpactProfile == null)
            {
                Debug.LogError("Golem Charger scene setup requires PlayerCombatEffects and the Card Impact Overcharge profile.");
                return;
            }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
            {
                Debug.LogError($"No Golem Charger prefab exists at {PrefabPath}.");
                return;
            }

            var golem = scene.GetRootGameObjects()
                .FirstOrDefault(root => root.name == InstanceName);
            if (golem == null)
            {
                golem = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
                golem.name = InstanceName;
            }

            golem.transform.position = player.transform.position + new Vector3(x: 3f, y: -1f, z: 0f);
            var brain = golem.GetComponent<GolemChargerBrain>();
            if (brain != null)
            {
                var serializedBrain = new SerializedObject(brain);
                serializedBrain.FindProperty("target").objectReferenceValue = player.transform;
                serializedBrain.ApplyModifiedPropertiesWithoutUndo();
            }

            combatEffects.ConfigureSupplementalDamageProfile(cardImpactProfile);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("Placed or updated the Golem Charger training instance in SampleScene.");
        }

        private static Scene OpenTargetScene()
        {
            var activeScene = SceneManager.GetActiveScene();
            if (activeScene.path == ScenePath)
            {
                return activeScene;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return default;
            }

            return EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }
    }
}
