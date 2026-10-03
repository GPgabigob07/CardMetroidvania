using System;

namespace TicGame.Architecture
{
    public readonly struct WardConfiguration : IEquatable<WardConfiguration>
    {
        public WardConfiguration(float duration, float height, float forwardOffset, float intersectionTolerance)
        { Duration = duration; Height = height; ForwardOffset = forwardOffset; IntersectionTolerance = intersectionTolerance; }
        public float Duration { get; }
        public float Height { get; }
        public float ForwardOffset { get; }
        public float IntersectionTolerance { get; }
        public bool Equals(WardConfiguration other) => Duration == other.Duration && Height == other.Height
            && ForwardOffset == other.ForwardOffset && IntersectionTolerance == other.IntersectionTolerance;
        public override bool Equals(object obj) => obj is WardConfiguration other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Duration, Height, ForwardOffset, IntersectionTolerance);
    }
}
