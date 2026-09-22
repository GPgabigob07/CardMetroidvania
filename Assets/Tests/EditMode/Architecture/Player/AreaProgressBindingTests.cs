using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace TicGame.Architecture.Tests
{
    public sealed class AreaProgressBindingTests
    {
        private readonly List<GameObject> objects = new();

        [TearDown]
        public void TearDown()
        {
            foreach (var value in objects) Object.DestroyImmediate(value);
            objects.Clear();
        }

        [Test]
        public void GateRestoresOpenedProgressBeforeAnyHit()
        {
            var progress = new RunProgress(new SpawnAddress("blue", "start"));
            progress.OpenGate("blue", "seal-1");
            var door = Create("Door");
            var barrier = door.AddComponent<BoxCollider2D>();
            var renderer = door.AddComponent<SpriteRenderer>();
            var gate = door.AddComponent<CardMeleeGate>();
            gate.Configure(barrier, renderer, "seal-1");

            gate.BindProgress(progress, "blue");

            Assert.IsTrue(gate.IsOpen);
            Assert.IsFalse(barrier.enabled);
            Assert.IsFalse(renderer.enabled);
        }

        [Test]
        public void GateProgressIsScopedToAreaAndOnlyPublishedByQualifyingDamage()
        {
            var progress = new RunProgress(new SpawnAddress("blue", "start"));
            var door = Create("Door");
            var barrier = door.AddComponent<BoxCollider2D>();
            var gate = door.AddComponent<CardMeleeGate>();
            gate.Configure(barrier, null, "seal-1");
            gate.BindProgress(progress, "blue");

            gate.ApplyDamage(new DamageContext(null, door, null, 10, Vector2.zero, Vector2.right));
            Assert.IsFalse(progress.IsGateOpen("blue", "seal-1"));
            gate.ApplyDamage(new DamageContext(null, door, null, 10, Vector2.zero, Vector2.right,
                isCardEnhancedMelee: true));
            Assert.IsTrue(progress.IsGateOpen("blue", "seal-1"));
            Assert.IsFalse(progress.IsGateOpen("pink", "seal-1"));
        }

        [Test]
        public void GuideDiscoverySurvivesZoneReloadAndTriggerDestruction()
        {
            var progress = new RunProgress(new SpawnAddress("blue", "start"));
            var owner = Create("Guide");
            var guide = owner.AddComponent<CardTimeGuideUI>();
            guide.Bind(progress);
            guide.Discover("actual guide");
            Assert.IsTrue(guide.IsDiscovered);
            Assert.IsTrue(guide.IsVisible);
            guide.Discover("actual guide");
            Assert.IsTrue(guide.IsVisible);

            var trigger = Create("Tutorial Trigger");
            trigger.AddComponent<BoxCollider2D>().isTrigger = true;
            var zone = trigger.AddComponent<CardTimeTutorialZone>();
            zone.BindGuide(guide);
            Object.DestroyImmediate(trigger);

            var reloaded = Create("Guide Reload").AddComponent<CardTimeGuideUI>();
            reloaded.Bind(progress);

            Assert.IsTrue(progress.GuideDiscovered);
            Assert.IsTrue(guide.IsDiscovered);
            Assert.IsFalse(reloaded.IsVisible);
        }

        [Test]
        public void StreamedZoneDoesNotUseSerializedLocalGuideWithoutBinding()
        {
            var trigger = Create("Streamed Tutorial Trigger");
            trigger.AddComponent<BoxCollider2D>().isTrigger = true;
            trigger.AddComponent<CardTimeGuideUI>();
            var zone = trigger.AddComponent<CardTimeTutorialZone>();
            zone.ConfigureForStreamedComposition();

            var resolved = typeof(CardTimeTutorialZone).GetMethod("ResolveGuide", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(zone, null);

            Assert.IsNull(resolved);
        }

        private GameObject Create(string name)
        {
            var value = new GameObject(name);
            objects.Add(value);
            return value;
        }
    }
}
