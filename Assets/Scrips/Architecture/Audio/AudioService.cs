using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

namespace TicGame.Architecture
{
    [DisallowMultipleComponent]
    public sealed class AudioService : MonoBehaviour, IGameplayModule, IAudioService, IAudioSettingsService
    {
        [Header("Mixer")]
        [SerializeField] private AudioMixer mixer;
        [Header("Pool")]
        [Min(1), SerializeField] private int capacity = 24;

        private sealed class Voice
        {
            public AudioSource Source;
            public GameObject Owner;
            public int Scene;
            public bool Active;
            public bool World;
            public bool Paused;
            public int StartedFrame;
            public Vector3 Position;
        }

        private static readonly string[] Parameters =
            { "MasterVolume", "SfxVolume", "UiVolume", "MusicVolume", "AmbienceVolume" };
        private readonly float[] gains = { 1f, 1f, 1f, 1f, 1f };
        private readonly HashSet<SoundCueSO> invalidCues = new();
        private Voice[] voices;
        private AudioVoicePolicy policy;
        private bool worldPaused;
        private bool defaultsApplied;
        public bool IsInitialized { get; private set; }
        public bool IsReady { get; private set; }
        public event Action Ready;
        public int ActiveVoiceCount
        {
            get
            {
                var count = 0;
                if (voices != null) foreach (var voice in voices) if (voice.Active) count++;
                return count;
            }
        }

        public void Configure(AudioMixer audioMixer, int voiceCapacity = 24)
        {
            if (IsInitialized) throw new InvalidOperationException("Configure audio before initialization.");
            mixer = audioMixer;
            capacity = Mathf.Max(1, voiceCapacity);
        }

        public void Initialize()
        {
            if (IsInitialized) return;
            policy = new AudioVoicePolicy(Mathf.Max(1, capacity));
            invalidCues.Clear();
            voices = new Voice[Mathf.Max(1, capacity)];
            for (var index = 0; index < voices.Length; index++)
            {
                var emitter = new GameObject($"Audio Voice {index}");
                emitter.transform.SetParent(transform, false);
                var source = emitter.AddComponent<AudioSource>();
                source.playOnAwake = false;
                voices[index] = new Voice { Source = source };
            }
            SceneManager.sceneUnloaded += HandleSceneUnloaded;
            defaultsApplied = false;
            IsInitialized = true;
        }

        public void Shutdown()
        {
            if (!IsInitialized) return;
            SceneManager.sceneUnloaded -= HandleSceneUnloaded;
            for (var index = 0; index < voices.Length; index++)
            {
                Release(index);
                voices[index].Source.transform.SetParent(null);
                if (Application.isPlaying) Destroy(voices[index].Source.gameObject);
                else DestroyImmediate(voices[index].Source.gameObject);
            }
            voices = null;
            policy = null;
            worldPaused = false;
            defaultsApplied = false;
            IsInitialized = false;
            IsReady = false;
        }

        private void OnDestroy() => Shutdown();

        private void Update()
        {
            if (!IsInitialized) return;
            if (!defaultsApplied && mixer != null)
            {
                defaultsApplied = true;
                foreach (AudioCategory category in Enum.GetValues(typeof(AudioCategory)))
                    if (!SetVolume(category, GetVolume(category)))
                        Debug.LogWarning($"Audio mixer is missing exposed parameter '{Parameters[(int)category]}'.", this);
                IsReady = true;
                Ready?.Invoke();
            }
            ReclaimFinishedVoices();
        }

        public void ReclaimFinishedVoices()
        {
            if (!IsInitialized) return;
            for (var index = 0; index < voices.Length; index++)
            {
                var voice = voices[index];
                if (!voice.Active) continue;
                if (voice.Owner == null || (!voice.Paused && Time.frameCount > voice.StartedFrame + 2 && !voice.Source.isPlaying))
                    Release(index);
                else if (voice.World) voice.Source.transform.position = voice.Position;
            }
        }

        public bool TryPlayWorld(SoundCueSO cue, Vector3 position, GameObject owner)
        {
            if (!AudioVolumeMath.IsFinite(position.x) || !AudioVolumeMath.IsFinite(position.y)
                || !AudioVolumeMath.IsFinite(position.z) || worldPaused) return false;
            return TryPlay(cue, position, owner, true);
        }

        public bool TryPlayUi(SoundCueSO cue, GameObject owner) => TryPlay(cue, Vector3.zero, owner, false);

        private bool TryPlay(SoundCueSO cue, Vector3 position, GameObject owner, bool world)
        {
            if (!IsInitialized || cue == null || owner == null) return false;
            if (cue.Resource == null || cue.MixerGroup == null)
            {
                if (invalidCues.Add(cue)) Debug.LogWarning($"Audio cue '{cue.name}' needs an audio resource and mixer route. Configure the cue or run TIC/Audio/Setup Prototype Audio.", cue);
                return false;
            }
            ReclaimFinishedVoices();
            if (!policy.TryAcquire(cue, Time.unscaledTimeAsDouble, out var index)) return false;
            var voice = voices[index];
            var source = voice.Source;
            source.Stop();
            source.resource = cue.Resource;
            source.outputAudioMixerGroup = cue.MixerGroup;
            source.transform.position = position;
            source.spatialBlend = world ? 1f : 0f;
            source.dopplerLevel = 0f;
            source.spread = 0f;
            source.panStereo = 0f;
            source.pitch = 1f;
            source.volume = cue.Gain;
            source.priority = cue.Priority;
            source.minDistance = cue.MinimumDistance;
            source.maxDistance = cue.MaximumDistance;
            source.rolloffMode = cue.Rolloff;
            source.loop = false;
            source.ignoreListenerPause = true;
            voice.Owner = owner;
            voice.Scene = owner.scene.handle;
            voice.Position = position;
            voice.World = world;
            voice.Paused = false;
            voice.Active = true;
            voice.StartedFrame = Time.frameCount;
            source.Play();
            return true;
        }

        public void SetWorldPaused(bool paused)
        {
            if (worldPaused == paused) return;
            worldPaused = paused;
            if (voices == null) return;
            foreach (var voice in voices)
            {
                if (!voice.Active || !voice.World) continue;
                voice.Paused = paused;
                if (paused) voice.Source.Pause();
                else voice.Source.UnPause();
            }
        }

        public bool SetVolume(AudioCategory category, float normalizedGain)
        {
            var index = (int)category;
            if (index < 0 || index >= gains.Length || !AudioVolumeMath.IsFinite(normalizedGain) || mixer == null) return false;
            if (!mixer.GetFloat(Parameters[index], out _) || !mixer.SetFloat(Parameters[index], AudioVolumeMath.ToDecibels(normalizedGain))) return false;
            gains[index] = Mathf.Clamp01(normalizedGain);
            return true;
        }

        public float GetVolume(AudioCategory category)
        {
            var index = (int)category;
            return index >= 0 && index < gains.Length ? gains[index] : 1f;
        }

        private void HandleSceneUnloaded(Scene scene)
        {
            for (var index = 0; index < voices.Length; index++)
                if (voices[index].Active && voices[index].Scene == scene.handle) Release(index);
        }

        private void Release(int index)
        {
            var voice = voices[index];
            voice.Source.Stop();
            voice.Source.resource = null;
            voice.Source.outputAudioMixerGroup = null;
            voice.Owner = null;
            voice.Active = false;
            voice.Paused = false;
            policy.Release(index);
        }
    }
}
