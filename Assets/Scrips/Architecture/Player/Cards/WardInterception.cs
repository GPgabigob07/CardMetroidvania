using UnityEngine;

namespace TicGame.Architecture
{
    public readonly struct WardInterception
    {
        public WardInterception(WardInterceptionKind kind, Vector2 clipPosition) { Kind = kind; ClipPosition = clipPosition; }
        public WardInterceptionKind Kind { get; }
        public Vector2 ClipPosition { get; }
    }
}
