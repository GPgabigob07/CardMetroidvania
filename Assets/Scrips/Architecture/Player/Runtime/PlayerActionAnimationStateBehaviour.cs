using UnityEngine;

namespace TicGame.Architecture
{
    public sealed class PlayerActionAnimationStateBehaviour : StateMachineBehaviour
    {
        [Header(header: "Action Frame")]
        [Tooltip(tooltip: "Action phase exposed to the active player action while this animation state is active.")]
        [SerializeField]
        private PlayerActionPhase phase = PlayerActionPhase.Reading;

        [Tooltip(tooltip: "Card Time state exposed to the active player action while this animation state is active.")]
        [SerializeField]
        private PlayerCardTimeState cardTimeState = PlayerCardTimeState.None;

        [Tooltip(tooltip: "Whether this animation state allows chaining to another action.")] [SerializeField]
        private bool allowChain;

        [Tooltip(tooltip: "Whether entering or updating this animation state should end the current action.")]
        [SerializeField]
        private bool endAction;

        [Tooltip(
            tooltip: "Whether the frame should be refreshed every animator update, not only when entering the state.")]
        [SerializeField]
        private bool applyEveryUpdate = true;

        public override void OnStateEnter(
            Animator animator,
            AnimatorStateInfo stateInfo,
            int layerIndex
        ) {
            Apply(animator: animator);
        }

        public override void OnStateUpdate(
            Animator animator,
            AnimatorStateInfo stateInfo,
            int layerIndex
        ) {
            if (applyEveryUpdate) {
                Apply(animator: animator);
            }
        }

        private void Apply(
            Animator animator
        ) {
            var controller = animator.GetComponentInParent<PlayerController>();
            if (controller == null) {
                return;
            }

            controller.ApplyAnimationFrame(frame: BuildFrame());
        }

        public PlayerActionFrame BuildFrame() {
            return new PlayerActionFrame(
                phase: phase,
                cardTimeState: cardTimeState,
                allowChain: allowChain,
                endAction: endAction,
                hasAnimatorAuthority: true);
        }
    }
}