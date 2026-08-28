using UnityEngine;

namespace TicGame.Architecture
{
    public sealed class GroundedLocomotionState : IPlayerLocomotionState
    {
        public PlayerLocomotionState Id => PlayerLocomotionState.Grounded;

        public bool CanEnter(PlayerContext context)
        {
            return context.Sensors.IsGrounded;
        }

        public void Enter(PlayerContext context)
        {
            context.Locomotion.ResetCoyoteTime(context: context);
            context.ExtraJumpRuntime?.Clear();
        }

        public void Tick(PlayerContext context, float deltaTime)
        {
        }

        public void FixedTick(PlayerContext context, float fixedDeltaTime)
        {
            context.Locomotion.ResetCoyoteTime(context: context);
        }

        public LocomotionFrame BuildFrame(PlayerContext context, float fixedDeltaTime)
        {
            var config = context.MovementConfig;
            var velocity = context.Motor.Velocity;
            var targetSpeed = context.Input.Move.x * config.MaxHorizontalSpeed;
            var rate = Mathf.Abs(f: targetSpeed) > 0.01f ? config.GroundAcceleration : config.GroundDeceleration;

            velocity.x = Mathf.MoveTowards(current: velocity.x, target: targetSpeed, maxDelta: rate * fixedDeltaTime);

            if (velocity.y < 0f)
            {
                velocity.y = 0f;
            }

            if (context.Locomotion.HasBufferedJump)
            {
                velocity.y = config.JumpVelocity;
                context.Locomotion.ConsumeJumpBuffer();
                context.Locomotion.ForceState(context: context, stateId: PlayerLocomotionState.Airborne);
            }

            return new LocomotionFrame(
                velocity: velocity,
                gravityScale: config.RiseGravityScale,
                allowHorizontalInput: true,
                allowGravity: velocity.y > 0f,
                lockFacing: false);
        }

        public void Exit(PlayerContext context)
        {
        }
    }
}
