using System.Collections.Generic;

namespace TicGame.Architecture
{
    public sealed class CardReadinessResult
    {
        private CardReadinessResult(
            bool succeeded,
            CardCommitFailure failure,
            CardDefinitionSO card,
            IReadOnlyList<ResourceAmount> costs,
            PreparedCardCommit commit)
        {
            Succeeded = succeeded;
            Failure = failure;
            Card = card;
            Costs = costs ?? System.Array.Empty<ResourceAmount>();
            Commit = commit;
        }

        public bool Succeeded { get; }
        public CardCommitFailure Failure { get; }
        public CardDefinitionSO Card { get; }
        public IReadOnlyList<ResourceAmount> Costs { get; }
        public PreparedCardCommit Commit { get; }

        public static CardReadinessResult Success(
            CardDefinitionSO card,
            IReadOnlyList<ResourceAmount> costs,
            PreparedCardCommit commit)
        {
            return new CardReadinessResult(
                succeeded: true,
                failure: CardCommitFailure.None,
                card,
                costs,
                commit);
        }

        public static CardReadinessResult Failed(
            CardCommitFailure failure,
            CardDefinitionSO card = null)
        {
            return new CardReadinessResult(
                succeeded: false,
                failure,
                card,
                costs: null,
                commit: null);
        }
    }
}
