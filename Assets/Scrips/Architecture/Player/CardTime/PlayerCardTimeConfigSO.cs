using UnityEngine;

namespace TicGame.Architecture
{
    [CreateAssetMenu(
        menuName = "TIC/Player/Card Time Configuration",
        fileName = "PlayerCardTimeConfig")]
    public sealed class PlayerCardTimeConfigSO : ScriptableObject
    {
        [Header(header: "Active Session")]
        [Min(min: 0.01f)]
        [Tooltip(tooltip: "Maximum real-time duration of an active Card Time session.")]
        [SerializeField] private float maximumActiveDuration = 5f;

        [Range(min: 0.01f, max: 1f)]
        [Tooltip(tooltip: "Gameplay time scale applied while Card Time is active.")]
        [SerializeField] private float activeTimeScale = 0.1f;

        [Header(header: "Input Leniency")]
        [Min(min: 0f)]
        [Tooltip(tooltip: "Real-time duration that stores Card Time input pressed before a valid window.")]
        [SerializeField] private float inputBufferDuration = 0.15f;

        [Min(min: 0f)]
        [Tooltip(tooltip: "Real-time duration that preserves a Card Time state after its animation window closes.")]
        [SerializeField] private float postWindowGraceDuration = 0.5f;

        [Min(min: 0f)]
        [Tooltip(tooltip: "Real-time gap allowed between the two Card Time chord buttons.")]
        [SerializeField] private float chordInputGraceDuration = 0.2f;

        public float MaximumActiveDuration => Mathf.Max(a: 0.01f, b: maximumActiveDuration);
        public float ActiveTimeScale => Mathf.Clamp(value: activeTimeScale, min: 0.01f, max: 1f);
        public float InputBufferDuration => Mathf.Max(a: 0f, b: inputBufferDuration);
        public float PostWindowGraceDuration => Mathf.Max(a: 0f, b: postWindowGraceDuration);
        public float ChordInputGraceDuration => Mathf.Max(a: 0f, b: chordInputGraceDuration);
    }
}
