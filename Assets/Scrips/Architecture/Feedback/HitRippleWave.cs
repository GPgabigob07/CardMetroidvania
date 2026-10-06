using UnityEngine;

namespace TicGame.Architecture
{
    public readonly struct HitRippleWave
    {
        public HitRippleWave(Vector2 origin, HitRippleKind kind, float travelDistance,
            float duration, float widthPixels, long startSequence, float age = 0)
        {
            Origin = origin;
            Kind = kind;
            TravelDistance = travelDistance;
            Duration = duration;
            WidthPixels = widthPixels;
            StartSequence = startSequence;
            Age = age;
        }
        public Vector2 Origin { get; }
        public HitRippleKind Kind { get; }
        public float TravelDistance { get; }
        public float Duration { get; }
        public float WidthPixels { get; }
        public long StartSequence { get; }
        public float Age { get; }
        public float Progress => Duration > 0 ? Mathf.Clamp01(Age / Duration) : 1;
        public float Radius => Progress * TravelDistance;
        public bool IsActive => Kind != HitRippleKind.None && Age < Duration;
        public HitRippleWave Advance(float delta) => new(Origin, Kind, TravelDistance, Duration, WidthPixels, StartSequence, Age + delta);
    }
}
