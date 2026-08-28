using UnityEngine;

namespace TicGame.Architecture
{
    [RequireComponent(requiredComponent: typeof(EnemyActor))]
    [RequireComponent(requiredComponent: typeof(EnemyHealth))]
    public sealed class TrainingDummy : MonoBehaviour
    {
        [Header(header: "Regeneration")]
        [Min(min: 0f)]
        [Tooltip(tooltip: "Unscaled seconds without accepted damage before regeneration begins.")]
        [SerializeField] private float regenerationDelay = 1f;

        [Min(min: 0f)]
        [Tooltip(tooltip: "Health restored per unscaled second after the regeneration delay.")]
        [SerializeField] private float regenerationPerSecond = 5f;

        [Tooltip(tooltip: "Restores full health after the defeat feedback delay instead of regenerating gradually from zero.")]
        [SerializeField] private bool restoreImmediatelyWhenDefeated = true;

        [Min(min: 0f)]
        [Tooltip(tooltip: "Unscaled delay that keeps zero health visible before immediate defeat restoration.")]
        [SerializeField] private float defeatFeedbackDelay = 0.1f;

        public float TimeSinceDamage { get; private set; }
        public int AcceptedHitCount { get; private set; }
        public float TotalDamageReceived { get; private set; }

        private EnemyHealth health;
        private bool waitingForDefeatRestore;

        private void Awake()
        {
            ResolveHealth();
        }

        private void OnEnable()
        {
            ResolveHealth();
            health.Damaged += OnDamaged;
            health.Defeated += OnDefeated;
        }

        private void OnDisable()
        {
            if (health == null)
            {
                return;
            }

            health.Damaged -= OnDamaged;
            health.Defeated -= OnDefeated;
        }

        private void Update()
        {
            TickRegeneration(deltaTime: Time.unscaledDeltaTime);
        }

        public void ConfigureRegeneration(
            float delay,
            float healthPerSecond,
            bool restoreImmediatelyOnDefeat,
            float defeatDelay = 0.1f)
        {
            regenerationDelay = Mathf.Max(a: 0f, b: delay);
            regenerationPerSecond = Mathf.Max(a: 0f, b: healthPerSecond);
            restoreImmediatelyWhenDefeated = restoreImmediatelyOnDefeat;
            defeatFeedbackDelay = Mathf.Max(a: 0f, b: defeatDelay);
        }

        public void TickRegeneration(float deltaTime)
        {
            ResolveHealth();

            if (deltaTime <= 0f || health.CurrentHealth >= health.MaximumHealth)
            {
                return;
            }

            TimeSinceDamage += deltaTime;

            if (waitingForDefeatRestore)
            {
                if (TimeSinceDamage >= defeatFeedbackDelay)
                {
                    health.RestoreToFull();
                    waitingForDefeatRestore = false;
                }

                return;
            }

            if (TimeSinceDamage < regenerationDelay)
            {
                return;
            }

            health.Restore(amount: regenerationPerSecond * deltaTime);
        }

        public void ResetStatistics()
        {
            AcceptedHitCount = 0;
            TotalDamageReceived = 0f;
        }

        private void ResolveHealth()
        {
            if (health == null)
            {
                health = GetComponent<EnemyHealth>();
            }
        }

        private void OnDamaged(EnemyDamageEvent payload)
        {
            TimeSinceDamage = 0f;
            AcceptedHitCount++;
            TotalDamageReceived += payload.Result.AppliedAmount;
        }

        private void OnDefeated(EnemyDamageEvent payload)
        {
            waitingForDefeatRestore = restoreImmediatelyWhenDefeated;
        }
    }
}
