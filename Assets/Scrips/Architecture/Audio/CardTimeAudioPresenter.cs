using UnityEngine;

namespace TicGame.Architecture
{
    [DisallowMultipleComponent]
    public sealed class CardTimeAudioPresenter : MonoBehaviour, IGameplayServicesConsumer
    {
        [Header("World Cues")]
        [SerializeField] private SoundCueSO enter;
        [SerializeField] private SoundCueSO exit;
        private IAudioService audio;
        private CardTimeSessionEventChannelSO channel;
        public void Configure(SoundCueSO entryCue, SoundCueSO exitCue) { enter = entryCue; exit = exitCue; }
        public void BindGameplayServices(IGameplayServices services)
        {
            if (channel != null) channel.Raised -= Present;
            audio = services?.Audio;
            channel = services?.CardTimeTransitions;
            if (isActiveAndEnabled && channel != null) channel.Raised += Present;
        }
        private void OnEnable() { if (channel != null) channel.Raised += Present; }
        private void OnDisable() { if (channel != null) channel.Raised -= Present; }
        public void Present(CardTimeSessionTransition transition)
        {
            if (!isActiveAndEnabled || transition.Previous.IsActive == transition.Current.IsActive) return;
            var cue = transition.Current.IsActive ? enter : exit;
            if (cue != null) audio?.TryPlayWorld(cue, transform.position, gameObject);
        }
    }
}
