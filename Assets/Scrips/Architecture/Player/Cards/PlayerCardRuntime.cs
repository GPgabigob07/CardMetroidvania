using System;
using System.Collections.Generic;
using UnityEngine;

namespace TicGame.Architecture
{
    public sealed class PlayerCardRuntime :
        MonoBehaviour,
        IPlayerCardReader,
        IGameplayServicesConsumer
    {
        [Header("Components")] [SerializeField]
        private PlayerResourceWallet wallet;

        [SerializeField] private PlayerCombatEffects combatEffects;
        [SerializeField] private PlayerExtraJumpRuntime extraJump;

        [Header("Equipped Cards")] [SerializeField]
        private CardDefinitionSO neutralCardDefinition;

        [SerializeField] private CardDefinitionSO chainCardDefinition;
        [SerializeField] private CardDefinitionSO finisherCardDefinition;

        private ICardFeedbackService cardFeedback;

        public event Action<PlayerCardTimeState, CardDefinitionSO> EquippedCardChanged;

        public CardDefinitionSO NeutralCard => neutralCardDefinition;
        public CardDefinitionSO ChainCard => chainCardDefinition;
        public CardDefinitionSO FinisherCard => finisherCardDefinition;

        public bool CanCommit(
            PlayerCardTimeState category,
            string attackExecutionId,
            bool isAirborne
        ) {
            var card = GetEquippedCard(category);
            if (!IsCardValidForCategory(card, category)
                || wallet == null
                || combatEffects == null
                || extraJump == null) {
                return false;
            }

            var context = new ExecutionContext(
                attackExecutionId,
                isAirborne);
            if (!AreConditionsMet(card.Effect.ActivationConditions, context)
                || !CanExecuteOperations(card.Effect, context)) {
                return false;
            }

            return wallet.CanSpend(BuildCosts(card));
        }

        public bool Commit(
            PlayerCardTimeState category,
            string attackExecutionId,
            bool isAirborne
        ) {
            if (!CanCommit(category, attackExecutionId, isAirborne)) {
                return false;
            }

            var card = GetEquippedCard(category);
            var costs = BuildCosts(card);
            if (!wallet.TrySpend(costs)) {
                return false;
            }

            ApplyOperations(
                card,
                card.Effect,
                new ExecutionContext(attackExecutionId, isAirborne));
            return true;
        }

        public CardReadinessResult TryPrepare(
            CardDefinitionSO card,
            CardTimeSelectionTransaction selection,
            PlayerCardCommitSnapshot snapshot
        ) {
            if (selection == null || !selection.IsValid) {
                return CardReadinessResult.Failed(
                    CardCommitFailure.InvalidSelection,
                    card);
            }

            if (card == null || !selection.TryGetSelectedCard(out var selected)) {
                return CardReadinessResult.Failed(CardCommitFailure.NoSelectedCard);
            }

            if (card != selected) {
                return CardReadinessResult.Failed(
                    CardCommitFailure.InvalidSelection,
                    card);
            }

            if (snapshot == null
                || snapshot.Category != selection.Category
                || card.Category != selection.Category) {
                return CardReadinessResult.Failed(
                    CardCommitFailure.CategoryMismatch,
                    card);
            }

            if (!IsCardValidForCategory(card, selection.Category)) {
                return CardReadinessResult.Failed(
                    CardCommitFailure.InvalidDefinition,
                    card);
            }

            if (wallet == null || combatEffects == null || extraJump == null) {
                return CardReadinessResult.Failed(
                    CardCommitFailure.MissingDependency,
                    card);
            }

            var context = new ExecutionContext(
                snapshot.AttackExecutionId,
                snapshot.IsAirborne);
            if (!AreConditionsMet(card.Effect.ActivationConditions, context, snapshot)) {
                return CardReadinessResult.Failed(
                    CardCommitFailure.UnmetCondition,
                    card);
            }

            if (!CanExecuteOperations(card.Effect, context)) {
                return CardReadinessResult.Failed(
                    CardCommitFailure.UnsupportedEffect,
                    card);
            }

            var costs = snapshot.BuildAdjustedCosts(BuildCosts(card));
            if (!snapshot.CanSpend(BuildCosts(card))) {
                return CardReadinessResult.Failed(
                    CardCommitFailure.InsufficientSnapshotResources,
                    card);
            }

            var commit = new PreparedCardCommit(
                this,
                card,
                selection.SessionId,
                costs,
                snapshot);
            return CardReadinessResult.Success(card, costs, commit);
        }

        public CardDefinitionSO GetEquippedCard(
            PlayerCardTimeState category
        ) {
            return category switch {
                PlayerCardTimeState.Neutral => neutralCardDefinition,
                PlayerCardTimeState.Chain => chainCardDefinition,
                PlayerCardTimeState.Finisher => finisherCardDefinition,
                _ => null
            };
        }

        public bool EquipCard(
            PlayerCardTimeState category,
            CardDefinitionSO card
        ) {
            if (category == PlayerCardTimeState.None
                || card != null && card.Category != category) {
                return false;
            }

            switch (category) {
                case PlayerCardTimeState.Neutral: neutralCardDefinition = card; break;
                case PlayerCardTimeState.Chain: chainCardDefinition = card; break;
                case PlayerCardTimeState.Finisher: finisherCardDefinition = card; break;
                default: return false;
            }

            EquippedCardChanged?.Invoke(category, card);
            return true;
        }

        public bool EquipCard(
            CardDefinitionSO card
        ) {
            return card != null && EquipCard(card.Category, card);
        }

        public void Configure(
            PlayerResourceWallet resourceWallet,
            PlayerCombatEffects effects,
            PlayerExtraJumpRuntime extraJumpRuntime
        ) {
            wallet = resourceWallet;
            combatEffects = effects;
            extraJump = extraJumpRuntime;
        }

        public void ConfigureCardDefinitions(
            CardDefinitionSO neutral,
            CardDefinitionSO chain,
            CardDefinitionSO finisher
        ) {
            EquipCard(PlayerCardTimeState.Neutral, neutral);
            EquipCard(PlayerCardTimeState.Chain, chain);
            EquipCard(PlayerCardTimeState.Finisher, finisher);
        }

        public void BindGameplayServices(IGameplayServices services)
        {
            cardFeedback = services?.CardFeedback;
        }

        internal bool TryApplyPreparedCommit(
            PreparedCardCommit commit
        ) {
            if (commit == null
                || commit.Card == null
                || commit.IsApplied
                || wallet == null) {
                commit?.SetFailure(CardCommitFailure.MissingDependency);
                return false;
            }

            if (!wallet.TrySpend(commit.Costs)) {
                commit.SetFailure(CardCommitFailure.InsufficientLiveResources);
                PublishCommitFeedback(commit.Card, CardFeedbackKind.Failed);
                return false;
            }

            ApplyOperations(
                commit.Card,
                commit.Card.Effect,
                new ExecutionContext(
                    commit.Snapshot.AttackExecutionId,
                    commit.Snapshot.IsAirborne));
            PublishCommitFeedback(commit.Card, CardFeedbackKind.Activated);
            return true;
        }

        private bool AreConditionsMet(
            IReadOnlyList<CardConditionDefinition> conditions,
            in ExecutionContext context
        ) {
            if (conditions == null) {
                return true;
            }

            foreach (var condition in conditions) {
                var passed = condition.Kind switch {
                    CardConditionKind.IsAirborne => context.IsAirborne,
                    CardConditionKind.IsGrounded => !context.IsAirborne,
                    CardConditionKind.HasAttackExecution =>
                        !string.IsNullOrWhiteSpace(context.AttackExecutionId),
                    CardConditionKind.ResourceAtLeast =>
                        wallet.GetCurrent(condition.Resource) >= condition.Amount,
                    CardConditionKind.AbilityAvailable =>
                        extraJump.Supports(condition.Ability),
                    CardConditionKind.AbilityUnlocked =>
                        extraJump.IsUnlocked(condition.Ability),
                    _ => false
                };

                if (!passed) {
                    return false;
                }
            }

            return true;
        }

        private bool AreConditionsMet(
            IReadOnlyList<CardConditionDefinition> conditions,
            in ExecutionContext context,
            PlayerCardCommitSnapshot snapshot
        ) {
            if (conditions == null) {
                return true;
            }

            foreach (var condition in conditions) {
                var passed = condition.Kind switch {
                    CardConditionKind.IsAirborne => context.IsAirborne,
                    CardConditionKind.IsGrounded => !context.IsAirborne,
                    CardConditionKind.HasAttackExecution =>
                        !string.IsNullOrWhiteSpace(context.AttackExecutionId),
                    CardConditionKind.ResourceAtLeast =>
                        snapshot.GetCurrent(condition.Resource) >= condition.Amount,
                    CardConditionKind.AbilityAvailable =>
                        extraJump.Supports(condition.Ability),
                    CardConditionKind.AbilityUnlocked =>
                        extraJump.IsUnlocked(condition.Ability),
                    _ => false
                };

                if (!passed) {
                    return false;
                }
            }

            return true;
        }

        private bool CanExecuteOperations(
            CardEffectDefinitionSO effect,
            in ExecutionContext context
        ) {
            foreach (var operation in effect.CommitOperations) {
                switch (operation.Kind) {
                    case CardOperationKind.GainResource:
                    case CardOperationKind.AddStatusCharges:
                    case CardOperationKind.AddStatusCapacity:
                    case CardOperationKind.ModifyDamage: break;
                    case CardOperationKind.ArmSupplementalDamage:
                        if (string.IsNullOrWhiteSpace(context.AttackExecutionId)) {
                            return false;
                        }

                        break;
                    case CardOperationKind.InvokeAbility:
                        if (!extraJump.CanInvoke(operation.Ability)) {
                            return false;
                        }

                        break;
                    default: return false;
                }
            }

            return HasSupportedEffectShape(effect);
        }

        private static bool HasSupportedEffectShape(
            CardEffectDefinitionSO effect
        ) {
            foreach (var operation in effect.CommitOperations) {
                if (operation.Kind is CardOperationKind.GainResource
                    or CardOperationKind.ArmSupplementalDamage
                    or CardOperationKind.InvokeAbility) {
                    return true;
                }

                if (operation.Kind == CardOperationKind.AddStatusCharges
                    && (TryGetReactiveMultiplier(
                            effect,
                            CardOperationKind.ModifyKnockback,
                            out _)
                        || TryGetReactiveMultiplier(
                            effect,
                            CardOperationKind.ModifyResourceGain,
                            out _))) {
                    return true;
                }

                if (operation.Kind == CardOperationKind.AddStatusCapacity
                    && TryGetCommitMultiplier(
                        effect,
                        CardOperationKind.ModifyDamage,
                        out _)) {
                    return true;
                }
            }

            return false;
        }

        private void ApplyOperations(
            CardDefinitionSO card,
            CardEffectDefinitionSO effect,
            in ExecutionContext context
        ) {
            foreach (var operation in effect.CommitOperations) {
                switch (operation.Kind) {
                    case CardOperationKind.GainResource: wallet.Gain(operation.Resource, operation.Amount); break;
                    case CardOperationKind.AddStatusCharges: ApplyChargeOperation(card, effect, operation); break;
                    case CardOperationKind.AddStatusCapacity: ApplyCapacityOperation(card, effect, operation); break;
                    case CardOperationKind.ModifyDamage: break;
                    case CardOperationKind.ArmSupplementalDamage:
                        var totalMultiplier = 1f
                                              + operation.Amount * operation.Multiplier;
                        combatEffects.ArmSupplementalDamage(
                            context.AttackExecutionId,
                            operation.EffectId,
                            totalMultiplier,
                            card);
                        break;
                    case CardOperationKind.InvokeAbility: extraJump.Invoke(operation.Ability, card); break;
                }
            }
        }

        private void ApplyChargeOperation(
            CardDefinitionSO card,
            CardEffectDefinitionSO effect,
            in CardOperationDefinition operation
        ) {
            var amount = Mathf.RoundToInt(operation.Amount);
            if (TryGetReactiveMultiplier(
                    effect,
                    CardOperationKind.ModifyKnockback,
                    out var knockbackMultiplier)) {
                combatEffects.AddKnockbackCharges(amount, knockbackMultiplier, card);
                return;
            }

            if (TryGetReactiveMultiplier(
                    effect,
                    CardOperationKind.ModifyResourceGain,
                    out var resourceMultiplier)) {
                combatEffects.AddEnergyGainCharges(amount, resourceMultiplier, card);
            }
        }

        private void ApplyCapacityOperation(
            CardDefinitionSO card,
            CardEffectDefinitionSO effect,
            in CardOperationDefinition operation
        ) {
            if (TryGetCommitMultiplier(
                    effect,
                    CardOperationKind.ModifyDamage,
                    out var damagePerIncrement)) {
                combatEffects.AddChainCapacity(
                    Mathf.RoundToInt(operation.Amount),
                    damagePerIncrement,
                    card);
            }
        }

        private void PublishCommitFeedback(CardDefinitionSO card, CardFeedbackKind kind)
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

        private IReadOnlyList<ResourceAmount> BuildCosts(
            CardDefinitionSO card
        ) {
            var costs = new List<ResourceAmount>(card.FixedCosts);
            foreach (var operation in card.Effect.CommitOperations) {
                if (operation.Kind == CardOperationKind.ArmSupplementalDamage
                    && operation.Resource != null
                    && operation.Amount > 0f) {
                    costs.Add(new ResourceAmount(operation.Resource, operation.Amount));
                }
            }

            return costs;
        }

        private static bool TryGetReactiveMultiplier(
            CardEffectDefinitionSO effect,
            CardOperationKind kind,
            out float multiplier
        ) {
            foreach (var rule in effect.ReactiveRules) {
                foreach (var operation in rule.Operations) {
                    if (operation.Kind == kind) {
                        multiplier = operation.Multiplier;
                        return true;
                    }
                }
            }

            multiplier = 0f;
            return false;
        }

        private static bool TryGetCommitMultiplier(
            CardEffectDefinitionSO effect,
            CardOperationKind kind,
            out float multiplier
        ) {
            foreach (var operation in effect.CommitOperations) {
                if (operation.Kind == kind) {
                    multiplier = operation.Multiplier;
                    return true;
                }
            }

            multiplier = 0f;
            return false;
        }

        private static bool IsCardValidForCategory(
            CardDefinitionSO card,
            PlayerCardTimeState category
        ) {
            return card != null
                   && card.Category == category
                   && card.GetValidationErrors().Count == 0;
        }

        private readonly struct ExecutionContext
        {
            public ExecutionContext(
                string attackExecutionId,
                bool isAirborne
            ) {
                AttackExecutionId = attackExecutionId;
                IsAirborne = isAirborne;
            }

            public string AttackExecutionId { get; }
            public bool IsAirborne { get; }
        }
    }
}
