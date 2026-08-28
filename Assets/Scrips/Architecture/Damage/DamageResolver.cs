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
            var sourceProvider = FindFirst<IDamageProvider>(owner: instance.SourceObject);
            var sourceListeners = FindAll<IDamageListener>(owner: instance.SourceObject);
            var modifiers = CollectModifiers(sourceProvider: sourceProvider);
            var targetResults = new List<DamageTargetResult>();

            var targets = SelectTargets(request: request);
            var targetIndex = 0;

            foreach (var target in targets)
            {
                var damageable = FindFirst<IDamageable>(owner: target);
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

                RunModifiers(modifiers: modifiers, phase: DamageModifierPhase.PreTargetResolve, values: ref values, instance: instance, targetObject: target, targetIndex: targetIndex);

                var formula = new ResolvedDamageFormula(values: values);
                var finalAmount = Mathf.Max(a: 0f, b: formula.RequestedFinalDamage);
                var context = new DamageContext(
                    source: instance.SourceObject,
                    target: target,
                    profile: instance.Profile,
                    amount: finalAmount,
                    hitPoint: request.HitPoint,
                    direction: request.Direction,
                    tags: instance.Tags);

                var result = damageable.ApplyDamage(context: context);
                var targetResult = new DamageTargetResult(
                    context: context,
                    result: result,
                    formula: formula);
                targetResults.Add(item: targetResult);

                foreach (var listener in FindAll<IDamageListener>(owner: target))
                {
                    listener.OnDamageReceived(context: context, result: result);
                }

                if (result.Accepted && result.AppliedAmount > 0f)
                {
                    foreach (var listener in sourceListeners)
                    {
                        listener.OnDamageDealt(context: context, result: result);
                    }
                }

                eventSink?.OnTargetResolved(context: context, result: result);
                targetIndex++;
            }

            var report = new DamageResolutionReport(instance: instance, request: request, targetResults: targetResults);
            sourceProvider?.OnDamageResolved(report: report);

            foreach (var modifier in modifiers)
            {
                modifier.OnDamageResolved(report: report);
            }

            foreach (var listener in sourceListeners)
            {
                listener.OnDamageResolutionComplete(report: report);
            }

            eventSink?.OnRequestResolved(report: report);
            return report;
        }

        private static IReadOnlyList<GameObject> SelectTargets(in DamageRequest request)
        {
            var limit = Mathf.Max(a: 1, b: Mathf.Min(a: request.TargetLimit, b: request.Instance.MaxTargets));
            var candidates = request.CandidateTargets.Where(predicate: target => target != null);

            if (request.TargetPriorityMode == TargetPriorityMode.ClosestToHitPoint)
            {
                var hitPoint = request.HitPoint;
                candidates = candidates.OrderBy(keySelector: target => Vector2.Distance(a: hitPoint, b: target.transform.position));
            }

            return candidates.Take(count: limit).ToArray();
        }

        private static IReadOnlyList<IDamageModifier> CollectModifiers(IDamageProvider sourceProvider)
        {
            if (sourceProvider == null)
            {
                return new List<IDamageModifier>();
            }

            return sourceProvider.GetDamageModifiers()
                .Where(predicate: modifier => modifier != null)
                .OrderBy(keySelector: modifier => modifier.Priority)
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
            var context = new DamageModifierContext(phase: phase, instance: instance, targetObject: targetObject, targetIndex: targetIndex);

            foreach (var modifier in modifiers)
            {
                if (modifier.Phase == phase && modifier.AppliesTo(context: context))
                {
                    modifier.Modify(values: ref values, context: context);
                }
            }
        }

        private static T FindFirst<T>(GameObject owner) where T : class
        {
            return FindAll<T>(owner: owner).FirstOrDefault();
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
