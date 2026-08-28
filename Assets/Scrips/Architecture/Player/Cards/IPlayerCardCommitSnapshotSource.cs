namespace TicGame.Architecture
{
    public interface IPlayerCardCommitSnapshotSource
    {
        /// <summary>
        /// Captures volatile player values for card interpretation at commit time.
        /// </summary>
        PlayerCardCommitSnapshot Capture(
            PlayerCardTimeState category,
            string attackExecutionId,
            bool isAirborne);
    }
}
