using TicGame.Architecture;

namespace TicGame.Architecture.Tests
{
    public sealed class FlatFinalDamageModifier : IDamageModifier
    {
        private readonly float finalDamagePercent;

        public FlatFinalDamageModifier(float finalDamagePercent)
        {
            this.finalDamagePercent = finalDamagePercent;
        }

        public int Priority => 0;
        public DamageModifierPhase Phase => DamageModifierPhase.PreTargetResolve;

        public bool AppliesTo(in DamageModifierContext context)
        {
            return true;
        }

        public void Modify(ref DamageFormulaValues values, in DamageModifierContext context)
        {
            values.FinalDamagePercent += finalDamagePercent;
        }

        public void OnDamageResolved(DamageResolutionReport report)
        {
        }
    }
}

