using System.Collections.Generic;

namespace TicGame.Architecture
{
    public readonly struct CardSelectionSnapshot
    {
        public CardSelectionSnapshot(
            bool isValid,
            long sessionId,
            PlayerCardTimeState category,
            IReadOnlyList<CardDefinitionSO> candidates,
            int selectedIndex)
        {
            IsValid = isValid;
            SessionId = sessionId;
            Category = category;
            Candidates = candidates ?? System.Array.Empty<CardDefinitionSO>();
            SelectedIndex = selectedIndex;
        }

        public bool IsValid { get; }
        public long SessionId { get; }
        public PlayerCardTimeState Category { get; }
        public IReadOnlyList<CardDefinitionSO> Candidates { get; }
        public int SelectedIndex { get; }
        public bool HasSelection => IsValid
                                    && SelectedIndex >= 0
                                    && SelectedIndex < Candidates.Count;
        public CardDefinitionSO SelectedCard => HasSelection
            ? Candidates[SelectedIndex]
            : null;
    }
}
