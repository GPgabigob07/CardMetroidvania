using System;
using UnityEngine;

namespace TicGame.Architecture
{
    [Serializable]
    public struct ResourceAmount
    {
        [SerializeField] private ResourceDefinitionSO resource;
        [Min(0f)]
        [SerializeField] private float amount;

        public ResourceAmount(ResourceDefinitionSO resource, float amount)
        {
            this.resource = resource;
            this.amount = Mathf.Max(0f, amount);
        }

        public ResourceDefinitionSO Resource => resource;
        public float Amount => Mathf.Max(0f, amount);
    }
}
