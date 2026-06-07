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

        public float CurrentHealth { get; private set; }
        public bool IsDead => CurrentHealth <= 0f;

        private void Awake()
        {
            Initialize();
        }

        public void Initialize()
        {
            CurrentHealth = maxHealth;
        }

        public DamageResult ApplyDamage(in DamageContext context)
        {
            if (IsDead)
            {
                return new DamageResult(accepted: false, killed: true, appliedAmount: 0f, remainingHealth: CurrentHealth);
            }

            float amount = context.Amount > 0f ? context.Amount : (context.Profile != null ? context.Profile.BaseDamage : 0f);
            CurrentHealth = Mathf.Max(a: 0f, b: CurrentHealth - amount);
            bool killed = CurrentHealth <= 0f;

            damageTakenEvent?.Raise(payload: context);

            if (killed)
            {
                deathEvent?.Raise();
            }

            return new DamageResult(accepted: true, killed: killed, appliedAmount: amount, remainingHealth: CurrentHealth);
        }
    }
}
