using UnityEngine;

namespace TicGame.Architecture
{
    public sealed class SimpleHealth : MonoBehaviour, IDamageable
    {
        [Header("Health")]
        [Min(1f)]
        [Tooltip("Maximum health restored when this component wakes up.")]
        [SerializeField] private float maxHealth = 5f;

        [Header("Events")]
        [Tooltip("Raised whenever this component accepts a damage context.")]
        [SerializeField] private DamageEventChannelSO damageTakenEvent;

        [Tooltip("Raised when health reaches zero.")]
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
                return new DamageResult(false, true, 0f, CurrentHealth);
            }

            float amount = context.Amount > 0f ? context.Amount : (context.Profile != null ? context.Profile.BaseDamage : 0f);
            CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);
            bool killed = CurrentHealth <= 0f;

            damageTakenEvent?.Raise(context);

            if (killed)
            {
                deathEvent?.Raise();
            }

            return new DamageResult(true, killed, amount, CurrentHealth);
        }
    }
}
