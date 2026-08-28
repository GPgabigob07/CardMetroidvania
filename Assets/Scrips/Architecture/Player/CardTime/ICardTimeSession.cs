namespace TicGame.Architecture
{
    public interface ICardTimeSession
    {
        /// <summary>
        /// Gets the latest authoritative Card Time session snapshot.
        /// </summary>
        CardTimeSessionSnapshot Current { get; }
    }
}
