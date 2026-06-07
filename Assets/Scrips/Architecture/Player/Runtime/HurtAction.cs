using UnityEngine;

namespace TicGame.Architecture
{
    public sealed class HurtAction : IPlayerAction, ILocomotionOverride
    {
        private readonly Vector2 knockbackVelocity;
        private readonly float duration;
        private float elapsed;

        public HurtAction(Vector2 knockbackVelocity, float duration)
        {
            this.knockbackVelocity = knockbackVelocity;
            this.duration = Mathf.Max(a: 0f, b: duration);
        }

        public PlayerActionState State => PlayerActionState.Hurt;
        public bool IsComplete { get; private set; }

        public void Enter(PlayerContext context)
        {
            elapsed = 0f;
            IsComplete = false;
        }

        public void Tick(PlayerContext context, float deltaTime)
        {
            elapsed += deltaTime;
            IsComplete = elapsed >= duration;
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
            frame.Velocity = knockbackVelocity;
            frame.AllowHorizontalInput = false;
            frame.AllowGravity = true;
            frame.LockFacing = true;
        }
    }
}
