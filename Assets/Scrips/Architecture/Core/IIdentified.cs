namespace TicGame.Architecture
{
    /// <summary>
    /// Exposes a stable identifier for assets, runtime objects, saves and debug tooling.
    /// </summary>
    public interface IIdentified
    {
        /// <summary>
        /// Gets the stable identifier for this object.
        /// </summary>
        string Id { get; }
    }
}
