using System;

namespace TicGame.Architecture
{
    [Serializable]
    public struct DamageFormulaValues
    {
        public float Attack;
        public float StrikePercent;
        public float StrikeBonusPercent;
        public float AttackBuffPercent;
        public float FlatDamage;
        public float FinalDamagePercent;
        public float CritValue;

        public DamageFormulaValues(
            float attack,
            float strikePercent,
            float strikeBonusPercent,
            float attackBuffPercent,
            float flatDamage,
            float finalDamagePercent,
            float critValue)
        {
            Attack = attack;
            StrikePercent = strikePercent;
            StrikeBonusPercent = strikeBonusPercent;
            AttackBuffPercent = attackBuffPercent;
            FlatDamage = flatDamage;
            FinalDamagePercent = finalDamagePercent;
            CritValue = critValue <= 0f ? 1f : critValue;
        }

        public float CalculateRawDamage()
        {
            var strikeScale = StrikePercent + StrikeBonusPercent;
            var scaledAttack = Attack * (1f + AttackBuffPercent);
            return strikeScale * scaledAttack + FlatDamage;
        }

        public float CalculateFinalDamage()
        {
            return CalculateRawDamage() * (1f + FinalDamagePercent) * CritValue;
        }
    }
}

