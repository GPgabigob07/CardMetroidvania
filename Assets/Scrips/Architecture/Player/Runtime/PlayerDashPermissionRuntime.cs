using UnityEngine;

namespace TicGame.Architecture
{
    public sealed class PlayerDashPermissionRuntime :
        MonoBehaviour,
        IGameplayServicesConsumer
    {
        private const string FeedbackId = "timed-dash";

        [SerializeField]
        [Tooltip("Player combat effects that report eligible primary melee hits.")]
        private PlayerCombatEffects combatEffects;

        private ICardFeedbackService cardFeedback;
        private CardDefinitionSO card;
        private float extensionPerHit;
        private bool isSubscribed;

        public bool CanActivate => !IsEnabled;
        public bool IsEnabled => RemainingSeconds > 0f;
        public float RemainingSeconds { get; private set; }

        private void Awake()
        {
            combatEffects ??= GetComponent<PlayerCombatEffects>();
        }

        private void OnEnable()
        {
            SubscribeEligibleHits();
        }

        private void OnDisable()
        {
            UnsubscribeEligibleHits();
        }

        public void BindGameplayServices(IGameplayServices services)
        {
            cardFeedback = services?.CardFeedback;
        }

        public bool Activate(
            float durationSeconds,
            float extensionSecondsPerHit,
            CardDefinitionSO sourceCard)
        {
            if (!CanActivate || !float.IsFinite(durationSeconds) || durationSeconds <= 0f)
            {
                return false;
            }

            RemainingSeconds = Mathf.Max(0f, durationSeconds);
            extensionPerHit = float.IsFinite(extensionSecondsPerHit)
                ? Mathf.Max(0f, extensionSecondsPerHit)
                : 0f;
            card = sourceCard != null ? sourceCard : card;
            RefreshHud();
            PublishWorld(CardFeedbackKind.Activated);
            return true;
        }

        public void Tick(float deltaTime)
        {
            if (!IsEnabled)
            {
                return;
            }

            var previousTenth = Mathf.RoundToInt(RemainingSeconds * 10f);
            RemainingSeconds = Mathf.Max(0f, RemainingSeconds - Mathf.Max(0f, deltaTime));
            if (!IsEnabled)
            {
                RemoveHud();
                PublishWorld(CardFeedbackKind.Expired);
                return;
            }

            if (Mathf.RoundToInt(RemainingSeconds * 10f) != previousTenth)
            {
                RefreshHud();
            }
        }

        public void Clear()
        {
            var wasEnabled = IsEnabled;
            RemainingSeconds = 0f;
            extensionPerHit = 0f;
            RemoveHud();
            if (wasEnabled)
            {
                PublishWorld(CardFeedbackKind.Cleared);
            }
        }

        private void SubscribeEligibleHits()
        {
            if (isSubscribed)
            {
                return;
            }

            combatEffects ??= GetComponent<PlayerCombatEffects>();
            if (combatEffects == null)
            {
                return;
            }

            combatEffects.EligiblePrimaryHitsResolved += NotifyEligibleHits;
            isSubscribed = true;
        }

        private void UnsubscribeEligibleHits()
        {
            if (!isSubscribed)
            {
                return;
            }

            combatEffects.EligiblePrimaryHitsResolved -= NotifyEligibleHits;
            isSubscribed = false;
        }

        private void NotifyEligibleHits(int count)
        {
            if (!IsEnabled || count <= 0)
            {
                return;
            }

            RemainingSeconds += count * extensionPerHit;
            RefreshHud();
            PublishWorld(CardFeedbackKind.Triggered);
        }

        private void RefreshHud()
        {
            cardFeedback?.UpsertHudEffect(new CardHudEffectViewModel(
                effectKey: BuildFeedbackKey(),
                sourceObject: gameObject,
                card: card,
                displayText: $"{RemainingSeconds:0.0}s"));
        }

        private void RemoveHud()
        {
            cardFeedback?.RemoveHudEffect(BuildFeedbackKey());
        }

        private void PublishWorld(CardFeedbackKind kind)
        {
            if (card == null)
            {
                return;
            }

            cardFeedback?.PublishWorldFeedback(new CardWorldFeedbackViewModel(
                card: card,
                sourceObject: gameObject,
                kind: kind));
        }

        private string BuildFeedbackKey()
        {
            return $"{GetInstanceID()}:{FeedbackId}";
        }
    }
}
