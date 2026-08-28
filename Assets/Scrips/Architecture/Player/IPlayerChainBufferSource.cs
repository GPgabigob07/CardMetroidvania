namespace TicGame.Architecture
{
    public interface IPlayerChainBufferSource
    {
        /// <summary>
        /// Gets whether the current action accepts one buffered follow-up input.
        /// </summary>
        bool CanBufferFollowUp { get; }

        /// <summary>
        /// Gets whether the current action may commit its buffered follow-up.
        /// </summary>
        bool CanCommitFollowUp { get; }

        /// <summary>
        /// Gets how long the next combo attack remains available after Recovery ends.
        /// </summary>
        float PostRecoveryBufferGraceDuration { get; }

        /// <summary>
        /// Gets how long the player must wait after Recovery before restarting at Attack1.
        /// </summary>
        float SequenceRestartCooldown { get; }
    }
}
