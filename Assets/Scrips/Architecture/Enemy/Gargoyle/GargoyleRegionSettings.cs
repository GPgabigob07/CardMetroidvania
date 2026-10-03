using System;
using UnityEngine;

namespace TicGame.Architecture
{
    [Serializable]
    public struct GargoyleRegionSettings
    {
        [SerializeField] private Vector2 size;
        [SerializeField] private Vector2 offset;
        [SerializeField, Min(0), Tooltip("Priority when overlapping other regions on the same actor.")]
        private int priority;

        public GargoyleRegionSettings(Vector2 size, Vector2 offset, int priority)
        { this.size = size; this.offset = offset; this.priority = priority; }

        public Vector2 Size => size;
        public Vector2 Offset => offset;
        public int Priority => priority;
        public bool IsValid => float.IsFinite(size.x) && float.IsFinite(size.y) && size.x > 0 && size.y > 0
            && float.IsFinite(offset.x) && float.IsFinite(offset.y) && priority >= 0;
    }
}
