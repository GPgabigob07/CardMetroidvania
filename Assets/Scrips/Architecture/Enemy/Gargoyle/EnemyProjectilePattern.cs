using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TicGame.Architecture
{
    [Serializable]
    public sealed class EnemyProjectilePattern
    {
        [SerializeField, Min(1)] private int count;
        [SerializeField, Min(0)] private float windupDuration;
        [SerializeField, Tooltip("One authored degree offset per shot, applied around the locked direction.")]
        private float[] angles;

        public EnemyProjectilePattern(int count, float windup, float[] angles)
        {
            this.count = count;
            windupDuration = windup;
            this.angles = angles != null ? (float[])angles.Clone() : null;
        }

        public int Count => count;
        public float WindupDuration => windupDuration;
        public IReadOnlyList<float> Angles => Array.AsReadOnly(angles ?? Array.Empty<float>());

        public IReadOnlyList<string> GetValidationErrors()
        {
            var errors = new List<string>();
            if (count < 1 || angles == null || angles.Length != count || angles.Any(angle => !float.IsFinite(angle)))
                errors.Add("Projectile pattern must contain one finite angle per authored shot.");
            if (!float.IsFinite(windupDuration) || windupDuration <= 0)
                errors.Add("Projectile pattern windup must be finite and positive.");
            return errors;
        }

        public EnemyProjectilePattern Copy() => new EnemyProjectilePattern(count, windupDuration, angles);
    }
}
