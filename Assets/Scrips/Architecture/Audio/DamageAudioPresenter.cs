using UnityEngine;

namespace TicGame.Architecture
{
    [DisallowMultipleComponent]
    public sealed class DamageAudioPresenter : MonoBehaviour, IDamageListener, IGameplayServicesConsumer
    {
        [Header("Recipient Feedback")]
        [Tooltip("Enemy impact cue or player hurt cue; this component is the sole audio listener on its recipient.")]
        [SerializeField] private SoundCueSO hit;
        private IAudioService audio;
        public void Configure(SoundCueSO cue) => hit = cue;
        public void BindAudio(IAudioService service) => audio = service;
        public void BindGameplayServices(IGameplayServices services) => BindAudio(services?.Audio);

        public void OnDamageReceived(in DamageContext context, in DamageResult result)
        {
            if (!isActiveAndEnabled || hit == null || !result.Accepted || result.AppliedAmount <= 0
                || (context.Provenance.HasValue && context.Provenance.Value.OriginKind == DamageOriginKind.Supplemental)) return;
            var point = context.HitPoint;
            var position = AudioVolumeMath.IsFinite(point.x) && AudioVolumeMath.IsFinite(point.y)
                ? new Vector3(point.x, point.y, transform.position.z) : transform.position;
            audio?.TryPlayWorld(hit, position, gameObject);
        }
        public void OnDamageDealt(in DamageContext context, in DamageResult result) { }
        public void OnDamageResolutionComplete(DamageResolutionReport report) { }
    }
}
