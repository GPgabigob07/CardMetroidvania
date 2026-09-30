using System.Collections.Generic;

namespace TicGame.Architecture
{
    public sealed class PreparedCardCommit : ICardCommitTransaction
    {
        private readonly PlayerCardRuntime owner;
        private readonly IReadOnlyList<ResourceAmount> costs;
        private readonly PlayerCardCommitSnapshot snapshot;
        private bool isApplying;

        internal PreparedCardCommit(
            PlayerCardRuntime owner,
            CardDefinitionSO card,
            long sessionId,
            IReadOnlyList<ResourceAmount> costs,
            PlayerCardCommitSnapshot snapshot,
            RecoveryCardQuote? recoveryQuote = null)
        {
            this.owner = owner;
            Card = card;
            Category = card != null ? card.Category : PlayerCardTimeState.None;
            SessionId = sessionId;
            this.costs = costs ?? System.Array.Empty<ResourceAmount>();
            this.snapshot = snapshot;
            RecoveryQuote = recoveryQuote;
        }

        public CardDefinitionSO Card { get; }
        public PlayerCardTimeState Category { get; }
        public long SessionId { get; }
        public IReadOnlyList<ResourceAmount> Costs => costs;
        public PlayerCardCommitSnapshot Snapshot => snapshot;
        public RecoveryCardQuote? RecoveryQuote { get; }
        public bool IsApplied { get; private set; }
        public CardCommitFailure Failure { get; private set; }

        public bool TryApply()
        {
            if (IsApplied || isApplying)
            {
                Failure = CardCommitFailure.AlreadyApplied;
                return false;
            }

            isApplying = true;
            try
            {
                if (owner == null || !owner.TryApplyPreparedCommit(this))
                {
                    if (Failure == CardCommitFailure.None)
                        Failure = CardCommitFailure.InsufficientLiveResources;
                    return false;
                }
                IsApplied = true;
                Failure = CardCommitFailure.None;
                return true;
            }
            finally { isApplying = false; }
        }

        internal void SetFailure(CardCommitFailure failure)
        {
            Failure = failure;
        }
    }
}
