using System.Collections.Generic;

namespace TicGame.Architecture
{
    public interface ICardLoadoutProvider
    {
        /// <summary>
        /// Gets equipped card ids for a Card Time category in presentation order.
        /// </summary>
        IReadOnlyList<string> GetEquippedCardIds(PlayerCardTimeState category);
    }
}
