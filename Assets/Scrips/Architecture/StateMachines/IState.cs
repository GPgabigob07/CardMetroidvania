namespace TicGame.Architecture
{
    /// <summary>
    /// Defines a state that can be owned by a typed state machine.
    /// </summary>
    public interface IState<out TStateId>
    {
        /// <summary>
        /// Gets the stable identifier used by the owning state machine.
        /// </summary>
        TStateId Id { get; }

        /// <summary>
        /// Runs when the state becomes active.
        /// </summary>
        void Enter();

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
