using System.Collections.Generic;

namespace TicGame.Architecture
{
    public sealed class PreparedCardCommit : ICardCommitTransaction
    {
        private readonly PlayerCardRuntime owner;
        private readonly IReadOnlyList<ResourceAmount> costs;
        private readonly PlayerCardCommitSnapshot snapshot;

        internal PreparedCardCommit(
            PlayerCardRuntime owner,
            CardDefinitionSO card,
            long sessionId,
            IReadOnlyList<ResourceAmount> costs,
            PlayerCardCommitSnapshot snapshot)
        {
            this.owner = owner;
            Card = card;
            Category = card != null ? card.Category : PlayerCardTimeState.None;
            SessionId = sessionId;
            this.costs = costs ?? System.Array.Empty<ResourceAmount>();
            this.snapshot = snapshot;
        }

        public CardDefinitionSO Card { get; }
        public PlayerCardTimeState Category { get; }
        public long SessionId { get; }
        public IReadOnlyList<ResourceAmount> Costs => costs;
        public PlayerCardCommitSnapshot Snapshot => snapshot;
        public bool IsApplied { get; private set; }
        public CardCommitFailure Failure { get; private set; }

        public bool TryApply()
        {
            if (IsApplied)
            {
                Failure = CardCommitFailure.AlreadyApplied;
                return false;
            }

            if (owner == null || !owner.TryApplyPreparedCommit(this))
            {
                if (Failure == CardCommitFailure.None)
                {
                    Failure = CardCommitFailure.InsufficientLiveResources;
                }

                return false;
            }

            IsApplied = true;
            Failure = CardCommitFailure.None;
            return true;
        }

        internal void SetFailure(CardCommitFailure failure)
        {
            Failure = failure;
        }
    }
}
