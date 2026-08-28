using System;
using UnityEngine;

namespace TicGame.Architecture
{
    public sealed class EnemyHealth : MonoBehaviour, IDamageable
    {
        [Header(header: "Fallback Health")]
        [Min(min: 1f)]
        [Tooltip(tooltip: "Maximum health used when this component is initialized without an EnemyActor definition.")]
        [SerializeField] private float fallbackMaximumHealth = 1f;

        public event Action<EnemyHealthChanged> HealthChanged;
        public event Action<EnemyDamageEvent> Damaged;
        public event Action<EnemyDamageEvent> Defeated;
        public event Action<EnemyHealthChanged> Restored;

        public float CurrentHealth { get; private set; }
        public float MaximumHealth { get; private set; }
        public float NormalizedHealth => MaximumHealth > 0f ? CurrentHealth / MaximumHealth : 0f;
        public bool IsDefeated => CurrentHealth <= 0f;
        public bool IsInitialized { get; private set; }

        private void Awake()
        {
            if (!IsInitialized)
            {
                Initialize(maximumHealth: fallbackMaximumHealth);
            }
        }

        public void Initialize(float maximumHealth)
        {
            MaximumHealth = Mathf.Max(a: 1f, b: maximumHealth);
            CurrentHealth = MaximumHealth;
            IsInitialized = true;
        }

        public DamageResult ApplyDamage(in DamageContext context)
        {
            EnsureInitialized();

            if (IsDefeated)
            {
                return new DamageResult(
                    accepted: false,
                    killed: true,
                    appliedAmount: 0f,
                    remainingHealth: CurrentHealth,
                    hitStopSeconds: 0f);
            }

            var requestedAmount = ResolveDamageAmount(context);
            if (requestedAmount <= 0f)
            {
                return new DamageResult(
                    accepted: false,
                    killed: false,
                    appliedAmount: 0f,
                    remainingHealth: CurrentHealth,
                    hitStopSeconds: 0f);
            }

            var previousHealth = CurrentHealth;
            var appliedAmount = Mathf.Min(a: requestedAmount, b: CurrentHealth);
            CurrentHealth = Mathf.Max(a: 0f, b: CurrentHealth - appliedAmount);
            var result = new DamageResult(
                accepted: true,
                killed: IsDefeated,
                appliedAmount: appliedAmount,
                remainingHealth: CurrentHealth,
                hitStopSeconds: context.Profile != null
                    ? context.Profile.HitStopSeconds
                    : 0.1f);
            var healthChange = new EnemyHealthChanged(previousHealth, CurrentHealth, MaximumHealth);
            var damageEvent = new EnemyDamageEvent(context, result);

            HealthChanged?.Invoke(healthChange);
            Damaged?.Invoke(damageEvent);

            if (IsDefeated)
            {
                Defeated?.Invoke(damageEvent);
            }

            return result;
        }

        public float Restore(float amount)
        {
            EnsureInitialized();

            if (amount <= 0f || CurrentHealth >= MaximumHealth)
            {
                return 0f;
            }

            var previousHealth = CurrentHealth;
            CurrentHealth = Mathf.Min(a: MaximumHealth, b: CurrentHealth + amount);
            var restoredAmount = CurrentHealth - previousHealth;
            var healthChange = new EnemyHealthChanged(previousHealth, CurrentHealth, MaximumHealth);

            HealthChanged?.Invoke(healthChange);
            Restored?.Invoke(healthChange);
            return restoredAmount;
        }

        public void RestoreToFull()
        {
            Restore(amount: MaximumHealth);
        }

        private static float ResolveDamageAmount(in DamageContext context)
        {
            if (context.Amount > 0f)
            {
                return context.Amount;
            }

            return context.Profile != null ? Mathf.Max(a: 0f, b: context.Profile.BaseDamage) : 0f;
        }

        private void EnsureInitialized()
        {
            if (!IsInitialized)
            {
                Initialize(maximumHealth: fallbackMaximumHealth);
            }
        }
    }
}
