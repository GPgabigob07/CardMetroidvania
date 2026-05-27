namespace TicGame.Architecture
{
    /// <summary>
    /// Defines a gameplay module that can be explicitly started and stopped by a coordinating system.
    /// </summary>
    public interface IGameplayModule
    {
        /// <summary>
        /// Gets whether the module has completed its initialization step.
        /// </summary>
        bool IsInitialized { get; }

        /// <summary>
        /// Starts the module and prepares any runtime resources it owns.
        /// </summary>
        void Initialize();

        /// <summary>
        /// Stops the module and releases or unsubscribes runtime resources it owns.
        /// </summary>
        void Shutdown();
    }
}
