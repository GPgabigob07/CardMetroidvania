using UnityEngine;

namespace TicGame.Architecture
{
    public sealed class AirborneLocomotionState : IPlayerLocomotionState
    {
        public PlayerLocomotionState Id => PlayerLocomotionState.Airborne;

        public bool CanEnter(PlayerContext context)
        {
            return true;
        }

        public void Enter(PlayerContext context)
        {
        }

        public void Tick(PlayerContext context, float deltaTime)
        {
        }

        public void FixedTick(PlayerContext context, float fixedDeltaTime)
        {
        }

        public LocomotionFrame BuildFrame(PlayerContext context, float fixedDeltaTime)
        {
            var config = context.MovementConfig;
            var velocity = context.Motor.Velocity;
            var targetSpeed = context.Input.Move.x * config.MaxHorizontalSpeed;
            var acceleration = config.GroundAcceleration * config.AirControlMultiplier;

            velocity.x = Mathf.MoveTowards(current: velocity.x, target: targetSpeed, maxDelta: acceleration * fixedDeltaTime);

            if (context.Locomotion.HasBufferedJump && context.Locomotion.HasCoyoteJump)
            {
                velocity.y = config.JumpVelocity;
                context.Locomotion.ConsumeJumpBuffer();
            }

            var gravityScale = ResolveGravityScale(context: context, velocity: velocity);
            velocity.y = Mathf.Max(a: velocity.y, b: -config.MaxFallSpeed);

            return new LocomotionFrame(
                velocity: velocity,
                gravityScale: gravityScale,
                allowHorizontalInput: true,
                allowGravity: true,
                lockFacing: false);
        }

        public void Exit(PlayerContext context)
        {
        }

        private static float ResolveGravityScale(PlayerContext context, Vector2 velocity)
        {
            var config = context.MovementConfig;

            if (velocity.y > 0f && context.Input.JumpHeld)
            {
                return config.RiseGravityScale;
            }

            if (velocity.y > 0f && !context.Input.JumpHeld)
            {
                return config.JumpCutGravityScale;
            }

            return config.FallGravityScale;
        }
    }
}
