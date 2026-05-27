using System.Collections.Generic;

namespace TicGame.Architecture
{
    /// <summary>
    /// Provides offensive data and receives reports for damage requests caused by this object.
    /// </summary>
    public interface IDamageProvider
    {
        /// <summary>
        /// Gets the current base attack value used when an instance does not provide one.
        /// </summary>
        float AttackValue { get; }

        /// <summary>
        /// Gets tags that describe this offensive provider.
        /// </summary>
        GameplayTagSet OffensiveTags { get; }

        /// <summary>
        /// Gets active modifiers that can affect outgoing damage.
        /// </summary>
        IEnumerable<IDamageModifier> GetDamageModifiers();

        /// <summary>
        /// Notifies the provider after a request caused by it has finished resolving.
        /// </summary>
        void OnDamageResolved(DamageResolutionReport report);
    }
}

