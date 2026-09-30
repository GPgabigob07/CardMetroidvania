using UnityEngine;

namespace TicGame.Architecture
{
    public sealed class PlayerRecoveryController : MonoBehaviour
    {
        [Header("Recovery")]
        [SerializeField] private PlayerRecoveryTuningSO tuning;
        [SerializeField] private ResourceDefinitionSO energy;
        [Header("Player")]
        [SerializeField] private PlayerResourceWallet wallet;
        [SerializeField] private SimpleHealth health;
        [SerializeField] private PlayerCardInventoryRuntime inventory;
        private PlayerController player;
        private PlayerResourceWallet subscribedWallet;
        private float delayRemaining;
        private bool applying;
        private int transactionRevision;

        public float NeutralCostBasis => PlayerRecoveryEconomy.ResolveNeutralCost(
            inventory != null ? inventory.GetEquippedCards(PlayerCardTimeState.Neutral) : null,
            energy, tuning != null ? tuning.FallbackNeutralEnergyCost : 20);
        public float PassiveEnergyCeiling => Ready ? PlayerRecoveryEconomy.GetPassiveCeiling(NeutralCostBasis, wallet.GetMaximum(energy), tuning.PassiveReserveMultiplier) : 0;
        public float EnergyPerHealth => tuning != null ? NeutralCostBasis * tuning.HealingChunkMultiplier : 0;
        public float SacrificeEnergyGain => tuning != null ? NeutralCostBasis * tuning.SacrificeEnergyMultiplier : 0;
        public CardCommitFailure Failure { get; private set; }
        private bool Ready => tuning != null && tuning.IsValid && energy != null && wallet != null && health != null && inventory != null;

        private void Awake()
        {
            player = GetComponent<PlayerController>();
            Configure(tuning, energy, wallet, health, inventory);
        }
        private void OnEnable() => Subscribe();
        private void OnDisable() => Unsubscribe();
        private void Update() => Tick(Time.deltaTime, player != null && player.CanRecoverEnergy && !PlaytestPauseController.IsGamePaused);
        public void Configure(PlayerRecoveryTuningSO settings, ResourceDefinitionSO resource, PlayerResourceWallet resourceWallet,
            SimpleHealth playerHealth, PlayerCardInventoryRuntime runInventory)
        {
            Unsubscribe();
            tuning = settings; energy = resource; wallet = resourceWallet; health = playerHealth; inventory = runInventory;
            delayRemaining = tuning != null ? tuning.PassiveDelayAfterSpendSeconds : 0;
            if (isActiveAndEnabled) Subscribe();
        }
        private void Subscribe()
        {
            if (subscribedWallet == wallet) return;
            Unsubscribe();
            subscribedWallet = wallet;
            if (subscribedWallet != null) subscribedWallet.Changed += OnResourceChanged;
        }
        private void Unsubscribe()
        {
            if (subscribedWallet != null) subscribedWallet.Changed -= OnResourceChanged;
            subscribedWallet = null;
        }
        private void OnResourceChanged(ResourceDefinitionSO resource, float previous, float current)
        {
            if (resource == energy && current < previous && tuning != null) delayRemaining = tuning.PassiveDelayAfterSpendSeconds;
        }
        public void Tick(float gameplayDeltaTime, bool canRecover)
        {
            if (!Ready || !canRecover || health.IsDead || applying || !float.IsFinite(gameplayDeltaTime) || gameplayDeltaTime <= 0) return;
            var waiting = Mathf.Min(delayRemaining, gameplayDeltaTime);
            delayRemaining -= waiting;
            var gain = Mathf.Min((gameplayDeltaTime - waiting) * tuning.PassiveEnergyPerSecond,
                Mathf.Max(0, PassiveEnergyCeiling - wallet.GetCurrent(energy)));
            if (gain > 0) wallet.Gain(energy, gain);
        }

        public static bool IsRecoveryCard(CardDefinitionSO card)
        {
            if (card?.Effect == null) return false;
            foreach (var op in card.Effect.CommitOperations)
                if (op.Kind is CardOperationKind.SacrificeHealthForEnergy or CardOperationKind.ConvertEnergyToHealth or CardOperationKind.Heal) return true;
            return false;
        }

