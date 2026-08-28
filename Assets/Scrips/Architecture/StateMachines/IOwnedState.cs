namespace TicGame.Architecture
{
    /// <summary>
    /// Defines a state that receives a strongly typed owner whenever it becomes active.
    /// </summary>
    public interface IOwnedState<out TStateId, in TOwner> : IAnimationAwareState
    {
        /// <summary>
        /// Gets the stable identifier used by the owning state machine.
        /// </summary>
        TStateId Id { get; }

        /// <summary>
        /// Runs when the state becomes active and receives its current owner.
        /// </summary>
        void Enter(TOwner owner);

        /// <summary>
        /// Runs once per variable-rate update while the state is active.
        /// </summary>
        void Tick(float deltaTime);

        /// <summary>
        /// Runs once per fixed-rate update while the state is active.
        /// </summary>
        void FixedTick(float fixedDeltaTime);

        /// <summary>
        /// Runs before the state stops being active.
        /// </summary>
        void Exit();
    }
}
