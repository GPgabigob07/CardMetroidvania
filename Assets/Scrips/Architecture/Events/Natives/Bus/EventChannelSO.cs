using System;

namespace TicGame.Architecture
{
    public abstract class EventChannelSO<TPayload> : EventChannelBaseSO
    {
        public event Action<TPayload> Raised;

        public void Raise(TPayload payload)
        {
            MarkRaised();
            Raised?.Invoke(payload);
        }
    }
}

