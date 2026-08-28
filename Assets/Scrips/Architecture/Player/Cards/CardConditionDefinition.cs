using System;
using UnityEngine;

namespace TicGame.Architecture
{
    [Serializable]
    public struct CardConditionDefinition
    {
        [SerializeField] private CardConditionKind kind;
        [Tooltip("Resource inspected by resource-based conditions.")]
        [SerializeField] private ResourceDefinitionSO resource;
        [Tooltip("Ability inspected by ability availability or unlock conditions.")]
        [SerializeField] private AbilityDefinitionSO ability;
        [Min(0f)]
        [Tooltip("Threshold used by numeric conditions.")]
        [SerializeField] private float amount;

        public CardConditionDefinition(
            CardConditionKind kind,
            ResourceDefinitionSO resource = null,
            AbilityDefinitionSO ability = null,
            float amount = 0f)
        {
            this.kind = kind;
            this.resource = resource;
            this.ability = ability;
            this.amount = amount;
        }

        public CardConditionKind Kind => kind;
        public ResourceDefinitionSO Resource => resource;
        public AbilityDefinitionSO Ability => ability;
        public float Amount => amount;

        public bool IsValid()
        {
            if (!float.IsFinite(amount) || amount < 0f)
            {
                return false;
            }

            return kind switch
            {
                CardConditionKind.ResourceAtLeast => resource != null,
                CardConditionKind.AbilityAvailable => ability != null,
                CardConditionKind.AbilityUnlocked => ability != null,
                _ => true
            };
        }
    }
}
