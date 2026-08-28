using System;
using System.Collections.Generic;
using UnityEngine;

namespace TicGame.Architecture
{
    public sealed class PlayerResourceWallet : MonoBehaviour
    {
        [Serializable]
        private sealed class Balance
        {
            [SerializeField] private ResourceDefinitionSO resource;
            [Min(0f)]
            [SerializeField] private float startingAmount = 30f;
            [Min(0f)]
            [SerializeField] private float maximumAmount = 100f;

            public ResourceDefinitionSO Resource => resource;
            public float StartingAmount => startingAmount;
            public float MaximumAmount => maximumAmount;
            public float CurrentAmount { get; set; }

            public Balance()
            {
            }

            public Balance(
                ResourceDefinitionSO resource,
                float startingAmount,
                float maximumAmount)
            {
                this.resource = resource;
                this.startingAmount = Mathf.Max(0f, startingAmount);
                this.maximumAmount = Mathf.Max(0f, maximumAmount);
                CurrentAmount = 0f;
            }
        }

        [Header("Balances")]
        [Tooltip("Authored resources owned by this player.")]
        [SerializeField] private List<Balance> balances = new();

        public event Action<ResourceDefinitionSO, float, float> Changed;

        private void Awake()
        {
            Initialize();
        }

        public void Initialize()
        {
            foreach (var balance in balances)
            {
                balance.CurrentAmount = Mathf.Clamp(
                    value: balance.StartingAmount,
                    min: 0f,
                    max: balance.MaximumAmount);
            }
        }

        public float GetCurrent(ResourceDefinitionSO resource)
        {
            return FindBalance(resource)?.CurrentAmount ?? 0f;
        }

        public float GetMaximum(ResourceDefinitionSO resource)
        {
            return FindBalance(resource)?.MaximumAmount ?? 0f;
        }

        public bool CanSpend(IReadOnlyList<ResourceAmount> costs)
        {
            if (costs != null)
            {
                foreach (var cost in costs)
                {
                    if (cost.Resource == null)
                    {
                        return false;
                    }
                }
            }

            var totals = AggregateCosts(costs);
            foreach (var pair in totals)
            {
                if (pair.Key == null || GetCurrent(pair.Key) + 0.0001f < pair.Value)
                {
                    return false;
                }
            }

            return true;
        }

        public bool TrySpend(IReadOnlyList<ResourceAmount> costs)
        {
            if (!CanSpend(costs))
            {
                return false;
            }

            foreach (var pair in AggregateCosts(costs))
            {
                Change(pair.Key, -pair.Value);
            }

            return true;
        }

        public float Gain(ResourceDefinitionSO resource, float amount)
        {
            return Change(resource, Mathf.Max(0f, amount));
        }

        public void ConfigureSingleResource(
            ResourceDefinitionSO resource,
            float startingAmount,
            float maximumAmount)
        {
            balances.Clear();
            balances.Add(new Balance(resource, startingAmount, maximumAmount));
            Initialize();
        }

        private float Change(ResourceDefinitionSO resource, float delta)
        {
            var balance = FindBalance(resource);
            if (balance == null || Mathf.Approximately(delta, 0f))
            {
                return 0f;
            }

            var previous = balance.CurrentAmount;
            balance.CurrentAmount = Mathf.Clamp(
                value: previous + delta,
                min: 0f,
                max: balance.MaximumAmount);
            var applied = balance.CurrentAmount - previous;
            if (!Mathf.Approximately(applied, 0f))
            {
                Changed?.Invoke(resource, previous, balance.CurrentAmount);
            }

            return applied;
        }

        private Balance FindBalance(ResourceDefinitionSO resource)
        {
            return balances.Find(match: balance => balance.Resource == resource);
        }

        private static Dictionary<ResourceDefinitionSO, float> AggregateCosts(
            IReadOnlyList<ResourceAmount> costs)
        {
            var totals = new Dictionary<ResourceDefinitionSO, float>();
            if (costs == null)
            {
                return totals;
            }

            foreach (var cost in costs)
            {
                if (cost.Resource == null)
                {
                    continue;
                }

                if (!totals.TryAdd(cost.Resource, cost.Amount))
                {
                    totals[cost.Resource] += cost.Amount;
                }
            }

            return totals;
        }

    }
}
