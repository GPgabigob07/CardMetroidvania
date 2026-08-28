namespace TicGame.Architecture
{
    public interface IPlayerCardReader
    {
        /// <summary>
        /// Prepares a selected card against a live Card Time selection transaction and frozen player commit snapshot.
        /// </summary>
        CardReadinessResult TryPrepare(
            CardDefinitionSO card,
            CardTimeSelectionTransaction selection,
            PlayerCardCommitSnapshot snapshot);
    }
}
