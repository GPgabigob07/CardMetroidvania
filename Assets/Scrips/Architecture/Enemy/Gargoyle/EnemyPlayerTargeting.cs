using UnityEngine;

namespace TicGame.Architecture
{
    public static class EnemyPlayerTargeting
    {
        public static GameObject CanonicalTarget(Collider2D collider)
        {
            if (collider == null || !collider.enabled || !collider.gameObject.activeInHierarchy
                || collider.GetComponentInParent<EnemyActor>() != null) return null;
            var health = collider.GetComponentInParent<SimpleHealth>();
            return health != null && IsAvailable(health.gameObject) ? health.gameObject : null;
        }

        public static bool IsAvailable(GameObject target)
        {
            if (target == null || !target.activeInHierarchy) return false;
            var health = target.GetComponent<SimpleHealth>();
            return health != null && !health.IsDead && target.GetComponentInParent<PlayerWorldHold>()?.IsHeld != true;
        }
    }
}
