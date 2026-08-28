namespace TicGame.Architecture
{
    public interface IPlayerCardTimeSourceConsumer
    {
        /// <summary>
        /// Receives the Card Time mutation authority reserved for this player source.
        /// </summary>
        void BindPlayerCardTimeSource(IPlayerCardTimeSource source);
    }
}
