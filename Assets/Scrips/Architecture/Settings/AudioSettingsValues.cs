using UnityEngine;

namespace TicGame.Architecture
{
    public sealed class AudioSettingsValues
    {
        private readonly float[] gains = { 1f, 1f, 1f, 1f, 1f };
        public static bool IsValidCategory(AudioCategory category) => (int)category >= 0 && (int)category < 5;
        public float GetGain(AudioCategory category) => IsValidCategory(category) ? gains[(int)category] : 1f;
        public bool TrySetGain(AudioCategory category, float gain)
        {
            if (!IsValidCategory(category) || !AudioVolumeMath.IsFinite(gain)) return false;
            gains[(int)category] = Mathf.Clamp01(gain);
            return true;
        }
    }
}
