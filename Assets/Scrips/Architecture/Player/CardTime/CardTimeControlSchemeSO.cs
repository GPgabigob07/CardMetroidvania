using System.Collections.Generic;
using UnityEngine;

namespace TicGame.Architecture
{
    [CreateAssetMenu(
        menuName = "TIC/Cards/Card Time Control Scheme",
        fileName = "CardTimeControlScheme_")]
    public sealed class CardTimeControlSchemeSO : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string schemeId;
        [SerializeField] private string displayName;
        [SerializeField] private CardTimeControlDeviceFamily deviceFamily;
        [TextArea]
        [SerializeField] private string description;

        [Header("Layouts")]
        [SerializeField] private List<CardTimeControlSchemeCategoryLayout> layouts = new();

        public string SchemeId => string.IsNullOrWhiteSpace(schemeId) ? name : schemeId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? SchemeId : displayName;
        public CardTimeControlDeviceFamily DeviceFamily => deviceFamily;
        public string Description => description;
        public IReadOnlyList<CardTimeControlSchemeCategoryLayout> Layouts => layouts;

        public void Configure(
            string stableId,
            string nameForDisplay,
            CardTimeControlDeviceFamily family,
            string schemeDescription,
            IEnumerable<CardTimeControlSchemeCategoryLayout> categoryLayouts)
        {
            schemeId = stableId;
            displayName = nameForDisplay;
            deviceFamily = family;
            description = schemeDescription;
            layouts = categoryLayouts != null
                ? new List<CardTimeControlSchemeCategoryLayout>(categoryLayouts)
                : new List<CardTimeControlSchemeCategoryLayout>();
        }

        public int GetSlotCount(PlayerCardTimeState category)
        {
            return GetLayout(category)?.SlotCount
                   ?? PlayerCardInventoryProfileSO.GetDefaultCapacity(category);
        }

        public IReadOnlyList<CardTimeControlSchemeSlotBinding> GetSlots(PlayerCardTimeState category)
        {
            return GetLayout(category)?.Slots
                   ?? System.Array.Empty<CardTimeControlSchemeSlotBinding>();
        }

        public string GetActionName(PlayerCardTimeState category, int slotIndex)
        {
            return GetLayout(category)?.GetSlot(slotIndex)?.ActionName;
        }

        public string GetDisplayLabel(PlayerCardTimeState category, int slotIndex)
        {
            return GetLayout(category)?.GetSlot(slotIndex)?.DisplayLabel ?? string.Empty;
        }

        public CardTimeControlSchemeSlotBinding GetSlot(
            PlayerCardTimeState category,
            int slotIndex)
        {
            return GetLayout(category)?.GetSlot(slotIndex);
        }

        public CardTimeControlSchemeCategoryLayout GetLayout(PlayerCardTimeState category)
        {
            foreach (var layout in layouts)
            {
                if (layout != null && layout.Category == category)
                {
                    return layout;
                }
            }

            return null;
        }
    }
}
