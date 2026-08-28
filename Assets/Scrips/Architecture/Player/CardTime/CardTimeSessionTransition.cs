namespace TicGame.Architecture
{
    public readonly struct CardTimeSessionTransition
    {
        public CardTimeSessionTransition(
            CardTimeSessionSnapshot previous,
            CardTimeSessionSnapshot current,
            CardTimeSessionOutcome outcome = CardTimeSessionOutcome.None)
        {
            Previous = previous;
            Current = current;
            Outcome = outcome;
        }

        public CardTimeSessionSnapshot Previous { get; }
        public CardTimeSessionSnapshot Current { get; }
        public CardTimeSessionOutcome Outcome { get; }
    }
}
