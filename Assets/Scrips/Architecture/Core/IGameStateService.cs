namespace TicGame.Architecture
{
    public interface IGameStateService
    {
        /// <summary>
        /// Gets the current global game state.
        /// </summary>
        GameState CurrentState { get; }

        /// <summary>
        /// Requests a transition to the supplied global game state.
        /// </summary>
        void RequestState(GameState nextState);
    }
}