        public bool TryQuote(CardDefinitionSO card, PlayerCardCommitSnapshot snapshot, out RecoveryCardQuote quote)
        {
            quote = default;
            Failure = CardCommitFailure.None;
            if (!Ready) return Fail(CardCommitFailure.MissingDependency);
            if (!IsRecoveryCard(card) || card.Category != PlayerCardTimeState.Neutral
                || card.Effect.CommitOperations.Count != 1 || card.Effect.ReactiveRules.Count != 0
                || card.GetValidationErrors().Count != 0) return Fail(CardCommitFailure.InvalidDefinition);
            foreach (var cost in card.FixedCosts)
                if (cost.Amount != 0) return Fail(CardCommitFailure.InvalidDefinition);
            if (snapshot != null)
                foreach (var delta in snapshot.ResourceCostDeltas)
                    if (delta.Amount != 0) return Fail(CardCommitFailure.UnsupportedEffect);
            var equipped = false;
            foreach (var candidate in inventory.GetEquippedCards(card.Category))
                if (candidate == card) equipped = true;
            if (!equipped) return Fail(CardCommitFailure.InvalidSelection);
            var currentHealth = snapshot?.CurrentHealth ?? health.CurrentHealth;
            var maximumHealth = snapshot?.MaximumHealth ?? health.MaximumHealth;
            var currentEnergy = snapshot?.GetCurrent(energy) ?? wallet.GetCurrent(energy);
            var maximumEnergy = snapshot?.GetMaximum(energy) ?? wallet.GetMaximum(energy);
            if (currentHealth <= 0) return Fail(CardCommitFailure.InsufficientHealth);
            var copies = card.ConsumptionPolicy == CardConsumptionPolicy.ConsumeOnSuccess ? 1 : 0;
            if (inventory.GetCount(card.Id) <= 0) return Fail(CardCommitFailure.DepletedStock);
            var basis = NeutralCostBasis;
            var spent = 0f; var gained = 0f; var healthSpent = 0f; var restored = 0f;
            switch (card.Effect.CommitOperations[0].Kind)
            {
                case CardOperationKind.SacrificeHealthForEnergy:
                    healthSpent = tuning.SacrificeHealthCost;
                    gained = SacrificeEnergyGain;
                    if (currentHealth - healthSpent < 1) return Fail(CardCommitFailure.InsufficientHealth);
                    if (maximumEnergy - currentEnergy < gained) return Fail(CardCommitFailure.InsufficientEnergyCapacity);
                    break;
                case CardOperationKind.ConvertEnergyToHealth:
                    if (currentHealth >= maximumHealth) return Fail(CardCommitFailure.FullHealth);
                    var conversion = PlayerRecoveryEconomy.QuoteEnergyHealing(currentEnergy, currentHealth, maximumHealth, basis, tuning.HealingChunkMultiplier);
                    if (!conversion.HasEffect) return Fail(CardCommitFailure.InsufficientLiveResources);
                    spent = conversion.EnergySpent; restored = conversion.HealthRestored;
                    break;
                case CardOperationKind.Heal:
                    if (copies != 1) return Fail(CardCommitFailure.InvalidDefinition);
                    if (currentHealth >= maximumHealth) return Fail(CardCommitFailure.FullHealth);
                    restored = Mathf.Min(tuning.ConsumableHealAmount, maximumHealth - currentHealth);
                    break;
            }
            quote = new RecoveryCardQuote(basis, spent, gained, healthSpent, restored, copies, inventory.Revision, transactionRevision);
            return quote.HasEffect;
        }

        public bool TryApply(CardDefinitionSO card, RecoveryCardQuote quote)
        {
            if (applying) return false;
            if (!TryQuote(card, null, out var live)) return false;
            if (!quote.HasEffect || !quote.Equals(live)) return Fail(CardCommitFailure.StaleRecoveryQuote);
            applying = true;
            try
            {
                // All validation precedes these writes; observers run only after every balance is final.
                var healthNotification = health.ChangeRecoveryDeferred(quote.HealthRestored - quote.HealthSpent);
                var energyNotification = wallet.ChangeRecoveryDeferred(energy, quote.EnergyGained - quote.EnergySpent);
                var inventoryNotification = inventory.ConsumeDeferred(card, quote.CopiesConsumed);
                transactionRevision++;
                healthNotification?.Invoke();
                energyNotification?.Invoke();
                inventoryNotification?.Invoke();
                return true;
            }
            finally { applying = false; }
        }
        private bool Fail(CardCommitFailure failure) { Failure = failure; return false; }

        public string GetPresentation(CardDefinitionSO card)
        {
            var stock = card.ConsumptionPolicy == CardConsumptionPolicy.ConsumeOnSuccess && inventory != null ? $" ×{inventory.GetCount(card.Id)}" : "";
            if (!TryQuote(card, null, out var quote))
            {
                var reason = Failure switch {
                    CardCommitFailure.FullHealth => "HP full",
                    CardCommitFailure.InsufficientHealth => "Must keep 1 HP",
                    CardCommitFailure.InsufficientEnergyCapacity => $"Needs {SacrificeEnergyGain:0.#} energy space",
                    CardCommitFailure.DepletedStock => "No copies left",
                    CardCommitFailure.InsufficientLiveResources => $"Needs {EnergyPerHealth:0.#} energy",
                    _ => "Unavailable" };
                return $"{card.DisplayName}{stock}\n{reason}";
            }
            return quote.HealthSpent > 0 ? $"{card.DisplayName}{stock}\n−{quote.HealthSpent:0.#} HP → +{quote.EnergyGained:0.#} energy"
                : quote.EnergySpent > 0 ? $"{card.DisplayName}{stock}\n−{quote.EnergySpent:0.#} energy → +{quote.HealthRestored:0.#} HP"
                : $"{card.DisplayName}{stock}\n+{quote.HealthRestored:0.#} HP • Consume 1";
        }
    }
}
