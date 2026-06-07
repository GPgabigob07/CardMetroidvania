namespace TicGame.Architecture
{
    public struct PlayerActionFrame
    {
        public PlayerActionFrame(
            PlayerActionPhase phase,
            PlayerCardTimeState cardTimeState,
            bool allowChain,
            bool endAction,
            bool hasAnimatorAuthority)
        {
            Phase = phase;
            CardTimeState = cardTimeState;
            AllowChain = allowChain;
            EndAction = endAction;
            HasAnimatorAuthority = hasAnimatorAuthority;
        }

        public PlayerActionPhase Phase { get; }
        public PlayerCardTimeState CardTimeState { get; }
        public bool AllowChain { get; }
        public bool EndAction { get; }
        public bool HasAnimatorAuthority { get; }

        public static PlayerActionFrame Default => new PlayerActionFrame(
            phase: PlayerActionPhase.Reading,
            cardTimeState: PlayerCardTimeState.None,
            allowChain: false,
            endAction: false,
            hasAnimatorAuthority: false);
    }
}
