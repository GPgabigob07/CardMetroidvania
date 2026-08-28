using UnityEngine;
using UnityEngine.UI;

namespace TicGame.Architecture
{
    public sealed class CardHudEffectIndicatorUI : MonoBehaviour
    {
        [Header("Graphics")]
        [SerializeField] private Image icon;
        [SerializeField] private Text label;
        [SerializeField] private CanvasGroup group;

        public void Configure(Image iconImage, Text textLabel, CanvasGroup canvasGroup)
        {
            icon = iconImage;
            label = textLabel;
            group = canvasGroup;
        }

        public void Bind(CardHudEffectViewModel model)
        {
            SetVisible(true);
            if (icon != null)
            {
                icon.sprite = model.Icon;
                icon.enabled = model.Icon != null;
            }

            if (label != null)
            {
                label.text = model.DisplayText;
                label.enabled = !string.IsNullOrWhiteSpace(model.DisplayText);
            }
        }

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
            if (group != null)
            {
                group.alpha = visible ? 1f : 0f;
                group.interactable = false;
                group.blocksRaycasts = false;
            }
        }

        public void SetAlpha(float alpha)
        {
            if (group != null)
            {
                group.alpha = Mathf.Clamp01(alpha);
            }
        }
    }
}
