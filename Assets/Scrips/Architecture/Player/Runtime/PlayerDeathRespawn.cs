using UnityEngine;

namespace TicGame.Architecture
{
    public sealed class PlayerDeathRespawn : MonoBehaviour
    {
        [Header(header: "Components")]
        [Tooltip(tooltip: "Health source that triggers prototype respawn when it reaches zero.")]
        [SerializeField] private SimpleHealth health;

        [Tooltip(tooltip: "Player controller reset before the transform is moved.")]
        [SerializeField] private PlayerController playerController;

        [Tooltip(tooltip: "Motor used to stop movement during respawn.")]
        [SerializeField] private PlayerMotor2D motor;

        [Header(header: "Respawn")]
        [Tooltip(tooltip: "Optional transform used as the respawn position.")]
        [SerializeField] private Transform respawnTarget;

        [Tooltip(tooltip: "Fallback world position used when no respawn target is assigned.")]
        [SerializeField] private Vector3 fallbackRespawnPosition = Vector3.zero;

        [Tooltip(tooltip: "Restores health immediately after the prototype respawn.")]
        [SerializeField] private bool restoreHealthOnRespawn = true;

        private bool isRespawning;
        private bool isSubscribed;

        private void Awake()
        {
            health ??= GetComponent<SimpleHealth>();
            playerController ??= GetComponent<PlayerController>();
            motor ??= GetComponent<PlayerMotor2D>();
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        public void Configure(
            SimpleHealth healthSource,
            PlayerController controller,
            PlayerMotor2D playerMotor,
            Transform target = null)
        {
            Unsubscribe();
            health = healthSource;
            playerController = controller;
            motor = playerMotor;
            respawnTarget = target;
            Subscribe();
        }

        private void Subscribe()
        {
            if (isSubscribed || health == null || !isActiveAndEnabled)
            {
                return;
            }

            health.Changed += HandleHealthChanged;
            isSubscribed = true;
        }

        private void Unsubscribe()
        {
            if (!isSubscribed || health == null)
            {
                return;
            }

            health.Changed -= HandleHealthChanged;
            isSubscribed = false;
        }

        private void HandleHealthChanged(SimpleHealthChanged change)
        {
            if (isRespawning || change.Current > 0f)
            {
                return;
            }

            Respawn();
        }

        private void Respawn()
        {
            isRespawning = true;
            playerController?.ResetTransientState();
            motor?.SetVelocity(Vector2.zero);
            transform.position = respawnTarget != null
                ? respawnTarget.position
                : fallbackRespawnPosition;

            if (restoreHealthOnRespawn)
            {
                health?.Initialize();
            }

            isRespawning = false;
        }
    }
}
