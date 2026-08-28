using System;

namespace TicGame.Architecture
{
    [Serializable]
    public readonly struct DamageTargetResult
    {
        public DamageTargetResult(
            DamageContext context,
            DamageResult result,
            ResolvedDamageFormula formula)
        {
            Context = context;
            Result = result;
            Formula = formula;
        }

        public DamageContext Context { get; }
        public DamageResult Result { get; }
        public ResolvedDamageFormula Formula { get; }
    }
}
