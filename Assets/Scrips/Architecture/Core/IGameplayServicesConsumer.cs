namespace TicGame.Architecture
{
    public interface IGameplayServicesConsumer
    {
        /// <summary>
        /// Receives the persistent gameplay services required by this object.
        /// </summary>
        void BindGameplayServices(IGameplayServices services);
    }
}
