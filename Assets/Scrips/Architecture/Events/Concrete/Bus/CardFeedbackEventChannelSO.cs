using UnityEngine;

namespace TicGame.Architecture
{
    [CreateAssetMenu(
        menuName = "TIC/Architecture/Event Channels/Card Feedback",
        fileName = "Event_CardFeedback_")]
    public sealed class CardFeedbackEventChannelSO :
        EventChannelSO<CardWorldFeedbackViewModel>
    {
    }
}
