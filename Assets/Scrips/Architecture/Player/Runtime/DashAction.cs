using UnityEngine;

namespace TicGame.Architecture
{
    public sealed class DashAction : IPlayerAction, ILocomotionOverride
    {
        private float elapsed;

        public PlayerActionState State => PlayerActionState.Dash;
        public bool IsComplete { get; private set; }

        public void Enter(PlayerContext context)
        {
            elapsed = 0f;
            IsComplete = false;
        }

        public void Tick(PlayerContext context, float deltaTime)
        {
            elapsed += deltaTime;

            if (elapsed >= context.DashDefinition.Duration)
            {
                IsComplete = true;
            }
        }

        public void FixedTick(PlayerContext context, float fixedDeltaTime)
        {
        }

        public void Exit(PlayerContext context)
        {
        }

        public void ModifyLocomotionFrame(
            ref LocomotionFrame frame,
            PlayerContext context,
            float fixedDeltaTime)
        {
            frame.Velocity = new Vector2(x: context.FacingDirection * context.DashDefinition.Speed, y: 0f);
            frame.GravityScale = 0f;
            frame.AllowHorizontalInput = false;
            frame.AllowGravity = false;
            frame.LockFacing = true;
        }
    }
}
