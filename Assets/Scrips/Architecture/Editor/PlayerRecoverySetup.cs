using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TicGame.Architecture.EditorTools
{
    public static class PlayerRecoverySetup
    {
        private const string Folder = "Assets/Data/Cards/Recovery";
        private const string PrefabPath = "Assets/Prefabs/Player/Player.prefab";

        [MenuItem("TIC/Setup/Create Or Update Player Recovery")]
        public static void CreateOrUpdate()
        {
            if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/Data/Cards", "Recovery");
            var tuning = Asset<PlayerRecoveryTuningSO>("PlayerRecoveryTuning");
            var energy = AssetDatabase.LoadAssetAtPath<ResourceDefinitionSO>("Assets/Data/Resources/Resource_Energy.asset");
            var prefab = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                var player = prefab.GetComponent<PlayerController>();
                var serializedPlayer = new SerializedObject(player);
                var profile = serializedPlayer.FindProperty("cardInventoryProfile").objectReferenceValue as PlayerCardInventoryProfileSO;
                var catalog = serializedPlayer.FindProperty("cardCatalog").objectReferenceValue as CardCatalogSO;
                if (profile == null || catalog == null || energy == null) throw new InvalidOperationException("Player recovery requires the existing profile, catalog and Energy resource.");
                var cards = new[] {
                    Card("blood-charge", "Blood Charge", "Trade HP for energy. Always keep at least 1 HP.", CardOperationKind.SacrificeHealthForEnergy),
                    Card("reconstitute", "Reconstitute", "Spend whole energy chunks to restore missing HP. Keep the remainder.", CardOperationKind.ConvertEnergyToHealth),
                    Card("mend", "Mend", "Restore up to 2 HP. Consume one copy on success.", CardOperationKind.Heal)
                };
                foreach (var card in cards)
                {
                    profile.TryAddOwnedCard(card, card.ConsumptionPolicy == CardConsumptionPolicy.ConsumeOnSuccess ? 2 : 1);
                    if (!profile.GetEquippedCards(PlayerCardTimeState.Neutral).Contains(card) && !profile.TryEquip(card))
                        throw new InvalidOperationException($"Neutral loadout has no room for {card.DisplayName}.");
                }
                catalog.Configure(catalog.Cards.Concat(cards).Distinct().ToArray());
                var authoredBasis = profile.GetEquippedCards(PlayerCardTimeState.Neutral)
                    .Where(card => card != null)
                    .Select(card => card.FixedCosts.Where(cost => cost.Resource == energy).Sum(cost => cost.Amount))
                    .DefaultIfEmpty(0).Max();
                if (!float.IsFinite(authoredBasis) || authoredBasis <= 0)
                    Debug.LogWarning("Recovery uses defensive cost 20: the Neutral loadout needs a valid positive authored Energy cost.", profile);
                var basis = PlayerRecoveryEconomy.ResolveNeutralCost(profile.GetEquippedCards(PlayerCardTimeState.Neutral), energy, tuning.FallbackNeutralEnergyCost);
                var capacity = prefab.GetComponent<PlayerResourceWallet>().GetMaximum(energy);
                if (basis > capacity || basis * tuning.HealingChunkMultiplier > capacity)
                    Debug.LogWarning("The Energy wallet cannot afford the Neutral reference card or a full healing chunk. Review recovery tuning and capacity.", profile);
                var inventory = prefab.GetComponent<PlayerCardInventoryRuntime>() ?? prefab.AddComponent<PlayerCardInventoryRuntime>();
                Assign(inventory, "profile", profile);
                var recovery = prefab.GetComponent<PlayerRecoveryController>() ?? prefab.AddComponent<PlayerRecoveryController>();
                recovery.Configure(tuning, energy, prefab.GetComponent<PlayerResourceWallet>(), prefab.GetComponent<SimpleHealth>(), inventory);
                Assign(prefab.GetComponent<PlayerCombatEffects>(), "recoveryTuning", tuning);
                EditorUtility.SetDirty(profile); EditorUtility.SetDirty(catalog);
                PrefabUtility.SaveAsPrefabAsset(prefab, PrefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(prefab); }

            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Gameplay.unity", OpenSceneMode.Single);
            foreach (var root in scene.GetRootGameObjects())
                foreach (var hud in root.GetComponentsInChildren<PlayerHudUI>(true))
                    foreach (var name in new[] { "Top Left", "Card Time" })
                    {
                        var group = hud.transform.Find(name);
                        if (group != null) group.localScale = Vector3.one * 2;
                    }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Recovery cards, player tuning and 2x corner HUD saved.");
        }

        private static CardDefinitionSO Card(string id, string displayName, string description, CardOperationKind kind)
        {
            var effect = Asset<CardEffectDefinitionSO>("Effect_" + id);
            effect.Configure(null, null, new[] { new CardOperationDefinition(kind) }, null,
                new[] { new CardLifetimeDefinition(CardLifetimeKind.Immediate) }, new CardStackingDefinition(CardStackingKind.RejectIfActive));
            var card = Asset<CardDefinitionSO>("Card_" + id);
            card.Configure(id, displayName, description, PlayerCardTimeState.Neutral, null, effect);
            card.ConfigureConsumption(kind == CardOperationKind.Heal ? CardConsumptionPolicy.ConsumeOnSuccess : CardConsumptionPolicy.Reusable);
            EditorUtility.SetDirty(effect); EditorUtility.SetDirty(card);
            return card;
        }
        private static T Asset<T>(string name) where T : ScriptableObject
        {
            var path = Folder + "/" + name + ".asset";
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }
        private static void Assign(UnityEngine.Object target, string property, UnityEngine.Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(property).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
