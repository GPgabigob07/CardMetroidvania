using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TicGame.Architecture
{
    [DisallowMultipleComponent]
    public sealed class HitRippleTestController : MonoBehaviour
    {
        [Header("Test Scene")]
        [SerializeField] private Camera testCamera;
        [SerializeField] private HitRippleTestTarget[] targets = Array.Empty<HitRippleTestTarget>();
        [SerializeField] private LayerMask targetLayers = ~0;
        [Header("Repeat Hits")]
        [Min(.02f)] [SerializeField] private float repeatInterval = .12f;
        private HitRippleKind mode = HitRippleKind.Damage;
        private HitRippleTestTarget selected;
        private float repeatRemaining;
        private int burstRemaining;
        private float burstDelay;
        private Vector2 burstPoint;
        private HitRippleKind burstKind;
        private bool paused;
        private GUIStyle titleStyle;
        private GUIStyle labelStyle;
        public bool IsPaused => paused;
        public void SetPaused(bool value) => paused = value;
        public void Configure(Camera camera, HitRippleTestTarget[] values, LayerMask layers)
        { testCamera = camera; targets = values ?? Array.Empty<HitRippleTestTarget>(); targetLayers = layers; }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.digit1Key.wasPressedThisFrame) mode = HitRippleKind.Damage;
                if (keyboard.digit2Key.wasPressedThisFrame) mode = HitRippleKind.Rejected;
                if (keyboard.digit3Key.wasPressedThisFrame) mode = HitRippleKind.Fatal;
                if (keyboard.pKey.wasPressedThisFrame) paused = !paused;
                if (keyboard.rKey.wasPressedThisFrame) Clear();
                if (keyboard.bKey.wasPressedThisFrame) Burst();
            }
            if (paused) return;
            var delta = Time.unscaledDeltaTime;
            repeatRemaining = Mathf.Max(0, repeatRemaining - delta);
            var mouse = Mouse.current;
            if (mouse != null && testCamera != null)
            {
                var point = mouse.position.ReadValue();
                // Ignore the diagnostic control panel, so clicking buttons cannot strike a target.
                var inPanel = point.y > Screen.height - 130;
                if (!inPanel && mouse.leftButton.isPressed && (mouse.leftButton.wasPressedThisFrame || repeatRemaining <= 0))
                { HitScreenPoint(point, mode); repeatRemaining = repeatInterval; }
                if (!inPanel && mouse.rightButton.wasPressedThisFrame) HitScreenPoint(point, HitRippleKind.Rejected);
                if (!inPanel && mouse.middleButton.wasPressedThisFrame) HitScreenPoint(point, HitRippleKind.Fatal);
            }
            if (burstRemaining > 0)
            {
                burstDelay -= delta;
                if (burstDelay <= 0)
                {
                    if (selected != null) selected.ResolveHit(burstPoint, burstKind);
                    burstRemaining--; burstDelay = repeatInterval;
                }
            }
        }
        private void LateUpdate()
        {
            foreach (var target in targets)
                if (target != null && target.Owner != null)
                    target.Owner.GetComponent<EnemyHitRipplePresenter>()?.Tick(Time.unscaledDeltaTime, paused);
        }
        public DamageResolutionReport HitScreenPoint(Vector2 screenPoint, HitRippleKind kind)
        {
            if (testCamera == null || paused) return null;
            var world = testCamera.ScreenToWorldPoint(new Vector3(screenPoint.x, screenPoint.y, -testCamera.transform.position.z));
            var shape = Physics2D.OverlapPoint(world, targetLayers);
            var target = shape != null ? shape.GetComponent<HitRippleTestTarget>() : null;
            if (target == null) return null;
            selected = target;
            return target.ResolveHit(world, kind);
        }
        private void Burst()
        {
            if (selected == null || selected.HitCount == 0) return;
            burstPoint = selected.LastHitPoint; burstKind = mode; burstRemaining = 3; burstDelay = 0;
        }
        private void Clear()
        {
            burstRemaining = 0;
            foreach (var target in targets)
                if (target != null && target.Owner != null) target.Owner.GetComponent<EnemyHitRipplePresenter>()?.Clear();
        }
        private void OnGUI()
        {
            titleStyle ??= new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold, normal = { textColor = Color.white } };
            labelStyle ??= new GUIStyle(GUI.skin.label) { fontSize = 15, normal = { textColor = Color.white } };
            GUI.Box(new Rect(10, 10, Screen.width - 20, 118), GUIContent.none);
            GUI.Label(new Rect(25, 16, Screen.width - 50, 32), "Hit Ripple Lab — click any point on a sprite", titleStyle);
            GUI.Label(new Rect(25, 49, Screen.width - 50, 24), "Left click / hold: selected mode   •   Right: rejected   •   Middle: fatal   •   B: 3-hit burst   •   P: freeze   •   R: clear", labelStyle);
            if (GUI.Button(new Rect(25, 82, 100, 28), "1 · Damage")) mode = HitRippleKind.Damage;
            if (GUI.Button(new Rect(133, 82, 100, 28), "2 · Rejected")) mode = HitRippleKind.Rejected;
            if (GUI.Button(new Rect(241, 82, 100, 28), "3 · Fatal")) mode = HitRippleKind.Fatal;
            if (GUI.Button(new Rect(355, 82, 92, 28), "3-hit burst")) Burst();
            if (GUI.Button(new Rect(455, 82, 85, 28), paused ? "Resume" : "Freeze")) paused = !paused;
            GUI.Label(new Rect(550, 86, Screen.width - 570, 24), $"Mode: {mode}   •   Orange + = pivot   •   Pink + = last hit", labelStyle);
            if (testCamera == null) return;
            foreach (var target in targets)
            {
                if (target == null || target.Owner == null) continue;
                var presenter = target.Owner.GetComponent<EnemyHitRipplePresenter>();
                if (presenter == null || presenter.Visuals.Count == 0) continue;
                var visual = presenter.Visuals[0];
                Mark(visual.transform.position, new Color(1, .65f, .15f));
                if (target.HitCount > 0) Mark(target.LastHitPoint, new Color(1, .3f, .75f));
                var screen = testCamera.WorldToScreenPoint(new Vector3(target.Owner.transform.position.x, visual.bounds.min.y - .4f, 0));
                GUI.Label(new Rect(screen.x - 165, Screen.height - screen.y, 330, 65),
                    $"{target.Owner.name}\n{presenter.ActiveCount}/3 waves · {presenter.Profile.WidthPixels:0} px · {presenter.Profile.TraversalSeconds:0.00}s\n" +
                    (target.HitCount > 0 ? $"Hit: ({target.LastHitPoint.x:0.00}, {target.LastHitPoint.y:0.00}) · {target.LastKind}" : "Click off-center to compare hit and pivot"), labelStyle);
            }
        }
        private void Mark(Vector3 worldPoint, Color color)
        {
            var screen = testCamera.WorldToScreenPoint(worldPoint);
            var previous = GUI.color; GUI.color = color;
            GUI.DrawTexture(new Rect(screen.x - 6, Screen.height - screen.y - 1, 12, 2), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(screen.x - 1, Screen.height - screen.y - 6, 2, 12), Texture2D.whiteTexture);
            GUI.color = previous;
        }
    }
}
