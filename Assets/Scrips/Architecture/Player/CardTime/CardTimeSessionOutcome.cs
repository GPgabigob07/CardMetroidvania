namespace TicGame.Architecture
{
    public enum CardTimeSessionOutcome
    {
        None = 0,
        Committed = 10,
        Cancelled = 20,
        TimedOut = 30
    }
}
