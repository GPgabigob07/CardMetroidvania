using System;
using UnityEngine;

namespace TicGame.Architecture
{
    public sealed class PlayerAnimationSnapshotSource : IPlayerAnimationSnapshotSource
    {
        private readonly PlayerContext context;
        private readonly float horizontalMotionThreshold;
        private readonly float verticalMotionThreshold;

        public PlayerAnimationSnapshotSource(
            PlayerContext context,
            float horizontalMotionThreshold = 0.01f,
            float verticalMotionThreshold = 0.01f)
        {
            this.context = context ?? throw new ArgumentNullException(paramName: nameof(context));
            this.horizontalMotionThreshold = Mathf.Max(a: 0f, b: horizontalMotionThreshold);
            this.verticalMotionThreshold = Mathf.Max(a: 0f, b: verticalMotionThreshold);
        }

        public PlayerAnimationSnapshot Capture(in LocomotionFrame frame)
        {
            var actionSource = context.ActionRunner.CurrentAction as IPlayerActionAnimationSource;
            var horizontalMotion = Mathf.Abs(f: frame.Velocity.x) > horizontalMotionThreshold
                ? PlayerHorizontalMotion.Moving
                : PlayerHorizontalMotion.Idle;

            return new PlayerAnimationSnapshot(
                locomotion: context.Locomotion.CurrentStateId,
                action: context.ActionRunner.CurrentState,
                actionPhase: actionSource?.AnimationPhase ?? PlayerActionPhase.Reading,
                horizontalMotion: horizontalMotion,
                verticalMotion: ResolveVerticalMotion(
                    locomotion: context.Locomotion.CurrentStateId,
                    verticalSpeed: frame.Velocity.y),
                cardTime: actionSource?.AnimationCardTime ?? PlayerCardTimeState.None,
                facingDirection: context.FacingDirection,
                verticalSpeed: frame.Velocity.y);
        }

        private PlayerVerticalMotion ResolveVerticalMotion(
            PlayerLocomotionState locomotion,
            float verticalSpeed)
        {
            if (locomotion == PlayerLocomotionState.Grounded)
            {
                return PlayerVerticalMotion.Stable;
            }

            if (verticalSpeed > verticalMotionThreshold)
            {
                return PlayerVerticalMotion.Rising;
            }

            return verticalSpeed < -verticalMotionThreshold
                ? PlayerVerticalMotion.Falling
                : PlayerVerticalMotion.Stable;
        }
    }
}
