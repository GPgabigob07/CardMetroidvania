using System;

namespace TicGame.Architecture
{
    [Serializable]
    public readonly struct ResolvedDamageFormula
    {
        public ResolvedDamageFormula(in DamageFormulaValues values)
        {
            Attack = values.Attack;
            StrikePercent = values.StrikePercent;
            StrikeBonusPercent = values.StrikeBonusPercent;
            AttackBuffPercent = values.AttackBuffPercent;
            FlatDamage = values.FlatDamage;
            FinalDamagePercent = values.FinalDamagePercent;
            CritValue = values.CritValue;
            EligibleBaseDamage = values.CalculateEligibleBaseDamage();
            RawDamage = values.CalculateRawDamage();
            RequestedFinalDamage = values.CalculateFinalDamage();
        }

        public float Attack { get; }
        public float StrikePercent { get; }
        public float StrikeBonusPercent { get; }
        public float AttackBuffPercent { get; }
        public float FlatDamage { get; }
        public float FinalDamagePercent { get; }
        public float CritValue { get; }
        public float EligibleBaseDamage { get; }
        public float RawDamage { get; }
        public float RequestedFinalDamage { get; }
    }
}
