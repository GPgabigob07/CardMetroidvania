using UnityEngine;

namespace TicGame.Architecture
{
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class GargoyleHurtbox : MonoBehaviour, IDamageable, IEnemyDamageRegion
    {
        [Header("Region Binding")]
        [SerializeField] private GargoyleDamagePolicy policy;
        [SerializeField] private GargoyleRegionKind region;
        [SerializeField] private BoxCollider2D shape;
        private GargoyleDamagePolicy subscribedPolicy;
        public EnemyActor Owner => policy != null ? policy.Actor : null;
        public GameObject DamageRecipient => gameObject;
        public int Priority => policy != null ? policy.GetRegionSettings(region).Priority : 0;
        private void OnEnable() { Subscribe(); Refresh(); }
        private void OnDisable() { Unsubscribe(); if (shape != null) shape.enabled = false; }
        private void OnDestroy() => Unsubscribe();
        public void Configure(GargoyleDamagePolicy policy, GargoyleRegionKind region)
        {
            Unsubscribe(); this.policy = policy; this.region = region;
            Subscribe(); Refresh();
        }
        public void Refresh()
        {
            if (shape == null) shape = GetComponent<BoxCollider2D>();
            if (shape == null) return;
            shape.isTrigger = true;
            shape.enabled = policy != null && policy.IsRegionEnabled(region);
            if (policy == null) return;
            var settings = policy.GetRegionSettings(region);
            shape.size = settings.Size;
            shape.offset = new Vector2(settings.Offset.x * policy.FacingDirection, settings.Offset.y);
        }
        public DamageResult ApplyDamage(in DamageContext context) => policy != null
            ? policy.ApplyDamage(region, context) : new DamageResult(false, false, 0, 0, 0);
        private void Subscribe()
        {
            if (subscribedPolicy == policy) return;
            Unsubscribe(); subscribedPolicy = policy;
            if (subscribedPolicy != null) subscribedPolicy.RegionsChanged += Refresh;
        }
        private void Unsubscribe()
        {
            if (subscribedPolicy != null) subscribedPolicy.RegionsChanged -= Refresh;
            subscribedPolicy = null;
        }
    }
}
