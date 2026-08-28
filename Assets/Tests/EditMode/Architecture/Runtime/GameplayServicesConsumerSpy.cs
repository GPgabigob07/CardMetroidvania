using UnityEngine;

namespace TicGame.Architecture.Tests
{
    public sealed class GameplayServicesConsumerSpy :
        MonoBehaviour,
        IGameplayServicesConsumer
    {
        public int BindCount { get; private set; }
        public IGameplayServices Services { get; private set; }

        public void BindGameplayServices(IGameplayServices services)
        {
            BindCount++;
            Services = services;
        }
    }
}
