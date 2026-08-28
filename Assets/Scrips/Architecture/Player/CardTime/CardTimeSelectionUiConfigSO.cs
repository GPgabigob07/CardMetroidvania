using System.Collections.Generic;
using UnityEngine;

namespace TicGame.Architecture
{
    [CreateAssetMenu(
        menuName = "TIC/Cards/Card Time Selection UI Config",
        fileName = "CardTimeSelectionUiConfig_")]
    public sealed class CardTimeSelectionUiConfigSO : ScriptableObject
    {
        [SerializeField] private List<CardTimeSelectionCategoryUiConfig> categories = new();

        public IReadOnlyList<CardTimeSelectionCategoryUiConfig> Categories => categories;

        public int GetSlotCount(PlayerCardTimeState category)
        {
            return GetCategory(category)?.SlotCount
                   ?? PlayerCardInventoryProfileSO.GetDefaultCapacity(category);
        }

        public IReadOnlyList<CardTimeSelectionSlotCommandBinding> GetCommandBindings(
            PlayerCardTimeState category)
        {
            return GetCategory(category)?.SlotCommands
                   ?? System.Array.Empty<CardTimeSelectionSlotCommandBinding>();
        }

        public string GetDisplayLabel(PlayerCardTimeState category, int slotIndex)
        {
            return GetCategory(category)?.GetDisplayLabel(slotIndex) ?? string.Empty;
        }

        public CardTimeSelectionCategoryUiConfig GetCategory(PlayerCardTimeState category)
        {
            foreach (var config in categories)
            {
                if (config != null && config.Category == category)
                {
                    return config;
                }
            }

            return null;
        }

        public void ConfigurePrototypeDefaults()
        {
            categories = new List<CardTimeSelectionCategoryUiConfig>
            {
                CreateCategory(PlayerCardTimeState.Neutral, 8),
                CreateCategory(PlayerCardTimeState.Chain, 6),
                CreateCategory(PlayerCardTimeState.Finisher, 4)
            };
        }

        private static CardTimeSelectionCategoryUiConfig CreateCategory(
            PlayerCardTimeState category,
            int slotCount)
        {
            var actionNames = new[]
            {
                "Attack",
                "Jump",
                "Crouch",
                "Previous",
                "Next",
                "Sprint",
                "CardTimeLeft",
                "CardTimeRight"
            };
            var labels = new[]
            {
                "ATK",
                "JMP",
                "CR",
                "PREV",
                "NEXT",
                "SPR",
                "L",
                "R"
            };
            var commands = new List<CardTimeSelectionSlotCommandBinding>();
            for (var index = 0; index < slotCount; index++)
            {
                var binding = new CardTimeSelectionSlotCommandBinding();
                binding.Configure(
                    index,
                    actionReference: null,
                    fallbackActionName: actionNames[index],
                    label: labels[index]);
                commands.Add(binding);
            }

            var config = new CardTimeSelectionCategoryUiConfig();
            config.Configure(category, slotCount, commands);
            return config;
        }
    }
}
