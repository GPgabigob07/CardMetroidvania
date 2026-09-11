using UnityEngine;

namespace TicGame.Architecture
{
    [RequireComponent(requiredComponent: typeof(SpriteRenderer))]
    public sealed class ProjectileSpinVisual : MonoBehaviour
    {
        private const int RequiredFrameCount = 5;

        [Header(header: "Dependencies")]
        [Tooltip(tooltip: "Launched projectile observed for travel-facing presentation.")]
        [SerializeField] private EnemyProjectile2D projectile;

        [Tooltip(tooltip: "Renderer receiving spin frames and horizontal mirroring.")]
        [SerializeField] private SpriteRenderer renderer;

        [Header(header: "Spin Frames")]
        [Tooltip(tooltip: "Exactly five right-facing frames played in order.")]
        [SerializeField] private Sprite[] frames = new Sprite[RequiredFrameCount];

        [Header(header: "Playback")]
        [Min(min: 0f)]
        [Tooltip(tooltip: "Spin animation frames advanced per second.")]
        [SerializeField] private float framesPerSecond = 5f;

        private float elapsed;
        private bool hasLoggedInvalidConfiguration;

        private void Awake()
        {
            ResolveDependencies();
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        public void Configure(
            EnemyProjectile2D targetProjectile,
            SpriteRenderer targetRenderer,
            Sprite[] targetFrames)
        {
            projectile = targetProjectile;
            renderer = targetRenderer;
            frames = targetFrames;
            elapsed = 0f;
            hasLoggedInvalidConfiguration = false;
        }

        public void Tick(float deltaTime)
        {
            if (!HasValidConfiguration())
            {
                WarnInvalidConfiguration();
                return;
            }

            elapsed += Mathf.Max(0f, deltaTime);
            renderer.sprite = frames[(int)(elapsed * framesPerSecond) % frames.Length];
            renderer.flipX = projectile.Direction.x < 0f;
        }

        private bool HasValidConfiguration()
        {
            if (projectile == null || renderer == null || frames == null || frames.Length != RequiredFrameCount)
            {
                return false;
            }

            foreach (var frame in frames)
            {
                if (frame == null)
                {
                    return false;
                }
            }

            return true;
        }

        private void ResolveDependencies()
        {
            if (projectile == null)
            {
                projectile = GetComponent<EnemyProjectile2D>();
            }

            if (renderer == null)
            {
                renderer = GetComponent<SpriteRenderer>();
            }
        }

        private void WarnInvalidConfiguration()
        {
            if (hasLoggedInvalidConfiguration)
            {
                return;
            }

            hasLoggedInvalidConfiguration = true;
            Debug.LogError("Projectile spin visual requires a projectile, renderer, and exactly 5 non-null frames.", this);
        }
    }
}
