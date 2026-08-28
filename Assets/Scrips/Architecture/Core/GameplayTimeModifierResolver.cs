using System.Collections.Generic;
using UnityEngine;

namespace TicGame.Architecture
{
    public static class GameplayTimeModifierResolver
    {
        public static float Resolve(
            float baselineScale,
            IEnumerable<GameplayTimeModifier> modifiers)
        {
            var effectiveScale = Mathf.Clamp01(baselineScale);
            foreach (var modifier in modifiers)
            {
                effectiveScale = Mathf.Min(effectiveScale, modifier.RequestedScale);
            }

            return effectiveScale;
        }
    }
}
