using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TicGame.Architecture;
using UnityEngine;

namespace TicGame.Architecture.Tests
{
    public sealed class DamageResolverTests
    {
        private readonly List<GameObject> objectsToDestroy = new List<GameObject>();

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
        public void Resolve_AppliesDamageToMultipleTargets_AndNotifiesSource()
        {
            var source = CreateObject("Source");
            var provider = source.AddComponent<TestDamageProvider>();
            provider.AttackValue = 10f;

            var firstTarget = CreateObject("Target A");
            var firstHealth = CreateInitializedHealth(firstTarget);
            var secondTarget = CreateObject("Target B");
            var secondHealth = CreateInitializedHealth(secondTarget);

            var formula = new DamageFormulaValues(
                attack: 0f,
                strikePercent: 0.5f,
                strikeBonusPercent: 0f,
                attackBuffPercent: 0f,
                flatDamage: 0f,
                finalDamagePercent: 0f,
                critValue: 1f);

            var instance = new DamageInstance(
                "test-instance",
                source,
                null,
                formula,
                maxTargets: 2);

            var request = new DamageRequest(
                instance,
                new[] { firstTarget, secondTarget },
                Vector2.zero,
                Vector2.right,
                targetLimit: 2);

            var report = DamageResolver.Resolve(request);

            Assert.AreEqual(2, report.EffectiveHitCount);
            Assert.AreEqual(10f, report.TotalAppliedAmount);
            Assert.AreEqual(0f, firstHealth.CurrentHealth);
            Assert.AreEqual(0f, secondHealth.CurrentHealth);
            Assert.AreEqual(2, provider.DamageDealtNotifications);
            Assert.AreEqual(1, provider.DamageResolutionNotifications);
            Assert.AreSame(report, provider.LastReport);
            Assert.AreEqual(0.1f, report.RequestedHitStopSeconds);
            Assert.IsTrue(report.IsPrimary);
            Assert.IsTrue(report.Allows(DamageProcPolicy.ConfirmAttackHit));
            Assert.AreEqual(5f, report.TargetResults[0].Formula.RequestedFinalDamage);
            Assert.AreEqual(5f, report.TargetResults[0].Formula.EligibleBaseDamage);
        }

        [Test]
        public void Resolve_UsesProviderModifier_BeforeApplyingDamage()
        {
            var source = CreateObject("Source");
            var provider = source.AddComponent<TestDamageProvider>();
            provider.AttackValue = 10f;
            provider.AddModifier(new FlatFinalDamageModifier(1f));

            var target = CreateObject("Target");
            var health = CreateInitializedHealth(target);

            var formula = new DamageFormulaValues(
                attack: 0f,
                strikePercent: 0.25f,
                strikeBonusPercent: 0f,
                attackBuffPercent: 0f,
                flatDamage: 0f,
                finalDamagePercent: 0f,
                critValue: 1f);

            var instance = new DamageInstance("buffed-instance", source, null, formula);
            var request = new DamageRequest(instance, new[] { target }, Vector2.zero, Vector2.right);

            var report = DamageResolver.Resolve(request);

            Assert.AreEqual(1, report.EffectiveHitCount);
            Assert.AreEqual(5f, report.TotalAppliedAmount);
            Assert.AreEqual(0f, health.CurrentHealth);
            Assert.AreEqual(1, provider.DamageDealtNotifications);
        }

        [Test]
        public void DamageResult_DefaultsToPointOneSecondHitStop()
        {
            var result = new DamageResult(
                accepted: true,
                killed: false,
                appliedAmount: 1f,
                remainingHealth: 4f);

            Assert.AreEqual(0.1f, result.HitStopSeconds);
        }

        [Test]
        public void Resolve_PreservesRequestedFormula_WhenEnemyHealthClampsAppliedDamage()
        {
            var source = CreateObject("Source");
            var target = CreateObject("Target");
            var health = target.AddComponent<EnemyHealth>();
            health.Initialize(maximumHealth: 3f);
            var instance = new DamageInstance(
                "clamped-instance",
                source,
                null,
                new DamageFormulaValues(
                    attack: 10f,
                    strikePercent: 1f,
                    strikeBonusPercent: 0f,
                    attackBuffPercent: 0f,
                    flatDamage: 0f,
                    finalDamagePercent: 0f,
                    critValue: 1f));
            var report = DamageResolver.Resolve(new DamageRequest(
                instance,
                new[] { target },
                Vector2.zero,
                Vector2.right));

            Assert.AreEqual(10f, report.TargetResults[0].Formula.RequestedFinalDamage);
            Assert.AreEqual(10f, report.TargetResults[0].Formula.EligibleBaseDamage);
            Assert.AreEqual(3f, report.TargetResults[0].Result.AppliedAmount);
        }

        [Test]
        public void Resolve_UsesDamageProfileTagsWhenInstanceTagsAreNotProvided()
        {
            var source = CreateObject("Source");
            var target = CreateObject("Target");
            var health = CreateInitializedHealth(target);
            var tag = ScriptableObject.CreateInstance<GameplayTagSO>();
            tag.name = "Damage.Impact";
            var tags = new GameplayTagSet();
            SetField(tags, "tags", new List<GameplayTagSO> { tag });
            var profile = ScriptableObject.CreateInstance<DamageProfileSO>();
            SetField(profile, "damageTags", tags);

            var report = DamageResolver.Resolve(new DamageRequest(
                instance: new DamageInstance(
                    instanceId: "profile-tags",
                    sourceObject: source,
                    profile: profile,
                    formula: new DamageFormulaValues(
                        attack: 1f,
                        strikePercent: 1f,
                        strikeBonusPercent: 0f,
                        attackBuffPercent: 0f,
                        flatDamage: 0f,
                        finalDamagePercent: 0f,
                        critValue: 1f)),
                candidateTargets: new[] { target },
                hitPoint: Vector2.zero,
                direction: Vector2.right));

            Assert.IsTrue(report.TargetResults[0].Context.Tags.Contains(tag));
            Object.DestroyImmediate(profile);
            Object.DestroyImmediate(tag);
            Assert.AreEqual(0f, health.CurrentHealth);
        }

        private GameObject CreateObject(string name)
        {
            var instance = new GameObject(name);
            objectsToDestroy.Add(instance);
            return instance;
        }

        private static SimpleHealth CreateInitializedHealth(GameObject owner)
        {
            var health = owner.AddComponent<SimpleHealth>();
            health.Initialize();
            return health;
        }

        private static void SetField<T>(object target, string fieldName, T value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(field, $"Expected private field '{fieldName}' on {target.GetType().Name}.");
            field.SetValue(target, value);
        }
    }
}
