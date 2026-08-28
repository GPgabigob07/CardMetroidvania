namespace TicGame.Architecture
{
    public interface IPlayerActionAnimationSource
    {
        /// <summary>
        /// Gets the current action phase exposed to presentation.
        /// </summary>
        PlayerActionPhase AnimationPhase { get; }

        /// <summary>
        /// Gets the current Card Time state exposed to presentation.
        /// </summary>
        PlayerCardTimeState AnimationCardTime { get; }
    }
}
