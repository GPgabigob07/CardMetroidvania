using UnityEngine;

namespace TicGame.Architecture
{
    [CreateAssetMenu(
        menuName = "TIC/Architecture/Event Channels/Hit Stop Request",
        fileName = "Event_HitStopRequest_")]
    public sealed class HitStopRequestEventChannelSO : EventChannelSO<HitStopRequest>
    {
    }
}
