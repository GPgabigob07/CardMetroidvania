using UnityEngine;

namespace TicGame.Architecture
{
    /// <summary>Routes a queried child region to an explicit recipient on one canonical enemy.</summary>
    public interface IEnemyDamageRegion
    {
        /// <summary>Gets the actor used for per-strike deduplication.</summary>
        EnemyActor Owner { get; }
        /// <summary>Gets the region GameObject with its single damage receiver.</summary>
        GameObject DamageRecipient { get; }
        /// <summary>Gets authored selection priority when this region overlaps another on the actor.</summary>
        int Priority { get; }
    }
}
