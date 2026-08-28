using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TicGame.Architecture;
using UnityEngine;

namespace TicGame.Architecture.Tests
{
    public sealed class GolemChargerTests
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
        public void DamagePolicy_IdleDamageIsHighlyReduced()
        {
            var rig = CreateGolem();

            var result = rig.Policy.ApplyDamage(CreateContext(rig.Root, amount: 10f));

            Assert.IsTrue(result.Accepted);
            Assert.AreEqual(1.5f, result.AppliedAmount);
            Assert.AreEqual(10.5f, rig.Health.CurrentHealth);
        }

        [TestCase(true, false)]
        [TestCase(false, true)]
        public void DamagePolicy_WindupImpactOrCardInterruptsAndAppliesDamage(bool includeImpact, bool includeCard)
        {
            var rig = CreateGolem();
            EnterWindup(rig);

            var result = rig.Policy.ApplyDamage(CreateContext(
                rig.Root,
                amount: 2f,
                tags: CreateTags(
                    includeImpact ? rig.ImpactTag : null,
                    includeCard ? rig.CardTag : null)));

            Assert.IsTrue(result.Accepted);
            Assert.AreEqual(2f, result.AppliedAmount);
            Assert.AreEqual(GolemChargerState.Interrupted, rig.Brain.CurrentState);
        }

        [TestCase(false, false)]
        [TestCase(true, false)]
        [TestCase(false, true)]
        public void DamagePolicy_ChargeRejectsDamageWithoutCardAndImpact(bool includeImpact, bool includeCard)
        {
            var rig = CreateGolem();
            EnterCharge(rig);

            var result = rig.Policy.ApplyDamage(CreateContext(
                rig.Root,
                amount: 2f,
                tags: CreateTags(
                    includeImpact ? rig.ImpactTag : null,
                    includeCard ? rig.CardTag : null)));

            Assert.IsFalse(result.Accepted);
            Assert.AreEqual(12f, rig.Health.CurrentHealth);
            Assert.AreEqual(GolemChargerState.Charge, rig.Brain.CurrentState);
        }

        [Test]
        public void DamagePolicy_ChargeCardAndImpactInterruptsAndAppliesDamage()
        {
            var rig = CreateGolem();
            EnterCharge(rig);

            var result = rig.Policy.ApplyDamage(CreateContext(
                rig.Root,
                amount: 2f,
                tags: CreateTags(rig.ImpactTag, rig.CardTag)));

            Assert.IsTrue(result.Accepted);
            Assert.AreEqual(2f, result.AppliedAmount);
            Assert.AreEqual(10f, rig.Health.CurrentHealth);
            Assert.AreEqual(GolemChargerState.Interrupted, rig.Brain.CurrentState);
            Assert.IsFalse(rig.Attack.IsCharging);
        }

        [Test]
        public void DamagePolicy_InterruptedHeadDamageExceedsBodyDamage()
        {
            var rig = CreateGolem();
            EnterCharge(rig);
            rig.Policy.ApplyDamage(CreateContext(
                rig.Root,
                amount: 1f,
                tags: CreateTags(rig.ImpactTag, rig.CardTag)));

            var bodyResult = rig.Policy.ApplyDamage(CreateContext(rig.Root, amount: 1f));
            var headResult = rig.Policy.ApplyDamage(
                CreateContext(rig.Root, amount: 1f),
                EnemyHurtboxRegionType.HeadWeakPoint);

            Assert.AreEqual(1.5f, bodyResult.AppliedAmount);
            Assert.AreEqual(3f, headResult.AppliedAmount);
        }

        [Test]
        public void HurtboxRegion_ForwardsHeadWeakPointDamageToPolicy()
        {
            var rig = CreateGolem();
            EnterCharge(rig);
            rig.Policy.ApplyDamage(CreateContext(
                rig.Root,
                amount: 1f,
                tags: CreateTags(rig.ImpactTag, rig.CardTag)));
            var head = CreateObject("Head Hurtbox");
            head.transform.SetParent(rig.Root.transform);
            var hurtbox = head.AddComponent<EnemyHurtboxRegion>();
            hurtbox.Configure(rig.Policy, EnemyHurtboxRegionType.HeadWeakPoint);

            var report = DamageResolver.Resolve(new DamageRequest(
                instance: new DamageInstance(
                    instanceId: "head-hit",
                    sourceObject: null,
                    profile: null,
                    formula: new DamageFormulaValues(
                        attack: 0f,
                        strikePercent: 0f,
                        strikeBonusPercent: 0f,
                        attackBuffPercent: 0f,
                        flatDamage: 1f,
                        finalDamagePercent: 0f,
                        critValue: 1f)),
                candidateTargets: new[] { head },
                hitPoint: Vector2.zero,
                direction: Vector2.right));

            Assert.AreEqual(1, report.EffectiveHitCount);
            Assert.AreEqual(3f, report.TotalAppliedAmount);
        }

        [Test]
        public void Brain_ChargeMovesInFixedTickAndReturnsToRecoveryWhenTimerEnds()
        {
            var rig = CreateGolem();
            EnterCharge(rig);

            rig.Brain.FixedTick(0.02f);

            Assert.AreEqual(8f, rig.Body.linearVelocity.x);

            rig.Brain.Tick(0.45f);

            Assert.AreEqual(GolemChargerState.Recovery, rig.Brain.CurrentState);
            Assert.IsFalse(rig.Attack.IsCharging);
            Assert.AreEqual(0f, rig.Body.linearVelocity.x);
        }

