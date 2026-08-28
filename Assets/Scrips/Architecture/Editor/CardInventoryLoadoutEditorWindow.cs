using System.Collections.Generic;
using System.Linq;
using TicGame.Architecture;
using UnityEditor;
using UnityEngine;

namespace TicGame.Architecture.EditorTools
{
    public sealed class CardInventoryLoadoutEditorWindow : EditorWindow
    {
        private const string CardSearchFolder = "Assets/Data/Cards/Definitions";

        private readonly List<CardDefinitionSO> cards = new();
        private PlayerCardInventoryProfileSO profile;
        private PlayerCardTimeState categoryFilter;
        private InventoryFilter inventoryFilter;
        private Vector2 scroll;

        [MenuItem("TIC/Cards/Card Inventory Loadout Editor")]
        public static void Open()
        {
            GetWindow<CardInventoryLoadoutEditorWindow>("Card Loadout");
        }

        private void OnEnable()
        {
            RefreshCards();
        }

        private void OnGUI()
        {
            DrawToolbar();
            if (profile == null)
            {
                EditorGUILayout.HelpBox(
                    "Select or create a Player Card Inventory Profile.",
                    MessageType.Info);
                return;
            }

            profile.EnsureDefaultLoadouts();
            scroll = EditorGUILayout.BeginScrollView(scroll);
            DrawDeck(PlayerCardTimeState.Neutral);
            DrawDeck(PlayerCardTimeState.Chain);
            DrawDeck(PlayerCardTimeState.Finisher);
            EditorGUILayout.Space(8f);
            DrawCardList();
            DrawValidation();
            EditorGUILayout.EndScrollView();
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                profile = (PlayerCardInventoryProfileSO)EditorGUILayout.ObjectField(
                    profile,
                    typeof(PlayerCardInventoryProfileSO),
                    false,
                    GUILayout.MinWidth(220f));

                if (GUILayout.Button("Test Inventory", EditorStyles.toolbarButton, GUILayout.Width(104f)))
                {
                    profile = CardInventoryProfileSetup.CreateOrUpdateTestInventory();
                    RefreshCards();
                }

                if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(64f)))
                {
                    RefreshCards();
                }

                GUILayout.FlexibleSpace();

                categoryFilter = (PlayerCardTimeState)EditorGUILayout.EnumPopup(
                    categoryFilter,
                    EditorStyles.toolbarPopup,
                    GUILayout.Width(96f));
                inventoryFilter = (InventoryFilter)EditorGUILayout.EnumPopup(
                    inventoryFilter,
                    EditorStyles.toolbarPopup,
                    GUILayout.Width(92f));
            }
        }

        private void DrawDeck(PlayerCardTimeState category)
        {
            if (categoryFilter != PlayerCardTimeState.None && categoryFilter != category)
            {
                return;
            }

            var loadout = profile.GetLoadout(category);
            EditorGUILayout.LabelField(
                $"{category} ({loadout?.EquippedCards.Count ?? 0}/{PlayerCardInventoryProfileSO.GetDefaultCapacity(category)})",
                EditorStyles.boldLabel);

            if (loadout == null || loadout.EquippedCards.Count == 0)
            {
                EditorGUILayout.LabelField("Empty", EditorStyles.miniLabel);
                return;
            }

            foreach (var card in loadout.EquippedCards.ToArray())
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.ObjectField(card, typeof(CardDefinitionSO), false);
                    GUILayout.Label(card != null ? card.Id : "missing", EditorStyles.miniLabel);
                    if (GUILayout.Button("Unequip", GUILayout.Width(72f)))
                    {
                        MutateProfile(() => profile.TryUnequip(card));
                    }
                }
            }
        }

        private void DrawCardList()
        {
            EditorGUILayout.LabelField("Cards", EditorStyles.boldLabel);
            foreach (var card in cards.Where(PassesFilters))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.ObjectField(card, typeof(CardDefinitionSO), false);
                    GUILayout.Label(card.Category.ToString(), GUILayout.Width(64f));
                    GUILayout.Label(card.Id, EditorStyles.miniLabel, GUILayout.MinWidth(120f));

                    using (new EditorGUI.DisabledScope(profile.Owns(card)))
                    {
                        if (GUILayout.Button("Own", GUILayout.Width(48f)))
                        {
                            MutateProfile(() => profile.TryAddOwnedCard(card));
                        }
                    }

                    using (new EditorGUI.DisabledScope(!profile.Owns(card) || IsEquipped(card)))
                    {
                        if (GUILayout.Button("Equip", GUILayout.Width(56f)))
                        {
                            MutateProfile(() => profile.TryEquip(card));
                        }
                    }

                    using (new EditorGUI.DisabledScope(!IsEquipped(card)))
                    {
                        if (GUILayout.Button("Unequip", GUILayout.Width(72f)))
                        {
                            MutateProfile(() => profile.TryUnequip(card));
                        }
                    }
                }
            }
        }

        private void DrawValidation()
        {
            var errors = profile.GetValidationErrors();
            if (errors.Count == 0)
            {
                return;
            }

            EditorGUILayout.Space(8f);
            foreach (var error in errors)
            {
                EditorGUILayout.HelpBox(error, MessageType.Warning);
            }
        }

        private void RefreshCards()
        {
            cards.Clear();
            cards.AddRange(AssetDatabase
                .FindAssets("t:CardDefinitionSO", new[] { CardSearchFolder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<CardDefinitionSO>)
                .Where(card => card != null)
                .OrderBy(card => card.Category)
                .ThenBy(card => card.DisplayName));
        }

        private bool PassesFilters(CardDefinitionSO card)
        {
            if (card == null)
            {
                return false;
            }

            if (categoryFilter != PlayerCardTimeState.None && card.Category != categoryFilter)
            {
                return false;
            }

            return inventoryFilter switch
            {
                InventoryFilter.Owned => profile.Owns(card),
                InventoryFilter.Equipped => IsEquipped(card),
                _ => true
            };
        }

        private bool IsEquipped(CardDefinitionSO card)
        {
            return profile != null
                && card != null
                && profile.GetLoadout(card.Category)?.Contains(card) == true;
        }

        private void MutateProfile(System.Func<bool> mutation)
        {
            Undo.RecordObject(profile, "Edit Card Inventory Loadout");
            if (!mutation())
            {
                return;
            }

            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
            Repaint();
        }

        private enum InventoryFilter
        {
            All,
            Owned,
            Equipped
        }
    }
}
