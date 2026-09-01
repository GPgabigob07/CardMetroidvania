using UnityEngine;

namespace TicGame.Architecture
{
    [RequireComponent(requiredComponent: typeof(EnemyHealth))]
    [RequireComponent(requiredComponent: typeof(EnemyPoise))]
    public sealed class BatMachineDamagePolicy : MonoBehaviour, IDamageable
    {
        [Header(header: "Dependencies")]
        [Tooltip(tooltip: "Health capability that receives accepted bat damage.")]
        [SerializeField] private EnemyHealth health;

        [Tooltip(tooltip: "Poise capability that receives authored poise damage after an accepted health hit.")]
        [SerializeField] private EnemyPoise poise;

        [Tooltip(tooltip: "Bat state owner used to reject transactions after the bat enters Dead.")]
        [SerializeField] private BatMachineBrain brain;

        private void Awake()
        {
            ResolveDependencies();
        }

        public DamageResult ApplyDamage(in DamageContext context)
        {
            ResolveDependencies();
            if (health == null || health.IsDefeated || (brain != null && brain.CurrentState == BatMachineState.Dead))
            {
                return CreateRejectedResult();
            }

            var result = health.ApplyDamage(context);
            if (result.Accepted && poise != null && context.PoiseDamage > 0f)
            {
                poise.ApplyPoiseDamage(context.PoiseDamage);
            }

            return result;
        }

        public void SetDependencies(EnemyHealth enemyHealth, EnemyPoise enemyPoise, BatMachineBrain stateOwner)
        {
            health = enemyHealth;
            poise = enemyPoise;
            brain = stateOwner;
        }

        private DamageResult CreateRejectedResult()
        {
            return new DamageResult(
                accepted: false,
                killed: health != null && health.IsDefeated,
                appliedAmount: 0f,
                remainingHealth: health != null ? health.CurrentHealth : 0f,
                hitStopSeconds: 0f);
        }

        private void ResolveDependencies()
        {
            if (health == null)
            {
                health = GetComponent<EnemyHealth>();
            }

            if (poise == null)
            {
                poise = GetComponent<EnemyPoise>();
            }

            if (brain == null)
            {
                brain = GetComponent<BatMachineBrain>();
            }
        }
    }
}
