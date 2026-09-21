using UnityEngine;

namespace TicGame.Architecture
{
    public sealed class PlayerGroundedJumpBoostRuntime :
        MonoBehaviour,
        IGameplayServicesConsumer
    {
        private float multiplier = 1f;
        private ICardFeedbackService cardFeedback;
        private CardDefinitionSO lastCard;

        public bool IsArmed { get; private set; }
        public bool CanArm => !IsArmed;

        public bool Arm(float jumpMultiplier, CardDefinitionSO card)
        {
            if (!CanArm || !float.IsFinite(jumpMultiplier) || jumpMultiplier <= 0f)
            {
                return false;
            }

            multiplier = jumpMultiplier;
            lastCard = card != null ? card : lastCard;
            IsArmed = true;
            RefreshHud();
            PublishWorld(lastCard, CardFeedbackKind.Activated);
            return true;
        }

        public void BindGameplayServices(IGameplayServices services)
        {
            cardFeedback = services?.CardFeedback;
        }

        public float ConsumeGroundedLaunch(float baseVelocity)
        {
            if (!IsArmed)
            {
                return baseVelocity;
            }

            var boostedVelocity = baseVelocity * multiplier;
            PublishWorld(lastCard, CardFeedbackKind.Triggered);
            Clear();
            return boostedVelocity;
        }

        public void Clear()
        {
            var wasArmed = IsArmed;
            multiplier = 1f;
            IsArmed = false;
            cardFeedback?.RemoveHudEffect(BuildFeedbackKey());
            if (wasArmed)
            {
                PublishWorld(lastCard, CardFeedbackKind.Cleared);
            }
        }

        private void RefreshHud()
        {
            if (!IsArmed)
            {
                cardFeedback?.RemoveHudEffect(BuildFeedbackKey());
                return;
            }

            cardFeedback?.UpsertHudEffect(new CardHudEffectViewModel(
                effectKey: BuildFeedbackKey(),
                sourceObject: gameObject,
                card: lastCard,
                displayText: "Ready"));
        }

        private void PublishWorld(CardDefinitionSO card, CardFeedbackKind kind)
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
            return $"{GetInstanceID()}:grounded-jump-boost";
        }
    }
}
