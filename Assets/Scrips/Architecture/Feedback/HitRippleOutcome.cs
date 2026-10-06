namespace TicGame.Architecture
{
    public static class HitRippleOutcome
    {
        public static HitRippleKind Classify(in DamageResult result)
        {
            if (result.Accepted && result.AppliedAmount > 0 && float.IsFinite(result.AppliedAmount))
                return result.Killed ? HitRippleKind.Fatal : HitRippleKind.Damage;
            return !result.Accepted && !result.Killed && result.RemainingHealth > 0
                && result.RejectionReason == DamageRejectionReason.GameplayBlocked
                ? HitRippleKind.Rejected : HitRippleKind.None;
        }
    }
}
