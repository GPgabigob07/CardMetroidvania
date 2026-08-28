using UnityEngine;

namespace TicGame.Architecture
{
    public sealed class CardWorldFeedbackPresenter :
        MonoBehaviour,
        IGameplayServicesConsumer
    {
        [Header("Popup")]
        [SerializeField] private CardWorldFeedbackPopup popupPrefab;
        [SerializeField] private Transform popupRoot;

        [Header("Anchors")]
        [SerializeField] private Vector3 sourceHeadOffset = new(0f, 1.65f, 0f);

        [Header("Colors")]
        [SerializeField] private Color activatedColor = new(0.45f, 0.9f, 1f, 1f);
        [SerializeField] private Color triggeredColor = new(0.5f, 1f, 0.58f, 1f);
        [SerializeField] private Color failedColor = new(1f, 0.28f, 0.25f, 1f);
        [SerializeField] private Color expiredColor = new(0.75f, 0.75f, 0.75f, 1f);

        private CardFeedbackEventChannelSO eventChannel;
        private bool subscribed;

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        public void BindGameplayServices(IGameplayServices services)
        {
            Unsubscribe();
            eventChannel = services?.CardFeedback?.WorldFeedbackEvent;
            Subscribe();
        }

        public void Configure(CardWorldFeedbackPopup prefab, Transform root = null)
        {
            popupPrefab = prefab;
            popupRoot = root;
        }

        public Vector3 ResolveWorldPosition(CardWorldFeedbackViewModel feedback)
        {
            if (feedback.Anchor == CardFeedbackAnchor.HitPoint
                && feedback.WorldPosition.HasValue)
            {
                return feedback.WorldPosition.Value;
            }

            return feedback.SourceObject != null
                ? feedback.SourceObject.transform.position + sourceHeadOffset
                : sourceHeadOffset;
        }

        private void HandleFeedback(CardWorldFeedbackViewModel feedback)
        {
            if (!feedback.IsValid)
            {
                return;
            }

            var popup = CreatePopup();
            popup.Initialize(
                feedback,
                ResolveWorldPosition(feedback),
                ResolveColor(feedback.Kind));
        }

        private CardWorldFeedbackPopup CreatePopup()
        {
            if (popupPrefab != null)
            {
                return Instantiate(popupPrefab, popupRoot);
            }

            var owner = new GameObject("Card World Feedback");
            if (popupRoot != null)
            {
                owner.transform.SetParent(popupRoot, worldPositionStays: true);
            }

            var renderer = owner.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = 100;
            var popup = owner.AddComponent<CardWorldFeedbackPopup>();
            popup.Configure(renderer);
            return popup;
        }

        private Color ResolveColor(CardFeedbackKind kind)
        {
            return kind switch
            {
                CardFeedbackKind.Activated => activatedColor,
                CardFeedbackKind.Triggered => triggeredColor,
                CardFeedbackKind.Failed => failedColor,
                CardFeedbackKind.Expired => expiredColor,
                CardFeedbackKind.Cleared => expiredColor,
                _ => Color.white
            };
        }

        private void Unsubscribe()
        {
            if (eventChannel != null && subscribed)
            {
                eventChannel.Raised -= HandleFeedback;
            }

            subscribed = false;
        }

        private void Subscribe()
        {
            if (!isActiveAndEnabled || eventChannel == null || subscribed)
            {
                return;
            }

            eventChannel.Raised += HandleFeedback;
            subscribed = true;
        }
    }
}
