using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace TicGame.Architecture.Tests
{
    public sealed class GargoyleDamagePolicyTests
    {
        private readonly List<Object> objects = new List<Object>();
        private EnemyActor actor;
        private EnemyPoise poise;
        private GargoyleTuningSO tuning;
        private GargoyleDamagePolicy policy;
        private GameObject source;

        [SetUp]
        public void SetUp()
        {
            var root = MakeObject("Gargoyle");
            actor = root.AddComponent<EnemyActor>();
            var definition = Track(ScriptableObject.CreateInstance<EnemyDefinitionSO>());
            JsonUtility.FromJsonOverwrite("{\"maxHealth\":45}", definition);
            actor.SetDefinition(definition); actor.Initialize();
            poise = root.AddComponent<EnemyPoise>(); poise.Initialize(12, 0);
            tuning = Track(ScriptableObject.CreateInstance<GargoyleTuningSO>());
            var presentation = Track(ScriptableObject.CreateInstance<GargoylePresentationSO>());
            typeof(GargoyleTuningSO).GetField("presentation", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(tuning, presentation);
            policy = root.AddComponent<GargoyleDamagePolicy>(); policy.Initialize(actor, poise, tuning);
            source = MakeObject("Player");
        }

        [TearDown]
        public void TearDown() { for (var index = objects.Count - 1; index >= 0; index--) Object.DestroyImmediate(objects[index]); objects.Clear(); }
        private T Track<T>(T value) where T : Object { objects.Add(value); return value; }
        private GameObject MakeObject(string name) => Track(new GameObject(name));
        private DamageContext Hit(string execution = "slash-1", float amount = 2, DamageOriginKind origin = DamageOriginKind.Primary, bool enhanced = true)
            => new DamageContext(source, actor.gameObject, null, amount, Vector2.zero, Vector2.right,
                poiseDamage: 2.4f, isCardEnhancedMelee: enhanced,
                provenance: new DamageProvenance(origin, null, "root", null, 0), attackExecutionId: execution);

        [Test]
        public void BodyAndExposedHead_UseCurrentAssetMultipliers()
        {
            Assert.AreEqual(2, policy.ApplyDamage(GargoyleRegionKind.Body, Hit()).AppliedAmount);
            policy.SetResponseState(false, true, false);
            Assert.AreEqual(3, policy.ApplyDamage(GargoyleRegionKind.Head, Hit("slash-2")).AppliedAmount);
            JsonUtility.FromJsonOverwrite("{\"simulation\":{\"bodyDamageMultiplier\":2}}", tuning);
            Assert.AreEqual(4, policy.ApplyDamage(GargoyleRegionKind.Body, Hit("slash-3")).AppliedAmount);
        }

        [Test]
        public void OverlappingRegionPackets_ApplyHealthPoiseAndCoreEvidenceOnce()
        {
            policy.SetResponseState(true, false, false);
            var coreHits = 0; policy.AcceptedCoreHit += _ => coreHits++;
            Assert.IsTrue(policy.ApplyDamage(GargoyleRegionKind.Core, Hit()).Accepted);
            Assert.IsFalse(policy.ApplyDamage(GargoyleRegionKind.Body, Hit()).Accepted);
            Assert.AreEqual(43, actor.Health.CurrentHealth);
            Assert.AreEqual(9.6f, poise.CurrentPoise, .001f);
            Assert.AreEqual(1, coreHits);
        }

        [TestCase(DamageOriginKind.Supplemental)]
        [TestCase(DamageOriginKind.Converted)]
        public void NonPrimaryCoreHits_AcceptHealth_ButDoNotQualifyForReactor(DamageOriginKind origin)
        {
            policy.SetResponseState(true, false, false);
            var count = 0; policy.AcceptedCoreHit += _ => count++;
            Assert.IsTrue(policy.ApplyDamage(GargoyleRegionKind.Core, Hit(origin: origin)).Accepted);
            Assert.AreEqual(0, count);
        }

        [Test]
        public void OrdinaryCoreHit_AndMissingExecution_AcceptHealthWithoutReactorEvidence()
        {
            policy.SetResponseState(true, false, false);
            var count = 0; policy.AcceptedCoreHit += _ => count++;
            Assert.IsTrue(policy.ApplyDamage(GargoyleRegionKind.Core, Hit(enhanced: false)).Accepted);
            Assert.IsTrue(policy.ApplyDamage(GargoyleRegionKind.Core, Hit(execution: null)).Accepted);
            Assert.AreEqual(0, count);
        }

        [Test]
        public void PoiseResistance_AcceptsHealthAndCoreCounter_WithoutDrainingGlobalPoise()
        {
            policy.SetResponseState(true, false, true);
            var count = 0; policy.AcceptedCoreHit += _ => count++;
            Assert.IsTrue(policy.ApplyDamage(GargoyleRegionKind.Core, Hit()).Accepted);
            Assert.AreEqual(12, poise.CurrentPoise);
            Assert.AreEqual(1, count);
        }

        [Test]
        public void LethalCoreHit_ChoosesDeath_BeforePoiseOrReactorEvents()
        {
            policy.SetResponseState(true, false, false);
            var events = 0; policy.AcceptedCoreHit += _ => events++; poise.Depleted += () => events++;
            var result = policy.ApplyDamage(GargoyleRegionKind.Core, Hit(amount: 45));
            Assert.IsTrue(result.Killed);
            Assert.AreEqual(0, events);
        }

        [Test]
        public void ReentrantDefeat_DoesNotContinueToPoiseOrCore_AndReturnsCurrentDeathState()
        {
            policy.SetResponseState(true, false, false);
            var coreHits = 0; policy.AcceptedCoreHit += _ => coreHits++;
            var nested = false;
            actor.Health.HealthChanged += _ =>
            {
                if (nested) return;
                nested = true;
                actor.Health.ApplyDamage(new DamageContext(source, actor.gameObject, null, 100, Vector2.zero, Vector2.right));
            };
            var result = policy.ApplyDamage(GargoyleRegionKind.Core, Hit());
            Assert.IsTrue(result.Killed);
            Assert.AreEqual(12, poise.CurrentPoise);
            Assert.AreEqual(0, coreHits);
        }

        [Test]
        public void ZeroMultiplier_RejectsWithoutFallingBackToProfileDamage()
        {
            JsonUtility.FromJsonOverwrite("{\"simulation\":{\"bodyDamageMultiplier\":0}}", tuning);
            var damageProfile = Track(ScriptableObject.CreateInstance<DamageProfileSO>());
            JsonUtility.FromJsonOverwrite("{\"baseDamage\":5}", damageProfile);
            var context = new DamageContext(source, actor.gameObject, damageProfile, 2, Vector2.zero, Vector2.right);
            Assert.IsFalse(policy.ApplyDamage(GargoyleRegionKind.Body, context).Accepted);
            Assert.AreEqual(45, actor.Health.CurrentHealth);
        }

        [Test]
        public void RegionSelection_ChoosesOpenCoreOnce_AndExcludesPhysicalRootBypass()
        {
            policy.SetResponseState(true, true, false);
            var rootCollider = actor.gameObject.AddComponent<BoxCollider2D>();
            var body = CreateRegion(GargoyleRegionKind.Body);
            var core = CreateRegion(GargoyleRegionKind.Core);
            var head = CreateRegion(GargoyleRegionKind.Head);
            var selected = EnemyDamageRegionSelection.ResolveTargets(new[] { rootCollider, body, head, core });
            Assert.AreEqual(1, selected.Count);
            Assert.AreSame(core.gameObject, selected[0].Recipient.gameObject);
            Assert.AreSame(actor, selected[0].Owner);
            var report = DamageResolver.Resolve(new DamageRequest(new DamageInstance("primary", source, null,
                new DamageFormulaValues(2, 1, 0, 0, 0, 0, 1), attackExecutionId: "slash-1", poiseDamage: 2.4f, isCardEnhancedMelee: true),
                new[] { selected[0].Recipient.gameObject }, Vector2.zero, Vector2.right));
            Assert.AreEqual(2, report.TotalAppliedAmount);
            Assert.AreEqual(9.6f, poise.CurrentPoise, .001f);
        }

        private BoxCollider2D CreateRegion(GargoyleRegionKind kind)
        {
            var child = MakeObject(kind.ToString()); child.transform.SetParent(actor.transform, false);
            var hurtbox = child.AddComponent<GargoyleHurtbox>(); hurtbox.Configure(policy, kind);
            return child.GetComponent<BoxCollider2D>();
        }

        [Test]
        public void Resolver_RejectsPhysicalRoot_WhenExplicitRegionsExist()
        {
            CreateRegion(GargoyleRegionKind.Body);
            var report = DamageResolver.Resolve(new DamageRequest(new DamageInstance("root-bypass", source, null,
                new DamageFormulaValues(2, 1, 0, 0, 0, 0, 1)), new[] { actor.gameObject }, Vector2.zero, Vector2.right));
            Assert.AreEqual(0, report.EffectiveHitCount);
            Assert.AreEqual(45, actor.Health.CurrentHealth);
        }
    }
}
