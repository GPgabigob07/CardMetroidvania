using System;
using UnityEngine;

namespace TicGame.Architecture
{
    [DisallowMultipleComponent]
    public sealed class UserSettingsService : MonoBehaviour, IGameplayModule, IUserSettingsService
    {
        [Header("Persistent Dependencies")]
        [Tooltip("Audio settings module on the same persistent services prefab.")]
        [SerializeField] private MonoBehaviour audioSettingsSource;
        private static readonly string[] Keys =
        {
            "TIC.Settings.Audio.v1.Master", "TIC.Settings.Audio.v1.Sfx", "TIC.Settings.Audio.v1.Ui",
            "TIC.Settings.Audio.v1.Music", "TIC.Settings.Audio.v1.Ambience"
        };
        private AudioSettingsValues values = new();
        private ISettingsStore store;
        private IAudioSettingsService audio;
        public bool IsInitialized { get; private set; }
        public bool IsDirty { get; private set; }
        public event Action Changed;

        public void Configure(IAudioSettingsService audioService, ISettingsStore settingsStore)
        {
            if (IsInitialized) throw new InvalidOperationException("Configure settings before initialization.");
            audio = audioService;
            store = settingsStore;
        }

        public void Initialize()
        {
            if (IsInitialized) return;
            store ??= new PlayerPrefsSettingsStore();
            audio ??= audioSettingsSource as IAudioSettingsService;
            // Keep unsaved values if a previous shutdown flush failed.
            if (!IsDirty) Load();
            IsInitialized = true;
            if (audio != null)
            {
                audio.Ready += HandleAudioReady;
                if (audio.IsReady) ApplyAudioSettings();
            }
            Changed?.Invoke();
        }

        private void Load()
        {
            values = new AudioSettingsValues();
            foreach (AudioCategory category in Enum.GetValues(typeof(AudioCategory)))
            {
                try
                {
                    if (store.TryReadFloat(Keys[(int)category], out var gain)) values.TrySetGain(category, gain);
                }
                catch (Exception exception) { Debug.LogWarning($"Could not load {category} audio preference: {exception.Message}", this); }
            }
        }

        public float GetAudioVolume(AudioCategory category) => values.GetGain(category);

        public bool TrySetAudioVolume(AudioCategory category, float gain)
        {
            if (!IsInitialized || !AudioSettingsValues.IsValidCategory(category) || !AudioVolumeMath.IsFinite(gain)) return false;
            var previous = values.GetGain(category);
            values.TrySetGain(category, gain);
            if (previous == values.GetGain(category)) return true;
            IsDirty = true;
            if (audio?.IsReady == true) ApplyCategory(category);
            Changed?.Invoke();
            return true;
        }

        public void ResetAudioDefaults()
        {
            if (!IsInitialized) return;
            var changed = false;
            foreach (AudioCategory category in Enum.GetValues(typeof(AudioCategory)))
            {
                if (values.GetGain(category) != 1f) changed = true;
                values.TrySetGain(category, 1f);
            }
            if (!changed) return;
            IsDirty = true;
            if (audio?.IsReady == true) ApplyAudioSettings();
            Changed?.Invoke();
        }

        public bool Save()
        {
            if (!IsDirty) return true;
            try
            {
                foreach (AudioCategory category in Enum.GetValues(typeof(AudioCategory))) store.WriteFloat(Keys[(int)category], values.GetGain(category));
                if (!store.Flush()) { Debug.LogWarning("Audio preferences could not be saved; changes remain pending for retry.", this); return false; }
                IsDirty = false;
                return true;
            }
            catch (Exception exception) { Debug.LogWarning($"Audio preferences could not be saved; changes remain pending: {exception.Message}", this); return false; }
        }

        public void Shutdown()
        {
            if (!IsInitialized) return;
            if (audio != null) audio.Ready -= HandleAudioReady;
            Save();
            IsInitialized = false;
        }

        private void HandleAudioReady() { if (IsInitialized) ApplyAudioSettings(); }

        public bool ApplyAudioSettings()
        {
            if (!IsInitialized || audio?.IsReady != true) return false;
            var succeeded = true;
            foreach (AudioCategory category in Enum.GetValues(typeof(AudioCategory))) succeeded &= ApplyCategory(category);
            return succeeded;
        }

        private bool ApplyCategory(AudioCategory category)
        {
            if (audio.SetVolume(category, values.GetGain(category))) return true;
            Debug.LogWarning($"Could not apply {category} audio preference. Check its exposed mixer parameter; the desired setting is retained.", this);
            return false;
        }
        private void OnApplicationPause(bool paused) { if (paused) Save(); }
        private void OnApplicationFocus(bool focused) { if (!focused) Save(); }
        private void OnApplicationQuit() => Save();
        private void OnDestroy() => Shutdown();
    }
}
