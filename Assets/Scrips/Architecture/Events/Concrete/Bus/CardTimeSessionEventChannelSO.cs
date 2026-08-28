using UnityEngine;

namespace TicGame.Architecture
{
    [CreateAssetMenu(
        menuName = "TIC/Architecture/Event Channels/Card Time Session",
        fileName = "Event_CardTimeSession_")]
    public sealed class CardTimeSessionEventChannelSO :
        EventChannelSO<CardTimeSessionTransition>
    {
    }
}
