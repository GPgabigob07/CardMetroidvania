using System;
using UnityEngine;

namespace TicGame.Architecture
{
    public sealed class SimpleHealth : MonoBehaviour, IDamageable
    {
        [Header(header: "Health")]
        [Min(min: 1f)]
        [Tooltip(tooltip: "Maximum health restored when this component wakes up.")]
        [SerializeField] private float maxHealth = 5f;

        [Header(header: "Events")]
        [Tooltip(tooltip: "Raised whenever this component accepts a damage context.")]
        [SerializeField] private DamageEventChannelSO damageTakenEvent;

        [Tooltip(tooltip: "Raised when health reaches zero.")]
        [SerializeField] private VoidEventChannelSO deathEvent;

        public event Action<SimpleHealthChanged> Changed;
        public event Action<DamageContext, DamageResult> Damaged;
        public event Action ResetPerformed;

        public float CurrentHealth { get; private set; }
        public float MaximumHealth => maxHealth;
        public bool IsDead => CurrentHealth <= 0f;

        private void Awake()
        {
            Initialize();
        }

        public void Initialize()
        {
            var previousHealth = CurrentHealth;
            CurrentHealth = maxHealth;
            ResetPerformed?.Invoke();
            Changed?.Invoke(new SimpleHealthChanged(
                previous: previousHealth,
                current: CurrentHealth,
                maximum: MaximumHealth));
        }

        public float Heal(float amount)
        {
            if (IsDead || !float.IsFinite(amount) || amount <= 0) return 0;
            var previous = CurrentHealth;
            var publish = ChangeRecoveryDeferred(amount);
            publish?.Invoke();
            return CurrentHealth - previous;
        }

        public bool TrySpendNonlethal(float amount, float minimumRemainingHealth = 1f)
        {
            if (!float.IsFinite(amount) || amount <= 0 || !float.IsFinite(minimumRemainingHealth)
                || minimumRemainingHealth < 1 || CurrentHealth - amount < minimumRemainingHealth) return false;
            ChangeRecoveryDeferred(-amount)?.Invoke();
            return true;
        }

        internal Action ChangeRecoveryDeferred(float delta)
        {
            var previous = CurrentHealth;
            CurrentHealth = Mathf.Clamp(CurrentHealth + delta, 0, MaximumHealth);
            var change = new SimpleHealthChanged(previous, CurrentHealth, MaximumHealth);
            return Mathf.Approximately(previous, CurrentHealth) ? null : () => Changed?.Invoke(change);
        }

        public DamageResult ApplyDamage(in DamageContext context)
        {
            if (IsDead)
            {
                return new DamageResult(accepted: false, killed: true, appliedAmount: 0f, remainingHealth: CurrentHealth, hitStopSeconds: 0f);
            }

            float amount = context.Amount > 0f ? context.Amount : (context.Profile != null ? context.Profile.BaseDamage : 0f);
            var previousHealth = CurrentHealth;
            CurrentHealth = Mathf.Max(a: 0f, b: CurrentHealth - amount);
            bool killed = CurrentHealth <= 0f;

            var result = new DamageResult(
                accepted: true,
                killed: killed,
                appliedAmount: amount,
                remainingHealth: CurrentHealth,
                hitStopSeconds: context.Profile != null
                    ? context.Profile.HitStopSeconds
                    : 0.1f);
            if (float.IsFinite(amount) && amount > 0f) Damaged?.Invoke(context, result);

            damageTakenEvent?.Raise(payload: context);
            Changed?.Invoke(new SimpleHealthChanged(
                previous: previousHealth,
                current: CurrentHealth,
                maximum: MaximumHealth));

            if (killed)
            {
                deathEvent?.Raise();
            }

            return result;
        }
    }
}
