using UnityEngine;

namespace TicGame.Architecture
{
    public sealed class BatShotPredictor
    {
        private float windupDuration = 0.2f;
        private float leadTime = 0.2f;
        private float maximumLeadDistance = 2f;
        private Vector2 sampleStartPosition;
        private Vector2 latestSamplePosition;
        private float sampledDuration;
        private bool isSampling;
        private bool isLocked;
        private Vector2 lockedPrediction;

        public void BeginSample(Vector2 playerPosition)
        {
            sampleStartPosition = playerPosition;
            latestSamplePosition = playerPosition;
            sampledDuration = 0f;
            isSampling = true;
            isLocked = false;
        }

        public void Sample(Vector2 playerPosition, float deltaTime)
        {
            if (!isSampling || isLocked || sampledDuration >= windupDuration)
            {
                return;
            }

            var providedDuration = Mathf.Max(0f, deltaTime);
            if (providedDuration <= 0f)
            {
                return;
            }

            var acceptedDuration = Mathf.Min(windupDuration - sampledDuration, providedDuration);
            latestSamplePosition = Vector2.Lerp(
                latestSamplePosition,
                playerPosition,
                acceptedDuration / providedDuration);
            sampledDuration += acceptedDuration;
        }

        public Vector2 LockPrediction()
        {
            if (isLocked)
            {
                return lockedPrediction;
            }

            var averageVelocity = sampledDuration > 0f
                ? (latestSamplePosition - sampleStartPosition) / sampledDuration
                : Vector2.zero;
            var lead = Vector2.ClampMagnitude(averageVelocity * Mathf.Max(0f, leadTime), Mathf.Max(0f, maximumLeadDistance));
            lockedPrediction = latestSamplePosition + lead;
            isSampling = false;
            isLocked = true;
            return lockedPrediction;
        }

        public void Configure(float windupDuration, float leadTime, float maximumLeadDistance)
        {
            this.windupDuration = Mathf.Max(0f, windupDuration);
            this.leadTime = Mathf.Max(0f, leadTime);
            this.maximumLeadDistance = Mathf.Max(0f, maximumLeadDistance);
        }
    }
}
