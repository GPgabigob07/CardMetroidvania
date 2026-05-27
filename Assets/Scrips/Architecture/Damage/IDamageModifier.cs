namespace TicGame.Architecture
{
    /// <summary>
    /// Defines a runtime damage modifier that can alter formula values during a resolution phase.
    /// </summary>
    public interface IDamageModifier
    {
        /// <summary>
        /// Gets lower-to-higher ordering for modifiers in the same phase.
        /// </summary>
        int Priority { get; }

        /// <summary>
        /// Gets the phase where this modifier should run.
        /// </summary>
        DamageModifierPhase Phase { get; }

        /// <summary>
        /// Returns whether this modifier applies to the current context.
        /// </summary>
        bool AppliesTo(in DamageModifierContext context);

        /// <summary>
        /// Mutates formula values for the current context.
        /// </summary>
        void Modify(ref DamageFormulaValues values, in DamageModifierContext context);

        /// <summary>
        /// Receives the full report after the request has finished resolving.
        /// </summary>
        void OnDamageResolved(DamageResolutionReport report);
    }
}

