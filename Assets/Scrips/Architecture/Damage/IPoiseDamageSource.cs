using UnityEngine;

namespace TicGame.Architecture
{
    /// <summary>
    /// Provides target-specific poise damage for a damage transaction.
    /// </summary>
    public interface IPoiseDamageSource
    {
        /// <summary>
        /// Gets the non-negative poise damage authored for a particular target.
        /// </summary>
        float GetPoiseDamage(in DamageInstance instance, GameObject target);
    }
}
