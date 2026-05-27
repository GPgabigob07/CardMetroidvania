namespace TicGame.Architecture
{
    /// <summary>
    /// Bridges pure damage resolution to event channels, logs or other external observers.
    /// </summary>
    public interface IDamageEventSink
    {
        /// <summary>
        /// Runs after a single target has resolved a damage context.
        /// </summary>
        void OnTargetResolved(in DamageContext context, in DamageResult result);

        /// <summary>
        /// Runs after the full request has resolved.
        /// </summary>
        void OnRequestResolved(DamageResolutionReport report);
    }
}

