using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TicGame.Architecture
{
    [Serializable]
    public sealed class EnemyAttackPayload
    {
        [Header("Kind And Target Budget")]
        [SerializeField] private EnemyAttackPayloadKind kind;
        [SerializeField, Min(1), Tooltip("Accepted hits per canonical target per strike/cast.")]
        private int acceptedHitLimit = 1;
        [Header("Melee And Advance")]
        [SerializeField] private Vector2 hitboxSize = new Vector2(1.5f, 1);
        [SerializeField] private Vector2 offset = new Vector2(1, 1);
        [SerializeField] private float hitboxAngle;
        [SerializeField, Min(0)] private float advanceDistance;
        [SerializeField, Min(0)] private float advanceSpeed = 4;
        [Header("Aim Commitment")]
        [SerializeField, Min(0), Tooltip("Final part of windup during which aim remains locked.")]
        private float lockedAimDuration;
        [Header("Projectile Motion")]
        [SerializeField, Min(0)] private float projectileSpeed = 6;
        [SerializeField, Min(0)] private float projectileLifetime = 3;
        [Header("Beam")]
        [SerializeField, Min(0)] private float beamLength = 10;
        [SerializeField, Min(0)] private float beamThickness = .3f;
        [SerializeField, Min(0)] private float beamOpeningDuration = .2f;
        [Header("Reactor Nova")]
        [SerializeField, Min(0)] private float novaRadius = 3.5f;
        [SerializeField, Min(0)] private float reactorStability = 4.8f;

        public EnemyAttackPayloadKind Kind => kind;
        public int AcceptedHitLimit => acceptedHitLimit;
        public Vector2 HitboxSize => hitboxSize;
        public Vector2 Offset => offset;
        public float HitboxAngle => hitboxAngle;
        public float AdvanceDistance => advanceDistance;
        public float AdvanceSpeed => advanceSpeed;
        public float LockedAimDuration => lockedAimDuration;
        public float ProjectileSpeed => projectileSpeed;
        public float ProjectileLifetime => projectileLifetime;
        public float BeamLength => beamLength;
        public float BeamThickness => beamThickness;
        public float BeamOpeningDuration => beamOpeningDuration;
        public float NovaRadius => novaRadius;
        public float ReactorStability => reactorStability;

        public IReadOnlyList<string> GetValidationErrors(float windup, float active)
        {
            var errors = new List<string>();
            if (!Enum.IsDefined(typeof(EnemyAttackPayloadKind), kind) || acceptedHitLimit < 1)
                errors.Add("Payload requires a supported kind and positive accepted-hit limit.");
            if (!Finite(hitboxSize) || hitboxSize.x <= 0 || hitboxSize.y <= 0
                || !Finite(offset) || !float.IsFinite(hitboxAngle))
                errors.Add("Payload geometry must be finite with positive hitbox dimensions.");
            if (new[] { advanceDistance, advanceSpeed, lockedAimDuration }.Any(value => !float.IsFinite(value) || value < 0)
                || lockedAimDuration > windup || (advanceDistance > 0 && advanceSpeed <= 0))
                errors.Add("Advance and aim-lock values must be finite and fit within the step.");
            if (kind == EnemyAttackPayloadKind.Volley
                && (!Positive(projectileSpeed) || !Positive(projectileLifetime)))
                errors.Add("Projectile motion values must be finite and positive.");
            if (kind == EnemyAttackPayloadKind.Beam
                && (!Positive(beamLength) || !Positive(beamThickness) || !Positive(beamOpeningDuration) || beamOpeningDuration > active))
                errors.Add("Beam geometry must be positive and its opening must fit inside Active.");
            if (kind == EnemyAttackPayloadKind.Nova && (!Positive(novaRadius) || !Positive(reactorStability)))
                errors.Add("Nova radius and reactor stability must be finite and positive.");
            return errors;
        }

        internal EnemyAttackPayload Copy() => (EnemyAttackPayload)MemberwiseClone();
        private static bool Positive(float value) => float.IsFinite(value) && value > 0;
        private static bool Finite(Vector2 value) => float.IsFinite(value.x) && float.IsFinite(value.y);
    }
}
