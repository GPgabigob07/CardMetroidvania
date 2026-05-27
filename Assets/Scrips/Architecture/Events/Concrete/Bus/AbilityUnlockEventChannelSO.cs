using UnityEngine;

namespace TicGame.Architecture
{
    [CreateAssetMenu(menuName = "TIC/Architecture/Event Channels/Ability Unlock", fileName = "Event_AbilityUnlock_")]
    public sealed class AbilityUnlockEventChannelSO : EventChannelSO<AbilityUnlockPayload>
    {
    }
}

