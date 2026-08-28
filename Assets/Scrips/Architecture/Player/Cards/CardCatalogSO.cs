using System.Collections.Generic;
using UnityEngine;

namespace TicGame.Architecture
{
    [CreateAssetMenu(
        menuName = "TIC/Cards/Card Catalog",
        fileName = "CardCatalog_")]
    public sealed class CardCatalogSO : ScriptableObject, ICardCatalog
    {
        [Header("Cards")]
        [Tooltip("Cards available for id lookup in this catalog.")]
        [SerializeField] private List<CardDefinitionSO> cards = new();

        private Dictionary<string, CardDefinitionSO> byId;

        public IReadOnlyList<CardDefinitionSO> Cards => cards;

        public void Configure(IEnumerable<CardDefinitionSO> definitions)
        {
            cards = definitions != null
                ? new List<CardDefinitionSO>(definitions)
                : new List<CardDefinitionSO>();
            byId = null;
        }

        public bool TryGetCard(string id, out CardDefinitionSO card)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                card = null;
                return false;
            }

            byId ??= BuildIndex(cards);
            return byId.TryGetValue(id, out card);
        }

        public IReadOnlyList<string> GetValidationErrors()
        {
            var errors = new List<string>();
            var seen = new HashSet<string>();
            foreach (var card in cards)
            {
                if (card == null)
                {
                    errors.Add("Catalog contains a null card.");
                    continue;
                }

                if (!seen.Add(card.Id))
                {
                    errors.Add($"Catalog contains duplicate card id {card.Id}.");
                }
            }

            return errors;
        }

        private static Dictionary<string, CardDefinitionSO> BuildIndex(
            IEnumerable<CardDefinitionSO> definitions)
        {
            var index = new Dictionary<string, CardDefinitionSO>();
            if (definitions == null)
            {
                return index;
            }

            foreach (var card in definitions)
            {
                if (card != null && !index.ContainsKey(card.Id))
                {
                    index.Add(card.Id, card);
                }
            }

            return index;
        }
    }
}
