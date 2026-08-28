using System;

namespace TicGame.Architecture
{
    public readonly struct PlayerAnimationSnapshot : IEquatable<PlayerAnimationSnapshot>
    {
        public PlayerAnimationSnapshot(
            PlayerLocomotionState locomotion,
            PlayerActionState action,
            PlayerActionPhase actionPhase,
            PlayerHorizontalMotion horizontalMotion,
            PlayerVerticalMotion verticalMotion,
            PlayerCardTimeState cardTime,
            int facingDirection,
            float verticalSpeed)
        {
            Locomotion = locomotion;
            Action = action;
            ActionPhase = actionPhase;
            HorizontalMotion = horizontalMotion;
            VerticalMotion = verticalMotion;
            CardTime = cardTime;
            FacingDirection = facingDirection < 0 ? -1 : 1;
            VerticalSpeed = verticalSpeed;
        }

        public PlayerLocomotionState Locomotion { get; }
        public PlayerActionState Action { get; }
        public PlayerActionPhase ActionPhase { get; }
        public PlayerHorizontalMotion HorizontalMotion { get; }
        public PlayerVerticalMotion VerticalMotion { get; }
        public PlayerCardTimeState CardTime { get; }
        public int FacingDirection { get; }
        public float VerticalSpeed { get; }

        public bool Equals(PlayerAnimationSnapshot other)
        {
            return Locomotion == other.Locomotion
                && Action == other.Action
                && ActionPhase == other.ActionPhase
                && HorizontalMotion == other.HorizontalMotion
                && VerticalMotion == other.VerticalMotion
                && CardTime == other.CardTime
                && FacingDirection == other.FacingDirection;
        }

        public override bool Equals(object obj)
        {
            return obj is PlayerAnimationSnapshot other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (int)Locomotion;
                hashCode = (hashCode * 397) ^ (int)Action;
                hashCode = (hashCode * 397) ^ (int)ActionPhase;
                hashCode = (hashCode * 397) ^ (int)HorizontalMotion;
                hashCode = (hashCode * 397) ^ (int)VerticalMotion;
                hashCode = (hashCode * 397) ^ (int)CardTime;
                hashCode = (hashCode * 397) ^ FacingDirection;
                return hashCode;
            }
        }

        public static bool operator ==(PlayerAnimationSnapshot left, PlayerAnimationSnapshot right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(PlayerAnimationSnapshot left, PlayerAnimationSnapshot right)
        {
            return !left.Equals(right);
        }
    }
}
