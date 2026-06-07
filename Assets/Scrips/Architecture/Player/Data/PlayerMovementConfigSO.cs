using UnityEngine;

namespace TicGame.Architecture
{
    [CreateAssetMenu(menuName = "TIC/Player/Movement Config", fileName = "PlayerMovementConfig")]
    public sealed class PlayerMovementConfigSO : ScriptableObject
    {
        [Header(header: "Horizontal")]
        [Min(min: 0f)]
        [Tooltip(tooltip: "Maximum horizontal movement speed.")]
        [SerializeField] private float maxHorizontalSpeed = 8f;

        [Min(min: 0f)]
        [Tooltip(tooltip: "Horizontal acceleration while grounded.")]
        [SerializeField] private float groundAcceleration = 80f;

        [Min(min: 0f)]
        [Tooltip(tooltip: "Horizontal deceleration while grounded and no input is held.")]
        [SerializeField] private float groundDeceleration = 90f;

        [Range(min: 0f, max: 1f)]
        [Tooltip(tooltip: "Fraction of ground acceleration used while airborne.")]
        [SerializeField] private float airControlMultiplier = 0.65f;

        [Header(header: "Jump")]
        [Min(min: 0f)]
        [Tooltip(tooltip: "Vertical velocity applied when a jump is executed.")]
        [SerializeField] private float jumpVelocity = 14f;

        [Min(min: 0f)]
        [Tooltip(tooltip: "Grace time after leaving ground where jump can still be consumed.")]
        [SerializeField] private float coyoteTime = 0.1f;

        [Min(min: 0f)]
        [Tooltip(tooltip: "Grace time before landing where a pressed jump is buffered.")]
        [SerializeField] private float jumpBufferTime = 0.1f;

        [Header(header: "Gravity")]
        [Min(min: 0f)]
        [Tooltip(tooltip: "Gravity scale used while rising and jump is held.")]
        [SerializeField] private float riseGravityScale = 3f;

        [Min(min: 0f)]
        [Tooltip(tooltip: "Gravity scale used while falling.")]
        [SerializeField] private float fallGravityScale = 5f;

        [Min(min: 0f)]
        [Tooltip(tooltip: "Gravity scale used after jump is released during upward movement.")]
        [SerializeField] private float jumpCutGravityScale = 7f;

        [Min(min: 0f)]
        [Tooltip(tooltip: "Maximum downward velocity.")]
        [SerializeField] private float maxFallSpeed = 22f;

        public float MaxHorizontalSpeed => maxHorizontalSpeed;
        public float GroundAcceleration => groundAcceleration;
        public float GroundDeceleration => groundDeceleration;
        public float AirControlMultiplier => airControlMultiplier;
        public float JumpVelocity => jumpVelocity;
        public float CoyoteTime => coyoteTime;
        public float JumpBufferTime => jumpBufferTime;
        public float RiseGravityScale => riseGravityScale;
        public float FallGravityScale => fallGravityScale;
        public float JumpCutGravityScale => jumpCutGravityScale;
        public float MaxFallSpeed => maxFallSpeed;
    }
}
