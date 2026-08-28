using System.Collections.Generic;

namespace TicGame.Architecture
{
    public sealed class CardTimeSelectionTransaction
    {
        private readonly List<CardDefinitionSO> candidates = new();
        private bool disposed;
        private int selectedIndex;

        private CardTimeSelectionTransaction(
            PlayerCardTimeState category,
            long sessionId)
        {
            Category = category;
            SessionId = sessionId;
            selectedIndex = -1;
        }

        public PlayerCardTimeState Category { get; }
        public long SessionId { get; }
        public bool IsValid => !disposed
                               && Category != PlayerCardTimeState.None
                               && SessionId > 0;
        public CardSelectionSnapshot Current => new(
            IsValid,
            SessionId,
            Category,
            candidates,
            IsValid ? selectedIndex : -1);

        public static bool TryCreate(
            PlayerCardTimeState category,
            long sessionId,
            IEnumerable<string> candidateIds,
            ICardCatalog catalog,
            out CardTimeSelectionTransaction transaction)
        {
            transaction = null;
            if (category == PlayerCardTimeState.None
                || sessionId <= 0
                || catalog == null)
            {
                return false;
            }

            var created = new CardTimeSelectionTransaction(category, sessionId);
            var seenIds = new HashSet<string>();
            if (candidateIds != null)
            {
                foreach (var id in candidateIds)
                {
                    if (string.IsNullOrWhiteSpace(id)
                        || !seenIds.Add(id)
                        || !catalog.TryGetCard(id, out var card)
                        || card == null
                        || card.Category != category)
                    {
                        continue;
                    }

                    created.candidates.Add(card);
                }
            }

            created.selectedIndex = created.candidates.Count > 0 ? 0 : -1;
            transaction = created;
            return true;
        }

        public bool MoveSelection(int direction)
        {
            if (!IsValid || candidates.Count == 0 || direction == 0)
            {
                return false;
            }

            var previous = selectedIndex;
            selectedIndex = Clamp(
                selectedIndex + direction,
                0,
                candidates.Count - 1);
            return selectedIndex != previous;
        }

        public bool SelectIndex(int index)
        {
            if (!IsValid || index < 0 || index >= candidates.Count)
            {
                return false;
            }

            selectedIndex = index;
            return true;
        }

        public bool TryGetSelectedCard(out CardDefinitionSO card)
        {
            var snapshot = Current;
            card = snapshot.SelectedCard;
            return card != null;
        }

        public void Dispose()
        {
            disposed = true;
            selectedIndex = -1;
        }

        private static int Clamp(int value, int minimum, int maximum)
        {
            if (value < minimum)
            {
                return minimum;
            }

            return value > maximum ? maximum : value;
        }
    }
}
