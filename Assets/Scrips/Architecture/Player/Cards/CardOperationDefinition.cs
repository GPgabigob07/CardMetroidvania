using System;
using UnityEngine;

namespace TicGame.Architecture
{
    [Serializable]
    public struct CardOperationDefinition
    {
        [SerializeField] private CardOperationKind kind;
        [Tooltip("Status affected by status and modifier operations.")]
        [SerializeField] private CardStatusDefinitionSO status;
        [Tooltip("Resource affected by resource operations.")]
        [SerializeField] private ResourceDefinitionSO resource;
        [Tooltip("Ability invoked by an ability operation.")]
        [SerializeField] private AbilityDefinitionSO ability;
        [Min(0f)]
        [Tooltip("Primary authored amount, charge count, or capacity increase.")]
        [SerializeField] private float amount;
        [Min(0f)]
        [Tooltip("Authored multiplier used by modifier operations.")]
        [SerializeField] private float multiplier;
        [Tooltip("Stable effect id used by linked supplemental damage provenance.")]
        [SerializeField] private string effectId;

        public CardOperationDefinition(
            CardOperationKind kind,
            CardStatusDefinitionSO status = null,
            ResourceDefinitionSO resource = null,
            AbilityDefinitionSO ability = null,
            float amount = 0f,
            float multiplier = 1f,
            string effectId = null)
        {
            this.kind = kind;
            this.status = status;
            this.resource = resource;
            this.ability = ability;
            this.amount = amount;
            this.multiplier = multiplier;
            this.effectId = effectId;
        }

        public CardOperationKind Kind => kind;
        public CardStatusDefinitionSO Status => status;
        public ResourceDefinitionSO Resource => resource;
        public AbilityDefinitionSO Ability => ability;
        public float Amount => amount;
        public float Multiplier => multiplier;
        public string EffectId => effectId;

        public bool IsValid()
        {
            if (!float.IsFinite(amount)
                || amount < 0f
                || !float.IsFinite(multiplier)
                || multiplier < 0f)
            {
                return false;
            }

            return kind switch
            {
                CardOperationKind.GainResource => resource != null,
                CardOperationKind.AddStatusCharges => status != null,
                CardOperationKind.AddStatusCapacity => status != null,
                CardOperationKind.AddStatusStacks => status != null,
                CardOperationKind.ModifyDamage => status != null,
                CardOperationKind.ModifyKnockback => status != null,
                CardOperationKind.ModifyResourceGain => status != null,
                CardOperationKind.ArmSupplementalDamage =>
                    !string.IsNullOrWhiteSpace(effectId),
                CardOperationKind.ClearStatusStacks => status != null,
                CardOperationKind.RemoveStatus => status != null,
                CardOperationKind.InvokeAbility => ability != null,
                _ => false
            };
        }
    }
}
