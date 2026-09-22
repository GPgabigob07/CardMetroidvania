using UnityEngine;

namespace TicGame.Architecture
{
    public sealed class CardMeleeGate : MonoBehaviour, IDamageable
    {
        [Header("Door")]
        [Tooltip("Solid barrier disabled after a qualifying hit.")]
        [SerializeField] private Collider2D barrier;
        [Tooltip("Door renderer hidden after opening.")]
        [SerializeField] private Renderer doorRenderer;
        [Tooltip("Stable gate key within the owning area.")]
        [SerializeField] private string gateId;

        private float blockedUntil;
        private MaterialPropertyBlock properties;
        private RunProgress progress;
        private string progressAreaId;
        public bool IsOpen { get; private set; }
        public string GateId => gateId;

        public void Configure(Collider2D solidBarrier, Renderer visual, string stableGateId = null)
        {
            barrier = solidBarrier;
            doorRenderer = visual;
            if (!string.IsNullOrWhiteSpace(stableGateId)) gateId = stableGateId;
        }

        /// <summary>
        /// Binds this disposable gate to the current run and restores its opened state.
        /// </summary>
        public void BindProgress(RunProgress runProgress, string areaId)
        {
            progress = runProgress;
            progressAreaId = areaId;
            if (progress != null && !string.IsNullOrWhiteSpace(progressAreaId)
                && !string.IsNullOrWhiteSpace(gateId)
                && progress.IsGateOpen(progressAreaId, gateId))
                OpenGate();
        }

        private void Awake()
        {
            barrier ??= GetComponent<Collider2D>();
            doorRenderer ??= GetComponent<Renderer>();
        }

        public DamageResult ApplyDamage(in DamageContext context)
        {
            if (IsOpen) return new DamageResult(false, false, 0, 0, 0);
            if (!context.IsCardEnhancedMelee || !(context.Amount > 0f))
            {
                blockedUntil = Time.unscaledTime + 0.6f;
                return new DamageResult(false, false, 0, 1, 0);
            }

            OpenGate();
            if (progress != null && !string.IsNullOrWhiteSpace(progressAreaId)
                && !string.IsNullOrWhiteSpace(gateId))
                progress.OpenGate(progressAreaId, gateId);
            // An effective hit consumes applicable card charges, without a kill reward.
            return new DamageResult(true, false, 1, 0);
        }

        private void OpenGate()
        {
            if (IsOpen) return;
            IsOpen = true;
            if (barrier != null) barrier.enabled = false;
            if (doorRenderer != null) doorRenderer.enabled = false;
        }

        private void Update()
        {
            if (doorRenderer == null || IsOpen) return;
            properties ??= new MaterialPropertyBlock();
            properties.SetColor("_BaseColor", Time.unscaledTime < blockedUntil
                ? new Color(1f, 0.25f, 0.15f) : new Color(0.2f, 0.85f, 1f));
            doorRenderer.SetPropertyBlock(properties);
        }
    }
}
