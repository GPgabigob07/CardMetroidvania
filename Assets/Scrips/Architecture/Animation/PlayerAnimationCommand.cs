namespace TicGame.Architecture
{
    public readonly struct PlayerAnimationCommand
    {
        public PlayerAnimationCommand(
            PlayerAnimationState state,
            float crossFadeDuration,
            bool restart,
            PlayerAnimationState fallbackState = PlayerAnimationState.Idle,
            bool hasFallback = false)
        {
            State = state;
            FallbackState = fallbackState;
            HasFallback = hasFallback;
            CrossFadeDuration = crossFadeDuration;
            Restart = restart;
        }

        public PlayerAnimationState State { get; }
        public PlayerAnimationState FallbackState { get; }
        public bool HasFallback { get; }
        public float CrossFadeDuration { get; }
        public bool Restart { get; }
    }
}
