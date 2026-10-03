using System.Collections.Generic;
using System;
using System.Linq;
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
        [SerializeField] private Vector2 bodyOffset = new Vector2(0, 1.05f);
        [SerializeField, Min(0)] private int bodyPriority;
        [SerializeField] private GargoyleRegionSettings coreRegion = new GargoyleRegionSettings(new Vector2(.55f, .6f), new Vector2(0, 1.2f), 20);
        [SerializeField] private GargoyleRegionSettings headRegion = new GargoyleRegionSettings(new Vector2(.5f, .5f), new Vector2(0, 1.85f), 10);
        [Header("Animation And Feedback")]
        [SerializeField, Tooltip("Single-frame draft until artist animations are supplied.")]
        private Sprite idleSprite;
        [SerializeField] private GargoyleAnimationBinding[] animationBindings = Array.Empty<GargoyleAnimationBinding>();
        [SerializeField, Min(0)] private float frameRate = 8;
        [SerializeField, Min(0)] private float flashDuration = .12f;
        [SerializeField, Min(0)] private float telegraphLineWidth = .05f;
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color windupColor = new Color(1, .8f, .2f);
        [SerializeField] private Color activeColor = new Color(1, .45f, .2f);
        [SerializeField] private Color reactorColor = Color.cyan;
        [SerializeField] private Color stunColor = new Color(.65f, .65f, .65f);
        [SerializeField] private Color hitColor = Color.red;
        [SerializeField] private GameObject hitVfxPrefab;
        [SerializeField] private AudioClip attackAudio;

        public Vector2Int FrameSize => frameSize;
        public Vector2Int OrdinaryPoseEnvelope => ordinaryPoseEnvelope;
        public Vector2 PixelPivot => pixelPivot;
        public Vector2 NormalizedPivot => new Vector2(pixelPivot.x / frameSize.x, pixelPivot.y / frameSize.y);
        public float PixelsPerUnit => pixelsPerUnit;
        public float MinimumBodyAreaRatio => minimumBodyAreaRatio;
        public Vector2 BodySize => bodySize;
        public GargoyleRegionSettings BodyRegion => new GargoyleRegionSettings(bodySize, bodyOffset, bodyPriority);
        public GargoyleRegionSettings CoreRegion => coreRegion;
        public GargoyleRegionSettings HeadRegion => headRegion;
        public Sprite IdleSprite => idleSprite;
        public IReadOnlyList<GargoyleAnimationBinding> AnimationBindings => Array.AsReadOnly(animationBindings ?? Array.Empty<GargoyleAnimationBinding>());
        public float FrameRate => frameRate;
        public float FlashDuration => flashDuration;
        public float TelegraphLineWidth => telegraphLineWidth;
        public Color NormalColor => normalColor;
        public Color WindupColor => windupColor;
        public Color ActiveColor => activeColor;
        public Color ReactorColor => reactorColor;
        public Color StunColor => stunColor;
        public Color HitColor => hitColor;
        public GameObject HitVfxPrefab => hitVfxPrefab;
        public AudioClip AttackAudio => attackAudio;

        public bool TryReadPresentation(out GargoylePresentationValues value)
        {
            value = default;
            if (GetValidationErrors().Count != 0) return false;
            value = new GargoylePresentationValues(this);
            return true;
        }

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
            if (!BodyRegion.IsValid || !coreRegion.IsValid || !headRegion.IsValid)
                errors.Add("Hurtbox regions require finite positive geometry and non-negative priority.");
            if (!float.IsFinite(frameRate) || frameRate <= 0 || !float.IsFinite(flashDuration) || flashDuration < 0
                || !float.IsFinite(telegraphLineWidth) || telegraphLineWidth <= 0)
                errors.Add("Playback and feedback durations/width must be finite and valid.");
            foreach (var color in new[] { normalColor, windupColor, activeColor, reactorColor, stunColor, hitColor })
                if (!float.IsFinite(color.r) || !float.IsFinite(color.g) || !float.IsFinite(color.b) || !float.IsFinite(color.a))
                    errors.Add("Feedback colors must be finite.");
            if (animationBindings != null && animationBindings.Any(binding => binding == null || binding.Frames.Any(frame => frame == null)))
                errors.Add("Animation bindings cannot contain missing entries or sprite frames.");
            return errors;
        }
    }
}
