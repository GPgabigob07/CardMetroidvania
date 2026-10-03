using System;
using System.Collections.Generic;
using UnityEngine;

namespace TicGame.Architecture
{
    public sealed class PlayerWardRuntime : MonoBehaviour
    {
        [Header("Guard Bindings")]
        [SerializeField, Tooltip("Live settings supplied by the Ward card operation.")]
        private WardDefinitionSO definition;
        [SerializeField] private SimpleHealth health;
        [SerializeField, Tooltip("Player's physical damage bounds; resolved on this root when omitted.")]
        private Collider2D bodyCollider;
        private WardConfiguration configuration;
        private bool initialized;
        private bool reserved;
        private bool warnedInvalid;
        private long reservationRevision;
        private SimpleHealth subscribedHealth;
        private readonly HashSet<long> counteredCasts = new();
        public event Action Armed;
        public event Action Expired;
        public event Action<long> Countered;
        public bool IsActive { get; private set; }
        public float Elapsed { get; private set; }
        public int FacingSign { get; private set; } = 1;
        public bool CanArm => initialized && CanArmDefinition(definition);
        public Vector2 GuardCenter { get { Refresh(); return (Vector2)transform.position + Vector2.right * (FacingSign * configuration.ForwardOffset); } }
        public WardConfiguration Configuration { get { Refresh(); return configuration; } }
        private bool Available => isActiveAndEnabled && health != null && !health.IsDead && Time.timeScale > 0
            && GetComponentInParent<PlayerWorldHold>()?.IsHeld != true;

        private void Awake()
        {
            ResolveDependencies();
            if (definition != null) Initialize(definition);
        }
        private void Update() => Tick(Time.deltaTime);
        private void OnDisable() => Clear();
        private void OnDestroy() => Unsubscribe();
        public void Initialize(WardDefinitionSO value)
        {
            if (value == null || !value.TryRead(out var current)) throw new ArgumentException("A valid Ward asset is required.", nameof(value));
            if ((IsActive || reserved) && value != definition) throw new InvalidOperationException("Cannot replace a committed Ward's definition.");
            definition = value; configuration = current; initialized = true; warnedInvalid = false;
            ResolveDependencies(); Subscribe();
        }

        public bool CanArmDefinition(WardDefinitionSO value) => value != null && value.TryRead(out _)
            && Available && !IsActive && !reserved;
        public bool Arm(int facingSign)
        {
            if (!initialized || !definition.TryRead(out var quoted) || !TryReserve(definition, quoted, out var lease)) return false;
            if (!CommitReserved(lease, facingSign)) { ReleaseReservation(lease); return false; }
            PublishArmed(); return true;
        }

        internal bool TryReserve(WardDefinitionSO value, WardConfiguration quoted, out long lease)
        {
            lease = 0;
            ResolveDependencies();
            if (!CanArmDefinition(value) || !value.TryRead(out var current) || !current.Equals(quoted)) return false;
            definition = value; configuration = current; initialized = true;
            Subscribe(); reserved = true; lease = ++reservationRevision;
            return true;
        }
        internal bool CommitReserved(long lease, int facingSign)
        {
            if (!reserved || lease != reservationRevision || !Available || IsActive) return false;
            reserved = false; FacingSign = facingSign < 0 ? -1 : 1; Elapsed = 0; IsActive = true;
            return true;
        }
        internal void ReleaseReservation(long lease) { if (lease == reservationRevision) reserved = false; }
        internal void PublishArmed() => Armed?.Invoke();

        public void Tick(float scaledDelta)
        {
            if (!float.IsFinite(scaledDelta) || scaledDelta < 0) throw new ArgumentOutOfRangeException(nameof(scaledDelta));
            ResolveDependencies();
            if (health != null && health.IsDead) { Clear(); return; }
            if (!Available || !initialized) return;
            Refresh();
            if (!IsActive) return;
            Elapsed += scaledDelta;
            ExpireIfDue();
        }

        public WardInterception TryIntercept(in BeamGuardQuery query)
        {
            if (!Available || !initialized || !IsActive || query.CastToken <= 0 || counteredCasts.Contains(query.CastToken)
                || !float.IsFinite(query.Thickness) || query.Thickness <= 0
                || !Finite(query.Start) || !Finite(query.End) || !Finite(query.EmitterPosition)) return default;
            Refresh(); ExpireIfDue();
            if (!IsActive || (query.EmitterPosition.x - transform.position.x) * FacingSign <= 0) return default;
            var radius = query.Thickness / 2;
            if (!BeamSegmentGeometry.TryIntersectVerticalGuard(query.Start, query.End, GuardCenter, configuration.Height,
                radius + configuration.IntersectionTolerance, out var guardFraction)) return default;
            bodyCollider ??= GetComponent<Collider2D>();
            if (bodyCollider != null && bodyCollider.enabled
                && BeamSegmentGeometry.TryIntersectBounds(query.Start, query.End, bodyCollider.bounds, radius, out var bodyFraction)
                && bodyFraction <= guardFraction) return default;
            var clip = Vector2.Lerp(query.Start, query.End, guardFraction);
            if (!query.IsOpening) return new WardInterception(WardInterceptionKind.Clipped, clip);
            counteredCasts.Add(query.CastToken); IsActive = false;
            Countered?.Invoke(query.CastToken);
            return new WardInterception(WardInterceptionKind.Countered, clip);
        }

        public void Clear()
        {
            IsActive = reserved = false; Elapsed = 0; reservationRevision++; counteredCasts.Clear();
        }
        private void ExpireIfDue()
        {
            if (!IsActive || (Elapsed < configuration.Duration && !Mathf.Approximately(Elapsed, configuration.Duration))) return;
            IsActive = false; Expired?.Invoke();
        }
        private void Refresh()
        {
            if (!initialized) return;
            if (definition != null && definition.TryRead(out var current)) { configuration = current; warnedInvalid = false; return; }
            if (warnedInvalid) return;
            warnedInvalid = true; Debug.LogWarning("Invalid live Ward configuration; retaining the last valid settings.", this);
        }
        private void ResolveDependencies() { health ??= GetComponent<SimpleHealth>(); bodyCollider ??= GetComponent<Collider2D>(); }
        private void Subscribe()
        {
            if (subscribedHealth == health) return;
            Unsubscribe(); subscribedHealth = health;
            if (subscribedHealth != null) subscribedHealth.Changed += OnHealthChanged;
        }
        private void OnHealthChanged(SimpleHealthChanged _) { if (health.IsDead) Clear(); }
        private void Unsubscribe() { if (subscribedHealth != null) subscribedHealth.Changed -= OnHealthChanged; subscribedHealth = null; }
        private static bool Finite(Vector2 value) => float.IsFinite(value.x) && float.IsFinite(value.y);
    }
}
