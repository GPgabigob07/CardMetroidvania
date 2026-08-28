namespace TicGame.Architecture
{
    public interface ICardCommitTransaction
    {
        /// <summary>
        /// Gets the card resolved for this single-use commit transaction.
        /// </summary>
        CardDefinitionSO Card { get; }

        /// <summary>
        /// Gets the Card Time category this transaction was prepared for.
        /// </summary>
        PlayerCardTimeState Category { get; }

        /// <summary>
        /// Gets whether this transaction has already applied successfully.
        /// </summary>
        bool IsApplied { get; }

        /// <summary>
        /// Attempts to apply this prepared transaction exactly once.
        /// </summary>
        bool TryApply();
    }
}
