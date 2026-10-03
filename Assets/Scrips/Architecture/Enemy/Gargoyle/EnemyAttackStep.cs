using System;
using System.Collections.Generic;
using UnityEngine;

namespace TicGame.Architecture
{
    [Serializable]
    public sealed class EnemyAttackStep
    {
        [Header("Identity")]
        [Tooltip("Stable within the attack; a running execution retains this step's identity.")]
        [SerializeField] private string id;
        [Header("Gameplay Phase Durations")]
        [Min(0), Tooltip("Anticipation before aim locks; must be positive.")]
        [SerializeField] private float windupDuration;
        [Min(0), Tooltip("Damaging interval; the runner requires a physics sample before advancing.")]
        [SerializeField] private float activeDuration;
        [Min(0), Tooltip("Recovery after this strike. Live edits preserve elapsed time.")]
        [SerializeField] private float recoveryDuration;
        [Header("Payload")]
        [Tooltip("Single owner of authored health damage and hitstop; no copied damage amount.")]
        [SerializeField] private DamageProfileSO damageProfile;
        [SerializeField] private EnemyAttackPayload payload = new EnemyAttackPayload();

        public EnemyAttackStep(string id, float windup, float active, float recovery, DamageProfileSO profile)
        {
            this.id = id;
            windupDuration = windup;
            activeDuration = active;
            recoveryDuration = recovery;
            damageProfile = profile;
        }

        public string Id => id;
        public float WindupDuration => windupDuration;
        public float ActiveDuration => activeDuration;
        public float RecoveryDuration => recoveryDuration;
        public DamageProfileSO DamageProfile => damageProfile;
        public EnemyAttackPayload Payload => payload;

        internal bool TryRead(out EnemyAttackStep snapshot)
        {
            snapshot = null;
            if (GetValidationErrors().Count != 0) return false;
            snapshot = (EnemyAttackStep)MemberwiseClone();
            snapshot.payload = payload.Copy();
            return true;
        }

        internal EnemyAttackStep CopyForKind(EnemyAttackPayloadKind committedKind)
        {
            var copy = (EnemyAttackStep)MemberwiseClone();
            copy.payload = payload.CopyForKind(committedKind);
            return copy;
        }

        public IReadOnlyList<string> GetValidationErrors()
        {
            var errors = new List<string>();
            if (string.IsNullOrWhiteSpace(id)) errors.Add("Attack step requires a stable ID.");
            if (!float.IsFinite(windupDuration) || windupDuration <= 0
                || !float.IsFinite(activeDuration) || activeDuration <= 0
                || !float.IsFinite(recoveryDuration) || recoveryDuration < 0)
                errors.Add("Step phase durations must be finite; windup and active must be positive, recovery non-negative.");
            if (damageProfile == null) errors.Add("Attack step requires a damage profile.");
            if (payload == null) errors.Add("Attack step requires a payload definition.");
            else foreach (var error in payload.GetValidationErrors(windupDuration, activeDuration)) errors.Add(error);
            return errors;
        }
    }
}
