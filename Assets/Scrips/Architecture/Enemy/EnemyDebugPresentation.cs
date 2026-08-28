using UnityEngine;

namespace TicGame.Architecture
{
    [RequireComponent(requiredComponent: typeof(EnemyHealth))]
    public sealed class EnemyDebugPresentation : MonoBehaviour
    {
        private const float FlashDuration = 0.12f;

        private EnemyHealth health;
        private SpriteRenderer spriteRenderer;
        private Color baseColor;
        private float flashRemaining;

        private void Awake()
        {
            health = GetComponent<EnemyHealth>();
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            baseColor = spriteRenderer != null ? spriteRenderer.color : Color.white;
        }

        private void OnEnable()
        {
            health.Damaged += HandleDamaged;
        }

        private void OnDisable()
        {
            health.Damaged -= HandleDamaged;

            if (spriteRenderer != null)
            {
                spriteRenderer.color = baseColor;
            }
        }

        private void Update()
        {
            if (flashRemaining <= 0f)
            {
                return;
            }

            flashRemaining -= Time.unscaledDeltaTime;
            if (flashRemaining <= 0f && spriteRenderer != null)
            {
                spriteRenderer.color = baseColor;
            }
        }

        private void OnGUI()
        {
            var camera = Camera.main;
            if (camera == null || health == null)
            {
                return;
            }

            var screen = camera.WorldToScreenPoint(
                position: transform.position + Vector3.up * 1.25f);
            if (screen.z < 0f)
            {
                return;
            }

            const float width = 90f;
            const float height = 10f;
            var rect = new Rect(
                x: screen.x - width * 0.5f,
                y: Screen.height - screen.y,
                width: width,
                height: height);
            GUI.color = Color.black;
            GUI.Box(position: rect, text: string.Empty);
            GUI.color = Color.red;
            GUI.DrawTexture(
                position: new Rect(
                    x: rect.x + 1f,
                    y: rect.y + 1f,
                    width: (rect.width - 2f) * health.NormalizedHealth,
                    height: rect.height - 2f),
                image: Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        private void HandleDamaged(EnemyDamageEvent payload)
        {
            flashRemaining = FlashDuration;

            if (spriteRenderer != null)
            {
                spriteRenderer.color = Color.white;
            }
        }
    }
}
