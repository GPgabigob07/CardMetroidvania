using System;
using UnityEngine;

namespace TicGame.Architecture
{
    [DisallowMultipleComponent]
    public sealed class PlayerRepelRuntime : MonoBehaviour, IGameplayServicesConsumer
    {
        [Header("Player")]
        [SerializeField, Tooltip("Living player that owns this temporary effect. Resolved on this root when omitted.")]
        private SimpleHealth health;

        private SimpleHealth subscribedHealth;
        private ICardFeedbackService feedback;
        private CardDefinitionSO card;
        private bool reserved;
        private long reservationRevision;
        private float reservedDuration;

        public event Action Armed;
        public event Action Expired;
        public bool IsActive => RemainingSeconds > 0f;
        public float RemainingSeconds { get; private set; }
        private string FeedbackKey => $"{GetInstanceID()}:repel";
        private bool Available => isActiveAndEnabled && health != null && !health.IsDead
            && Time.timeScale > 0f && !PlaytestPauseController.IsGamePaused
            && GetComponentInParent<PlayerWorldHold>()?.IsHeld != true;

        private void Awake() => ResolveHealth();
        private void Update() => Tick(Time.deltaTime);
        private void OnDisable() => Clear();
        private void OnDestroy()
        {
            if (subscribedHealth != null) subscribedHealth.Changed -= OnHealthChanged;
            feedback?.RemoveHudEffect(FeedbackKey);
        }

        public void BindGameplayServices(IGameplayServices services)
        {
            feedback?.RemoveHudEffect(FeedbackKey);
            feedback = services?.CardFeedback;
            RefreshHud();
        }

        public bool CanArm(float duration)
        {
            ResolveHealth();
            return float.IsFinite(duration) && duration > 0f && Available && !IsActive && !reserved;
        }

        public bool Arm(float duration)
        {
            if (!TryReserve(duration, out var lease)) return false;
            if (!CommitReserved(lease)) { ReleaseReservation(lease); return false; }
            PublishArmed();
            return true;
        }

        internal bool TryReserve(float duration, out long lease)
        {
            lease = 0;
            if (!CanArm(duration)) return false;
            reserved = true;
            reservedDuration = duration;
            lease = ++reservationRevision;
            return true;
        }

        internal bool CommitReserved(long lease, CardDefinitionSO sourceCard = null)
        {
            if (!reserved || lease != reservationRevision || !Available || IsActive) return false;
            reserved = false;
            RemainingSeconds = reservedDuration;
            card = sourceCard;
            return true;
        }

        internal void ReleaseReservation(long lease)
        {
            if (lease == reservationRevision) reserved = false;
        }

        internal void PublishArmed()
        {
            RefreshHud();
            Armed?.Invoke();
        }

        public void Tick(float scaledDelta)
        {
            if (!float.IsFinite(scaledDelta) || scaledDelta < 0f)
                throw new ArgumentOutOfRangeException(nameof(scaledDelta));
            ResolveHealth();
            if (health != null && health.IsDead) { Clear(); return; }
            if (!Available || !IsActive) return;
            RemainingSeconds = Mathf.Max(0f, RemainingSeconds - scaledDelta);
            if (RemainingSeconds <= .0001f)
            {
                RemainingSeconds = 0f;
                feedback?.RemoveHudEffect(FeedbackKey);
                PublishWorld(CardFeedbackKind.Expired);
                Expired?.Invoke();
                return;
            }
            RefreshHud();
        }

        public bool TryRepel(EnemyProjectile2D projectile)
        {
            ResolveHealth();
            if (!Available || !IsActive || projectile == null || !projectile.IsLaunched
                || projectile.SourceObject == null || projectile.SourceObject.transform.IsChildOf(transform)) return false;
            var direction = (Vector2)projectile.transform.position - (Vector2)transform.position;
            if (direction.sqrMagnitude <= .000001f) direction = -projectile.Direction;
            var converted = projectile.TryDeflect(gameObject, direction);
            if (converted) PublishWorld(CardFeedbackKind.Triggered);
            return converted;
        }

        public void Clear()
        {
            var wasActive = IsActive;
            RemainingSeconds = 0f;
            reserved = false;
            reservationRevision++;
            feedback?.RemoveHudEffect(FeedbackKey);
            if (wasActive) PublishWorld(CardFeedbackKind.Cleared);
            card = null;
        }

        private void ResolveHealth()
        {
            health ??= GetComponent<SimpleHealth>();
            if (health == subscribedHealth) return;
            if (subscribedHealth != null) subscribedHealth.Changed -= OnHealthChanged;
            subscribedHealth = health;
            if (subscribedHealth != null) subscribedHealth.Changed += OnHealthChanged;
        }

        private void OnHealthChanged(SimpleHealthChanged change)
        {
            if (health.IsDead) Clear();
        }

        private void RefreshHud()
        {
            if (IsActive && card != null)
                feedback?.UpsertHudEffect(new CardHudEffectViewModel(FeedbackKey, gameObject, card, $"{RemainingSeconds:0.0}s"));
        }

        private void PublishWorld(CardFeedbackKind kind)
        {
            if (card != null)
                feedback?.PublishWorldFeedback(new CardWorldFeedbackViewModel(
                    card: card, sourceObject: gameObject, kind: kind, worldPosition: transform.position));
        }
    }
}
