namespace TicGame.Architecture
{
    public struct PlayerActionFrame
    {
        public PlayerActionFrame(
            PlayerActionPhase phase,
            PlayerCardTimeState cardTimeState,
            float normalizedPhaseTime,
            bool canBufferFollowUp,
            bool canCommitFollowUp,
            float postRecoveryBufferGraceDuration,
            float sequenceRestartCooldown,
            bool endAction,
            bool hasAnimatorAuthority)
        {
            Phase = phase;
            CardTimeState = cardTimeState;
            NormalizedPhaseTime = normalizedPhaseTime;
            CanBufferFollowUp = canBufferFollowUp;
            CanCommitFollowUp = canCommitFollowUp;
            PostRecoveryBufferGraceDuration = postRecoveryBufferGraceDuration;
            SequenceRestartCooldown = sequenceRestartCooldown;
            EndAction = endAction;
            HasAnimatorAuthority = hasAnimatorAuthority;
        }

        public PlayerActionPhase Phase { get; }
        public PlayerCardTimeState CardTimeState { get; }
        public float NormalizedPhaseTime { get; }
        public bool CanBufferFollowUp { get; }
        public bool CanCommitFollowUp { get; }
        public float PostRecoveryBufferGraceDuration { get; }
        public float SequenceRestartCooldown { get; }
        public bool EndAction { get; }
        public bool HasAnimatorAuthority { get; }

        public static PlayerActionFrame Default => new PlayerActionFrame(
            phase: PlayerActionPhase.Reading,
            cardTimeState: PlayerCardTimeState.None,
            normalizedPhaseTime: 0f,
            canBufferFollowUp: false,
            canCommitFollowUp: false,
            postRecoveryBufferGraceDuration: 0f,
            sequenceRestartCooldown: 0f,
            endAction: false,
            hasAnimatorAuthority: false);
    }
}
