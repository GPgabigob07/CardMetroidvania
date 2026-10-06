using UnityEngine;

namespace TicGame.Architecture
{
    public static class AudioVolumeMath
    {
        public static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

        public static float ToDecibels(float normalizedGain)
        {
            if (!IsFinite(normalizedGain)) return -80f;
            var gain = Mathf.Clamp01(normalizedGain);
            return gain <= 0f ? -80f : Mathf.Max(-80f, 20f * Mathf.Log10(gain));
        }
    }
}
