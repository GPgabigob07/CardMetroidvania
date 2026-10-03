using System.Collections.Generic;
using UnityEngine;

namespace TicGame.Architecture
{
    [CreateAssetMenu(menuName = "TIC/Enemies/Gargoyle Presentation")]
    public sealed class GargoylePresentationSO : ScriptableObject
    {
        [Header("Pixel Contract")]
        [Tooltip("Approved padded sprite frame. Changing this is a design change; apply import settings outside Play Mode.")]
        [SerializeField] private Vector2Int frameSize = new Vector2Int(200, 200);
        [Tooltip("Maximum ordinary-pose alpha bounds. Extended attack poses may use the padded frame.")]
        [SerializeField] private Vector2Int ordinaryPoseEnvelope = new Vector2Int(128, 128);
        [Tooltip("Fixed foot anchor, measured from the bottom-left. Import changes require Editor apply outside Play Mode.")]
        [SerializeField] private Vector2 pixelPivot = new Vector2(100, 36);
        [Min(0), Tooltip("Provisional import scale against the current player; requires reimport outside Play Mode.")]
        [SerializeField] private float pixelsPerUnit = 48;
        [Min(0), Tooltip("Minimum world-space occupied body-mask area relative to the player, excluding wings and effects.")]
        [SerializeField] private float minimumBodyAreaRatio = 1.5f;
        [Header("Body Geometry")]
        [Tooltip("Provisional solid-body collider dimensions in world units, independent of transparent padding.")]
        [SerializeField] private Vector2 bodySize = new Vector2(1.8f, 2.1f);

        public Vector2Int FrameSize => frameSize;
        public Vector2Int OrdinaryPoseEnvelope => ordinaryPoseEnvelope;
        public Vector2 PixelPivot => pixelPivot;
        public Vector2 NormalizedPivot => new Vector2(pixelPivot.x / frameSize.x, pixelPivot.y / frameSize.y);
        public float PixelsPerUnit => pixelsPerUnit;
        public float MinimumBodyAreaRatio => minimumBodyAreaRatio;
        public Vector2 BodySize => bodySize;

        public IReadOnlyList<string> GetValidationErrors()
        {
            var errors = new List<string>();
            if (frameSize.x <= 0 || frameSize.y <= 0) errors.Add("Sprite frame dimensions must be positive.");
            if (ordinaryPoseEnvelope.x <= 0 || ordinaryPoseEnvelope.y <= 0
                || ordinaryPoseEnvelope.x > frameSize.x || ordinaryPoseEnvelope.y > frameSize.y)
                errors.Add("Ordinary pose envelope must fit within the sprite frame.");
            if (!float.IsFinite(pixelPivot.x) || !float.IsFinite(pixelPivot.y)
                || pixelPivot.x < 0 || pixelPivot.y < 0 || pixelPivot.x > frameSize.x || pixelPivot.y > frameSize.y)
                errors.Add("Pixel pivot must be finite and within the frame.");
            if (!float.IsFinite(pixelsPerUnit) || pixelsPerUnit <= 0) errors.Add("Pixels per unit must be finite and positive.");
            if (!float.IsFinite(minimumBodyAreaRatio) || minimumBodyAreaRatio <= 0)
                errors.Add("Minimum body area ratio must be finite and positive.");
            if (!float.IsFinite(bodySize.x) || !float.IsFinite(bodySize.y) || bodySize.x <= 0 || bodySize.y <= 0)
                errors.Add("Body dimensions must be finite and positive.");
            return errors;
        }
    }
}
