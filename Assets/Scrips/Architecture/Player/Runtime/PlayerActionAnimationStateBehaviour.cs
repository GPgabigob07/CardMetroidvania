using UnityEngine;
using UnityEngine.Serialization;

namespace TicGame.Architecture
{
    public sealed class PlayerActionAnimationStateBehaviour : StateMachineBehaviour
    {
        [Header(header: "Action Frame")]
        [Tooltip(tooltip: "Gameplay action allowed to consume frames from this Animator state.")]
        [SerializeField]
        private PlayerActionState actionState = PlayerActionState.Attack1;

        [Tooltip(tooltip: "Action phase exposed to the active player action while this animation state is active.")]
        [SerializeField]
        private PlayerActionPhase phase = PlayerActionPhase.Reading;

        [Tooltip(tooltip: "Card Time state exposed to the active player action while this animation state is active.")]
        [SerializeField]
        private PlayerCardTimeState cardTimeState = PlayerCardTimeState.None;

        [Header(header: "Follow-up Window")]
        [FormerlySerializedAs(oldName: "allowChain")]
        [Tooltip(tooltip: "Whether this animation state accepts one buffered follow-up attack.")]
        [SerializeField]
        private bool supportsChainBuffer;

        [Range(min: 0f, max: 1f)]
        [Tooltip(tooltip: "Normalized state progress at which follow-up buffering begins.")]
        [SerializeField]
        private float chainBufferStartNormalized = 0.5f;

        [Range(min: 0f, max: 1f)]
        [Tooltip(tooltip: "Normalized state progress at which follow-up buffering ends.")]
        [SerializeField]
        private float chainBufferEndNormalized = 1f;

        [Tooltip(tooltip: "Whether this state may commit a buffered follow-up attack.")]
        [SerializeField]
        private bool supportsFollowUpCommit;

        [Range(min: 0f, max: 1f)]
        [Tooltip(tooltip: "Normalized state progress at which a buffered follow-up may begin.")]
        [SerializeField]
        private float followUpCommitStartNormalized = 0.5f;

        [Header(header: "Post-Recovery Timing")]
        [Min(min: 0f)]
        [Tooltip(tooltip: "Seconds after this state completes during which attack input continues the combo.")]
        [SerializeField]
        private float postRecoveryBufferGraceDuration;

        [Min(min: 0f)]
        [Tooltip(tooltip: "Seconds after this state completes before attack input restarts the sequence at Attack1.")]
        [SerializeField]
        private float sequenceRestartCooldown;

        [Tooltip(tooltip: "Whether entering or updating this animation state should end the current action.")]
        [SerializeField]
        private bool endAction;

        [Tooltip(tooltip: "Whether this state should end the current action when its first playback completes.")]
        [SerializeField]
        private bool endActionAtCompletion;

        [Tooltip(
            tooltip: "Whether the frame should be refreshed every animator update, not only when entering the state.")]
        [SerializeField]
        private bool applyEveryUpdate = true;

        public override void OnStateEnter(
            Animator animator,
            AnimatorStateInfo stateInfo,
            int layerIndex
        )
        {
            Apply(
                animator: animator,
                normalizedTime: stateInfo.normalizedTime,
                shouldEndAction: endAction);
        }

        public override void OnStateUpdate(
            Animator animator,
            AnimatorStateInfo stateInfo,
            int layerIndex
        )
        {
            if (applyEveryUpdate)
            {
                Apply(
                    animator: animator,
                    normalizedTime: stateInfo.normalizedTime,
                    shouldEndAction: endAction
                        || endActionAtCompletion && stateInfo.normalizedTime >= 1f);
            }
        }

        private void Apply(Animator animator, float normalizedTime, bool shouldEndAction)
        {
            var controller = animator.GetComponentInParent<PlayerController>();
            if (controller == null)
            {
                return;
            }

            controller.ApplyAnimationFrame(
                actionState: actionState,
                frame: BuildFrame(
                    normalizedTime: normalizedTime,
                    endActionOverride: shouldEndAction));
        }

        public PlayerActionFrame BuildFrame()
        {
            return BuildFrame(normalizedTime: 0f);
        }

        public PlayerActionFrame BuildFrame(float normalizedTime)
        {
            return BuildFrame(normalizedTime: normalizedTime, endActionOverride: endAction);
        }

        private PlayerActionFrame BuildFrame(float normalizedTime, bool endActionOverride)
        {
            var normalizedPhaseTime = Mathf.Clamp01(value: normalizedTime);
            var canBufferFollowUp = supportsChainBuffer
                && normalizedPhaseTime >= chainBufferStartNormalized
                && normalizedPhaseTime <= chainBufferEndNormalized;
            var canCommitFollowUp = supportsFollowUpCommit
                && normalizedPhaseTime >= followUpCommitStartNormalized;

            return new PlayerActionFrame(
                phase: phase,
                cardTimeState: cardTimeState,
                normalizedPhaseTime: normalizedPhaseTime,
                canBufferFollowUp: canBufferFollowUp,
                canCommitFollowUp: canCommitFollowUp,
                postRecoveryBufferGraceDuration: postRecoveryBufferGraceDuration,
                sequenceRestartCooldown: sequenceRestartCooldown,
                endAction: endActionOverride,
                hasAnimatorAuthority: true);
        }
    }
}
