using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace TicGame.Architecture.Tests
{
    public sealed class HitRippleContactTests
    {
        [Test]
        public void MappedOriginAndSecondTarget_UseIndependentContacts_WithoutSplittingReport()
        {
            var first = new GameObject("First");
            var second = new GameObject("Second");
            try
            {
                first.AddComponent<EnemyHealth>().Initialize(10);
                second.AddComponent<EnemyHealth>().Initialize(10);
                var request = new DamageRequest(new DamageInstance("contact", null, null,
                    new DamageFormulaValues(1, 1, 0, 0, 0, 0, 1), maxTargets: 2),
                    new[] { first, second }, new Vector2(99, 99), Vector2.right, targetLimit: 2);
                request.TargetHitPoints = new Dictionary<GameObject, Vector2>
                { [first] = Vector2.zero, [second] = new Vector2(5, 2) };
                var report = DamageResolver.Resolve(request);
                Assert.That(report.TargetResults.Count, Is.EqualTo(2));
                Assert.That(report.TargetResults[0].Context.HitPoint, Is.EqualTo(Vector2.zero));
                Assert.That(report.TargetResults[1].Context.HitPoint, Is.EqualTo(new Vector2(5, 2)));
                Assert.That(report.TargetResults[0].Result.AppliedAmount, Is.EqualTo(1));
                Assert.That(report.TargetResults[1].Result.AppliedAmount, Is.EqualTo(1));
            }
            finally { Object.DestroyImmediate(first); Object.DestroyImmediate(second); }
        }

        [Test]
        public void OldRequest_UsesExistingContactFallback()
        {
            var request = new DamageRequest(default, null, new Vector2(3, 4), Vector2.right);
            Assert.That(request.GetHitPoint(null), Is.EqualTo(new Vector2(3, 4)));
        }
    }
}
