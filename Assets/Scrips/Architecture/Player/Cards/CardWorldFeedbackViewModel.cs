using UnityEngine;

namespace TicGame.Architecture
{
    public readonly struct CardWorldFeedbackViewModel
    {
        public CardWorldFeedbackViewModel(
            CardDefinitionSO card,
            GameObject sourceObject,
            CardFeedbackKind kind,
            CardFeedbackAnchor anchor = CardFeedbackAnchor.SourceHead,
            Vector3? worldPosition = null,
            float displaySeconds = 0.75f,
            Sprite iconOverride = null)
        {
            Card = card;
            SourceObject = sourceObject;
            Kind = kind;
            Anchor = anchor;
            WorldPosition = worldPosition;
            DisplaySeconds = Mathf.Max(0f, displaySeconds);
            IconOverride = iconOverride;
        }

        public CardDefinitionSO Card { get; }
        public string CardId => Card != null ? Card.Id : string.Empty;
        public Sprite Icon => IconOverride != null ? IconOverride : Card != null ? Card.Icon : null;
        public GameObject SourceObject { get; }
        public CardFeedbackKind Kind { get; }
        public CardFeedbackAnchor Anchor { get; }
        public Vector3? WorldPosition { get; }
        public float DisplaySeconds { get; }
        public Sprite IconOverride { get; }
        public bool HasExplicitWorldPosition => WorldPosition.HasValue;
        public bool IsValid => Card != null || Icon != null;
    }
}
