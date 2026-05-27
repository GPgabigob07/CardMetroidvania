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

        public DamageInstance(
            string instanceId,
            GameObject sourceObject,
            DamageProfileSO profile,
            DamageFormulaValues formula,
            GameplayTagSet tags = null,
            int maxTargets = 1,
            TargetPriorityMode targetPriorityMode = TargetPriorityMode.ExplicitOrder)
        {
            InstanceId = string.IsNullOrWhiteSpace(instanceId) ? Guid.NewGuid().ToString("N") : instanceId;
            SourceObject = sourceObject;
            Profile = profile;
            Formula = formula;
            Tags = tags;
            MaxTargets = Mathf.Max(1, maxTargets);
            TargetPriorityMode = targetPriorityMode;
        }
    }
}

