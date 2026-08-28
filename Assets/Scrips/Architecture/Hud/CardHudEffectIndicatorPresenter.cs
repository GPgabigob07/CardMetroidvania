using System.Collections.Generic;
using UnityEngine;

namespace TicGame.Architecture
{
    public sealed class CardHudEffectIndicatorPresenter :
        MonoBehaviour,
        IGameplayServicesConsumer
    {
        [Header("Slots")]
        [Min(1)]
        [SerializeField] private int maxVisibleEffects = 8;
        [SerializeField] private List<CardHudEffectIndicatorUI> indicators = new();

        [Header("Timed Effect Blink")]
        [Range(0f, 1f)]
        [SerializeField] private float blinkMinimumAlpha = 0.35f;
        [Min(0f)]
        [SerializeField] private float blinkSpeed = 8f;

        private ICardHudEffectViewModelSource source;

        private void OnEnable()
        {
            Refresh();
        }

        private void Update()
        {
            Refresh();
        }

        public void BindGameplayServices(IGameplayServices services)
        {
            source = services?.CardFeedback;
            Refresh();
        }

        public void Configure(IReadOnlyList<CardHudEffectIndicatorUI> slotViews)
        {
            indicators = slotViews != null
                ? new List<CardHudEffectIndicatorUI>(slotViews)
                : new List<CardHudEffectIndicatorUI>();
            Refresh();
        }

        public void Refresh()
        {
            var effects = source?.GetHudEffects();
            var visibleCount = effects != null
                ? Mathf.Min(effects.Count, maxVisibleEffects, indicators.Count)
                : 0;

            for (var index = 0; index < indicators.Count; index++)
            {
                var indicator = indicators[index];
                if (indicator == null)
                {
                    continue;
                }

                if (index >= visibleCount)
                {
                    indicator.SetVisible(false);
                    continue;
                }

                var model = effects[index];
                indicator.Bind(model);
                indicator.SetAlpha(ResolveAlpha(model));
            }
        }

        private float ResolveAlpha(CardHudEffectViewModel model)
        {
            if (!model.BlinkWhenExpiring
                || model.VisualState != CardHudEffectVisualState.Expiring)
            {
                return 1f;
            }

            var pulse = (Mathf.Sin(Time.unscaledTime * blinkSpeed) + 1f) * 0.5f;
            return Mathf.Lerp(blinkMinimumAlpha, 1f, pulse);
        }
    }
}
