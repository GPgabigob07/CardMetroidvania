using System.Collections.Generic;
using UnityEngine;

namespace TicGame.Architecture
{
    public sealed class PreparedRepelQuote
    {
        private readonly CardTimeSelectionTransaction selection;
        private readonly string effectRevision;
        public float Duration { get; }

        internal PreparedRepelQuote(CardDefinitionSO card, CardTimeSelectionTransaction selection)
        {
            this.selection = selection;
            Duration = card.Effect.CommitOperations[0].Amount;
            effectRevision = JsonUtility.ToJson(card.Effect);
        }

        internal bool IsCurrent(CardDefinitionSO card, long sessionId,
            IReadOnlyList<ResourceAmount> quotedCosts, IReadOnlyList<ResourceAmount> currentCosts)
        {
            if (card == null || card.Category != PlayerCardTimeState.Chain || card.GetValidationErrors().Count != 0
                || card.Effect.CommitOperations.Count != 1
                || card.Effect.CommitOperations[0].Kind != CardOperationKind.ArmProjectileRepel
                || card.Effect.CommitOperations[0].Amount != Duration
                || JsonUtility.ToJson(card.Effect) != effectRevision || quotedCosts.Count != currentCosts.Count)
                return false;
            if (selection != null && (!selection.IsValid || selection.SessionId != sessionId
                || selection.Category != card.Category || !selection.TryGetSelectedCard(out var selected) || selected != card))
                return false;
            for (var index = 0; index < quotedCosts.Count; index++)
                if (quotedCosts[index].Resource != currentCosts[index].Resource || quotedCosts[index].Amount != currentCosts[index].Amount)
                    return false;
            return true;
        }
    }
}
