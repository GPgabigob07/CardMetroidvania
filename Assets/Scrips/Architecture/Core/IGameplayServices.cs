namespace TicGame.Architecture
{
    public interface IGameplayServices
    {
        /// <summary>
        /// Gets the persistent global gameplay-time service.
        /// </summary>
        IGameplayTimeService Time { get; }

        /// <summary>
        /// Gets the persistent hitstop service.
        /// </summary>
        IHitStopService HitStop { get; }

        /// <summary>
        /// Gets the channel used by combat systems to request global hitstop.
        /// </summary>
        HitStopRequestEventChannelSO HitStopRequests { get; }

        /// <summary>
        /// Gets the authoritative observable Card Time session.
        /// </summary>
        ICardTimeSession CardTime { get; }

        /// <summary>
        /// Gets the channel that broadcasts Card Time transitions.
        /// </summary>
        CardTimeSessionEventChannelSO CardTimeTransitions { get; }

        /// <summary>
        /// Gets the optional card feedback presentation service.
        /// </summary>
        ICardFeedbackService CardFeedback { get; }

        /// <summary>
        /// Gets the persistent global game-state service.
        /// </summary>
        IGameStateService GameState { get; }
    }
}
