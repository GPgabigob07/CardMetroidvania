namespace TicGame.Architecture
{
    public interface IPlayerAction
    {
        /// <summary>
        /// Gets the action state represented by this runtime action.
        /// </summary>
        PlayerActionState State { get; }

        /// <summary>
        /// Gets whether the action finished and should be cleared by the runner.
        /// </summary>
        bool IsComplete { get; }

        /// <summary>
        /// Called once when the action becomes active.
        /// </summary>
        void Enter(PlayerContext context);

        /// <summary>
        /// Updates input, timers and non-physics action rules.
        /// </summary>
        void Tick(PlayerContext context, float deltaTime);

        /// <summary>
        /// Updates physics-step action rules.
        /// </summary>
        void FixedTick(PlayerContext context, float fixedDeltaTime);

        /// <summary>
        /// Called once before the action is removed or replaced.
        /// </summary>
        void Exit(PlayerContext context);
    }
}
