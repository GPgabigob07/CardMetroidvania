using System.Collections.Generic;
using UnityEngine;

namespace TicGame.Architecture
{
    public static class PlayerRecoveryEconomy
    {
        public static float ResolveNeutralCost(IEnumerable<CardDefinitionSO> equipped, ResourceDefinitionSO energy, float fallback = 20)
        {
            var highest = 0f;
            if (equipped != null && energy != null)
                foreach (var card in equipped)
                {
                    if (card == null || card.Category != PlayerCardTimeState.Neutral) continue;
                    var sum = 0f;
                    foreach (var cost in card.FixedCosts)
                        if (cost.Resource == energy && float.IsFinite(cost.Amount) && cost.Amount >= 0) sum += cost.Amount;
                    if (float.IsFinite(sum)) highest = Mathf.Max(highest, sum);
                }
            return highest > 0 ? highest : float.IsFinite(fallback) && fallback > 0 ? fallback : 20;
        }

        public static float GetPassiveCeiling(float basis, float maximumEnergy, float multiplier)
            => Mathf.Min(maximumEnergy, basis * multiplier);

        public static RecoveryCardQuote QuoteEnergyHealing(float energy, float health, float maxHealth, float basis, float multiplier)
        {
            var price = basis * multiplier;
            if (!float.IsFinite(energy) || !float.IsFinite(health) || !float.IsFinite(maxHealth)
                || !float.IsFinite(price) || price <= 0 || health <= 0 || health >= maxHealth || energy < price)
                return default;
            var chunks = Mathf.Min(Mathf.Floor(energy / price), Mathf.Ceil(maxHealth - health));
            return new RecoveryCardQuote(basis, spent: chunks * price, restored: Mathf.Min(chunks, maxHealth - health));
        }
    }
}
