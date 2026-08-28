using System.Collections.Generic;

namespace TicGame.Architecture
{
    public interface ICardHudEffectViewModelSource
    {
        IReadOnlyList<CardHudEffectViewModel> GetHudEffects();
    }
}
