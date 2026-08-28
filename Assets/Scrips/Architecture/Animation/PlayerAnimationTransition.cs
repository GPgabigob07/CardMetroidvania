namespace TicGame.Architecture
{
    public readonly struct PlayerAnimationTransition
    {
        public PlayerAnimationTransition(
            PlayerAnimationSnapshot previous,
            PlayerAnimationSnapshot current,
            bool hasPrevious)
        {
            Previous = previous;
            Current = current;
            HasPrevious = hasPrevious;
        }

        public PlayerAnimationSnapshot Previous { get; }
        public PlayerAnimationSnapshot Current { get; }
        public bool HasPrevious { get; }
    }
}
