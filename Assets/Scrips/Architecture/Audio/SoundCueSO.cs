using UnityEngine;
using UnityEngine.Audio;

namespace TicGame.Architecture
{
    [CreateAssetMenu(menuName = "TIC/Audio/Sound Cue", fileName = "Sound_")]
    public sealed class SoundCueSO : ScriptableObject
    {
        [Header("Native Audio")]
        [Tooltip("AudioClip or native Audio Random Container. Author variation in the container.")]
        [SerializeField] private AudioResource resource;
        [SerializeField] private AudioMixerGroup mixerGroup;
        [Range(0f, 1f), SerializeField] private float gain = 0.5f;

        [Header("Voice Policy")]
        [Tooltip("Lower values are more important. Only less important voices may be replaced.")]
        [Range(0, 256), SerializeField] private int priority = 128;
        [Min(1), SerializeField] private int maximumInstances = 4;
        [Min(0f), SerializeField] private float cooldown;

        [Header("World Attenuation")]
        [Tooltip("Distances include camera/listener depth; initial values suit the 2D camera at z=-10.")]
        [Min(0.01f), SerializeField] private float minimumDistance = 12f;
        [Min(0.01f), SerializeField] private float maximumDistance = 40f;
        [SerializeField] private AudioRolloffMode rolloff = AudioRolloffMode.Linear;

        public AudioResource Resource => resource;
        public AudioMixerGroup MixerGroup => mixerGroup;
        public float Gain => Mathf.Clamp01(gain);
        public int Priority => Mathf.Clamp(priority, 0, 256);
        public int MaximumInstances => Mathf.Max(1, maximumInstances);
        public float Cooldown => Mathf.Max(0, cooldown);
        public float MinimumDistance => Mathf.Max(0.01f, minimumDistance);
        public float MaximumDistance => Mathf.Max(MinimumDistance, maximumDistance);
        public AudioRolloffMode Rolloff => rolloff;

        public void Configure(AudioResource audio, AudioMixerGroup route, float volume = 0.5f)
        {
            resource = audio;
            mixerGroup = route;
            gain = volume;
        }

        public void ConfigurePolicy(int voicePriority, int limit, float seconds)
        {
            priority = voicePriority;
            maximumInstances = limit;
            cooldown = seconds;
        }
    }
}
