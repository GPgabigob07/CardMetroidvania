using UnityEngine;

namespace TicGame.Architecture
{
    public readonly struct CardHudEffectViewModel
    {
        public CardHudEffectViewModel(
            string effectKey,
            GameObject sourceObject,
            CardDefinitionSO card,
            string displayText = "",
            float normalizedLifetime = 1f,
            bool hasLifetime = false,
            CardHudEffectVisualState visualState = CardHudEffectVisualState.Active,
            bool blinkWhenExpiring = false,
            Sprite iconOverride = null)
        {
            EffectKey = effectKey ?? string.Empty;
            SourceObject = sourceObject;
            Card = card;
            DisplayText = displayText ?? string.Empty;
            NormalizedLifetime = Mathf.Clamp01(normalizedLifetime);
            HasLifetime = hasLifetime;
            VisualState = visualState;
            BlinkWhenExpiring = blinkWhenExpiring;
            IconOverride = iconOverride;
        }

        public string EffectKey { get; }
        public GameObject SourceObject { get; }
        public CardDefinitionSO Card { get; }
        public string CardId => Card != null ? Card.Id : string.Empty;
        public Sprite Icon => IconOverride != null ? IconOverride : Card != null ? Card.Icon : null;
        public string DisplayText { get; }
        public float NormalizedLifetime { get; }
        public bool HasLifetime { get; }
        public CardHudEffectVisualState VisualState { get; }
        public bool BlinkWhenExpiring { get; }
        public Sprite IconOverride { get; }
        public bool IsValid => !string.IsNullOrWhiteSpace(EffectKey);
    }
}
