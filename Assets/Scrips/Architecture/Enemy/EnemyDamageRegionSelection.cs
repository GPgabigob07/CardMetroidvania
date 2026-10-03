using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TicGame.Architecture
{
    public static class EnemyDamageRegionSelection
    {
        public static IReadOnlyList<EnemyDamageRegionTarget> ResolveTargets(IEnumerable<Collider2D> colliders)
        {
            var result = new List<EnemyDamageRegionTarget>();
            var indexByOwner = new Dictionary<MonoBehaviour, int>();
            foreach (var collider in colliders)
            {
                if (collider == null || !collider.enabled || !collider.gameObject.activeInHierarchy) continue;
                var actor = collider.GetComponentInParent<EnemyActor>();
                var region = collider.GetComponentsInParent<MonoBehaviour>(false).OfType<IEnemyDamageRegion>().FirstOrDefault();
                MonoBehaviour recipient;
                var priority = 0;
                if (region != null)
                {
                    if (region.Owner == null || region.Owner != actor || region.DamageRecipient == null) continue;
                    var receivers = region.DamageRecipient.GetComponents<MonoBehaviour>().Where(component => component is IDamageable).ToArray();
                    if (receivers.Length != 1) continue;
                    recipient = receivers[0]; priority = region.Priority;
                }
                else
                {
                    if (actor != null && actor.GetComponentsInChildren<MonoBehaviour>(true)
                        .OfType<IEnemyDamageRegion>().Any(candidate => candidate.Owner == actor)) continue;
                    recipient = collider.GetComponentsInParent<MonoBehaviour>(false).FirstOrDefault(component => component is IDamageable);
                    if (recipient == null) continue;
                    var legacyRegion = collider.GetComponentInParent<EnemyHurtboxRegion>();
                    if (actor != null && legacyRegion == null)
                        recipient = (MonoBehaviour)actor.GetComponent<GolemChargerDamagePolicy>() ?? recipient;
                    priority = legacyRegion != null ? legacyRegion.Region == EnemyHurtboxRegionType.HeadWeakPoint ? 2 : 1 : 0;
                }
                var owner = actor != null ? (MonoBehaviour)actor : recipient;
                var target = new EnemyDamageRegionTarget(owner, recipient, collider, priority);
                if (indexByOwner.TryGetValue(owner, out var index))
                {
                    if (priority > result[index].Priority) result[index] = target;
                }
                else { indexByOwner.Add(owner, result.Count); result.Add(target); }
            }
            return result;
        }
    }
}
