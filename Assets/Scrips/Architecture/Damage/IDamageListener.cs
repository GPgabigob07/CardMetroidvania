namespace TicGame.Architecture
{
    /// <summary>
    /// Receives local notifications for damage dealt, received and completed requests.
    /// </summary>
    public interface IDamageListener
    {
        /// <summary>
        /// Runs when this object caused accepted damage to a target.
        /// </summary>
        void OnDamageDealt(in DamageContext context, in DamageResult result);

        /// <summary>
        /// Runs when this object received a damage context.
        /// </summary>
        void OnDamageReceived(in DamageContext context, in DamageResult result);

        /// <summary>
        /// Runs after an entire request has finished resolving.
        /// </summary>
        void OnDamageResolutionComplete(DamageResolutionReport report);
    }
}

