namespace TicGame.Architecture
{
    public interface IPlayerCardTimeSource
    {
        /// <summary>
        /// Gets the authoritative Card Time configuration used by this source.
        /// </summary>
        PlayerCardTimeConfigSO Configuration { get; }

        /// <summary>
        /// Publishes the Card Time opportunity currently offered by the player.
        /// </summary>
        void PublishAvailability(PlayerCardTimeState state);

        /// <summary>
        /// Requests activation from the current player-owned opportunity.
        /// </summary>
        CardTimeActivationRequestResult RequestActivation();

        /// <summary>
        /// Attempts to commit the active player-owned Card Time session.
        /// </summary>
        bool TryCommit();

        /// <summary>
        /// Attempts to commit the active player-owned Card Time session by applying a prepared card transaction.
        /// </summary>
        bool TryCommit(ICardCommitTransaction transaction);

        /// <summary>
        /// Cancels the active player-owned Card Time session.
        /// </summary>
        bool Cancel();

        /// <summary>
        /// Releases this player source and clears any state it owns.
        /// </summary>
        void Unregister();
    }
}
