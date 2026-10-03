using System;
using System.Collections.Generic;
using UnityEngine;

namespace TicGame.Architecture
{
    public sealed class EnemyCastHitBudget
    {
        private readonly int limit;
        private readonly Dictionary<GameObject, int> acceptedHits = new();
        private readonly HashSet<GameObject> reservations = new();
        public long CastToken { get; }
        public EnemyCastHitBudget(long castToken, int acceptedHitLimit)
        {
            if (castToken <= 0) throw new ArgumentOutOfRangeException(nameof(castToken));
            if (acceptedHitLimit < 1) throw new ArgumentOutOfRangeException(nameof(acceptedHitLimit));
            CastToken = castToken; limit = acceptedHitLimit;
        }
        public bool TryReserve(GameObject canonicalTarget)
        {
            if (canonicalTarget == null || reservations.Contains(canonicalTarget)
                || (acceptedHits.TryGetValue(canonicalTarget, out var hits) && hits >= limit)) return false;
            return reservations.Add(canonicalTarget);
        }
        public void Complete(GameObject canonicalTarget, bool accepted)
        {
            if (canonicalTarget == null || !reservations.Remove(canonicalTarget) || !accepted) return;
            acceptedHits.TryGetValue(canonicalTarget, out var hits);
            acceptedHits[canonicalTarget] = hits + 1;
        }
    }
}
