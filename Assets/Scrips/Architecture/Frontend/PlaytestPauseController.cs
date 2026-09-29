using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TicGame.Architecture
{
    [DefaultExecutionOrder(-1000)]
    public sealed class PlaytestPauseController : MonoBehaviour
    {
        private PlaytestPauseLease lease;
        private PlaytestSessionController session;
        public bool IsPaused => lease != null;
        public static bool IsGamePaused => PlaytestSessionController.Instance != null
            && PlaytestSessionController.Instance.GetComponent<PlaytestPauseController>().IsPaused;
        public event Action<bool> PauseChanged;

        private void Awake() => session = GetComponent<PlaytestSessionController>();

        private void Update()
        {
            if (session == null || session.IsTitle || session.IsTransitioning || session.LastError != null) return;
            if (Keyboard.current?.escapeKey.wasPressedThisFrame == true || Gamepad.current?.startButton.wasPressedThisFrame == true)
            {
                if (IsPaused) GetComponent<PlaytestMenuView>().Back();
                else TryPause();
            }
        }

        public bool TryPause()
        {
            if (IsPaused || session?.Gameplay == null || session.IsTransitioning || session.LastError != null || !session.Gameplay.IsReady
                || session.Gameplay.IsRecovering || session.Services?.Time == null) return false;
            lease = new PlaytestPauseLease(session.Services.Time);
            session.Gameplay.Player.SetMenuInputSuppressed(true);
            session.Services.GameState.RequestState(GameState.Pause);
            PauseChanged?.Invoke(true);
            return true;
        }

        public void Resume()
        {
            if (!IsPaused) return;
            lease.Dispose();
            lease = null;
            session.RequireInputRelease();
            session.Services?.GameState?.RequestState(GameState.Gameplay);
            PauseChanged?.Invoke(false);
        }

        public static bool AnySubmitHeld()
        {
            var keyboard = Keyboard.current;
            var pad = Gamepad.current;
            return keyboard?.enterKey.isPressed == true || keyboard?.spaceKey.isPressed == true
                || keyboard?.escapeKey.isPressed == true || keyboard?.jKey.isPressed == true
                || keyboard?.kKey.isPressed == true || Mouse.current?.leftButton.isPressed == true
                || Mouse.current?.backButton.isPressed == true || Mouse.current?.forwardButton.isPressed == true
                || pad?.buttonSouth.isPressed == true || pad?.buttonWest.isPressed == true
                || pad?.buttonEast.isPressed == true || pad?.startButton.isPressed == true
                || pad?.leftShoulder.isPressed == true || pad?.rightShoulder.isPressed == true;
        }

        private void OnApplicationFocus(bool focused) { if (!focused) TryPause(); }
        private void OnDestroy() { lease?.Dispose(); lease = null; }
    }
}
