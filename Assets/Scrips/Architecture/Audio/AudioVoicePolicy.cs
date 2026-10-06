using System;
using System.Collections.Generic;

namespace TicGame.Architecture
{
    public sealed class AudioVoicePolicy
    {
        private readonly SoundCueSO[] cues;
        private readonly double[] started;
        private readonly int[] priorities;
        private readonly Dictionary<SoundCueSO, double> lastAccepted = new();

        public AudioVoicePolicy(int capacity)
        {
            if (capacity < 1) throw new ArgumentOutOfRangeException(nameof(capacity));
            cues = new SoundCueSO[capacity];
            started = new double[capacity];
            priorities = new int[capacity];
        }

        public bool TryAcquire(SoundCueSO cue, double unscaledNow, out int slot)
        {
            slot = -1;
            if (cue == null || double.IsNaN(unscaledNow) || double.IsInfinity(unscaledNow)) return false;
            if (lastAccepted.TryGetValue(cue, out var previous) && unscaledNow - previous < cue.Cooldown) return false;
            var count = 0;
            for (var index = 0; index < cues.Length; index++)
            {
                if (cues[index] == cue) count++;
                if (cues[index] == null && slot < 0) slot = index;
            }
            if (count >= cue.MaximumInstances) { slot = -1; return false; }
            if (slot < 0)
            {
                for (var index = 0; index < cues.Length; index++)
                    if (priorities[index] > cue.Priority && (slot < 0 || started[index] < started[slot])) slot = index;
            }
            if (slot < 0) return false;
            cues[slot] = cue;
            started[slot] = unscaledNow;
            priorities[slot] = cue.Priority;
            lastAccepted[cue] = unscaledNow;
            return true;
        }

        public void Release(int slot)
        {
            if (slot >= 0 && slot < cues.Length) cues[slot] = null;
        }
    }
}
