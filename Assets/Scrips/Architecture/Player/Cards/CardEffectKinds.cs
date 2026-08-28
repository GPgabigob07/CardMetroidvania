namespace TicGame.Architecture
{
    public enum CardTriggerKind
    {
        OnCardCommitted = 0,
        OnEffectivePrimaryAttackResolved = 10,
        OnPrimaryAttackCompleted = 20,
        OnPlayerLanded = 30,
        OnAttackExecutionCompleted = 40,
        OnPlayerDeath = 50,
        OnSceneTransition = 60
    }

    public enum CardConditionKind
    {
        IsAirborne = 0,
        IsGrounded = 10,
        HasAttackExecution = 20,
        HasEffectiveHit = 30,
        WasMiss = 40,
        HasRemainingCharges = 50,
        ResourceAtLeast = 60,
        AbilityAvailable = 70,
        AbilityUnlocked = 80
    }

    public enum CardOperationKind
    {
        GainResource = 0,
        AddStatusCharges = 10,
        AddStatusCapacity = 20,
        AddStatusStacks = 25,
        ModifyDamage = 30,
        ModifyKnockback = 40,
        ModifyResourceGain = 50,
        ArmSupplementalDamage = 60,
        ClearStatusStacks = 70,
        RemoveStatus = 80,
        InvokeAbility = 90
    }

    public enum CardLifetimeKind
    {
        Immediate = 0,
        UntilChargesExhausted = 10,
        UntilMiss = 20,
        UntilLanding = 30,
        UntilAttackExecutionCompletes = 40,
        UntilPlayerDeath = 50,
        UntilSceneTransition = 60,
        PersistentUntilExplicitRemoval = 70
    }

    public enum CardStackingKind
    {
        RejectIfActive = 0,
        ReplaceExisting = 10,
        RefreshLifetime = 20,
        AddCharges = 30,
        AddCapacity = 40,
        AddStacks = 50
    }
}
