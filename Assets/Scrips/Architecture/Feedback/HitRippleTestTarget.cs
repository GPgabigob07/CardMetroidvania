using UnityEngine;

namespace TicGame.Architecture
{
    /// <summary>A stationary diagnostic recipient that simulates outcomes without hiding its sprite.</summary>
    [DisallowMultipleComponent]
    public sealed class HitRippleTestTarget : MonoBehaviour, IDamageable
    {
        [Header("Diagnostic Target")]
        [SerializeField] private EnemyActor owner;
        [SerializeField] private Collider2D hitShape;
        private HitRippleKind requestedKind = HitRippleKind.Damage;
        public EnemyActor Owner => owner;
        public Vector2 LastHitPoint { get; private set; }
        public HitRippleKind LastKind { get; private set; }
        public int HitCount { get; private set; }
        public void Configure(EnemyActor actor, Collider2D shape) { owner = actor; hitShape = shape; }

        public DamageResolutionReport ResolveHit(Vector2 worldPoint, HitRippleKind kind)
        {
            if (owner == null || hitShape == null || !hitShape.isActiveAndEnabled || !hitShape.OverlapPoint(worldPoint)
                || kind == HitRippleKind.None) return null;
            requestedKind = kind;
            var instance = new DamageInstance("ripple-lab", null, null, new DamageFormulaValues(1, 1, 0, 0, 0, 0, 1));
            return DamageResolver.Resolve(new DamageRequest(instance, new[] { gameObject }, worldPoint, Vector2.right));
        }
        public DamageResult ApplyDamage(in DamageContext context)
        {
            LastHitPoint = context.HitPoint; LastKind = requestedKind; HitCount++;
            // This lab tests the presentation of a result; no health/death lifecycle obscures it.
            return requestedKind == HitRippleKind.Rejected
                ? new DamageResult(false, false, 0, 5, 0, DamageRejectionReason.GameplayBlocked)
                : new DamageResult(true, requestedKind == HitRippleKind.Fatal, 1,
                    requestedKind == HitRippleKind.Fatal ? 0 : 5, 0);
        }
    }
}
