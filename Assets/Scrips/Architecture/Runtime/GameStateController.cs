using UnityEngine;

namespace TicGame.Architecture
{
    public sealed class GameStateController :
        MonoBehaviour,
        IGameplayModule,
        IGameStateService
    {
        [Header(header: "State")]
        [Tooltip(tooltip: "State applied when this gameplay module initializes.")]
        [SerializeField] private GameState initialState = GameState.Boot;

        [Header(header: "Events")]
        [Tooltip(tooltip: "Raised whenever the current game state changes.")]
        [SerializeField] private GameStateEventChannelSO stateChangedEvent;

        public GameState CurrentState { get; private set; }
        public bool IsInitialized { get; private set; }

        public void Initialize()
        {
            if (IsInitialized)
            {
                return;
            }

            CurrentState = initialState;
            IsInitialized = true;
            stateChangedEvent?.Raise(payload: CurrentState);
        }

        public void Shutdown()
        {
            IsInitialized = false;
        }

        public void RequestState(GameState nextState)
        {
            if (!IsInitialized || CurrentState == nextState)
            {
                return;
            }

            CurrentState = nextState;
            stateChangedEvent?.Raise(payload: CurrentState);
        }

        public void RequestGameplay()
        {
            RequestState(nextState: GameState.Gameplay);
        }

        public void RequestPause()
        {
            RequestState(nextState: GameState.Pause);
        }

        public void RequestDeath()
        {
            RequestState(nextState: GameState.Death);
        }

        public void RequestRespawn()
        {
            RequestState(nextState: GameState.Respawn);
        }
    }
}
