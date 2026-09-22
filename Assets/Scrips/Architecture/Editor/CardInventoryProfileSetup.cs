using System.Collections.Generic;
using System.Linq;
using TicGame.Architecture;
using UnityEditor;
using UnityEngine;

namespace TicGame.Architecture.EditorTools
{
    public static class CardInventoryProfileSetup
    {
        private const string InventoryFolder = "Assets/Data/Cards/Inventory";
        private const string ProfilePath = InventoryFolder + "/TestCardInventory.asset";
        private const string CardSearchFolder = "Assets/Data/Cards/Definitions";

        [MenuItem("TIC/Setup/Create Or Update Test Card Inventory")]
        public static PlayerCardInventoryProfileSO CreateOrUpdateTestInventory()
        {
            EnsureFolder(InventoryFolder);
            var profile =
                AssetDatabase.LoadAssetAtPath<PlayerCardInventoryProfileSO>(ProfilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<PlayerCardInventoryProfileSO>();
                profile.name = "TestCardInventory";
                AssetDatabase.CreateAsset(profile, ProfilePath);
            }

            var cards = LoadCardDefinitions();
            profile.EnsureDefaultLoadouts();
            var existingFinishers = profile
                .GetEquippedCards(PlayerCardTimeState.Finisher)
                .Where(card => card != null)
                .ToArray();
            foreach (var card in cards)
            {
                profile.TryAddOwnedCard(card);
            }

            foreach (var loadout in profile.Loadouts)
            {
                foreach (var card in loadout.EquippedCards.ToArray())
                {
                    profile.TryUnequip(card);
                }
            }

            EquipById(profile, cards, "card.neutral.grounded-double-jump");
            EquipById(profile, cards, "card.neutral.dash-enabler");
            EquipById(profile, cards, "card.neutral.jump-boost");
            EquipById(profile, cards, "card.chain.poise-damage");
            EquipById(profile, cards, "card.chain.growing-reach");

            var finishers = existingFinishers.Length > 0
                ? existingFinishers
                : cards.Where(card =>
                        card.Id == "card.finisher.extra-jump"
                        || card.Id == "card.finisher.base-damage-overcharge")
                    .OrderBy(card => card.Id)
                    .ToArray();
            foreach (var finisher in finishers)
            {
                profile.TryEquip(finisher);
            }

            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
            Selection.activeObject = profile;

            var errors = profile.GetValidationErrors();
            if (errors.Count > 0)
            {
                Debug.LogWarning(
                    "Created test card inventory with validation warnings:\n"
                    + string.Join("\n", errors),
                    profile);
            }
            else
            {
                Debug.Log(
                    $"Created or updated test card inventory with {profile.OwnedCards.Count} owned cards.",
                    profile);
            }

            return profile;
        }

        private static void EquipById(
            PlayerCardInventoryProfileSO profile,
            IReadOnlyList<CardDefinitionSO> cards,
            string id)
        {
            var card = cards.FirstOrDefault(candidate => candidate.Id == id);
            if (card == null || !profile.TryEquip(card))
            {
                Debug.LogError($"Could not equip required prototype card '{id}'.");
            }
        }

        private static IReadOnlyList<CardDefinitionSO> LoadCardDefinitions()
        {
            return AssetDatabase.FindAssets("t:CardDefinitionSO", new[] { CardSearchFolder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<CardDefinitionSO>)
                .Where(card => card != null)
                .OrderBy(card => card.Category)
                .ThenBy(card => card.DisplayName)
                .ToList();
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
