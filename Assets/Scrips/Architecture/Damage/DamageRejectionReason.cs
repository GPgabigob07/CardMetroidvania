namespace TicGame.Architecture
{
    public enum DamageRejectionReason
    {
        Unspecified,
        GameplayBlocked,
        AlreadyDefeated,
        InvalidTarget,
        NonPositiveDamage,
        DuplicateExecution
    }
}
