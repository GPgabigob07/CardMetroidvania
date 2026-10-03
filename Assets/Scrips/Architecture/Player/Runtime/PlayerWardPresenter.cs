using UnityEngine;
namespace TicGame.Architecture
{
    public sealed class PlayerWardPresenter : MonoBehaviour
    {
        [Header("Guard Feedback")]
        [SerializeField] private PlayerWardRuntime runtime;
        [SerializeField] private WardDefinitionSO definition;
        [SerializeField] private LineRenderer line;
        public void Configure(PlayerWardRuntime runtime, WardDefinitionSO definition, LineRenderer line)
        {
            this.runtime = runtime; this.definition = definition; this.line = line;
        }
        private void LateUpdate() => RefreshVisuals();
        private void OnDisable() { if (line != null) line.enabled = false; }
        public void RefreshVisuals()
        {
            if (line == null) return;
            line.enabled = runtime != null && runtime.IsActive && definition != null;
            if (!line.enabled) return;
            line.useWorldSpace = true; line.positionCount = 2;
            var center = runtime.GuardCenter; var halfHeight = Vector2.up * (runtime.Configuration.Height * .5f);
            line.SetPosition(0, center - halfHeight); line.SetPosition(1, center + halfHeight);
            if (float.IsFinite(definition.LineWidth) && definition.LineWidth > 0) line.startWidth = line.endWidth = definition.LineWidth;
            line.startColor = line.endColor = definition.GuardColor;
        }
    }
}
