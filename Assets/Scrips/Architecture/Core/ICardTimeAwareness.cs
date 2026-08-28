namespace TicGame.Architecture
{
    public interface ICardTimeAwareness
    {
        /// <summary>
        /// Gets whether Card Time is currently active in gameplay.
        /// </summary>
        bool IsCardTimeActive { get; }

        /// <summary>
        /// Gets the active Card Time category, or None while inactive.
        /// </summary>
        PlayerCardTimeState ActiveCardTime { get; }
    }
}
