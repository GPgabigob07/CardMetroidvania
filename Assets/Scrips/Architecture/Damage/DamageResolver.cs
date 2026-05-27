using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TicGame.Architecture
{
    public static class DamageResolver
    {
        public static DamageResolutionReport Resolve(in DamageRequest request, IDamageEventSink eventSink = null)
        {
            var instance = request.Instance;
            var sourceProvider = FindFirst<IDamageProvider>(instance.SourceObject);
            var sourceListeners = FindAll<IDamageListener>(instance.SourceObject);
            var modifiers = CollectModifiers(sourceProvider);
            var targetResults = new List<DamageTargetResult>();

            var targets = SelectTargets(request);
            var targetIndex = 0;

            foreach (var target in targets)
            {
                var damageable = FindFirst<IDamageable>(target);
                if (damageable == null)
                {
                    if (!request.AllowPartialResolution)
                    {
                        break;
                    }

                    targetIndex++;
                    continue;
                }

                var values = instance.Formula;
                if (values.Attack <= 0f && sourceProvider != null)
                {
                    values.Attack = sourceProvider.AttackValue;
                }

                RunModifiers(modifiers, DamageModifierPhase.PreTargetResolve, ref values, instance, target, targetIndex);

                var finalAmount = Mathf.Max(0f, values.CalculateFinalDamage());
                var context = new DamageContext(
                    instance.SourceObject,
                    target,
                    instance.Profile,
                    finalAmount,
                    request.HitPoint,
                    request.Direction,
                    instance.Tags);

                var result = damageable.ApplyDamage(context);
                var targetResult = new DamageTargetResult(context, result);
                targetResults.Add(targetResult);

                foreach (var listener in FindAll<IDamageListener>(target))
                {
                    listener.OnDamageReceived(context, result);
                }

                if (result.Accepted && result.AppliedAmount > 0f)
                {
                    foreach (var listener in sourceListeners)
                    {
                        listener.OnDamageDealt(context, result);
                    }
                }

                eventSink?.OnTargetResolved(context, result);
                targetIndex++;
            }

            var report = new DamageResolutionReport(instance, request, targetResults);
            sourceProvider?.OnDamageResolved(report);

            foreach (var modifier in modifiers)
            {
                modifier.OnDamageResolved(report);
            }

            foreach (var listener in sourceListeners)
            {
                listener.OnDamageResolutionComplete(report);
            }

            eventSink?.OnRequestResolved(report);
            return report;
        }

        private static IReadOnlyList<GameObject> SelectTargets(in DamageRequest request)
        {
            var limit = Mathf.Max(1, Mathf.Min(request.TargetLimit, request.Instance.MaxTargets));
            var candidates = request.CandidateTargets.Where(target => target != null);

            if (request.TargetPriorityMode == TargetPriorityMode.ClosestToHitPoint)
            {
                var hitPoint = request.HitPoint;
                candidates = candidates.OrderBy(target => Vector2.Distance(hitPoint, target.transform.position));
            }

            return candidates.Take(limit).ToArray();
        }

        private static IReadOnlyList<IDamageModifier> CollectModifiers(IDamageProvider sourceProvider)
        {
            if (sourceProvider == null)
            {
                return new List<IDamageModifier>();
            }

            return sourceProvider.GetDamageModifiers()
                .Where(modifier => modifier != null)
                .OrderBy(modifier => modifier.Priority)
                .ToArray();
        }

        private static void RunModifiers(
            IEnumerable<IDamageModifier> modifiers,
            DamageModifierPhase phase,
            ref DamageFormulaValues values,
            in DamageInstance instance,
            GameObject targetObject,
            int targetIndex)
        {
            var context = new DamageModifierContext(phase, instance, targetObject, targetIndex);

            foreach (var modifier in modifiers)
            {
                if (modifier.Phase == phase && modifier.AppliesTo(context))
                {
                    modifier.Modify(ref values, context);
                }
            }
        }

        private static T FindFirst<T>(GameObject owner) where T : class
        {
            return FindAll<T>(owner).FirstOrDefault();
        }

        private static IReadOnlyList<T> FindAll<T>(GameObject owner) where T : class
        {
            if (owner == null)
            {
                return new List<T>();
            }

            return owner.GetComponents<MonoBehaviour>().OfType<T>().ToArray();
        }
    }
}
