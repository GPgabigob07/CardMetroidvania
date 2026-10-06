using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TicGame.Architecture.EditorTools
{
    public static class RepelCardSetup
    {
        private const string CardPath = "Assets/Data/Cards/Definitions/Card_Chain_Repel.asset";
        private const string EffectPath = "Assets/Data/Cards/Effects/Effect_Repel.asset";
        private const string ProfilePath = "Assets/Data/Cards/Inventory/RepelTestCardInventory.asset";
        private const string ScenePath = "Assets/Scenes/Test_BatRepel.unity";
        private const string PlayerPrefabPath = "Assets/Prefabs/Player/Player.prefab";

        [MenuItem("TIC/Setup/Create Or Update Repel Card Assets")]
        public static void CreateOrUpdateRepelAssets()
        {
            var energy = Required<ResourceDefinitionSO>("Assets/Data/Resources/Resource_Energy.asset");
            var effect = LoadOrCreate<CardEffectDefinitionSO>(EffectPath);
            if (effect.CommitOperations.Count == 0)
            {
                effect.Configure(null, null, new[] { new CardOperationDefinition(CardOperationKind.ArmProjectileRepel, amount: 3f) },
                    null, new[] { new CardLifetimeDefinition(CardLifetimeKind.Immediate) }, new CardStackingDefinition(CardStackingKind.RejectIfActive));
                EditorUtility.SetDirty(effect);
            }
            var card = LoadOrCreate<CardDefinitionSO>(CardPath);
            if (card.Effect == null)
            {
                card.Configure("repel", "Repel", "For 3 seconds, melee strikes repel enemy projectiles.", PlayerCardTimeState.Chain,
                    new[] { new ResourceAmount(energy, 15f) }, effect);
                card.ConfigureConsumption(CardConsumptionPolicy.Reusable);
                EditorUtility.SetDirty(card);
            }

            var catalog = Required<CardCatalogSO>("Assets/Data/Cards/Inventory/TestCardCatalog.asset");
            if (!catalog.Cards.Contains(card))
            {
                if (catalog.Cards.Any(existing => existing != null && existing.Id == card.Id))
                    throw new InvalidOperationException("Another catalog card already uses the Repel id.");
                catalog.Configure(catalog.Cards.Append(card).ToArray()); EditorUtility.SetDirty(catalog);
            }

            if (AssetDatabase.LoadAssetAtPath<PlayerCardInventoryProfileSO>(ProfilePath) == null
                && !AssetDatabase.CopyAsset("Assets/Data/Cards/Inventory/TestCardInventory.asset", ProfilePath))
                throw new InvalidOperationException("Could not copy the prototype inventory for Repel testing.");
            var profile = Required<PlayerCardInventoryProfileSO>(ProfilePath);
            profile.TryAddOwnedCard(card);
            var displaced = profile.GetEquippedCards(PlayerCardTimeState.Chain)
                .FirstOrDefault(existing => existing != null && existing.Id == "card.chain.growing-reach");
            if (displaced != null) profile.TryUnequip(displaced);
            if (!profile.GetEquippedCards(PlayerCardTimeState.Chain).Contains(card) && !profile.TryEquip(card))
                throw new InvalidOperationException("The Repel playtest inventory has no available Chain slot.");
            EditorUtility.SetDirty(profile);
            EnsurePlayerCapability();
            AssetDatabase.SaveAssets();
        }

        [MenuItem("TIC/Setup/Create Or Update Repel Playtest")]
        public static void CreateOrUpdateRepelPlaytest()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            CreateOrUpdateRepelAssets();
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null
                && !AssetDatabase.CopyAsset("Assets/Scenes/Test_BatMachine.unity", ScenePath))
                throw new InvalidOperationException("Could not copy the existing Bat Machine test arena.");
            var scene = SceneManager.GetSceneByPath(ScenePath);
            if (!scene.isLoaded) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var player = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<PlayerController>()).Single();
            var profile = Required<PlayerCardInventoryProfileSO>(ProfilePath);
            Assign(player, "cardInventoryProfile", profile);
            Assign(player.GetComponent<PlayerCardInventoryRuntime>(), "profile", profile);
            if (player.GetComponent<PlayerRepelRuntime>() == null) player.gameObject.AddComponent<PlayerRepelRuntime>();
            if (!scene.GetRootGameObjects().Any(root => root.name == "[Player HUD]")) PlayerHudSetup.CreateOrUpdateHud(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("Repel playtest ready: Test_BatRepel; Chain, 15 Energy, 3 seconds, melee interception.");
        }

        private static void EnsurePlayerCapability()
        {
            var prefab = Required<GameObject>(PlayerPrefabPath);
            if (prefab.GetComponent<PlayerRepelRuntime>() != null) return;
            var root = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            try { root.AddComponent<PlayerRepelRuntime>(); PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath); }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        private static T Required<T>(string path) where T : UnityEngine.Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            return asset != null ? asset : throw new InvalidOperationException($"Repel setup requires {path}.");
        }

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static void Assign(UnityEngine.Object target, string propertyName, UnityEngine.Object value)
        {
            if (target == null) throw new InvalidOperationException($"Missing component for {propertyName}.");
            var serialized = new SerializedObject(target);
            serialized.FindProperty(propertyName).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
