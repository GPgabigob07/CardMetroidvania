using System;
using UnityEngine;

namespace TicGame.Architecture
{
    [CreateAssetMenu(menuName = "TIC/Architecture/Event Channels/Void", fileName = "Event_Void_")]
    public sealed class VoidEventChannelSO : EventChannelBaseSO
    {
        public event Action Raised;

        public void Raise()
        {
            MarkRaised();
            Raised?.Invoke();
        }
    }
}

