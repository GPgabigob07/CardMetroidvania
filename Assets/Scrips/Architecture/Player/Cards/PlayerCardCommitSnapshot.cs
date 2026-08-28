using System.Collections.Generic;
using UnityEngine;

namespace TicGame.Architecture
{
    public sealed class PlayerCardCommitSnapshot
    {
        private readonly List<PlayerCardResourceSnapshot> resources;
        private readonly List<ResourceAmount> resourceCostDeltas;

        public PlayerCardCommitSnapshot(
            PlayerCardTimeState category,
            string attackExecutionId,
            bool isAirborne,
            float currentHealth,
            float maximumHealth,
            IEnumerable<PlayerCardResourceSnapshot> resourceSnapshots,
            IEnumerable<ResourceAmount> costDeltas = null)
        {
            Category = category;
            AttackExecutionId = attackExecutionId;
            IsAirborne = isAirborne;
            CurrentHealth = Mathf.Max(0f, currentHealth);
            MaximumHealth = Mathf.Max(0f, maximumHealth);
            resources = resourceSnapshots != null
                ? new List<PlayerCardResourceSnapshot>(resourceSnapshots)
                : new List<PlayerCardResourceSnapshot>();
            resourceCostDeltas = costDeltas != null
                ? new List<ResourceAmount>(costDeltas)
                : new List<ResourceAmount>();
        }

        public PlayerCardTimeState Category { get; }
        public string AttackExecutionId { get; }
        public bool IsAirborne { get; }
        public float CurrentHealth { get; }
        public float MaximumHealth { get; }
        public IReadOnlyList<PlayerCardResourceSnapshot> Resources => resources;
        public IReadOnlyList<ResourceAmount> ResourceCostDeltas => resourceCostDeltas;

        public float GetCurrent(ResourceDefinitionSO resource)
        {
            return FindResource(resource)?.Current ?? 0f;
        }

        public float GetMaximum(ResourceDefinitionSO resource)
        {
            return FindResource(resource)?.Maximum ?? 0f;
        }

        public bool CanSpend(IReadOnlyList<ResourceAmount> costs)
        {
            foreach (var pair in AggregateCosts(costs, resourceCostDeltas))
            {
                if (pair.Key == null || GetCurrent(pair.Key) + 0.0001f < pair.Value)
                {
                    return false;
                }
            }

            return true;
        }

        public IReadOnlyList<ResourceAmount> BuildAdjustedCosts(
            IReadOnlyList<ResourceAmount> costs)
        {
            var adjusted = new List<ResourceAmount>();
            foreach (var pair in AggregateCosts(costs, resourceCostDeltas))
            {
                if (pair.Key != null && pair.Value > 0f)
                {
                    adjusted.Add(new ResourceAmount(pair.Key, pair.Value));
                }
            }

            return adjusted;
        }

        public PlayerCardCommitSnapshot WithResourceCostDelta(ResourceAmount delta)
        {
            var deltas = new List<ResourceAmount>(resourceCostDeltas);
            deltas.Add(delta);
            return new PlayerCardCommitSnapshot(
                Category,
                AttackExecutionId,
                IsAirborne,
                CurrentHealth,
                MaximumHealth,
                resources,
                deltas);
        }

        private PlayerCardResourceSnapshot? FindResource(ResourceDefinitionSO resource)
        {
            if (resource == null)
            {
                return null;
            }

            foreach (var snapshot in resources)
            {
                if (snapshot.Resource == resource)
                {
                    return snapshot;
                }
            }

            return null;
        }

        private static Dictionary<ResourceDefinitionSO, float> AggregateCosts(
            IReadOnlyList<ResourceAmount> costs,
            IReadOnlyList<ResourceAmount> deltas)
        {
            var totals = new Dictionary<ResourceDefinitionSO, float>();
            AddCosts(totals, costs);
            AddCosts(totals, deltas);
            return totals;
        }

        private static void AddCosts(
            Dictionary<ResourceDefinitionSO, float> totals,
            IReadOnlyList<ResourceAmount> costs)
        {
            if (costs == null)
            {
                return;
            }

            foreach (var cost in costs)
            {
                if (cost.Resource == null || cost.Amount <= 0f)
                {
                    continue;
                }

                if (!totals.TryAdd(cost.Resource, cost.Amount))
                {
                    totals[cost.Resource] += cost.Amount;
                }
            }
        }
    }
}
