using UnityEngine;

namespace TicGame.Architecture
{
    public sealed class BatMachineHurtbox : MonoBehaviour, IDamageable
    {
        [Header(header: "Damage Routing")]
        [Tooltip(tooltip: "Root bat damage policy that receives every transaction resolved against this hurtbox.")]
        [SerializeField] private BatMachineDamagePolicy rootDamageable;

        private void Awake()
        {
            ResolveDamageable();
        }

        public DamageResult ApplyDamage(in DamageContext context)
        {
            ResolveDamageable();
            return rootDamageable != null
                ? rootDamageable.ApplyDamage(context)
                : new DamageResult(
                    accepted: false,
                    killed: false,
                    appliedAmount: 0f,
                    remainingHealth: 0f,
                    hitStopSeconds: 0f);
        }

        public void Configure(BatMachineDamagePolicy policy)
        {
            rootDamageable = policy;
        }

        private void ResolveDamageable()
        {
            if (rootDamageable == null)
            {
                rootDamageable = GetComponentInParent<BatMachineDamagePolicy>();
            }
        }
    }
}
