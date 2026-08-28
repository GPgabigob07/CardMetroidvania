using System;

namespace TicGame.Architecture
{
    [Flags]
    public enum DamageProcPolicy
    {
        None = 0,
        ConfirmAttackHit = 1 << 0,
        AdvanceChain = 1 << 1,
        RollHitResourceGain = 1 << 2,
        ConsumeHitCharge = 1 << 3,
        TriggerOnDamageEffects = 1 << 4,
        RequestHitStop = 1 << 5,
        ApplyKnockback = 1 << 6,
        GrantKillRewards = 1 << 7,
        PrimaryAttack = ConfirmAttackHit
            | AdvanceChain
            | RollHitResourceGain
            | ConsumeHitCharge
            | TriggerOnDamageEffects
            | RequestHitStop
            | ApplyKnockback
            | GrantKillRewards,
        SupplementalDefault = GrantKillRewards
    }
}
