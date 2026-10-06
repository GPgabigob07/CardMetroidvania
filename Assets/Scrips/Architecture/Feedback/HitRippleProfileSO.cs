using UnityEngine;

namespace TicGame.Architecture
{
    [CreateAssetMenu(menuName = "TicGame/Feedback/Hit Ripple Profile")]
    public sealed class HitRippleProfileSO : ScriptableObject
    {
        [Header("Wave Geometry")]
        [Tooltip("Radial band thickness in the original sprite pixels, not screen pixels.")]
        [Min(1)] [SerializeField] private float widthPixels = 4;
        [Tooltip("Unscaled seconds to traverse the impact-time visual bounds including band width.")]
        [Min(.01f)] [SerializeField] private float traversalSeconds = .65f;
        [Header("Outcome Colors")]
        [SerializeField] private Color blueColor = new(.35f, .8f, 1f, 1f);
        [SerializeField] private Color rejectedColor = new(.92f, .96f, 1f, 1f);
        [SerializeField] private Color fatalLeadingColor = new(.35f, .8f, 1f, 1f);
        [SerializeField] private Color fatalTrailingColor = new(1f, .25f, .35f, 1f);
        [Header("Highlight")]
        [Range(0, 1)] [SerializeField] private float strength = 1f;
        [Tooltip("Fraction of wave lifetime used to fade the final highlight.")]
        [Range(0, 1)] [SerializeField] private float fadeOutFraction = .08f;
        public float WidthPixels => PositiveOr(widthPixels, 4);
        public float TraversalSeconds => PositiveOr(traversalSeconds, .65f);
        public Color BlueColor => blueColor;
        public Color RejectedColor => rejectedColor;
        public Color FatalLeadingColor => fatalLeadingColor;
        public Color FatalTrailingColor => fatalTrailingColor;
        public float Strength => float.IsFinite(strength) ? Mathf.Clamp01(strength) : 1f;
        public float FadeOutFraction => float.IsFinite(fadeOutFraction) ? Mathf.Clamp01(fadeOutFraction) : .08f;
        private static float PositiveOr(float value, float fallback) => float.IsFinite(value) && value > 0 ? value : fallback;
    }
}
