using System;
using System.Collections.Generic;
using UnityEngine;

namespace TicGame.Architecture
{
    [Serializable]
    public sealed class CardLoadoutDefinition
    {
        [Tooltip("Card Time type that owns this equipped-card group.")]
        [SerializeField] private PlayerCardTimeState category;

        [Min(0)]
        [Tooltip("Maximum cards this Card Time type can expose in combat.")]
        [SerializeField] private int capacity;

        [Tooltip("Equipped cards shown by this Card Time type in combat.")]
        [SerializeField] private List<CardDefinitionSO> equippedCards = new();

        public PlayerCardTimeState Category => category;
        public int Capacity => Mathf.Max(0, capacity);
        public IReadOnlyList<CardDefinitionSO> EquippedCards => equippedCards;

        public void Configure(PlayerCardTimeState cardTimeCategory, int maxCards)
        {
            category = cardTimeCategory;
            capacity = Mathf.Max(0, maxCards);
            equippedCards ??= new List<CardDefinitionSO>();
        }

        public bool Contains(CardDefinitionSO card)
        {
            return card != null && equippedCards.Contains(card);
        }

        public bool TryEquip(CardDefinitionSO card)
        {
            if (card == null
                || card.Category != category
                || equippedCards.Contains(card)
                || equippedCards.Count >= Capacity)
            {
                return false;
            }

            equippedCards.Add(card);
            return true;
        }

        public bool Unequip(CardDefinitionSO card)
        {
            return card != null && equippedCards.Remove(card);
        }

        public void Clear()
        {
            equippedCards.Clear();
        }

        public IReadOnlyList<string> GetValidationErrors()
        {
            var errors = new List<string>();
            if (category == PlayerCardTimeState.None)
            {
                errors.Add("Loadout category cannot be None.");
            }

            if (capacity < 0)
            {
                errors.Add("Loadout capacity cannot be negative.");
            }

            if (equippedCards.Count > Capacity)
            {
                errors.Add($"Loadout {category} exceeds capacity {Capacity}.");
            }

            var seenIds = new HashSet<string>();
            foreach (var card in equippedCards)
            {
                if (card == null)
                {
                    errors.Add($"Loadout {category} contains a null card.");
                    continue;
                }

                if (card.Category != category)
                {
                    errors.Add($"{card.DisplayName} belongs to {card.Category}, not {category}.");
                }

                if (!seenIds.Add(card.Id))
                {
                    errors.Add($"Loadout {category} contains duplicate card id {card.Id}.");
                }
            }

            return errors;
        }
    }
}
