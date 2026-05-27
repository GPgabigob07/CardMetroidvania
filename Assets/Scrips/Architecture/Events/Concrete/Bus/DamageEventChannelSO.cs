using UnityEngine;

namespace TicGame.Architecture
{
    [CreateAssetMenu(menuName = "TIC/Architecture/Event Channels/Damage", fileName = "Event_Damage_")]
    public sealed class DamageEventChannelSO : EventChannelSO<DamageContext>
    {
    }
}

