namespace TicGame.Architecture
{
    public interface ICardFeedbackReporter
    {
        void PublishWorldFeedback(CardWorldFeedbackViewModel feedback);
        void UpsertHudEffect(CardHudEffectViewModel effect);
        void RemoveHudEffect(string effectKey);
    }
}
