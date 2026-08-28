using System.Collections.Generic;
using UnityEngine;

namespace TicGame.Architecture
{
    [CreateAssetMenu(
        menuName = "TIC/Cards/Player Card Inventory Profile",
        fileName = "PlayerCardInventory_")]
    public sealed class PlayerCardInventoryProfileSO : ScriptableObject, ICardLoadoutProvider
    {
        [Header("Owned Cards")]
        [Tooltip("Cards currently owned by this test/profile inventory.")]
        [SerializeField] private List<CardInventoryEntry> ownedCards = new();

        [Header("Loadouts")]
        [Tooltip("Equipped cards grouped by Card Time type.")]
        [SerializeField] private List<CardLoadoutDefinition> loadouts = new();

        public IReadOnlyList<CardInventoryEntry> OwnedCards => ownedCards;
        public IReadOnlyList<CardLoadoutDefinition> Loadouts => loadouts;

        public static int GetDefaultCapacity(PlayerCardTimeState category)
        {
            return category switch
            {
                PlayerCardTimeState.Neutral => 8,
                PlayerCardTimeState.Chain => 6,
                PlayerCardTimeState.Finisher => 4,
                _ => 0
            };
        }

        public void EnsureDefaultLoadouts()
        {
            EnsureLoadout(PlayerCardTimeState.Neutral, GetDefaultCapacity(PlayerCardTimeState.Neutral));
            EnsureLoadout(PlayerCardTimeState.Chain, GetDefaultCapacity(PlayerCardTimeState.Chain));
            EnsureLoadout(PlayerCardTimeState.Finisher, GetDefaultCapacity(PlayerCardTimeState.Finisher));
        }

        public bool Owns(CardDefinitionSO card)
        {
            return FindOwnedEntry(card) != null;
        }

        public bool TryAddOwnedCard(CardDefinitionSO card, int count = 1)
        {
            if (card == null || card.Category == PlayerCardTimeState.None)
            {
                return false;
            }

            if (FindOwnedEntry(card) != null)
            {
                return false;
            }

            var entry = new CardInventoryEntry();
            entry.Configure(card, count);
            ownedCards.Add(entry);
            return true;
        }

        public bool TryEquip(CardDefinitionSO card)
        {
            if (!Owns(card))
            {
                return false;
            }

            var loadout = EnsureLoadout(card.Category, GetDefaultCapacity(card.Category));
            return loadout.TryEquip(card);
        }

        public bool TryUnequip(CardDefinitionSO card)
        {
            if (card == null)
            {
                return false;
            }

            var loadout = GetLoadout(card.Category);
            return loadout != null && loadout.Unequip(card);
        }

        public CardLoadoutDefinition GetLoadout(PlayerCardTimeState category)
        {
            foreach (var loadout in loadouts)
            {
                if (loadout.Category == category)
                {
                    return loadout;
                }
            }

            return null;
        }

        public IReadOnlyList<CardDefinitionSO> GetEquippedCards(PlayerCardTimeState category)
        {
            return GetLoadout(category)?.EquippedCards ?? System.Array.Empty<CardDefinitionSO>();
        }

        public IReadOnlyList<string> GetEquippedCardIds(PlayerCardTimeState category)
        {
            var loadout = GetLoadout(category);
            if (loadout == null)
            {
                return System.Array.Empty<string>();
            }

            var ids = new List<string>();
            foreach (var card in loadout.EquippedCards)
            {
                if (card != null)
                {
                    ids.Add(card.Id);
                }
            }

            return ids;
        }

        public CardInventorySaveData ExportSaveData()
        {
            var saveData = new CardInventorySaveData();
            var ownedIds = new HashSet<string>();
            foreach (var entry in ownedCards)
            {
                if (entry?.Card == null || !ownedIds.Add(entry.Card.Id))
                {
                    continue;
                }

                saveData.ownedCardIds.Add(entry.Card.Id);
            }

            foreach (var loadout in loadouts)
            {
                var loadoutData = new CardLoadoutSaveData
                {
                    category = loadout.Category
                };

                var equippedIds = new HashSet<string>();
                foreach (var card in loadout.EquippedCards)
                {
                    if (card != null && equippedIds.Add(card.Id))
                    {
                        loadoutData.equippedCardIds.Add(card.Id);
                    }
                }

                saveData.loadouts.Add(loadoutData);
            }

            return saveData;
        }

        public void ApplySaveData(CardInventorySaveData saveData, IEnumerable<CardDefinitionSO> cardCatalog)
        {
            ownedCards.Clear();
            loadouts.Clear();
            EnsureDefaultLoadouts();

            if (saveData == null)
            {
                return;
            }

            var catalog = BuildCatalog(cardCatalog);
            foreach (var cardId in saveData.ownedCardIds)
            {
                if (catalog.TryGetValue(cardId, out var card))
                {
                    TryAddOwnedCard(card);
                }
            }

            foreach (var savedLoadout in saveData.loadouts)
            {
                var loadout = EnsureLoadout(
                    savedLoadout.category,
                    GetDefaultCapacity(savedLoadout.category));
                loadout.Clear();

                foreach (var cardId in savedLoadout.equippedCardIds)
                {
                    if (catalog.TryGetValue(cardId, out var card))
                    {
                        TryEquip(card);
                    }
                }
            }
        }

        public IReadOnlyList<string> GetValidationErrors()
        {
            var errors = new List<string>();
            var ownedIds = new HashSet<string>();
            foreach (var entry in ownedCards)
            {
                if (entry?.Card == null)
                {
                    errors.Add("Inventory contains a null owned card.");
                    continue;
                }

                if (entry.Card.Category == PlayerCardTimeState.None)
                {
                    errors.Add($"{entry.Card.DisplayName} has no Card Time category.");
                }

                if (!ownedIds.Add(entry.Card.Id))
                {
                    errors.Add($"Inventory contains duplicate owned card id {entry.Card.Id}.");
                }
            }

            foreach (var loadout in loadouts)
            {
                foreach (var error in loadout.GetValidationErrors())
                {
                    errors.Add(error);
                }

                foreach (var card in loadout.EquippedCards)
                {
                    if (card != null && !Owns(card))
                    {
                        errors.Add($"{card.DisplayName} is equipped but not owned.");
                    }
                }
            }

            return errors;
        }

        private CardInventoryEntry FindOwnedEntry(CardDefinitionSO card)
        {
            if (card == null)
            {
                return null;
            }

            foreach (var entry in ownedCards)
            {
                if (entry != null && entry.References(card))
                {
                    return entry;
                }
            }

            return null;
        }

        private CardLoadoutDefinition EnsureLoadout(PlayerCardTimeState category, int capacity)
        {
            var loadout = GetLoadout(category);
            if (loadout != null)
            {
                loadout.Configure(category, capacity);
                return loadout;
            }

            loadout = new CardLoadoutDefinition();
            loadout.Configure(category, capacity);
            loadouts.Add(loadout);
            return loadout;
        }

        private static Dictionary<string, CardDefinitionSO> BuildCatalog(
            IEnumerable<CardDefinitionSO> cards)
        {
            var catalog = new Dictionary<string, CardDefinitionSO>();
            if (cards == null)
            {
                return catalog;
            }

            foreach (var card in cards)
            {
                if (card != null && !catalog.ContainsKey(card.Id))
                {
                    catalog.Add(card.Id, card);
                }
            }

            return catalog;
        }
    }
}
