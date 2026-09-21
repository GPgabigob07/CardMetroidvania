using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace TicGame.Architecture.Tests
{
    public sealed class CardMeleeGateTests
    {
        private readonly List<GameObject> objects = new();

        [TearDown]
        public void TearDown()
        {
            foreach (var value in objects) Object.DestroyImmediate(value);
            objects.Clear();
        }

        [Test]
        public void LargeOrdinaryHitDoesNotOpenGate()
        {
            var door = Create("Door");
            var collider = door.AddComponent<BoxCollider2D>();
            var gate = door.AddComponent<CardMeleeGate>();
            gate.Configure(collider, null);
            var report = DamageResolver.Resolve(new DamageRequest(
                Primary(Create("Player").AddComponent<PlayerCombatEffects>(), 999),
                new[] { door }, Vector2.zero, Vector2.left));
            Assert.IsFalse(gate.IsOpen);
            Assert.IsTrue(collider.enabled);
            Assert.AreEqual(0, report.EffectiveHitCount);
        }

        [Test]
        public void KnockbackEnhancedMeleeOpensOnceAndConsumesTheHit()
        {
            var effects = Create("Player").AddComponent<PlayerCombatEffects>();
            effects.AddKnockbackCharges(1, 2);
            var door = Create("Door");
            var collider = door.AddComponent<BoxCollider2D>();
            var gate = door.AddComponent<CardMeleeGate>();
            gate.Configure(collider, null);
            var hit = Primary(effects);
            Assert.IsTrue(hit.IsCardEnhancedMelee);
            var request = new DamageRequest(hit, new[] { door }, Vector2.zero, Vector2.left);
            var first = DamageResolver.Resolve(request);
            Assert.IsTrue(gate.IsOpen);
            Assert.IsFalse(collider.enabled);
            Assert.AreEqual(1, first.EffectiveHitCount);
            Assert.IsTrue(first.TargetResults[0].Context.IsCardEnhancedMelee);
            Assert.AreEqual(0, effects.KnockbackCharges);
            Assert.AreEqual(0, DamageResolver.Resolve(request).EffectiveHitCount);
            Assert.IsFalse(Primary(effects).IsCardEnhancedMelee);
        }

        [Test]
        public void EnergyEffectAndUnmatchedOverchargeDoNotQualify()
        {
            var effects = Create("Player").AddComponent<PlayerCombatEffects>();
            effects.AddEnergyGainCharges(3, 2);
            effects.ArmSupplementalDamage("other-attack", "test", 3);
            Assert.IsFalse(Primary(effects).IsCardEnhancedMelee);
            effects.ArmSupplementalDamage("attack", "test", 3);
            Assert.IsTrue(Primary(effects).IsCardEnhancedMelee);
        }

        [Test]
        public void EnhancedZeroDamageDoesNotOpenGate()
        {
            var door = Create("Door");
            var gate = door.AddComponent<CardMeleeGate>();
            gate.Configure(door.AddComponent<BoxCollider2D>(), null);
            var context = new DamageContext(null, door, null, 0,
                Vector2.zero, Vector2.left, isCardEnhancedMelee: true);
            Assert.IsFalse(gate.ApplyDamage(context).Accepted);
            Assert.IsFalse(gate.IsOpen);
        }

        private GameObject Create(string name)
        {
            var value = new GameObject(name);
            objects.Add(value);
            return value;
        }

        private static DamageInstance Primary(PlayerCombatEffects effects, float damage = 1)
        {
            return effects.BuildPrimaryDamageInstance("hit", "attack", damage, 1, 1, 1);
        }
    }
}
