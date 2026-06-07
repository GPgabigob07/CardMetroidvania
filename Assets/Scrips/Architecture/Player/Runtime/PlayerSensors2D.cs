using UnityEngine;

namespace TicGame.Architecture
{
    public sealed class PlayerSensors2D : MonoBehaviour
    {
        [Header(header: "Ground")]
        [Tooltip(tooltip: "Point used to test whether the player is touching ground. Falls back to this transform when empty.")]
        [SerializeField] private Transform groundCheck;

        [Min(min: 0.01f)]
        [Tooltip(tooltip: "Radius used by the ground overlap check.")]
        [SerializeField] private float groundCheckRadius = 0.12f;

        [Tooltip(tooltip: "Layers considered ground by the player controller.")]
        [SerializeField] private LayerMask groundLayer = ~0;

        private bool useManualGrounded;
        private bool manualGrounded;

        public bool IsGrounded { get; private set; }

        public void Refresh()
        {
            if (useManualGrounded)
            {
                IsGrounded = manualGrounded;
                return;
            }

            var origin = groundCheck != null ? groundCheck.position : transform.position;
            IsGrounded = Physics2D.OverlapCircle(point: origin, radius: groundCheckRadius, layerMask: groundLayer) != null;
        }

        public void SetManualGrounded(bool enabled, bool grounded)
        {
            useManualGrounded = enabled;
            manualGrounded = grounded;
            IsGrounded = grounded;
        }
    }
}
