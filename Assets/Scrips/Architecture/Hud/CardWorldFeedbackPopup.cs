using UnityEngine;

namespace TicGame.Architecture
{
    public sealed class CardWorldFeedbackPopup : MonoBehaviour
    {
        [Header("Graphics")]
        [SerializeField] private SpriteRenderer iconRenderer;

        [Header("Motion")]
        [SerializeField] private Vector3 drift = new(0f, 0.65f, 0f);

        private float remaining;
        private float duration;
        private Color baseColor = Color.white;

        private void Awake()
        {
            iconRenderer ??= GetComponentInChildren<SpriteRenderer>();
        }

        private void Update()
        {
            if (duration <= 0f)
            {
                Destroy(gameObject);
                return;
            }

            remaining = Mathf.Max(0f, remaining - Time.deltaTime);
            transform.position += drift * Time.deltaTime;
            if (iconRenderer != null)
            {
                var color = baseColor;
                color.a = Mathf.Clamp01(remaining / duration);
                iconRenderer.color = color;
            }

            if (remaining <= 0f)
            {
                Destroy(gameObject);
            }
        }

        public void Configure(SpriteRenderer renderer)
        {
            iconRenderer = renderer;
        }

        public void Initialize(
            CardWorldFeedbackViewModel model,
            Vector3 position,
            Color color)
        {
            transform.position = position;
            duration = Mathf.Max(0.05f, model.DisplaySeconds);
            remaining = duration;
            baseColor = color;

            if (iconRenderer != null)
            {
                iconRenderer.sprite = model.Icon;
                iconRenderer.color = color;
                iconRenderer.enabled = model.Icon != null;
            }
        }
    }
}
