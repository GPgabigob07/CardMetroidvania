using UnityEngine;

namespace TicGame.Architecture
{
    [CreateAssetMenu(menuName = "TIC/Player/Dash Definition", fileName = "PlayerDash")]
    public sealed class PlayerDashDefinitionSO : ScriptableObject
    {
        [Header(header: "Timing")]
        [Min(min: 0.01f)]
        [Tooltip(tooltip: "How long the dash action remains active.")]
        [SerializeField] private float duration = 0.16f;

        [Header(header: "Movement")]
        [Min(min: 0f)]
        [Tooltip(tooltip: "Horizontal velocity used while dashing.")]
        [SerializeField] private float speed = 18f;

        public float Duration => duration;
        public float Speed => speed;
    }
}
