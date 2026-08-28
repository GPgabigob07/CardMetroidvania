namespace TicGame.Architecture
{
    public interface IPlayerAnimationSnapshotSource
    {
        /// <summary>
        /// Captures presentation facts from the resolved gameplay frame.
        /// </summary>
        PlayerAnimationSnapshot Capture(in LocomotionFrame frame);
    }
}
