using UnityEngine;

namespace TicGame.Architecture
{
    [DisallowMultipleComponent]
    public sealed class PlayerAudioPresenter : MonoBehaviour, IGameplayServicesConsumer
    {
        [Header("Cues")]
        [SerializeField] private SoundCueSO swing;
        [SerializeField] private SoundCueSO dash;
        [SerializeField] private SoundCueSO jump;
        [SerializeField] private SoundCueSO landing;
        private IAudioService audio;
        private PlayerActionRunner actions;
        private PlayerLocomotionController locomotion;

        public void Configure(SoundCueSO attackCue, SoundCueSO dashCue, SoundCueSO jumpCue, SoundCueSO landingCue)
        { swing = attackCue; dash = dashCue; jump = jumpCue; landing = landingCue; }
        public void BindAudio(IAudioService service) => audio = service;
        public void BindGameplayServices(IGameplayServices services) => BindAudio(services?.Audio);
        private void Start() => Subscribe();
        private void OnEnable() => Subscribe();
        private void OnDisable()
        {
            if (actions != null) actions.ActionStarted -= PresentAction;
            if (locomotion != null) { locomotion.JumpStarted -= PresentJump; locomotion.Landed -= PresentLanding; }
            actions = null;
            locomotion = null;
        }
        private void Subscribe()
        {
            if (!isActiveAndEnabled || actions != null) return;
            var player = GetComponent<PlayerController>();
            if (player?.ActionRunner == null || player.Locomotion == null) return;
            actions = player.ActionRunner;
            locomotion = player.Locomotion;
            actions.ActionStarted += PresentAction;
            locomotion.JumpStarted += PresentJump;
            locomotion.Landed += PresentLanding;
        }
        public void PresentAction(PlayerActionState state, Vector3 position)
        {
            if (state == PlayerActionState.Dash) Play(dash, position);
            else if (state == PlayerActionState.Attack1 || state == PlayerActionState.Attack2
                || state == PlayerActionState.Attack3 || state == PlayerActionState.CardChain || state == PlayerActionState.Finisher) Play(swing, position);
        }
        public void PresentJump(Vector3 position) => Play(jump, position);
        public void PresentLanding(Vector3 position) => Play(landing, position);
        private void Play(SoundCueSO cue, Vector3 position)
        { if (isActiveAndEnabled && cue != null) audio?.TryPlayWorld(cue, position, gameObject); }
    }
}
