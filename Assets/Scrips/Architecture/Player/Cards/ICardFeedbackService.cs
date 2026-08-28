namespace TicGame.Architecture
{
    public interface ICardFeedbackService :
        ICardFeedbackReporter,
        ICardHudEffectViewModelSource
    {
        CardFeedbackEventChannelSO WorldFeedbackEvent { get; }
        void RemoveHudEffectsFromSource(UnityEngine.GameObject sourceObject);
        void ClearHudEffects();
    }
}
