using UnityEngine;

namespace TicGame.Architecture
{
    [RequireComponent(requiredComponent: typeof(Rigidbody2D))]
    public sealed class AerialSteeringMotor2D : MonoBehaviour
    {
        [Header(header: "Body")]
        [Tooltip(tooltip: "Rigidbody2D controlled by this aerial motor. Falls back to this GameObject when empty.")]
        [SerializeField] private Rigidbody2D body;

        private float authoredGravityScale;
        private bool hasCapturedAuthoredGravity;
        private bool isFlying = true;

        private void Awake()
        {
            ResolveBody();
            CaptureAuthoredGravity();
            ResumeFlight();
        }

        public void MoveTowards(Vector2 target, float maxSpeed, float acceleration, float fixedDeltaTime)
        {
            ResolveBody();
            if (!isFlying || body == null)
            {
                return;
            }

            var offset = target - body.position;
            var desiredVelocity = offset.sqrMagnitude > 0f
                ? offset.normalized * Mathf.Max(0f, maxSpeed)
                : Vector2.zero;
            var maximumVelocityChange = Mathf.Max(0f, acceleration) * Mathf.Max(0f, fixedDeltaTime);
            body.linearVelocity = Vector2.MoveTowards(body.linearVelocity, desiredVelocity, maximumVelocityChange);
        }

        public void Stop()
        {
            ResolveBody();
            if (body != null)
            {
                body.linearVelocity = Vector2.zero;
            }
        }

        public void BeginFall()
        {
            ResolveBody();
            CaptureAuthoredGravity();
            isFlying = false;
            if (body != null)
            {
                body.gravityScale = authoredGravityScale;
            }
        }

        public void ResumeFlight()
        {
            ResolveBody();
            CaptureAuthoredGravity();
            isFlying = true;
            if (body != null)
            {
                body.gravityScale = 0f;
            }
        }

        public void SetBody(Rigidbody2D value)
        {
            body = value;
            CaptureAuthoredGravity();
            if (isFlying)
            {
                body.gravityScale = 0f;
            }
        }

        private void ResolveBody()
        {
            if (body == null)
            {
                body = GetComponent<Rigidbody2D>();
            }
        }

        private void CaptureAuthoredGravity()
        {
            if (body != null && !hasCapturedAuthoredGravity)
            {
                authoredGravityScale = body.gravityScale;
                hasCapturedAuthoredGravity = true;
            }
        }
    }
}
