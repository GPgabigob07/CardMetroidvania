using System;

namespace TicGame.Architecture
{
    public readonly struct CardTimeSessionSnapshot : IEquatable<CardTimeSessionSnapshot>
    {
        public CardTimeSessionSnapshot(
            CardTimeSessionState state,
            PlayerCardTimeState availableCardTime,
            PlayerCardTimeState sessionCardTime,
            float activeElapsed,
            float maximumActiveDuration,
            long activeSessionId = 0)
        {
            State = state;
            AvailableCardTime = availableCardTime;
            SessionCardTime = sessionCardTime;
            ActiveElapsed = activeElapsed;
            MaximumActiveDuration = maximumActiveDuration;
            ActiveSessionId = activeSessionId;
        }

        public CardTimeSessionState State { get; }
        public PlayerCardTimeState AvailableCardTime { get; }
        public PlayerCardTimeState SessionCardTime { get; }
        public float ActiveElapsed { get; }
        public float MaximumActiveDuration { get; }
        public long ActiveSessionId { get; }
        public float ActiveRemaining => IsActive
            ? Math.Max(
                val1: 0f,
                val2: MaximumActiveDuration - ActiveElapsed)
            : 0f;
        public bool IsAvailable => State == CardTimeSessionState.Available;
        public bool IsActive => State == CardTimeSessionState.Active;

        public bool Equals(CardTimeSessionSnapshot other)
        {
            return State == other.State
                && AvailableCardTime == other.AvailableCardTime
                && SessionCardTime == other.SessionCardTime
                && ActiveElapsed.Equals(other.ActiveElapsed)
                && MaximumActiveDuration.Equals(other.MaximumActiveDuration)
                && ActiveSessionId == other.ActiveSessionId;
        }

        public override bool Equals(object obj)
        {
            return obj is CardTimeSessionSnapshot other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (int)State;
                hashCode = (hashCode * 397) ^ (int)AvailableCardTime;
                hashCode = (hashCode * 397) ^ (int)SessionCardTime;
                hashCode = (hashCode * 397) ^ ActiveElapsed.GetHashCode();
                hashCode = (hashCode * 397) ^ MaximumActiveDuration.GetHashCode();
                hashCode = (hashCode * 397) ^ ActiveSessionId.GetHashCode();
                return hashCode;
            }
        }

        public static bool operator ==(
            CardTimeSessionSnapshot left,
            CardTimeSessionSnapshot right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(
            CardTimeSessionSnapshot left,
            CardTimeSessionSnapshot right)
        {
            return !left.Equals(right);
        }
    }
}
