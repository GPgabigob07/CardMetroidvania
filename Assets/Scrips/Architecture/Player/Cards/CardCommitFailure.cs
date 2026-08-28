namespace TicGame.Architecture
{
    public enum CardCommitFailure
    {
        None = 0,
        InvalidSelection = 10,
        NoSelectedCard = 20,
        CategoryMismatch = 30,
        InvalidDefinition = 40,
        MissingDependency = 50,
        UnmetCondition = 60,
        UnsupportedEffect = 70,
        InsufficientSnapshotResources = 80,
        InsufficientLiveResources = 90,
        AlreadyApplied = 100
    }
}
