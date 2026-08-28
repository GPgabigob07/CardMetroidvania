using UnityEngine;

namespace TicGame.Architecture
{
    [RequireComponent(requiredComponent: typeof(Rigidbody2D))]
    public sealed class GroundedEnemyPatrolMotor2D : MonoBehaviour, IEnemyPatrolMotor2D
    {
        [Header(header: "Body")]
        [Tooltip(tooltip: "Rigidbody2D moved by this patrol motor. Falls back to this GameObject when empty.")]
        [SerializeField] private Rigidbody2D body;

        [Header(header: "Environment")]
        [Tooltip(tooltip: "Layers treated as solid ground and walls by the patrol probes.")]
        [SerializeField] private LayerMask environmentLayer = ~0;

        [Tooltip(tooltip: "Local probe offset used to require supporting ground ahead. Authored for right-facing movement.")]
        [SerializeField] private Vector2 ledgeProbeOffset = new Vector2(x: 0.45f, y: -0.55f);

        [Tooltip(tooltip: "Local probe offset used to detect a wall ahead. Authored for right-facing movement.")]
        [SerializeField] private Vector2 wallProbeOffset = new Vector2(x: 0.45f, y: 0f);

        [Min(min: 0.01f)]
        [Tooltip(tooltip: "Radius used by wall and ledge overlap probes.")]
        [SerializeField] private float probeRadius = 0.08f;

        public Vector2 Position => body != null ? body.position : transform.position;
        public int FacingDirection { get; private set; } = 1;

        private void Awake()
        {
            ResolveBody();
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

            var deltaX = target.x - body.position.x;
            if (Mathf.Abs(deltaX) <= Mathf.Max(a: 0f, b: arrivalDistance))
            {
                Stop();
                return EnemyPatrolMoveResult.Arrived;
            }

            var direction = deltaX > 0f ? 1 : -1;
            if (HasWallAhead(direction) || !HasGroundAhead(direction))
            {
                Stop();
                return EnemyPatrolMoveResult.Blocked;
            }

            SetFacing(direction);
            body.linearVelocity = new Vector2(
                x: direction * Mathf.Max(a: 0f, b: speed),
                y: body.linearVelocity.y);
            return EnemyPatrolMoveResult.Moving;
        }

        public void Stop()
        {
            ResolveBody();
            if (body != null)
            {
                body.linearVelocity = new Vector2(x: 0f, y: body.linearVelocity.y);
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
        }

        public void ConfigureProbes(
            LayerMask layer,
            Vector2 ledgeOffset,
            Vector2 wallOffset,
            float radius)
        {
            environmentLayer = layer;
            ledgeProbeOffset = ledgeOffset;
            wallProbeOffset = wallOffset;
            probeRadius = Mathf.Max(a: 0.01f, b: radius);
        }

        private bool HasWallAhead(int direction)
        {
            return Physics2D.OverlapCircle(
                point: ResolveProbePosition(wallProbeOffset, direction),
                radius: probeRadius,
                layerMask: environmentLayer) != null;
        }

        private bool HasGroundAhead(int direction)
        {
            return Physics2D.OverlapCircle(
                point: ResolveProbePosition(ledgeProbeOffset, direction),
                radius: probeRadius,
                layerMask: environmentLayer) != null;
        }

        private Vector2 ResolveProbePosition(Vector2 offset, int direction)
        {
            var origin = body != null ? body.position : (Vector2)transform.position;
            return origin + new Vector2(x: offset.x * direction, y: offset.y);
        }

        private void ResolveBody()
        {
            if (body == null)
            {
                body = GetComponent<Rigidbody2D>();
            }
        }
    }
}
