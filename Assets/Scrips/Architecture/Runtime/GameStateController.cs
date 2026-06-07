using UnityEngine;

namespace TicGame.Architecture
{
    public sealed class GameStateController : MonoBehaviour
    {
        [Header(header: "State")]
        [Tooltip(tooltip: "State applied when this controller wakes up.")]
        [SerializeField] private GameState initialState = GameState.Boot;

        [Header(header: "Events")]
        [Tooltip(tooltip: "Raised whenever the current game state changes.")]
        [SerializeField] private GameStateEventChannelSO stateChangedEvent;

        public GameState CurrentState { get; private set; }

        private void Awake()
        {
            CurrentState = initialState;
            stateChangedEvent?.Raise(payload: CurrentState);
        }

        public void RequestState(GameState nextState)
        {
            if (CurrentState == nextState)
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
