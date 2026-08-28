using System;
using System.Collections.Generic;
using UnityEngine;

namespace TicGame.Architecture
{
    [Serializable]
    public sealed class CardTimeControlSchemeCategoryLayout
    {
        [SerializeField] private PlayerCardTimeState category;

        [Min(0)]
        [SerializeField] private int slotCount;

        [SerializeField] private List<CardTimeControlSchemeSlotBinding> slots = new();

        public PlayerCardTimeState Category => category;
        public int SlotCount => Mathf.Max(0, slotCount);
        public IReadOnlyList<CardTimeControlSchemeSlotBinding> Slots => slots;

        public void Configure(
            PlayerCardTimeState cardTimeCategory,
            int visibleSlotCount,
            IEnumerable<CardTimeControlSchemeSlotBinding> slotBindings)
        {
            category = cardTimeCategory;
            slotCount = Mathf.Max(0, visibleSlotCount);
            slots = slotBindings != null
                ? new List<CardTimeControlSchemeSlotBinding>(slotBindings)
                : new List<CardTimeControlSchemeSlotBinding>();
        }

        public CardTimeControlSchemeSlotBinding GetSlot(int slotIndex)
        {
            foreach (var slot in slots)
            {
                if (slot != null && slot.SlotIndex == slotIndex)
                {
                    return slot;
                }
            }

            return null;
        }
    }
}
