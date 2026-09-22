using TicGame.Architecture;
using UnityEditor;
using UnityEngine;

namespace TicGame.Architecture.EditorTools
{
    public static class FiveCardPlayerPrefabSetup
    {
        private const string PlayerPrefabPath = "Assets/Prefabs/Player/Player.prefab";

        [MenuItem("TIC/Setup/Create Or Update Five Card Player Prefab")]
        public static void CreateOrUpdateFiveCardPlayerPrefab()
        {
            var root = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            try
            {
                var controller = root.GetComponent<PlayerController>();
                var runtime = root.GetComponent<PlayerCardRuntime>();
                var wallet = root.GetComponent<PlayerResourceWallet>();
                var effects = root.GetComponent<PlayerCombatEffects>();
                var sensors = root.GetComponent<PlayerSensors2D>();
                if (controller == null || runtime == null || wallet == null
                    || effects == null || sensors == null)
                {
                    Debug.LogError(
                        "The existing Player prefab is missing a required card or movement component.");
                    return;
                }

                var extraJump = GetOrAdd<PlayerExtraJumpRuntime>(root);
                var jumpBoost = GetOrAdd<PlayerGroundedJumpBoostRuntime>(root);
                var dashPermission = GetOrAdd<PlayerDashPermissionRuntime>(root);
                runtime.Configure(wallet, effects, extraJump, jumpBoost, dashPermission);

                var serializedRuntime = new SerializedObject(runtime);
                serializedRuntime.FindProperty("sensors").objectReferenceValue = sensors;
                serializedRuntime.ApplyModifiedPropertiesWithoutUndo();

                var serializedDash = new SerializedObject(dashPermission);
                serializedDash.FindProperty("combatEffects").objectReferenceValue = effects;
                serializedDash.ApplyModifiedPropertiesWithoutUndo();

                var respawn = root.GetComponent<PlayerDeathRespawn>();
                if (respawn != null)
                {
                    var serializedRespawn = new SerializedObject(respawn);
                    serializedRespawn.FindProperty("cardRuntime").objectReferenceValue = runtime;
                    serializedRespawn.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(respawn);
                }

                var serializedController = new SerializedObject(controller);
                serializedController.FindProperty("extraJumpRuntime").objectReferenceValue = extraJump;
                serializedController.FindProperty("groundedJumpBoostRuntime").objectReferenceValue =
                    jumpBoost;
                serializedController.FindProperty("dashPermission").objectReferenceValue =
                    dashPermission;
                serializedController.ApplyModifiedPropertiesWithoutUndo();

                EditorUtility.SetDirty(runtime);
                EditorUtility.SetDirty(dashPermission);
                EditorUtility.SetDirty(controller);
                PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
                AssetDatabase.SaveAssets();
                Debug.Log("Created or updated the five-card Player prefab.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        [MenuItem("TIC/Setup/Create Or Update Five Card Prototype")]
        public static void CreateOrUpdateFiveCardPrototype()
        {
            PrototypeCardAssetSetup.CreateOrUpdateAssets();
            CardInventoryProfileSetup.CreateOrUpdateTestInventory();
            CreateOrUpdateFiveCardPlayerPrefab();
            GolemChargerPrefabSetup.CreateOrUpdateGolemCharger();
            AssetDatabase.SaveAssets();
            Debug.Log("Created or updated the five-card prototype assets and prefabs.");
        }

        private static T GetOrAdd<T>(GameObject root) where T : Component
        {
            var component = root.GetComponent<T>();
            return component != null ? component : root.AddComponent<T>();
        }
    }
}
