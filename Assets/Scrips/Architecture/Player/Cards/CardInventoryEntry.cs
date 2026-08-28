using System;
using UnityEngine;

namespace TicGame.Architecture
{
    [Serializable]
    public sealed class CardInventoryEntry
    {
        [Tooltip("Card definition owned by this inventory.")]
        [SerializeField] private CardDefinitionSO card;

        [Min(1)]
        [Tooltip("Owned count. Unique cards normally use 1, but stacks are kept for future rewards.")]
        [SerializeField] private int count = 1;

        public CardDefinitionSO Card => card;
        public int Count => Mathf.Max(0, count);

        public void Configure(CardDefinitionSO definition, int ownedCount)
        {
            card = definition;
            count = Mathf.Max(1, ownedCount);
        }

        public bool References(CardDefinitionSO definition)
        {
            return card == definition || card != null && definition != null && card.Id == definition.Id;
        }
    }
}
