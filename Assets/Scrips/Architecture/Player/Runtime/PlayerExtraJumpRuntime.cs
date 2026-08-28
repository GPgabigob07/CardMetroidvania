using UnityEngine;

namespace TicGame.Architecture
{
    public sealed class PlayerExtraJumpRuntime :
        MonoBehaviour,
        IGameplayServicesConsumer
    {
        [Header("Ability")]
        [Tooltip("Stable ability identity implemented by this runtime.")]
        [SerializeField] private AbilityDefinitionSO abilityDefinition;
        [Tooltip("Prototype unlock state until progression supplies the authoritative value.")]
        [SerializeField] private bool unlockedOverride = true;

        [Header("Charges")]
        [Min(1)]
        [Tooltip("Maximum temporary card-granted jumps that may be stored.")]
        [SerializeField] private int maximumCharges = 1;

        private ICardFeedbackService cardFeedback;
        private CardDefinitionSO lastCard;

        public int Charges { get; private set; }
        public bool CanGrant => Charges < maximumCharges;

        public bool Supports(AbilityDefinitionSO ability)
        {
            return ability != null && abilityDefinition == ability;
        }

        public bool IsUnlocked(AbilityDefinitionSO ability)
        {
            return Supports(ability)
                && (unlockedOverride || ability.UnlockedByDefault);
        }

        public bool CanInvoke(AbilityDefinitionSO ability)
        {
            return IsUnlocked(ability) && CanGrant;
        }

        public bool Invoke(AbilityDefinitionSO ability, CardDefinitionSO card = null)
        {
            return CanInvoke(ability) && TryGrant(card: card);
        }

        public void BindGameplayServices(IGameplayServices services)
        {
            cardFeedback = services?.CardFeedback;
        }

        public void ConfigureAbility(
            AbilityDefinitionSO ability,
            bool unlocked = true)
        {
            abilityDefinition = ability;
            unlockedOverride = unlocked;
        }

        public bool TryGrant(int amount = 1, CardDefinitionSO card = null)
        {
            var previous = Charges;
            Charges = Mathf.Clamp(Charges + Mathf.Max(0, amount), 0, maximumCharges);
            var granted = Charges > previous;
            if (granted)
            {
                lastCard = card != null ? card : lastCard;
                RefreshHud();
                PublishWorld(lastCard, CardFeedbackKind.Activated);
            }

            return granted;
        }

        public bool TryConsume()
        {
            if (Charges <= 0)
            {
                return false;
            }

            Charges--;
            RefreshHud();
            PublishWorld(lastCard, CardFeedbackKind.Triggered);
            return true;
        }

        public void Clear()
        {
            var hadCharges = Charges > 0;
            Charges = 0;
            cardFeedback?.RemoveHudEffect(BuildFeedbackKey());
            if (hadCharges)
            {
                PublishWorld(lastCard, CardFeedbackKind.Cleared);
            }
        }

        private void RefreshHud()
        {
            if (Charges <= 0)
            {
                cardFeedback?.RemoveHudEffect(BuildFeedbackKey());
                return;
            }

            cardFeedback?.UpsertHudEffect(new CardHudEffectViewModel(
                effectKey: BuildFeedbackKey(),
                sourceObject: gameObject,
                card: lastCard,
                displayText: Charges.ToString()));
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
            return $"{GetInstanceID()}:extra-jump";
        }
    }
}
