using System.Collections.Generic;
using NUnit.Framework;
using TicGame.Architecture;
using UnityEngine;

namespace TicGame.Architecture.Tests
{
    public sealed class BatThreatMonitorTests
    {
        private readonly List<Object> objectsToDestroy = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (var instance in objectsToDestroy)
            {
                Object.DestroyImmediate(instance);
            }

            objectsToDestroy.Clear();
        }

        [Test]
        public void Evaluate_TargetInsideMonitorAndClosing_ReportsPositiveClosingSpeed()
        {
            var monitor = CreateMonitor(Vector2.zero);
            var target = CreateTarget(new Vector2(5f, 0f));
            monitor.Configure(monitorRadius: 6f, baseMeleeReach: 3f, outerBandThickness: 0.5f);

            var facts = monitor.Evaluate(target.transform, new Vector2(-4f, 0f), Vector2.zero);

            Assert.IsTrue(facts.IsMonitored);
            Assert.AreEqual(4f, facts.RelativeClosingSpeed);
        }

        [Test]
        public void Evaluate_InsideBaseReachOuterBand_ReportsThreatWithoutCardReach()
        {
            var monitor = CreateMonitor(Vector2.zero);
            var target = CreateTarget(new Vector2(2.75f, 0f));
            monitor.Configure(monitorRadius: 10f, baseMeleeReach: 3f, outerBandThickness: 0.5f);

            var facts = monitor.Evaluate(target.transform, Vector2.zero, Vector2.zero);

            Assert.IsTrue(facts.IsWithinBaseMeleeReachOuterBand);
        }

        [Test]
        public void Evaluate_InsideInnerBaseReach_ReportsOutsideOuterBand()
        {
            var monitor = CreateMonitor(Vector2.zero);
            var target = CreateTarget(new Vector2(1f, 0f));
            monitor.Configure(monitorRadius: 10f, baseMeleeReach: 3f, outerBandThickness: 0.5f);

            var facts = monitor.Evaluate(target.transform, Vector2.zero, Vector2.zero);

            Assert.IsFalse(facts.IsWithinBaseMeleeReachOuterBand);
        }

        private BatThreatMonitor CreateMonitor(Vector2 position)
        {
            var owner = new GameObject("Bat Threat Monitor");
            owner.transform.position = position;
            objectsToDestroy.Add(owner);
            return owner.AddComponent<BatThreatMonitor>();
        }

        private GameObject CreateTarget(Vector2 position)
        {
            var target = new GameObject("Target");
            target.transform.position = position;
            objectsToDestroy.Add(target);
            return target;
        }
    }
}
