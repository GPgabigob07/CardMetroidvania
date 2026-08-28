using System;
using System.Collections.Generic;
using UnityEngine;

namespace TicGame.Architecture
{
    [Serializable]
    public sealed class CardTimeSelectionCategoryUiConfig
    {
        [SerializeField] private PlayerCardTimeState category;

        [Min(0)]
        [SerializeField] private int slotCount;

        [SerializeField] private List<CardTimeSelectionSlotCommandBinding> slotCommands = new();

        public PlayerCardTimeState Category => category;
        public int SlotCount => Mathf.Max(0, slotCount);
        public IReadOnlyList<CardTimeSelectionSlotCommandBinding> SlotCommands => slotCommands;

        public void Configure(
            PlayerCardTimeState cardTimeCategory,
            int visibleSlotCount,
            IEnumerable<CardTimeSelectionSlotCommandBinding> commands)
        {
            category = cardTimeCategory;
            slotCount = Mathf.Max(0, visibleSlotCount);
            slotCommands = commands != null
                ? new List<CardTimeSelectionSlotCommandBinding>(commands)
                : new List<CardTimeSelectionSlotCommandBinding>();
        }

        public string GetDisplayLabel(int slotIndex)
        {
            foreach (var binding in slotCommands)
            {
                if (binding != null && binding.SlotIndex == slotIndex)
                {
                    return binding.DisplayLabel;
                }
            }

            return string.Empty;
        }
    }
}
