using System;
using UnityEngine;

namespace TicGame.Architecture
{
    [Serializable]
    public readonly struct HitStopRequest
    {
        public HitStopRequest(
            float duration,
            GameObject sourceObject,
            string damageInstanceId)
        {
            Duration = Mathf.Max(a: 0f, b: duration);
            SourceObject = sourceObject;
            DamageInstanceId = damageInstanceId;
        }

        public float Duration { get; }
        public GameObject SourceObject { get; }
        public string DamageInstanceId { get; }
    }
}
