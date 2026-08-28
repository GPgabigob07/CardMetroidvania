using UnityEngine;
using UnityEngine.UI;

namespace TicGame.Architecture
{
    public sealed class CardTimeSelectionSlotUI : MonoBehaviour
    {
        [Header("Presentation")]
        [SerializeField] private GameObject root;
        [SerializeField] private Text cardLabel;
        [SerializeField] private Text commandLabel;
        [SerializeField] private Graphic commandBackground;
        [SerializeField] private Graphic frame;

        [Header("Colors")]
        [SerializeField] private Color normalColor =
            new(r: 0.12f, g: 0.22f, b: 0.25f, a: 0.88f);

        [SerializeField] private Color selectedColor =
            new(r: 0.05f, g: 0.82f, b: 0.95f, a: 1f);

        [SerializeField] private Color committedColor =
            new(r: 0.45f, g: 1f, b: 0.55f, a: 1f);

        [SerializeField] private Color invalidColor =
            new(r: 1f, g: 0.18f, b: 0.12f, a: 1f);

        [SerializeField] private Color commandBackgroundColor = Color.white;
        [SerializeField] private Color commandTextColor = Color.black;

        public void Configure(
            Text cardName,
            Text commandName,
            Graphic frameGraphic,
            Graphic commandBackgroundGraphic = null)
        {
            cardLabel = cardName;
            commandLabel = commandName;
            frame = frameGraphic;
            commandBackground = commandBackgroundGraphic;
            ApplyCommandStyle();
        }

        public void Bind(
            int index,
            CardDefinitionSO card,
            string commandDisplay)
        {
            if (cardLabel != null)
            {
                cardLabel.text = card != null ? card.DisplayName : string.Empty;
            }

            if (commandLabel != null)
            {
                commandLabel.text = string.IsNullOrWhiteSpace(commandDisplay)
                    ? (index + 1).ToString()
                    : commandDisplay;
                ApplyCommandStyle();
            }
        }

        public void ConfigureCommandStyle(
            Graphic background,
            Color backgroundColor,
            Color textColor)
        {
            commandBackground = background;
            commandBackgroundColor = backgroundColor;
            commandTextColor = textColor;
            ApplyCommandStyle();
        }

        public void SetVisible(bool visible)
        {
            var target = root != null ? root : gameObject;
            target.SetActive(visible);
        }

        public void SetSelected(bool selected)
        {
            ApplyColor(selected ? selectedColor : normalColor);
        }

        public void PlayAnimation(CardTimeSelectionSlotAnimation animation)
        {
            switch (animation)
            {
                case CardTimeSelectionSlotAnimation.Show:
                    SetVisible(true);
                    ApplyColor(normalColor);
                    break;
                case CardTimeSelectionSlotAnimation.Hide:
                    SetVisible(false);
                    break;
                case CardTimeSelectionSlotAnimation.Selected:
                    ApplyColor(selectedColor);
                    break;
                case CardTimeSelectionSlotAnimation.Deselected:
                    ApplyColor(normalColor);
                    break;
                case CardTimeSelectionSlotAnimation.Committed:
                    ApplyColor(committedColor);
                    break;
                case CardTimeSelectionSlotAnimation.Invalid:
                    ApplyColor(invalidColor);
                    break;
            }
        }

        private void ApplyColor(Color color)
        {
            if (frame != null)
            {
                frame.color = color;
            }
        }

        private void ApplyCommandStyle()
        {
            if (commandBackground != null)
            {
                commandBackground.color = commandBackgroundColor;
                commandBackground.raycastTarget = false;
            }

            if (commandLabel != null)
            {
                commandLabel.color = commandTextColor;
                commandLabel.raycastTarget = false;
            }
        }
    }
}
