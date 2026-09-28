using System.Threading.Tasks;
using UnityEngine;

namespace TicGame.Architecture
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public sealed class EnvironmentalHazard2D : MonoBehaviour
    {
        [Header("Damage")]
        [Tooltip("Positive health damage applied when the configured player enters this hazard.")]
        [SerializeField, Min(0.01f)] private float damageAmount = 1f;

        private GameplayAreaCoordinator coordinator;
        private PlayerController boundPlayer;
        private bool contactLatched;
        private int contactGeneration;

        /// <summary>
        /// Binds this area hazard to the persistent gameplay coordinator and player.
        /// </summary>
        public void Bind(GameplayAreaCoordinator configuredCoordinator, PlayerController player)
        {
            coordinator = configuredCoordinator;
            boundPlayer = player;
        }

        /// <summary>
        /// Sets the authored damage amount used for subsequent contacts.
        /// </summary>
        public void ConfigureDamage(float amount)
        {
            damageAmount = amount;
        }

        /// <summary>
        /// Applies one damage transaction and starts nonlethal recovery for an authorized player contact.
        /// </summary>
        public bool TryContact(PlayerController candidate)
        {
            var trigger = GetComponent<Collider2D>();
            if (!isActiveAndEnabled || trigger == null || !trigger.enabled || !trigger.isTrigger
                || coordinator == null || candidate == null || candidate != boundPlayer
                || !coordinator.IsConfiguredPlayer(candidate) || float.IsNaN(damageAmount)
                || float.IsInfinity(damageAmount) || damageAmount <= 0f
                || coordinator.IsRecovering || contactLatched)
            {
                return false;
            }

            contactLatched = true;
            var generation = ++contactGeneration;
            var instance = new DamageInstance(
                instanceId: $"{name}-environment",
                sourceObject: gameObject,
                profile: null,
                formula: new DamageFormulaValues(
                    attack: 0f,
                    strikePercent: 0f,
                    strikeBonusPercent: 0f,
                    attackBuffPercent: 0f,
                    flatDamage: damageAmount,
                    finalDamagePercent: 0f,
                    critValue: 1f));
            var report = DamageResolver.Resolve(new DamageRequest(
                instance,
                new[] { candidate.gameObject },
                transform.position,
                Vector2.zero));

            if (report.EffectiveHitCount <= 0)
            {
                contactLatched = false;
                return false;
            }

            if (report.KilledTargets > 0)
            {
                _ = ReleaseLatchAfterRecoveryAsync(generation);
                return true;
            }

            _ = RecoverAndReleaseLatchAsync(generation);
            return true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            TryContact(other.GetComponentInParent<PlayerController>());
        }

        private async Task RecoverAndReleaseLatchAsync(int generation)
        {
            try
            {
                await coordinator.RecoverFromHazardAsync();
            }
            finally
            {
                ReleaseLatch(generation);
            }
        }

        private async Task ReleaseLatchAfterRecoveryAsync(int generation)
        {
            while (this != null && coordinator != null && coordinator.IsRecovering)
            {
                await Task.Yield();
            }

            ReleaseLatch(generation);
        }

        private void ReleaseLatch(int generation)
        {
            if (this != null && generation == contactGeneration)
            {
                contactLatched = false;
            }
        }
    }
}
