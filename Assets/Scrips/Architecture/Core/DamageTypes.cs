using System;
using UnityEngine;

namespace TicGame.Architecture
{
    /// <summary>
    /// Describes an object that can provide source data for damage transactions.
    /// </summary>
    public interface IDamageSource
    {
        /// <summary>
        /// Gets the GameObject responsible for the damage.
        /// </summary>
        GameObject SourceObject { get; }

        /// <summary>
        /// Gets tags that describe the damage source.
        /// </summary>
        GameplayTagSet SourceTags { get; }
    }

    /// <summary>
    /// Describes an object that can receive a damage transaction.
    /// </summary>
    public interface IDamageable
    {
        /// <summary>
        /// Applies damage and returns the resolved result.
        /// </summary>
        DamageResult ApplyDamage(in DamageContext context);
    }

    [Serializable]
    public struct DamageContext
    {
        [SerializeField] private GameObject source;
        [SerializeField] private GameObject target;
        [SerializeField] private DamageProfileSO profile;
        [SerializeField] private float amount;
        [SerializeField] private Vector2 hitPoint;
        [SerializeField] private Vector2 direction;
        [SerializeField] private GameplayTagSet tags;

        public DamageContext(
            GameObject source,
            GameObject target,
            DamageProfileSO profile,
            float amount,
            Vector2 hitPoint,
            Vector2 direction,
            GameplayTagSet tags = null)
        {
            this.source = source;
            this.target = target;
            this.profile = profile;
            this.amount = amount;
            this.hitPoint = hitPoint;
            this.direction = direction;
            this.tags = tags;
        }

        public GameObject Source => source;
        public GameObject Target => target;
        public DamageProfileSO Profile => profile;
        public float Amount => amount;
        public Vector2 HitPoint => hitPoint;
        public Vector2 Direction => direction;
        public GameplayTagSet Tags => tags;
    }

    [Serializable]
    public struct DamageResult
    {
        [SerializeField] private bool accepted;
        [SerializeField] private bool killed;
        [SerializeField] private float appliedAmount;
        [SerializeField] private float remainingHealth;
        [SerializeField] private float hitStopSeconds;

        public DamageResult(
            bool accepted,
            bool killed,
            float appliedAmount,
            float remainingHealth,
            float hitStopSeconds = 0.033f)
        {
            this.accepted = accepted;
            this.killed = killed;
            this.appliedAmount = appliedAmount;
            this.remainingHealth = remainingHealth;
            this.hitStopSeconds = Mathf.Max(a: 0f, b: hitStopSeconds);
        }

        public bool Accepted => accepted;
        public bool Killed => killed;
        public float AppliedAmount => appliedAmount;
        public float RemainingHealth => remainingHealth;
        public float HitStopSeconds => hitStopSeconds;
    }
}
