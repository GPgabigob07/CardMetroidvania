using UnityEngine;

namespace TicGame.Architecture
{
    public sealed class PlayerPrefsSettingsStore : ISettingsStore
    {
        public bool TryReadFloat(string key, out float value)
        {
            value = PlayerPrefs.GetFloat(key, 1f);
            return PlayerPrefs.HasKey(key);
        }
        public void WriteFloat(string key, float value) => PlayerPrefs.SetFloat(key, value);
        public bool Flush() { PlayerPrefs.Save(); return true; }
    }
}
