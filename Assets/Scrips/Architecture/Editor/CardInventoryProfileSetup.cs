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
            foreach (var card in cards)
            {
                profile.TryAddOwnedCard(card);
                profile.TryEquip(card);
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
