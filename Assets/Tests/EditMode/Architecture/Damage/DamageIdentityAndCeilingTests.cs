using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace TicGame.Architecture.Tests
{
    public sealed class DamageIdentityAndCeilingTests
    {
        private readonly List<GameObject> objects = new List<GameObject>();
        [TearDown] public void TearDown() { foreach (var item in objects) UnityEngine.Object.DestroyImmediate(item); objects.Clear(); }
        private GameObject CreateObject(string name) { var item = new GameObject(name); objects.Add(item); return item; }

        [Test]
        public void Resolver_ForwardsOptionalProvenanceAndStringExecutionIdentity()
        {
            var target = CreateObject("Target");
            target.AddComponent<EnemyHealth>().Initialize(10);
            var instance = new DamageInstance("extra", null, null,
                new DamageFormulaValues(1, 1, 0, 0, 0, 0, 1), attackExecutionId: "slash-1",
                provenance: DamageProvenance.Supplemental("primary", "root", "overcharge"));
            var report = DamageResolver.Resolve(new DamageRequest(instance, new[] { target }, Vector2.zero, Vector2.right));
            var context = report.TargetResults[0].Context;
            Assert.IsTrue(context.Provenance.HasValue);
            Assert.AreEqual(DamageOriginKind.Supplemental, context.Provenance.Value.OriginKind);
            Assert.AreEqual("root", context.Provenance.Value.RootInstanceId);
            Assert.AreEqual("slash-1", context.AttackExecutionId);
        }

        [Test]
        public void DirectLegacyContext_HasNoFabricatedPrimaryEvidence()
        {
            var context = new DamageContext(null, null, null, 1, Vector2.zero, Vector2.right);
            Assert.IsFalse(context.Provenance.HasValue);
            Assert.IsNull(context.AttackExecutionId);
        }

        [Test]
        public void HealthCeilingEdits_ClampAbsoluteHealth_WithoutHealingOrReviving()
        {
            var health = CreateObject("Health").AddComponent<EnemyHealth>();
            health.Initialize(10);
            health.ApplyDamage(new DamageContext(null, null, null, 3, Vector2.zero, Vector2.right));
            health.UpdateMaximumHealth(20);
            Assert.AreEqual(7, health.CurrentHealth);
            health.UpdateMaximumHealth(4);
            Assert.AreEqual(4, health.CurrentHealth);
            health.ApplyDamage(new DamageContext(null, null, null, 4, Vector2.zero, Vector2.right));
            health.UpdateMaximumHealth(30);
            Assert.AreEqual(0, health.CurrentHealth);
            Assert.IsTrue(health.IsDefeated);
        }

        [Test]
        public void PoiseCeilingEdits_ClampWithoutRefill_AndDepleteOnceAtZero()
        {
            var poise = CreateObject("Poise").AddComponent<EnemyPoise>();
            poise.Initialize(12, 0);
            poise.ApplyPoiseDamage(2);
            poise.UpdateConfiguration(20, .5f);
            Assert.AreEqual(10, poise.CurrentPoise);
            Assert.AreEqual(.5f, poise.RegenerationPerSecond);
            var depleted = 0;
            poise.Depleted += () => depleted++;
            poise.UpdateConfiguration(0, 0);
            poise.UpdateConfiguration(0, 0);
            Assert.AreEqual(1, depleted);
            poise.UpdateConfiguration(20, 0);
            Assert.AreEqual(0, poise.CurrentPoise);
        }

        [Test]
        public void InvalidCeilingEdits_RejectWithoutChangingCurrentValues()
        {
            var health = CreateObject("Health").AddComponent<EnemyHealth>(); health.Initialize(10);
            var poise = CreateObject("Poise").AddComponent<EnemyPoise>(); poise.Initialize(12, 0);
            Assert.Throws<ArgumentOutOfRangeException>(() => health.UpdateMaximumHealth(float.NaN));
            Assert.Throws<ArgumentOutOfRangeException>(() => poise.UpdateConfiguration(12, float.PositiveInfinity));
            Assert.AreEqual(10, health.MaximumHealth);
            Assert.AreEqual(12, poise.MaximumPoise);
        }

        [TestCase(DamageOriginKind.Supplemental)]
        [TestCase(DamageOriginKind.Converted)]
        public void NonPrimaryPackets_DoNotConsumePoiseCardCharges(DamageOriginKind kind)
        {
            var effects = CreateObject("Player").AddComponent<PlayerCombatEffects>();
            effects.ArmPoiseHits(5, 2, 1.2f);
            var target = CreateObject("Enemy"); target.AddComponent<EnemyActor>();
            var context = new DamageContext(null, target, null, 1, Vector2.zero, Vector2.right,
                poiseDamage: 2.4f, isCardEnhancedMelee: true,
                provenance: new DamageProvenance(kind, "primary", "root", "extra", 1), attackExecutionId: "slash-1");
            effects.OnDamageDealt(context, new DamageResult(true, false, 1, 9));
            Assert.AreEqual(5, effects.RemainingPoiseHits);
        }

        [Test]
        public void RepeatedPrimaryPacketForOneActor_ConsumesOnlyOnePoiseCharge()
        {
            var effects = CreateObject("Player").AddComponent<PlayerCombatEffects>();
            effects.ArmPoiseHits(5, 2, 1.2f);
            var target = CreateObject("Enemy"); target.AddComponent<EnemyActor>();
            var context = new DamageContext(effects.gameObject, target, null, 1, Vector2.zero, Vector2.right,
                poiseDamage: 2.4f, isCardEnhancedMelee: true,
                provenance: DamageProvenance.Primary("primary"), attackExecutionId: "slash-1");
            effects.OnDamageDealt(context, new DamageResult(true, false, 1, 9));
            effects.OnDamageDealt(context, new DamageResult(true, false, 1, 8));
            Assert.AreEqual(4, effects.RemainingPoiseHits);
        }

        [Test]
        public void ReentrantLethalHealthCallback_PublishesDefeatOnlyOnce()
        {
            var health = CreateObject("Health").AddComponent<EnemyHealth>(); health.Initialize(10);
            var deaths = 0;
            var reentered = false;
            health.Defeated += _ => deaths++;
            health.HealthChanged += _ =>
            {
                if (reentered) return;
                reentered = true;
                health.ApplyDamage(new DamageContext(null, health.gameObject, null, 20, Vector2.zero, Vector2.right));
            };
            health.ApplyDamage(new DamageContext(null, health.gameObject, null, 1, Vector2.zero, Vector2.right));
            Assert.AreEqual(1, deaths);
        }
    }
}
