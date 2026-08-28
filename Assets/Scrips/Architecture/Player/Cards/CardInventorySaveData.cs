using System;
using System.Collections.Generic;

namespace TicGame.Architecture
{
    [Serializable]
    public sealed class CardInventorySaveData
    {
        public List<string> ownedCardIds = new();
        public List<CardLoadoutSaveData> loadouts = new();
    }

    [Serializable]
    public sealed class CardLoadoutSaveData
    {
        public PlayerCardTimeState category;
        public List<string> equippedCardIds = new();
    }
}
