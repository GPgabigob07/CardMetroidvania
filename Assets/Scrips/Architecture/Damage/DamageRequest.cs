using System;
using System.Collections.Generic;
using UnityEngine;

namespace TicGame.Architecture
{
    [Serializable]
    public struct DamageRequest
    {
        public DamageInstance Instance;
        public IReadOnlyList<GameObject> CandidateTargets;
        public Vector2 HitPoint;
        public Vector2 Direction;
        public GameplayTagSet RequestTags;
        public int TargetLimit;
        public TargetPriorityMode TargetPriorityMode;
        public bool AllowPartialResolution;

        public DamageRequest(
            DamageInstance instance,
            IReadOnlyList<GameObject> candidateTargets,
            Vector2 hitPoint,
            Vector2 direction,
            GameplayTagSet requestTags = null,
            int targetLimit = 1,
            TargetPriorityMode targetPriorityMode = TargetPriorityMode.ExplicitOrder,
            bool allowPartialResolution = true)
        {
            Instance = instance;
            CandidateTargets = candidateTargets ?? Array.Empty<GameObject>();
            HitPoint = hitPoint;
            Direction = direction;
            RequestTags = requestTags;
            TargetLimit = Mathf.Max(1, targetLimit);
            TargetPriorityMode = targetPriorityMode;
            AllowPartialResolution = allowPartialResolution;
        }
    }
}

