using UnityEngine;

namespace TicGame.Architecture
{
    public sealed class EnemyKnockbackReceiver : MonoBehaviour, IKnockbackReceiver
    {
        [Tooltip("Rigidbody that receives outgoing attack impulses.")]
        [SerializeField] private Rigidbody2D body;

        [Tooltip("Optional patrol brain paused briefly so its motor does not overwrite the impulse.")]
        [SerializeField] private EnemyPatrolBrain patrolBrain;

        [Min(0f)]
        [Tooltip("Multiplier applied to requested knockback force.")]
        [SerializeField] private float resistanceMultiplier = 1f;

        [Min(0f)]
        [Tooltip("Scaled gameplay seconds that patrol movement yields to knockback.")]
        [SerializeField] private float movementSuppressionDuration = 0.15f;

        private void Awake()
        {
            if (body == null)
            {
                body = GetComponent<Rigidbody2D>();
            }

            if (patrolBrain == null)
            {
                patrolBrain = GetComponent<EnemyPatrolBrain>();
            }
        }

        public void ApplyKnockback(Vector2 direction, float force)
        {
            if (body == null || force <= 0f)
            {
                return;
            }

            var normalizedDirection = direction.sqrMagnitude > 0f
                ? direction.normalized
                : Vector2.right;
            patrolBrain?.SuppressMovement(movementSuppressionDuration);
            body.AddForce(
                force: normalizedDirection * force * resistanceMultiplier,
                mode: ForceMode2D.Impulse);
        }

        public void SetBody(Rigidbody2D value)
        {
            body = value;
        }
    }
}
