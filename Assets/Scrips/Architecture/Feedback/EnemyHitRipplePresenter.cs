using System;
using UnityEngine;

namespace TicGame.Architecture
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(200)]
    public sealed class EnemyHitRipplePresenter : MonoBehaviour
    {
        [Header("Enemy Visuals")]
        [SerializeField] private EnemyActor owner;
        [SerializeField] private HitRippleProfileSO profile;
        [Tooltip("Only enemy body sprites; do not include telegraphs or projectiles.")]
        [SerializeField] private SpriteRenderer[] visuals = Array.Empty<SpriteRenderer>();
        [Header("Diagnostic Playback")]
        [Tooltip("A test controller supplies Tick calls instead of automatic LateUpdate progression.")]
        [SerializeField] private bool manualPlayback;
        private readonly HitRippleVisualRuntime rendering = new();
        private EnemyActor subscribedOwner;
        public int ActiveCount => rendering.ActiveCount;
        public EnemyActor Owner => owner;
        public HitRippleProfileSO Profile => profile;
        public System.Collections.Generic.IReadOnlyList<SpriteRenderer> Visuals => visuals;
        public void Configure(EnemyActor enemy, HitRippleProfileSO definition, SpriteRenderer[] renderers)
        {
            Clear(); Unsubscribe(); owner = enemy; profile = definition; visuals = renderers ?? Array.Empty<SpriteRenderer>();
            rendering.Configure(owner != null ? owner.transform : null, profile, visuals); Subscribe();
        }
        private void OnEnable() { rendering.Configure(owner != null ? owner.transform : null, profile, visuals); Subscribe(); }
        private void OnDisable() { Clear(); Unsubscribe(); }
        private void OnDestroy() => Unsubscribe();
        private void LateUpdate() { if (!manualPlayback) Tick(Time.unscaledDeltaTime, PlaytestPauseController.IsGamePaused); }
        public void SetManualPlayback(bool enabled) => manualPlayback = enabled;
        private void Subscribe()
        {
            if (!isActiveAndEnabled || subscribedOwner == owner) return;
            Unsubscribe(); subscribedOwner = owner;
            if (subscribedOwner != null) subscribedOwner.ResetPerformed += Clear;
        }
        private void Unsubscribe()
        {
            if (subscribedOwner != null) subscribedOwner.ResetPerformed -= Clear;
            subscribedOwner = null;
        }
        public void Present(in DamageContext context, in DamageResult result) { if (isActiveAndEnabled) rendering.Present(context, result); }
        public void Tick(float unscaledDeltaTime, bool paused) => rendering.Tick(unscaledDeltaTime, paused);
        public void Clear() => rendering.Clear();
    }
}
