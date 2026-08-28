using UnityEngine;

namespace TicGame.Architecture
{
    [CreateAssetMenu(menuName = "TIC/Player/Attack Definition", fileName = "PlayerAttack")]
    public sealed class PlayerAttackDefinitionSO : ScriptableObject
    {
        [Header(header: "Timing")]
        [Min(min: 0f)]
        [Tooltip(tooltip: "Fallback reading/startup duration used without animator-authored flags.")]
        [SerializeField] private float readingDuration = 0.08f;

        [Min(min: 0f)]
        [Tooltip(tooltip: "Fallback execution/active duration used without animator-authored flags.")]
        [SerializeField] private float executionDuration = 0.12f;

        [Min(min: 0f)]
        [Tooltip(tooltip: "Fallback recovery duration used without animator-authored flags.")]
        [SerializeField] private float recoveryDuration = 0.18f;

        [Min(min: 0f)]
        [Tooltip(tooltip: "Fallback seconds after Recovery during which attack input continues the combo.")]
        [SerializeField] private float postRecoveryBufferGraceDuration = 0.5f;

        [Min(min: 0f)]
        [Tooltip(tooltip: "Fallback seconds after Recovery before attack input restarts at Attack1.")]
        [SerializeField] private float sequenceRestartCooldown = 0.5f;

        [Header(header: "Ground Movement")]
        [Range(min: 0f, max: 1f)]
        [Tooltip(tooltip: "Horizontal velocity multiplier while attacking on the ground.")]
        [SerializeField] private float groundedHorizontalMultiplier = 0.35f;

        [Tooltip(tooltip: "Horizontal nudge applied during execution while grounded.")]
        [SerializeField] private float groundedExecutionNudge = 1.5f;

        [Header(header: "Air Movement")]
        [Range(min: 0f, max: 1f)]
        [Tooltip(tooltip: "Gravity multiplier while an aerial attack is executing.")]
        [SerializeField] private float airborneExecutionGravityMultiplier = 0.25f;

        [Tooltip(tooltip: "Minimum upward velocity preserved during aerial attack execution.")]
        [SerializeField] private float airborneExecutionMinLift = 1f;

        [Tooltip(tooltip: "Horizontal nudge applied during execution while airborne.")]
        [SerializeField] private float airborneExecutionNudge = 1.2f;

        public float ReadingDuration => readingDuration;
        public float ExecutionDuration => executionDuration;
        public float RecoveryDuration => recoveryDuration;
        public float PostRecoveryBufferGraceDuration => postRecoveryBufferGraceDuration;
        public float SequenceRestartCooldown => sequenceRestartCooldown;
        public float GroundedHorizontalMultiplier => groundedHorizontalMultiplier;
        public float GroundedExecutionNudge => groundedExecutionNudge;
        public float AirborneExecutionGravityMultiplier => airborneExecutionGravityMultiplier;
        public float AirborneExecutionMinLift => airborneExecutionMinLift;
        public float AirborneExecutionNudge => airborneExecutionNudge;
        public float TotalDuration => readingDuration + executionDuration + recoveryDuration;
    }
}
