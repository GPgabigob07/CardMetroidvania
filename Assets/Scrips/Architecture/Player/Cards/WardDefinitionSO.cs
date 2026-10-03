using UnityEngine;

namespace TicGame.Architecture
{
    [CreateAssetMenu(menuName = "TIC/Cards/Ward Definition")]
    public sealed class WardDefinitionSO : ScriptableObject
    {
        [Header("Live Guard Settings")]
        [SerializeField, Min(0), Tooltip("Scaled gameplay lifetime; an edit preserves the active guard's elapsed clock.")]
        private float duration = .6f;
        [SerializeField, Min(0)] private float height = 1.5f;
        [SerializeField, Min(0)] private float forwardOffset = .6f;
        [SerializeField, Min(0), Tooltip("Numerical tolerance for tangency, measured in world units; not an enemy-distance allowance.")]
        private float intersectionTolerance = .00001f;
        [Header("Guard Presentation")]
        [SerializeField, Min(0)] private float lineWidth = .05f;
        [SerializeField] private Color guardColor = Color.cyan;
        public float LineWidth => lineWidth;
        public Color GuardColor => guardColor;
        public bool TryRead(out WardConfiguration values)
        {
            values = default;
            if (!float.IsFinite(duration) || duration <= 0 || !float.IsFinite(height) || height <= 0
                || !float.IsFinite(forwardOffset) || forwardOffset < 0
                || !float.IsFinite(intersectionTolerance) || intersectionTolerance < 0) return false;
            values = new WardConfiguration(duration, height, forwardOffset, intersectionTolerance);
            return true;
        }
    }
}
