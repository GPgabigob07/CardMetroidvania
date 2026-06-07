namespace TicGame.Architecture
{
    public interface IPlayerLocomotionState
    {
        /// <summary>
        /// Gets the stable locomotion identifier represented by this state.
        /// </summary>
        PlayerLocomotionState Id { get; }

        /// <summary>
        /// Gets whether this state can currently become active.
        /// </summary>
        bool CanEnter(PlayerContext context);

        /// <summary>
        /// Called once when this locomotion state becomes active.
        /// </summary>
        void Enter(PlayerContext context);

        /// <summary>
        /// Updates variable-rate locomotion logic.
        /// </summary>
        void Tick(PlayerContext context, float deltaTime);

        /// <summary>
        /// Updates fixed-rate locomotion logic.
        /// </summary>
        void FixedTick(PlayerContext context, float fixedDeltaTime);

        /// <summary>
        /// Builds the baseline locomotion frame before action overrides run.
        /// </summary>
        LocomotionFrame BuildFrame(PlayerContext context, float fixedDeltaTime);

        /// <summary>
        /// Called before this locomotion state stops being active.
        /// </summary>
        void Exit(PlayerContext context);
    }
}
