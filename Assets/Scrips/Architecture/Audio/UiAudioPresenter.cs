using UnityEngine;

namespace TicGame.Architecture
{
    [DisallowMultipleComponent]
    public sealed class UiAudioPresenter : MonoBehaviour, IGameplayServicesConsumer
    {
        [Header("Card UI")]
        [SerializeField] private SoundCueSO selection;
        [SerializeField] private SoundCueSO confirmation;
        private IAudioService audio;
        private PlayerController player;
        public void Configure(SoundCueSO selectionCue, SoundCueSO confirmationCue) { selection = selectionCue; confirmation = confirmationCue; }
        public void BindAudio(IAudioService service) => audio = service;
        public void BindGameplayServices(IGameplayServices services) => BindAudio(services?.Audio);
        private void OnEnable()
        {
            player = GetComponent<PlayerController>();
            if (player == null) return;
            player.CardSelectionChanged += PresentSelection;
            player.CardCommitted += PresentConfirmation;
        }
        private void OnDisable()
        {
            if (player == null) return;
            player.CardSelectionChanged -= PresentSelection;
            player.CardCommitted -= PresentConfirmation;
            player = null;
        }
        public void PresentSelection() { if (isActiveAndEnabled && selection != null) audio?.TryPlayUi(selection, gameObject); }
        public void PresentConfirmation() { if (isActiveAndEnabled && confirmation != null) audio?.TryPlayUi(confirmation, gameObject); }
    }
}
