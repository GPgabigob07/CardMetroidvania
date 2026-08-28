using UnityEngine;

namespace TicGame.Architecture
{
    public sealed class EnemyHurtboxRegion : MonoBehaviour, IDamageable
    {
        [Header(header: "Damage Routing")]
        [Tooltip(tooltip: "Root golem damage policy that receives damage from this hurtbox region.")]
        [SerializeField] private GolemChargerDamagePolicy rootDamageable;

        [Tooltip(tooltip: "Semantic hit region reported to the root damage policy.")]
        [SerializeField] private EnemyHurtboxRegionType region = EnemyHurtboxRegionType.Body;

        public EnemyHurtboxRegionType Region => region;

        private void Awake()
        {
            ResolveDamageable();
        }

        public DamageResult ApplyDamage(in DamageContext context)
        {
            ResolveDamageable();
            return rootDamageable != null
                ? rootDamageable.ApplyDamage(context, region)
                : new DamageResult(
                    accepted: false,
                    killed: false,
                    appliedAmount: 0f,
                    remainingHealth: 0f,
                    hitStopSeconds: 0f);
        }

        public void Configure(GolemChargerDamagePolicy policy, EnemyHurtboxRegionType hitRegion)
        {
            rootDamageable = policy;
            region = hitRegion;
        }

        private void ResolveDamageable()
        {
            if (rootDamageable == null)
            {
                rootDamageable = GetComponentInParent<GolemChargerDamagePolicy>();
            }
        }
    }
}
