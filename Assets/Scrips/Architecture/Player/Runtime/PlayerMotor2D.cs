using UnityEngine;

namespace TicGame.Architecture
{
    public sealed class PlayerMotor2D : MonoBehaviour
    {
        [Header(header: "Body")]
        [Tooltip(tooltip: "Rigidbody2D moved by the player controller. Falls back to this GameObject when empty.")]
        [SerializeField] private Rigidbody2D body;

        public Vector2 Velocity => body != null ? body.linearVelocity : Vector2.zero;
        public int FacingDirection { get; private set; } = 1;

        private void Awake()
        {
            if (body == null)
            {
                body = GetComponent<Rigidbody2D>();
            }
        }

        public void ApplyFrame(in LocomotionFrame frame)
        {
            if (body == null)
            {
                return;
            }

            body.gravityScale = frame.AllowGravity ? frame.GravityScale : 0f;
            body.linearVelocity = frame.Velocity;
        }

        public void SetBody(Rigidbody2D nextBody)
        {
            body = nextBody;
        }

        public void SetVelocity(Vector2 velocity)
        {
            if (body != null)
            {
                body.linearVelocity = velocity;
            }
        }

        public void SetFacing(int direction)
        {
            if (direction == 0)
            {
                return;
            }

            FacingDirection = direction > 0 ? 1 : -1;
        }
    }
}
