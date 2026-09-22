using UnityEngine;

namespace TicGame.Architecture
{
    internal sealed class CardTimeActiveSession
    {
        public CardTimeActiveSession(
            long id,
            PlayerCardTimeState category,
            float maximumDuration)
        {
            Id = id;
            Category = category;
            MaximumDuration = maximumDuration;
        }

        public long Id { get; }
        public PlayerCardTimeState Category { get; }
        public float Elapsed { get; private set; }
        public float MaximumDuration { get; }

        public void Tick(float unscaledDeltaTime)
        {
            Elapsed = Mathf.Min(
                a: MaximumDuration,
                b: Elapsed + unscaledDeltaTime);
        }
    }
}
