using System;
using UnityEngine;

namespace TicGame.Architecture
{
    [Serializable]
    public struct DamageInstance
    {
        public string InstanceId;
        public GameObject SourceObject;
        public DamageProfileSO Profile;
        public DamageFormulaValues Formula;
        public GameplayTagSet Tags;
        public int MaxTargets;
        public TargetPriorityMode TargetPriorityMode;
        public string AttackExecutionId;
        public float KnockbackForce;
        public DamageProvenance Provenance;
        public DamageProcPolicy ProcPolicy;

        public DamageInstance(
            string instanceId,
            GameObject sourceObject,
            DamageProfileSO profile,
            DamageFormulaValues formula,
            GameplayTagSet tags = null,
            int maxTargets = 1,
            TargetPriorityMode targetPriorityMode = TargetPriorityMode.ExplicitOrder,
            string attackExecutionId = null,
            float knockbackForce = 0f,
            DamageProvenance? provenance = null,
            DamageProcPolicy procPolicy = DamageProcPolicy.PrimaryAttack)
        {
            InstanceId = string.IsNullOrWhiteSpace(value: instanceId) ? Guid.NewGuid().ToString(format: "N") : instanceId;
            SourceObject = sourceObject;
            Profile = profile;
            Formula = formula;
            Tags = tags ?? profile?.DamageTags;
            MaxTargets = Mathf.Max(a: 1, b: maxTargets);
            TargetPriorityMode = targetPriorityMode;
            AttackExecutionId = attackExecutionId;
            KnockbackForce = Mathf.Max(a: 0f, b: knockbackForce);
            Provenance = provenance ?? DamageProvenance.Primary(instanceId: InstanceId);
            ProcPolicy = procPolicy;
        }
    }
}
