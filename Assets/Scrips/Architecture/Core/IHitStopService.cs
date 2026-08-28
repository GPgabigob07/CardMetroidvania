namespace TicGame.Architecture
{
    public interface IHitStopService
    {
        /// <summary>
        /// Gets whether a hitstop modifier is currently active.
        /// </summary>
        bool IsActive { get; }

        /// <summary>
        /// Requests hitstop for the supplied unscaled duration.
        /// </summary>
        void Request(HitStopRequest request);
    }
}
