using UnityEngine;

namespace TicGame.Architecture
{
    public sealed class PlayerCardTimeChordRuntime
    {
        private readonly float graceDuration;
        private float leftRecentRemaining;
        private float rightRecentRemaining;
        private bool consumed;

        public PlayerCardTimeChordRuntime(float graceDuration)
        {
            this.graceDuration = Mathf.Max(a: 0f, b: graceDuration);
        }

        public bool Tick(
            float unscaledDeltaTime,
            bool leftPressed,
            bool leftHeld,
            bool rightPressed,
            bool rightHeld)
        {
            leftRecentRemaining = leftPressed
                ? graceDuration
                : Mathf.Max(a: 0f, b: leftRecentRemaining - unscaledDeltaTime);
            rightRecentRemaining = rightPressed
                ? graceDuration
                : Mathf.Max(a: 0f, b: rightRecentRemaining - unscaledDeltaTime);

            if (!leftHeld && !rightHeld)
            {
                consumed = false;
            }

            if (consumed)
            {
                return false;
            }

            var leftReady = leftHeld || leftRecentRemaining > 0f;
            var rightReady = rightHeld || rightRecentRemaining > 0f;
            if (!leftReady || !rightReady)
            {
                return false;
            }

            consumed = true;
            leftRecentRemaining = 0f;
            rightRecentRemaining = 0f;
            return true;
        }
    }
}
