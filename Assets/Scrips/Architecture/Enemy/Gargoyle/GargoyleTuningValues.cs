using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TicGame.Architecture
{
    [Serializable]
    public sealed class GargoyleTuningValues
    {
        [Header("Poise And Responses")]
        [SerializeField, Min(0)] private float maximumPoise = 12;
        [SerializeField, Min(0)] private float poiseRegeneration;
        [SerializeField, Range(0, 1), Tooltip("Fraction of the current ceiling restored after a completed response.")]
        private float restoredPoiseFraction = 1;
        [SerializeField, Min(0)] private float stunDuration = 1.25f;
        [SerializeField, Min(0)] private float beamStaggerDuration = .65f;
        [SerializeField, Min(0)] private float poiseResistanceDuration = .75f;
        [Header("Grounded Movement")]
        [SerializeField, Min(0)] private float movementSpeed = 2;
        [SerializeField, Min(0)] private float arrivalTolerance = .1f;
        [SerializeField, Min(0)] private float monitorRange = 16;
        [SerializeField, Min(0)] private float engageRange = 12;
        [SerializeField, Min(0)] private float meleeDistance = 1.8f;
        [SerializeField, Min(0)] private float rangedDistance = 5;
        [SerializeField, Tooltip("Right-facing supporting-ground probe measured from the foot root.")]
        private Vector2 ledgeProbeOffset = new Vector2(1.05f, -.05f);
        [SerializeField, Tooltip("Right-facing wall probe measured from the foot root.")]
        private Vector2 wallProbeOffset = new Vector2(1, 1);
        [SerializeField, Min(0)] private float probeRadius = .08f;
        [SerializeField, Min(0)] private float blockedRepositionTimeout = 1.5f;
        [Header("Pass And Nova Scheduling")]
        [SerializeField, Min(0)] private float familyRecovery = .25f;
        [SerializeField, Min(0)] private float passRecovery = .8f;
        [SerializeField, Min(1)] private int novaBagCadence = 2;
        [SerializeField, Min(0)] private float novaCooldown = 12;
        [Header("Bounded Feint")]
        [SerializeField, Range(0, 1)] private float feintProbability = .2f;
        [SerializeField, Min(0)] private int feintBudget = 1;
        [SerializeField, Min(0)] private float feintCueDelay = .15f;
        [SerializeField, Min(0)] private float feintResponseDuration = .35f;
        [Header("Incoming Damage")]
        [SerializeField, Min(0)] private float bodyDamageMultiplier = 1;
        [SerializeField, Min(0)] private float exposedHeadMultiplier = 1.5f;

        public float MaximumPoise => maximumPoise;
        public float PoiseRegeneration => poiseRegeneration;
        public float RestoredPoiseFraction => restoredPoiseFraction;
        public float StunDuration => stunDuration;
        public float BeamStaggerDuration => beamStaggerDuration;
        public float PoiseResistanceDuration => poiseResistanceDuration;
        public float MovementSpeed => movementSpeed;
        public float ArrivalTolerance => arrivalTolerance;
        public float MonitorRange => monitorRange;
        public float EngageRange => engageRange;
        public float MeleeDistance => meleeDistance;
        public float RangedDistance => rangedDistance;
        public Vector2 LedgeProbeOffset => ledgeProbeOffset;
        public Vector2 WallProbeOffset => wallProbeOffset;
        public float ProbeRadius => probeRadius;
        public float BlockedRepositionTimeout => blockedRepositionTimeout;
        public float FamilyRecovery => familyRecovery;
        public float PassRecovery => passRecovery;
        public int NovaBagCadence => novaBagCadence;
        public float NovaCooldown => novaCooldown;
        public float FeintProbability => feintProbability;
        public int FeintBudget => feintBudget;
        public float FeintCueDelay => feintCueDelay;
        public float FeintResponseDuration => feintResponseDuration;
        public float BodyDamageMultiplier => bodyDamageMultiplier;
        public float ExposedHeadMultiplier => exposedHeadMultiplier;

        public IReadOnlyList<string> GetValidationErrors()
        {
            var errors = new List<string>();
            var nonNegative = new[] { maximumPoise, poiseRegeneration, restoredPoiseFraction,
                stunDuration, beamStaggerDuration, poiseResistanceDuration, movementSpeed,
                arrivalTolerance, monitorRange, engageRange, meleeDistance, rangedDistance,
                blockedRepositionTimeout, familyRecovery, passRecovery, novaCooldown,
                feintProbability, feintCueDelay, feintResponseDuration, bodyDamageMultiplier, exposedHeadMultiplier };
            if (nonNegative.Any(value => !float.IsFinite(value) || value < 0))
                errors.Add("Simulation tuning values must be finite and non-negative.");
            if (!float.IsFinite(probeRadius) || probeRadius < .01f
                || !Finite(ledgeProbeOffset) || !Finite(wallProbeOffset))
                errors.Add("Probe geometry must be finite; radius must satisfy the existing motor's 0.01-unit minimum.");
            if (feintProbability > 1 || restoredPoiseFraction > 1 || feintBudget < 0 || novaBagCadence < 1)
                errors.Add("Feint, restoration and cadence values are outside their supported ranges.");
            if (monitorRange < engageRange || meleeDistance > engageRange || rangedDistance > engageRange)
                errors.Add("Engagement and spacing distances must fit within the monitor range.");
            if (stunDuration <= 0 || beamStaggerDuration <= 0 || blockedRepositionTimeout <= 0
                || (feintProbability > 0 && feintBudget > 0 && feintResponseDuration <= 0))
                errors.Add("Response and reposition durations must preserve a positive anticipation/recovery opportunity.");
            return errors;
        }

        internal GargoyleTuningValues Copy() => (GargoyleTuningValues)MemberwiseClone();
        private static bool Finite(Vector2 value) => float.IsFinite(value.x) && float.IsFinite(value.y);
    }
}
