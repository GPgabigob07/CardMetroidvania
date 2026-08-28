using System;
using System.Collections.Generic;
using UnityEngine;

namespace TicGame.Architecture
{
    [Serializable]
    public sealed class CardReactiveRule
    {
        [SerializeField] private CardTriggerKind trigger;
        [SerializeField] private List<CardConditionDefinition> conditions = new();
        [SerializeField] private List<CardOperationDefinition> operations = new();
        [Tooltip("Whether a matching event consumes one charge from the status instance.")]
        [SerializeField] private bool consumesCharge;

        public CardReactiveRule(
            CardTriggerKind trigger,
            IEnumerable<CardConditionDefinition> conditions,
            IEnumerable<CardOperationDefinition> operations,
            bool consumesCharge = false)
        {
            this.trigger = trigger;
            this.conditions = conditions != null
                ? new List<CardConditionDefinition>(conditions)
                : new List<CardConditionDefinition>();
            this.operations = operations != null
                ? new List<CardOperationDefinition>(operations)
                : new List<CardOperationDefinition>();
            this.consumesCharge = consumesCharge;
        }

        public CardTriggerKind Trigger => trigger;
        public IReadOnlyList<CardConditionDefinition> Conditions => conditions;
        public IReadOnlyList<CardOperationDefinition> Operations => operations;
        public bool ConsumesCharge => consumesCharge;

        public bool IsValid()
        {
            if (operations == null || operations.Count == 0)
            {
                return false;
            }

            if (conditions != null)
            {
                foreach (var condition in conditions)
                {
                    if (!condition.IsValid())
                    {
                        return false;
                    }
                }
            }

            foreach (var operation in operations)
            {
                if (!operation.IsValid())
                {
                    return false;
                }
            }

            return true;
        }
    }
}
