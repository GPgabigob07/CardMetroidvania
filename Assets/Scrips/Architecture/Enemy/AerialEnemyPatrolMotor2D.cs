using UnityEngine;

namespace TicGame.Architecture
{
    [RequireComponent(requiredComponent: typeof(Rigidbody2D))]
    public sealed class AerialEnemyPatrolMotor2D : MonoBehaviour, IEnemyPatrolMotor2D
    {
        private const float FacingEpsilon = 0.0001f;

        [Header(header: "Body")]
        [Tooltip(tooltip: "Rigidbody2D moved by this patrol motor. Falls back to this GameObject when empty.")]
        [SerializeField] private Rigidbody2D body;

        public Vector2 Position => body != null ? body.position : transform.position;
        public int FacingDirection { get; private set; } = 1;

        private void Awake()
        {
            ResolveBody();
            DisableGravity();
        }

        public EnemyPatrolMoveResult MoveTowards(
            Vector2 target,
            float speed,
            float arrivalDistance,
            float fixedDeltaTime)
        {
            ResolveBody();
            if (body == null)
            {
                return EnemyPatrolMoveResult.Blocked;
            }

            DisableGravity();

            var toTarget = target - body.position;
            var distance = toTarget.magnitude;
            var arrivalThreshold = Mathf.Max(a: 0f, b: arrivalDistance);
            var stepDistance = Mathf.Max(a: 0f, b: speed) * Mathf.Max(a: 0f, b: fixedDeltaTime);
            if (distance <= arrivalThreshold || distance <= stepDistance)
            {
                body.position = target;
                Stop();
                return EnemyPatrolMoveResult.Arrived;
            }

            if (Mathf.Abs(toTarget.x) > FacingEpsilon)
            {
                SetFacing(toTarget.x > 0f ? 1 : -1);
            }

            body.linearVelocity = toTarget.normalized * Mathf.Max(a: 0f, b: speed);
            return EnemyPatrolMoveResult.Moving;
        }

        public void Stop()
        {
            ResolveBody();
            if (body != null)
            {
                body.linearVelocity = Vector2.zero;
            }
        }

        public void SetFacing(int direction)
        {
            if (direction != 0)
            {
                FacingDirection = direction > 0 ? 1 : -1;
            }
        }

        public void SetBody(Rigidbody2D value)
        {
            body = value;
            DisableGravity();
        }

        private void ResolveBody()
        {
            if (body == null)
            {
                body = GetComponent<Rigidbody2D>();
            }
        }

        private void DisableGravity()
        {
            if (body != null)
            {
                body.gravityScale = 0f;
            }
        }
    }
}
