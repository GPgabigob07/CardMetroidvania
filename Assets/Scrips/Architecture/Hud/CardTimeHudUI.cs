using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TicGame.Architecture
{
    public sealed class CardTimeHudUI : MonoBehaviour, IGameplayServicesConsumer
    {
        [Header("Cards")]
        [Tooltip("Three card graphics in Neutral, Chain, Finisher reveal order.")]
        [SerializeField] private List<Graphic> cards = new();

        [SerializeField] private Color availableColor =
            new(r: 0.32f, g: 0.64f, b: 0.72f, a: 0.85f);

        [SerializeField] private Color activeColor =
            new(r: 0.12f, g: 0.9f, b: 1f, a: 1f);

        [Header("Input Indicator")]
        [SerializeField] private Text inputLabel;
        [SerializeField] private Graphic inputBackground;
        [SerializeField] private CardTimeControlSchemeProfileSO controlSchemeProfile;
        [SerializeField] private CardTimeInputDisplayMapperSO inputDisplayMapper;
        [SerializeField] private string selectedSchemeId;
        [SerializeField] private string leftActionName = "CardTimeLeft";
        [SerializeField] private string rightActionName = "CardTimeRight";
        [SerializeField] private string separator = " + ";
        [SerializeField] private Color inputBackgroundColor = Color.white;
        [SerializeField] private Color inputTextColor = Color.black;

        private CardTimeSessionEventChannelSO transitionEvent;
        private bool subscribed;

        private void Awake()
        {
            Apply(default);
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        public void Configure(IReadOnlyList<Graphic> cardGraphics)
        {
            cards = new List<Graphic>(cardGraphics);
        }

        public void Configure(
            IReadOnlyList<Graphic> cardGraphics,
            Text actionLabel,
            Graphic actionBackground,
            CardTimeControlSchemeProfileSO schemeProfile,
            CardTimeInputDisplayMapperSO displayMapper,
            string schemeId = "")
        {
            Configure(cardGraphics);
            inputLabel = actionLabel;
            inputBackground = actionBackground;
            controlSchemeProfile = schemeProfile;
            inputDisplayMapper = displayMapper;
            selectedSchemeId = schemeId;
            ApplyInputStyle();
        }

        public void BindGameplayServices(IGameplayServices services)
        {
            Unsubscribe();
            transitionEvent = services?.CardTimeTransitions;
            Apply(services?.CardTime.Current ?? default);
            Subscribe();
        }

        private void Subscribe()
        {
            if (!isActiveAndEnabled || subscribed || transitionEvent == null)
            {
                return;
            }

            transitionEvent.Raised += HandleTransition;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed || transitionEvent == null)
            {
                return;
            }

            transitionEvent.Raised -= HandleTransition;
            subscribed = false;
        }

        private void HandleTransition(CardTimeSessionTransition transition)
        {
            Apply(transition.Current);
        }

        private void Apply(CardTimeSessionSnapshot snapshot)
        {
            var state = snapshot.IsActive
                ? snapshot.SessionCardTime
                : snapshot.IsAvailable
                    ? snapshot.AvailableCardTime
                    : PlayerCardTimeState.None;
            var visibleCount = HudValueMath.GetCardCount(state);
            var color = snapshot.IsActive ? activeColor : availableColor;
            var showInput = visibleCount > 0;

            for (var index = 0; index < cards.Count; index++)
            {
                var card = cards[index];
                if (card == null)
                {
                    continue;
                }

                card.gameObject.SetActive(index < visibleCount);
                card.color = color;
            }

            SetInputIndicatorVisible(showInput);
            if (showInput && inputLabel != null)
            {
                inputLabel.text = ResolveInputLabel();
            }
        }

        private string ResolveInputLabel()
        {
            var scheme = controlSchemeProfile != null
                ? controlSchemeProfile.ResolveScheme(selectedSchemeId)
                : null;
            var family = scheme != null
                ? scheme.DeviceFamily
                : CardTimeControlDeviceFamily.KeyboardMouse;
            var leftFallback = family == CardTimeControlDeviceFamily.Gamepad ? "LB" : "J";
            var rightFallback = family == CardTimeControlDeviceFamily.Gamepad ? "RB" : "K";
            var left = inputDisplayMapper != null
                ? inputDisplayMapper.ResolveActionLabel(family, leftActionName, leftFallback)
                : leftFallback;
            var right = inputDisplayMapper != null
                ? inputDisplayMapper.ResolveActionLabel(family, rightActionName, rightFallback)
                : rightFallback;
            return $"{left}{separator}{right}";
        }

        private void SetInputIndicatorVisible(bool visible)
        {
            ApplyInputStyle();
            if (inputLabel != null)
            {
                inputLabel.gameObject.SetActive(visible);
            }

            if (inputBackground != null)
            {
                inputBackground.gameObject.SetActive(visible);
            }
        }

        private void ApplyInputStyle()
        {
            if (inputBackground != null)
            {
                inputBackground.color = inputBackgroundColor;
                inputBackground.raycastTarget = false;
            }

            if (inputLabel != null)
            {
                inputLabel.color = inputTextColor;
                inputLabel.raycastTarget = false;
            }
        }
    }
}
