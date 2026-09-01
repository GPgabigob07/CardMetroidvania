using UnityEngine;

namespace TicGame.Architecture
{
    public sealed class BatDodgeEvaluator
    {
        private IRandomRollSource rollSource = new UnityRandomRollSource();
        private float minimumClosingSpeed = 0.1f;

        public bool LastEvaluationConsumesCooldown { get; private set; }

        public bool TryEvaluate(BatThreatFacts threat, float currentPoise, float maximumPoise, bool cooldownReady = true)
        {
            LastEvaluationConsumesCooldown = false;
            if (!cooldownReady || !IsEligible(threat))
            {
                return false;
            }

            var normalizedPoise = maximumPoise <= 10f
                ? (currentPoise > 10f ? 1f : 0f)
                : Mathf.Clamp01(Mathf.InverseLerp(10f, maximumPoise, currentPoise));
            var chance = Mathf.Lerp(0.2f, 0.7f, normalizedPoise);
            var shouldDodge = Mathf.Clamp01(rollSource.NextNormalized()) < chance;
            LastEvaluationConsumesCooldown = shouldDodge;
            return shouldDodge;
        }

        public void SetRollSource(IRandomRollSource value)
        {
            rollSource = value ?? new UnityRandomRollSource();
        }

        public void SetMinimumClosingSpeed(float value)
        {
            minimumClosingSpeed = Mathf.Max(0f, value);
        }

        private bool IsEligible(BatThreatFacts threat)
        {
            return threat.IsMonitored
                && (threat.RelativeClosingSpeed >= minimumClosingSpeed || threat.IsWithinBaseMeleeReachOuterBand);
        }
    }
}
