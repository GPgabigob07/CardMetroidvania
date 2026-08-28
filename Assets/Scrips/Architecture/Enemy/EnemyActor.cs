using System;
using UnityEngine;

namespace TicGame.Architecture
{
    [RequireComponent(requiredComponent: typeof(EnemyHealth))]
    public sealed class EnemyActor : MonoBehaviour, IIdentified
    {
        [Header(header: "Definition")]
        [Tooltip(tooltip: "Authored identity and baseline health for this enemy instance.")]
        [SerializeField] private EnemyDefinitionSO definition;

        [Header(header: "Components")]
        [Tooltip(tooltip: "Health capability coordinated by this actor. Resolved from this GameObject when omitted.")]
        [SerializeField] private EnemyHealth health;

        public event Action<EnemyDamageEvent> Damaged;
        public event Action<EnemyDamageEvent> Defeated;
        public event Action<EnemyHealthChanged> Restored;

        public EnemyDefinitionSO Definition => definition;
        public EnemyHealth Health => health;
        public string Id => definition != null && !string.IsNullOrWhiteSpace(value: definition.Id)
            ? definition.Id
            : gameObject.name;
        public string DisplayName => definition != null && !string.IsNullOrWhiteSpace(value: definition.DisplayName)
            ? definition.DisplayName
            : Id;
        public bool IsDefeated => health != null && health.IsDefeated;
        public bool IsOperational => IsInitialized && !IsDefeated;
        public bool IsInitialized { get; private set; }

        private void Awake()
        {
            ResolveHealth();

            if (definition != null)
            {
                Initialize();
            }
        }

        private void OnDestroy()
        {
            UnsubscribeFromHealth();
        }

        public void Initialize()
        {
            ResolveHealth();

            if (definition == null)
            {
                Debug.LogError(message: $"{nameof(EnemyActor)} on '{name}' requires an {nameof(EnemyDefinitionSO)}.", context: this);
                IsInitialized = false;
                return;
            }

            if (health == null)
            {
                Debug.LogError(message: $"{nameof(EnemyActor)} on '{name}' requires an {nameof(EnemyHealth)}.", context: this);
                IsInitialized = false;
                return;
            }

            UnsubscribeFromHealth();
            health.Initialize(maximumHealth: definition.MaxHealth);
            SubscribeToHealth();
            IsInitialized = true;
        }

        public void ResetActor()
        {
            if (!IsInitialized)
            {
                Initialize();
                return;
            }

            health.RestoreToFull();
        }

        public void SetDefinition(EnemyDefinitionSO value)
        {
            definition = value;
        }

        private void ResolveHealth()
        {
            if (health == null)
            {
                health = GetComponent<EnemyHealth>();
            }
        }

        private void SubscribeToHealth()
        {
            health.Damaged += OnDamaged;
            health.Defeated += OnDefeated;
            health.Restored += OnRestored;
        }

        private void UnsubscribeFromHealth()
        {
            if (health == null)
            {
                return;
            }

            health.Damaged -= OnDamaged;
            health.Defeated -= OnDefeated;
            health.Restored -= OnRestored;
        }

        private void OnDamaged(EnemyDamageEvent payload)
        {
            Damaged?.Invoke(payload);
        }

        private void OnDefeated(EnemyDamageEvent payload)
        {
            Defeated?.Invoke(payload);
        }

        private void OnRestored(EnemyHealthChanged payload)
        {
            Restored?.Invoke(payload);
        }
    }
}
