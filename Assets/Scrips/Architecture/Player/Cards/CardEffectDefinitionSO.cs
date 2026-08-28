using System.Collections.Generic;
using UnityEngine;

namespace TicGame.Architecture
{
    [CreateAssetMenu(
        menuName = "TIC/Cards/Effect Definition",
        fileName = "Effect_")]
    public sealed class CardEffectDefinitionSO : ScriptableObject
    {
        [Header("Status")]
        [Tooltip("Shared status identity used for stacking. Optional for immediate and ability effects.")]
        [SerializeField] private CardStatusDefinitionSO status;

        [Header("Activation")]
        [SerializeField] private List<CardConditionDefinition> activationConditions = new();

        [Header("Commit")]
        [SerializeField] private List<CardOperationDefinition> commitOperations = new();

        [Header("Reactive Rules")]
        [SerializeField] private List<CardReactiveRule> reactiveRules = new();

        [Header("Lifetime")]
        [SerializeField] private List<CardLifetimeDefinition> lifetimes = new()
        {
            new CardLifetimeDefinition(CardLifetimeKind.Immediate)
        };

        [Header("Stacking")]
        [SerializeField] private CardStackingDefinition stacking =
            new(CardStackingKind.RejectIfActive);

        public CardStatusDefinitionSO Status => status;
        public IReadOnlyList<CardConditionDefinition> ActivationConditions =>
            activationConditions;
        public IReadOnlyList<CardOperationDefinition> CommitOperations =>
            commitOperations;
        public IReadOnlyList<CardReactiveRule> ReactiveRules => reactiveRules;
        public IReadOnlyList<CardLifetimeDefinition> Lifetimes => lifetimes;
        public CardStackingDefinition Stacking => stacking;

        public void Configure(
            CardStatusDefinitionSO statusDefinition,
            IEnumerable<CardConditionDefinition> conditions,
            IEnumerable<CardOperationDefinition> operations,
            IEnumerable<CardReactiveRule> rules,
            IEnumerable<CardLifetimeDefinition> lifetimeDefinitions,
            CardStackingDefinition stackingDefinition)
        {
            status = statusDefinition;
            activationConditions = conditions != null
                ? new List<CardConditionDefinition>(conditions)
                : new List<CardConditionDefinition>();
            commitOperations = operations != null
                ? new List<CardOperationDefinition>(operations)
                : new List<CardOperationDefinition>();
            reactiveRules = rules != null
                ? new List<CardReactiveRule>(rules)
                : new List<CardReactiveRule>();
            lifetimes = lifetimeDefinitions != null
                ? new List<CardLifetimeDefinition>(lifetimeDefinitions)
                : new List<CardLifetimeDefinition>();
            stacking = stackingDefinition;
        }

        public IReadOnlyList<string> GetValidationErrors()
        {
            var errors = new List<string>();
            if ((commitOperations == null || commitOperations.Count == 0)
                && (reactiveRules == null || reactiveRules.Count == 0))
            {
                errors.Add("Effect must contain at least one operation or reactive rule.");
            }

            ValidateConditions(errors);
            ValidateOperations(errors);
            ValidateRules(errors);

            if (lifetimes == null || lifetimes.Count == 0)
            {
                errors.Add("Effect must define at least one lifetime.");
            }

            if (RequiresStatus() && status == null)
            {
                errors.Add("Status-based effects require a status definition.");
            }

            return errors;
        }

        private void ValidateConditions(List<string> errors)
        {
            if (activationConditions == null)
            {
                return;
            }

            foreach (var condition in activationConditions)
            {
                if (!condition.IsValid())
                {
                    errors.Add($"Invalid activation condition: {condition.Kind}.");
                }
            }
        }

        private void ValidateOperations(List<string> errors)
        {
            if (commitOperations == null)
            {
                return;
            }

            foreach (var operation in commitOperations)
            {
                if (!operation.IsValid())
                {
                    errors.Add($"Invalid commit operation: {operation.Kind}.");
                }
            }
        }

        private void ValidateRules(List<string> errors)
        {
            if (reactiveRules == null)
            {
                return;
            }

            foreach (var rule in reactiveRules)
            {
                if (rule == null || !rule.IsValid())
                {
                    errors.Add("Effect contains an invalid reactive rule.");
                }
            }
        }

        private bool RequiresStatus()
        {
            if (stacking.Kind != CardStackingKind.RejectIfActive)
            {
                return true;
            }

            if (reactiveRules != null && reactiveRules.Count > 0)
            {
                return true;
            }

            if (commitOperations == null)
            {
                return false;
            }

            foreach (var operation in commitOperations)
            {
                switch (operation.Kind)
                {
                    case CardOperationKind.AddStatusCharges:
                    case CardOperationKind.AddStatusCapacity:
                    case CardOperationKind.AddStatusStacks:
                    case CardOperationKind.ModifyDamage:
                    case CardOperationKind.ModifyKnockback:
                    case CardOperationKind.ModifyResourceGain:
                    case CardOperationKind.ClearStatusStacks:
                    case CardOperationKind.RemoveStatus:
                        return true;
                }
            }

            return false;
        }
    }
}
