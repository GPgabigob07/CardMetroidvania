using System;
using UnityEngine;

namespace TicGame.Architecture
{
    public sealed class EnemyPoise : MonoBehaviour
    {
        [Header(header: "Fallback Poise")]
        [Min(min: 0f)]
        [Tooltip(tooltip: "Maximum poise used when this component is initialized by Unity without explicit configuration.")]
        [SerializeField] private float fallbackMaximumPoise = 1f;

        [Min(min: 0f)]
        [Tooltip(tooltip: "Poise restored per second by the fallback initialization.")]
        [SerializeField] private float fallbackRegenerationPerSecond;

        private bool hasDepleted;

        public event Action Depleted;

        public float CurrentPoise { get; private set; }
        public float MaximumPoise { get; private set; }
        public float RegenerationPerSecond { get; private set; }
        public float NormalizedPoise => MaximumPoise > 0f ? CurrentPoise / MaximumPoise : 0f;
        public bool IsInitialized { get; private set; }

        private void Awake()
        {
            if (!IsInitialized)
            {
                Initialize(fallbackMaximumPoise, fallbackRegenerationPerSecond);
            }
        }

        public void Initialize(float maximum, float regenerationPerSecond)
        {
            MaximumPoise = Mathf.Max(0f, maximum);
            RegenerationPerSecond = Mathf.Max(0f, regenerationPerSecond);
            CurrentPoise = MaximumPoise;
            hasDepleted = false;
            IsInitialized = true;
        }

        public void Tick(float deltaTime)
        {
            EnsureInitialized();

            if (CurrentPoise <= 0f || deltaTime <= 0f || RegenerationPerSecond <= 0f)
            {
                return;
            }

            CurrentPoise = Mathf.Min(MaximumPoise, CurrentPoise + (RegenerationPerSecond * deltaTime));
        }

        public float ApplyPoiseDamage(float amount)
        {
            EnsureInitialized();

            var applied = Mathf.Min(Mathf.Max(0f, amount), CurrentPoise);
            CurrentPoise -= applied;
            if (CurrentPoise <= 0f && !hasDepleted)
            {
                hasDepleted = true;
                Depleted?.Invoke();
            }

            return applied;
        }

        public float Restore(float amount)
        {
            EnsureInitialized();

            if (amount <= 0f || CurrentPoise >= MaximumPoise)
            {
                return 0f;
            }

            var previousPoise = CurrentPoise;
            CurrentPoise = Mathf.Min(MaximumPoise, CurrentPoise + amount);
            if (CurrentPoise > 0f)
            {
                hasDepleted = false;
            }

            return CurrentPoise - previousPoise;
        }

        public void RestoreToFull()
        {
            Restore(MaximumPoise);
        }

        private void EnsureInitialized()
        {
            if (!IsInitialized)
            {
                Initialize(fallbackMaximumPoise, fallbackRegenerationPerSecond);
            }
        }
    }
}
