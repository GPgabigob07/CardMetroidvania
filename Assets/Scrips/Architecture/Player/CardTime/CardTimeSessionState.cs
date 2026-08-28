namespace TicGame.Architecture
{
    public enum CardTimeSessionState
    {
        Unavailable = 0,
        Available = 10,
        Active = 20,
        Committed = 30,
        Cancelled = 40
    }
}
