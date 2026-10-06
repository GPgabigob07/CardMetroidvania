using UnityEngine;

namespace TicGame.Architecture
{
    [DisallowMultipleComponent]
    public sealed class EnemyHitRippleDamageListener : MonoBehaviour, IDamageListener
    {
        [Header("Feedback Routing")]
        [Tooltip("Presenter belonging to this damage recipient's enemy owner.")]
        [SerializeField] private EnemyHitRipplePresenter presenter;
        public EnemyHitRipplePresenter Presenter => presenter;
        public void Configure(EnemyHitRipplePresenter value) => presenter = value;
        public void OnDamageReceived(in DamageContext context, in DamageResult result)
        {
            if (isActiveAndEnabled && presenter != null) presenter.Present(context, result);
        }
        public void OnDamageDealt(in DamageContext context, in DamageResult result) { }
        public void OnDamageResolutionComplete(DamageResolutionReport report) { }
    }
}
