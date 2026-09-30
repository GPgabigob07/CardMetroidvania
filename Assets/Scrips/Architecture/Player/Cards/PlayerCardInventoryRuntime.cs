using System;
using System.Collections.Generic;
using UnityEngine;

namespace TicGame.Architecture
{
    public sealed class PlayerCardInventoryRuntime : MonoBehaviour, ICardLoadoutProvider
    {
        [Header("Run Inventory")]
        [SerializeField] private PlayerCardInventoryProfileSO profile;
        private readonly Dictionary<string, int> counts = new();
        private readonly Dictionary<PlayerCardTimeState, IReadOnlyList<CardDefinitionSO>> cards = new();
        private readonly Dictionary<PlayerCardTimeState, IReadOnlyList<string>> ids = new();
        public event Action Changed;
        public int Revision { get; private set; }
        public bool IsInitialized { get; private set; }

        private void Awake() => Initialize(profile);
        public void Initialize(PlayerCardInventoryProfileSO source)
        {
            if (IsInitialized || source == null) return;
            foreach (var entry in source.OwnedCards)
                if (entry?.Card != null) counts[entry.Card.Id] = entry.Count;
            foreach (var category in new[] { PlayerCardTimeState.Neutral, PlayerCardTimeState.Chain, PlayerCardTimeState.Finisher })
            {
                cards[category] = new List<CardDefinitionSO>(source.GetEquippedCards(category)).AsReadOnly();
                ids[category] = new List<string>(source.GetEquippedCardIds(category)).AsReadOnly();
            }
            IsInitialized = true;
        }
        public int GetCount(string cardId) => cardId != null && counts.TryGetValue(cardId, out var count) ? count : 0;
        public IReadOnlyList<CardDefinitionSO> GetEquippedCards(PlayerCardTimeState category)
            => cards.TryGetValue(category, out var result) ? result : Array.Empty<CardDefinitionSO>();
        public IReadOnlyList<string> GetEquippedCardIds(PlayerCardTimeState category)
            => ids.TryGetValue(category, out var result) ? result : Array.Empty<string>();
        internal Action ConsumeDeferred(CardDefinitionSO card, int amount)
        {
            if (amount == 0) return null;
            counts[card.Id] -= amount;
            Revision++;
            return () => Changed?.Invoke();
        }
    }
}