        [Test]
        public void Brain_NoTargetTransitionsToPatrolAndMovesAtConfiguredSpeed()
        {
            var rig = CreateGolem();

            rig.Brain.Tick(0f);
            rig.Brain.FixedTick(0.02f);

            Assert.AreEqual(GolemChargerState.Patrol, rig.Brain.CurrentState);
            Assert.AreEqual(-0.75f, rig.Body.linearVelocity.x);
        }

        [Test]
        public void Brain_DefeatStopsChargeAndEntersDeadState()
        {
            var rig = CreateGolem();
            EnterCharge(rig);

            rig.Health.ApplyDamage(CreateContext(rig.Root, amount: 100f));

            Assert.IsTrue(rig.Health.IsDefeated);
            Assert.AreEqual(GolemChargerState.Dead, rig.Brain.CurrentState);
            Assert.IsFalse(rig.Attack.IsCharging);
        }

        private GolemRig CreateGolem()
        {
            var root = CreateObject("Golem");
            var body = root.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            var health = root.AddComponent<EnemyHealth>();
            var actor = root.AddComponent<EnemyActor>();
            actor.SetDefinition(CreateDefinition("golem-charger", maximumHealth: 12f));
            actor.Initialize();
            var attack = root.AddComponent<GolemChargeAttack2D>();
            attack.ConfigureForTests(actor, body);
            attack.ConfigureTuning(speed: 8f, damage: 1f);
            var brain = root.AddComponent<GolemChargerBrain>();
            brain.SetDependenciesForTests(actor, attack);
            brain.ConfigureTiming(range: 4f, windup: 0.75f, charge: 0.45f, interrupted: 1.25f, recovery: 0.35f);
            var impactTag = CreateTag("Damage.Impact");
            var cardTag = CreateTag("Damage.Card");
            var policy = root.AddComponent<GolemChargerDamagePolicy>();
            policy.ConfigureForTests(health, brain, impactTag, cardTag);
            brain.Initialize();

            return new GolemRig(root, body, health, attack, brain, policy, impactTag, cardTag);
        }

        private void EnterWindup(GolemRig rig)
        {
            var target = CreateObject("Target");
            target.transform.position = Vector3.right;
            rig.Brain.SetTargetForTests(target.transform);
            rig.Brain.Tick(0f);

            Assert.AreEqual(GolemChargerState.Windup, rig.Brain.CurrentState);
        }

        private void EnterCharge(GolemRig rig)
        {
            EnterWindup(rig);
            rig.Brain.Tick(0.75f);

            Assert.AreEqual(GolemChargerState.Charge, rig.Brain.CurrentState);
        }

        private GameObject CreateObject(string name)
        {
            var instance = new GameObject(name);
            objectsToDestroy.Add(instance);
            return instance;
        }

        private EnemyDefinitionSO CreateDefinition(string id, float maximumHealth)
        {
            var definition = ScriptableObject.CreateInstance<EnemyDefinitionSO>();
            definition.name = id;
            SetField(definition, "id", id);
            SetField(definition, "maxHealth", maximumHealth);
            objectsToDestroy.Add(definition);
            return definition;
        }

        private GameplayTagSO CreateTag(string id)
        {
            var tag = ScriptableObject.CreateInstance<GameplayTagSO>();
            tag.name = id;
            SetField(tag, "id", id);
            objectsToDestroy.Add(tag);
            return tag;
        }

        private static DamageContext CreateContext(GameObject target, float amount, GameplayTagSet tags = null)
        {
            return new DamageContext(
                source: null,
                target: target,
                profile: null,
                amount: amount,
                hitPoint: Vector2.zero,
                direction: Vector2.right,
                tags: tags);
        }

        private static GameplayTagSet CreateTags(params GameplayTagSO[] tags)
        {
            var set = new GameplayTagSet();
            SetField(set, "tags", new List<GameplayTagSO>(tags ?? new GameplayTagSO[0]));
            return set;
        }

        private static void SetField<T>(object target, string fieldName, T value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(field, $"Expected private field '{fieldName}' on {target.GetType().Name}.");
            field.SetValue(target, value);
        }

        private sealed class GolemRig
        {
            public GolemRig(
                GameObject root,
                Rigidbody2D body,
                EnemyHealth health,
                GolemChargeAttack2D attack,
                GolemChargerBrain brain,
                GolemChargerDamagePolicy policy,
                GameplayTagSO impactTag,
                GameplayTagSO cardTag)
            {
                Root = root;
                Body = body;
                Health = health;
                Attack = attack;
                Brain = brain;
                Policy = policy;
                ImpactTag = impactTag;
                CardTag = cardTag;
            }

            public GameObject Root { get; }
            public Rigidbody2D Body { get; }
            public EnemyHealth Health { get; }
            public GolemChargeAttack2D Attack { get; }
            public GolemChargerBrain Brain { get; }
            public GolemChargerDamagePolicy Policy { get; }
            public GameplayTagSO ImpactTag { get; }
            public GameplayTagSO CardTag { get; }
        }
    }
}
