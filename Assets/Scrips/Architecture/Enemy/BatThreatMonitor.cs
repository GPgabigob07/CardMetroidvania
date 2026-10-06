using System.Collections.Generic;
using UnityEngine;

namespace TicGame.Architecture
{
    public readonly struct BatThreatFacts
    {
        public BatThreatFacts(bool isMonitored, float relativeClosingSpeed, bool isWithinBaseMeleeReachOuterBand)
        {
            IsMonitored = isMonitored;
            RelativeClosingSpeed = relativeClosingSpeed;
            IsWithinBaseMeleeReachOuterBand = isWithinBaseMeleeReachOuterBand;
        }

        public bool IsMonitored { get; }
        public float RelativeClosingSpeed { get; }
        public bool IsWithinBaseMeleeReachOuterBand { get; }
    }

    public sealed class BatThreatMonitor : MonoBehaviour
    {
        [Header(header: "Monitoring")]
        [Min(min: 0f)]
        [Tooltip(tooltip: "Distance at which the player becomes relevant to the bat.")]
        [SerializeField] private float monitorRadius = 8f;

        [Header(header: "Base Melee Threat")]
        [Min(min: 0f)]
        [Tooltip(tooltip: "Unmodified player melee reach used by bat dodge prediction.")]
        [SerializeField] private float baseMeleeReach = 2f;

        [Min(min: 0f)]
        [Tooltip(tooltip: "Thickness of the outer base-reach band that can trigger a dodge threat.")]
        [SerializeField] private float outerBandThickness = 0.5f;

        private readonly List<Collider2D> detectionHits = new List<Collider2D>(8);

        public bool IsInRange(Transform target)
        {
            return target != null && target.gameObject.activeInHierarchy
                && ((Vector2)target.position - (Vector2)transform.position).sqrMagnitude
                    <= Mathf.Max(0f, monitorRadius) * Mathf.Max(0f, monitorRadius);
        }

        public Transform AcquirePlayer(out Rigidbody2D playerBody)
        {
            var filter = new ContactFilter2D { useTriggers = true };
            filter.SetLayerMask(LayerMask.GetMask("PlayerHitbox"));
            Physics2D.OverlapCircle(transform.position, Mathf.Max(0f, monitorRadius), filter, detectionHits);
            Transform closest = null;
            var closestDistance = float.PositiveInfinity;
            foreach (var hit in detectionHits)
            {
                var candidate = EnemyPlayerTargeting.CanonicalTarget(hit);
                if (candidate == null || !IsInRange(candidate.transform))
                {
                    continue;
                }

                var distance = ((Vector2)candidate.transform.position - (Vector2)transform.position).sqrMagnitude;
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closest = candidate.transform;
                }
            }

            playerBody = closest != null ? closest.GetComponent<Rigidbody2D>() : null;
            return closest;
        }

        public BatThreatFacts Evaluate(Transform target, Vector2 targetVelocity, Vector2 selfVelocity)
        {
            if (target == null)
            {
                return new BatThreatFacts(false, 0f, false);
            }

            var offset = (Vector2)target.position - (Vector2)transform.position;
            var distance = offset.magnitude;
            var isMonitored = IsInRange(target);
            var directionToTarget = distance > 0f ? offset / distance : Vector2.zero;
            var relativeClosingSpeed = -Vector2.Dot(targetVelocity - selfVelocity, directionToTarget);
            var clampedBaseReach = Mathf.Max(0f, baseMeleeReach);
            var innerBandEdge = Mathf.Max(0f, clampedBaseReach - Mathf.Max(0f, outerBandThickness));
            var isWithinOuterBand = distance >= innerBandEdge && distance <= clampedBaseReach;

            return new BatThreatFacts(isMonitored, relativeClosingSpeed, isWithinOuterBand);
        }

        public void Configure(float monitorRadius, float baseMeleeReach, float outerBandThickness)
        {
            this.monitorRadius = Mathf.Max(0f, monitorRadius);
            this.baseMeleeReach = Mathf.Max(0f, baseMeleeReach);
            this.outerBandThickness = Mathf.Max(0f, outerBandThickness);
        }
    }
}
