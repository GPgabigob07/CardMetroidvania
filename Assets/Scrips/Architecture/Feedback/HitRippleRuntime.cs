using System;
using UnityEngine;

namespace TicGame.Architecture
{
    public sealed class HitRippleRuntime
    {
        public const int Capacity = 3;
        private readonly HitRippleWave[] waves = new HitRippleWave[Capacity];
        private long sequence;
        public int ActiveCount
        {
            get
            {
                var count = 0;
                foreach (var wave in waves) if (wave.IsActive) count++;
                return count;
            }
        }
        public HitRippleWave GetWave(int slot) => waves[slot];
        public void Add(Vector2 origin, HitRippleKind kind, float travelDistance, float duration, float widthPixels)
        {
            if (!float.IsFinite(origin.x) || !float.IsFinite(origin.y) || kind == HitRippleKind.None
                || !Positive(travelDistance) || !Positive(duration) || !Positive(widthPixels)) return;
            var slot = 0;
            for (var index = 0; index < Capacity; index++)
            {
                if (!waves[index].IsActive) { slot = index; break; }
                if (waves[index].StartSequence < waves[slot].StartSequence) slot = index;
            }
            waves[slot] = new HitRippleWave(origin, kind, travelDistance, duration, widthPixels, ++sequence);
        }
        public void Tick(float unscaledDeltaTime, bool paused)
        {
            if (paused || !Positive(unscaledDeltaTime)) return;
            for (var index = 0; index < Capacity; index++)
                if (waves[index].IsActive) waves[index] = waves[index].Advance(unscaledDeltaTime);
        }
        public void Clear() => Array.Clear(waves, 0, waves.Length);
        private static bool Positive(float value) => float.IsFinite(value) && value > 0;
    }
}
