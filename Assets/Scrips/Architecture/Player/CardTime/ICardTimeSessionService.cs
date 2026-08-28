using UnityEngine;

namespace TicGame.Architecture
{
    public interface ICardTimeSessionService : ICardTimeSession
    {
        /// <summary>
        /// Registers one player-owned source with Card Time mutation authority.
        /// </summary>
        IPlayerCardTimeSource RegisterPlayerSource(Object owner);
    }
}
