using UnityEngine;

namespace TicGame.Architecture
{
    public readonly struct EnemyDamageRegionTarget
    {
        public EnemyDamageRegionTarget(MonoBehaviour owner, MonoBehaviour recipient, Collider2D collider, int priority)
        { Owner = owner; Recipient = recipient; Collider = collider; Priority = priority; }
        public MonoBehaviour Owner { get; }
        public MonoBehaviour Recipient { get; }
        public Collider2D Collider { get; }
        public int Priority { get; }
    }
}
