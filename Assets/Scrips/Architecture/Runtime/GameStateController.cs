using UnityEngine;

namespace TicGame.Architecture
{
    public sealed class GameStateController : MonoBehaviour
    {
        [Header("State")]
        [Tooltip("State applied when this controller wakes up.")]
        [SerializeField] private GameState initialState = GameState.Boot;

        [Header("Events")]
        [Tooltip("Raised whenever the current game state changes.")]
        [SerializeField] private GameStateEventChannelSO stateChangedEvent;

        public GameState CurrentState { get; private set; }

        private void Awake()
        {
            CurrentState = initialState;
            stateChangedEvent?.Raise(CurrentState);
        }

        public void RequestState(GameState nextState)
        {
            if (CurrentState == nextState)
            {
                return;
            }

            CurrentState = nextState;
            stateChangedEvent?.Raise(CurrentState);
        }

        public void RequestGameplay()
        {
            RequestState(GameState.Gameplay);
        }

        public void RequestPause()
        {
            RequestState(GameState.Pause);
        }

        public void RequestDeath()
        {
            RequestState(GameState.Death);
        }

        public void RequestRespawn()
        {
            RequestState(GameState.Respawn);
        }
    }
}
