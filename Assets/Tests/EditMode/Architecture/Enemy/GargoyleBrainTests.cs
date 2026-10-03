using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TicGame.Architecture.Tests
{
    public sealed class GargoyleBrainTests
    {
        private readonly List<Object> objects = new();
        private EnemyActor actor;
        private EnemyPoise poise;
        private GargoyleTuningSO tuning;
        private GargoyleBrain brain;
        private GameObject target;
        private FakeMotor motor;
        private EnemyAttackDefinitionSO basic;
        private GargoyleDamagePolicy policy;
        [SetUp]
        public void SetUp()
        {
            var root = Track(new GameObject("Gargoyle"));
            actor = root.AddComponent<EnemyActor>();
            var identity = Track(ScriptableObject.CreateInstance<EnemyDefinitionSO>());
            JsonUtility.FromJsonOverwrite("{\"maxHealth\":45}", identity); actor.SetDefinition(identity); actor.Initialize();
            poise = root.AddComponent<EnemyPoise>(); poise.Initialize(12, 0);
            tuning = Track(ScriptableObject.CreateInstance<GargoyleTuningSO>());
            JsonUtility.FromJsonOverwrite("{\"simulation\":{\"feintProbability\":0}}", tuning);
            Set(tuning, "environmentLayer", (LayerMask)1);
            var profile = Track(ScriptableObject.CreateInstance<DamageProfileSO>());
            basic = Attack(profile, "sweep", "backhand", "slam");
            Set(tuning, "basicAttack", basic);
            foreach (var name in new[] { "wingbreakerAttack", "volleyAttack", "beamAttack", "novaAttack" }) Set(tuning, name, Attack(profile, name));
            Set(tuning, "presentation", Track(ScriptableObject.CreateInstance<GargoylePresentationSO>()));
            policy = root.AddComponent<GargoyleDamagePolicy>();
            brain = root.AddComponent<GargoyleBrain>();
            motor = new FakeMotor();
            Assert.That(tuning.GetValidationErrors(), Is.Empty, string.Join("; ", tuning.GetValidationErrors()));
            brain.Initialize(actor, poise, tuning, motor);
            target = Track(new GameObject("Player")); target.transform.position = new Vector2(1, 1);
            target.AddComponent<SimpleHealth>().Initialize(); brain.SetTarget(target);
        }
        [TearDown]
        public void TearDown() { Time.timeScale = 1; for (var i = objects.Count - 1; i >= 0; i--) Object.DestroyImmediate(objects[i]); objects.Clear(); }
        private T Track<T>(T value) where T : Object { objects.Add(value); return value; }
        private static void Set(object owner, string name, object value) => owner.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(owner, value);
        private EnemyAttackDefinitionSO Attack(DamageProfileSO profile, params string[] ids)
        {
            var asset = Track(ScriptableObject.CreateInstance<EnemyAttackDefinitionSO>());
            var steps = new List<EnemyAttackStep>(); foreach (var id in ids) steps.Add(new EnemyAttackStep(id, .35f, .12f, .18f, profile));
            asset.SetSteps(steps.ToArray()); return asset;
        }
        private void StartBasic() { brain.Tick(0); brain.TickPhysics(.02f); brain.Tick(0); Assert.AreEqual(GargoyleState.Attack, brain.CurrentState); }
        private void CompleteAttack()
        {
            var token = brain.CurrentAttack.ExecutionToken;
            for (var i = 0; i < 12 && brain.CurrentState == GargoyleState.Attack && brain.CurrentAttack.ExecutionToken == token; i++)
            { brain.Tick(10); brain.TickPhysics(.02f); }
        }

        [Test]
        public void BasicCompletesThreeSeparateStrikes_ThenConsumesOnlyTheQueuedFamily()
        {
            StartBasic(); Assert.AreSame(basic, brain.CurrentAttack.Definition); Assert.AreEqual(3, brain.RemainingFamilies);
            var queued = brain.QueuedFamily;
            brain.Tick(10); Assert.AreEqual("sweep", brain.CurrentAttack.StepId); brain.TickPhysics(.02f);
            brain.Tick(0); Assert.AreEqual("backhand", brain.CurrentAttack.StepId); brain.TickPhysics(.02f);
            brain.Tick(0); Assert.AreEqual("slam", brain.CurrentAttack.StepId); brain.TickPhysics(.02f);
            brain.Tick(0); Assert.AreEqual(GargoyleState.FamilyRecovery, brain.CurrentState);
            brain.Tick(.25f); brain.TickPhysics(.02f); brain.Tick(0);
            Assert.AreSame(tuning.GetAttackDefinition(queued), brain.CurrentAttack.Definition);
            Assert.AreEqual(2, brain.RemainingFamilies);
        }

        [Test]
        public void StunIsSingleResponse_RestoresAtExit_AndResistanceDoesNotBlockHealth()
        {
            StartBasic(); poise.ApplyPoiseDamage(12);
            Assert.IsFalse(brain.CanEmit); brain.Tick(0); Assert.AreEqual(GargoyleState.Stunned, brain.CurrentState);
            brain.Tick(1); brain.RequestStun(); brain.Tick(.25f);
            Assert.AreEqual(12, poise.CurrentPoise); Assert.IsTrue(brain.IsPoiseResistant);
            actor.Health.ApplyDamage(new DamageContext(target, actor.gameObject, null, 1, Vector2.zero, Vector2.right));
            Assert.AreEqual(44, actor.Health.CurrentHealth);
            brain.Tick(.75f); Assert.IsFalse(brain.IsPoiseResistant);
        }

        [Test]
        public void SimultaneousStunAndLethalDamage_ChooseDeathWithoutRestoration()
        {
            StartBasic(); brain.RequestBeamStagger(brain.CurrentAttack.ExecutionToken); brain.RequestStun();
            actor.Health.ApplyDamage(new DamageContext(target, actor.gameObject, null, 45, Vector2.zero, Vector2.right));
            Assert.IsFalse(brain.CanEmit); brain.Tick(10); Assert.AreEqual(GargoyleState.Dead, brain.CurrentState);
            Assert.IsFalse(brain.CurrentAttack.IsRunning);
        }

        [Test]
        public void StunBeatsBeamStagger_AndOldCastCannotInterruptNewAttack()
        {
            StartBasic(); var old = brain.CurrentAttack.ExecutionToken;
            brain.RequestBeamStagger(old); brain.RequestStun(); brain.Tick(0);
            Assert.AreEqual(GargoyleState.Stunned, brain.CurrentState);
            brain.Tick(1.25f); brain.Tick(0); brain.TickPhysics(.02f); brain.Tick(0);
            brain.RequestBeamStagger(old); brain.Tick(0); Assert.AreEqual(GargoyleState.Attack, brain.CurrentState);
        }

        [Test]
        public void InterruptedFamilyRemainsConsumed_AndResumeBeginsBasicWithRemainingBag()
        {
            StartBasic(); CompleteAttack(); brain.Tick(.25f); brain.TickPhysics(.02f); brain.Tick(0);
            Assert.AreEqual(2, brain.RemainingFamilies);
            brain.RequestStun(); brain.Tick(0); brain.Tick(1.25f); brain.Tick(0); brain.TickPhysics(.02f); brain.Tick(0);
            Assert.AreSame(basic, brain.CurrentAttack.Definition); Assert.AreEqual(2, brain.RemainingFamilies);
        }

        [Test]
        public void BlockedRepositionTimesOut_WithoutRerollingOrConsumingSelection()
        {
            StartBasic(); CompleteAttack(); brain.Tick(.25f);
            target.transform.position = new Vector2(10, 1); motor.Result = EnemyPatrolMoveResult.Blocked;
            var queued = brain.QueuedFamily; var remaining = brain.RemainingFamilies;
            brain.TickPhysics(.02f); brain.Tick(1.5f);
            Assert.AreEqual(GargoyleState.PassRecovery, brain.CurrentState);
            Assert.AreEqual(queued, brain.QueuedFamily); Assert.AreEqual(remaining, brain.RemainingFamilies);
            brain.Tick(.8f); Assert.AreEqual(GargoyleState.Reposition, brain.CurrentState);
        }

        [Test]
        public void HeldLostAndPausedTargetsSuppressPhysicsEvenAtZeroDelta()
        {
            StartBasic(); brain.Tick(.35f);
            using (target.AddComponent<PlayerWorldHold>().Acquire())
            { brain.TickPhysics(0); brain.Tick(0); Assert.IsFalse(brain.CurrentAttack.IsRunning); Assert.IsFalse(brain.CanEmit); }
            brain.SetTarget(null); brain.Tick(0); Assert.AreEqual(GargoyleState.Idle, brain.CurrentState);
            brain.SetTarget(target); StartBasic(); brain.Tick(.35f);
            Time.timeScale = 0; brain.TickPhysics(.02f); brain.Tick(1);
            Assert.AreEqual(0, brain.CurrentAttack.Elapsed); Assert.IsFalse(brain.CanEmit);
        }

        [Test]
        public void LiveCeilingsClampWithoutHealing_AndShorterStunUsesExistingElapsed()
        {
            StartBasic(); actor.Health.ApplyDamage(new DamageContext(target, actor.gameObject, null, 5, Vector2.zero, Vector2.right));
            poise.ApplyPoiseDamage(2); JsonUtility.FromJsonOverwrite("{\"maxHealth\":60}", actor.Definition);
            JsonUtility.FromJsonOverwrite("{\"simulation\":{\"maximumPoise\":20}}", tuning); brain.Tick(0);
            Assert.AreEqual(40, actor.Health.CurrentHealth); Assert.AreEqual(10, poise.CurrentPoise);
            brain.RequestStun(); brain.Tick(0); brain.Tick(.6f);
            JsonUtility.FromJsonOverwrite("{\"simulation\":{\"stunDuration\":0.5}}", tuning); brain.Tick(0);
            Assert.AreEqual(20, poise.CurrentPoise); Assert.IsTrue(brain.IsPoiseResistant);
            Assert.AreNotEqual(GargoyleState.Stunned, brain.CurrentState);
        }

        [Test]
        public void ReadingDebugSelection_CannotRefillAnExhaustedPass()
        {
            StartBasic(); CompleteAttack();
            for (var i = 0; i < 3; i++) { brain.Tick(.25f); brain.TickPhysics(.02f); brain.Tick(0); CompleteAttack(); }
            Assert.AreEqual(GargoyleState.PassRecovery, brain.CurrentState);
            Assert.AreEqual(0, brain.RemainingFamilies);
            _ = brain.QueuedFamily;
            Assert.AreEqual(0, brain.RemainingFamilies);
        }

        [Test]
        public void DisabledBrainCannotAdvanceOrMove_WhenCalledDirectly()
        {
            StartBasic(); brain.enabled = false; brain.Tick(10); brain.TickPhysics(.02f);
            Assert.AreEqual(GargoyleState.Idle, brain.CurrentState);
            Assert.IsFalse(brain.CurrentAttack.IsRunning); Assert.IsFalse(brain.CanEmit);
        }

        [Test]
        public void VerticalTargetOutsideEngageRange_CannotCommitAnAttack()
        {
            target.transform.position = new Vector2(1, 14); brain.Tick(0); brain.TickPhysics(.02f); brain.Tick(0);
            Assert.IsFalse(brain.CurrentAttack.IsRunning); Assert.AreEqual(3, brain.RemainingFamilies);
        }

        [Test]
        public void BeamStaggerExposesHead_AndResistanceSuppressesPoiseWhileAcceptingHealth()
        {
            StartBasic(); brain.RequestBeamStagger(brain.CurrentAttack.ExecutionToken); brain.Tick(0);
            Assert.AreEqual(GargoyleState.Staggered, brain.CurrentState); Assert.IsTrue(policy.HeadExposed);
            var hit = new DamageContext(target, actor.gameObject, null, 2, Vector2.zero, Vector2.right, poiseDamage: 3);
            Assert.AreEqual(3, policy.ApplyDamage(GargoyleRegionKind.Head, hit).AppliedAmount);
            Assert.AreEqual(12, poise.CurrentPoise);
            brain.Tick(.65f); Assert.IsFalse(policy.HeadExposed);
            Assert.AreEqual(2, policy.ApplyDamage(GargoyleRegionKind.Body, hit).AppliedAmount);
            Assert.AreEqual(12, poise.CurrentPoise);
            brain.Tick(.75f); policy.ApplyDamage(GargoyleRegionKind.Body, hit);
            Assert.AreEqual(9, poise.CurrentPoise);
        }

        [Test]
        public void VolleyIsReleasedOnce_NormalCompletionKeepsShots_AndTargetLossCleansThem()
        {
            JsonUtility.FromJsonOverwrite("{\"families\":[1,0]}", tuning);
            var volley = tuning.GetAttackDefinition(GargoyleAttackFamily.Volley);
            JsonUtility.FromJsonOverwrite("{\"payload\":{\"kind\":1}}", volley.Steps[0]);
            volley.SetPatterns(new[] { new EnemyProjectilePattern(1, .45f, new[] { 0f }),
                new EnemyProjectilePattern(3, .55f, new[] { -12f, 0, 12f }),
                new EnemyProjectilePattern(5, .65f, new[] { -30f, -15f, 0, 15f, 30f }) });
            var template = Track(new GameObject("ProjectileTemplate")); template.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
            template.AddComponent<CircleCollider2D>().isTrigger = true;
            Set(volley, "projectilePrefab", template.AddComponent<EnemyProjectile2D>()); template.SetActive(false);
            var launcher = actor.gameObject.AddComponent<EnemyProjectilePatternLauncher>(); launcher.Initialize(actor, tuning, brain); Set(brain, "volley", launcher);
            var shots = new List<EnemyProjectile2D>(); launcher.ProjectileLaunched += shot => { shots.Add(shot); Track(shot.gameObject); };
            brain.ResetEncounter(); StartBasic(); CompleteAttack(); brain.Tick(.25f); brain.TickPhysics(.02f); brain.Tick(0);
            Assert.AreEqual(EnemyAttackPayloadKind.Volley, brain.CurrentAttack.Kind);
            brain.Tick(10); brain.TickPhysics(.02f); var count = shots.Count; Assert.Greater(count, 0);
            brain.TickPhysics(.02f); Assert.AreEqual(count, shots.Count);
            brain.Tick(0); Assert.AreEqual(GargoyleState.FamilyRecovery, brain.CurrentState);
            Assert.That(shots, Has.All.Matches<EnemyProjectile2D>(shot => shot.IsLaunched));
            brain.SetTarget(null); Assert.That(shots, Has.All.Matches<EnemyProjectile2D>(shot => !shot.IsLaunched));
        }

        [Test]
        public void ActualWardOpeningCounterEntersOneBeamStagger_AndExposesHead()
        {
            JsonUtility.FromJsonOverwrite("{\"families\":[2,0]}", tuning); Set(tuning, "environmentLayer", (LayerMask)(1 << 8));
            var beamDefinition = tuning.GetAttackDefinition(GargoyleAttackFamily.Beam);
            JsonUtility.FromJsonOverwrite("{\"windupDuration\":0.8,\"activeDuration\":0.7,\"payload\":{\"kind\":2,\"lockedAimDuration\":0.3}}", beamDefinition.Steps[0]);
            var emitter = actor.gameObject.AddComponent<EnemyBeamAttack2D>(); emitter.Initialize(actor, tuning, brain); Set(brain, "beam", emitter);
            brain.ResetEncounter(); StartBasic(); CompleteAttack();
            target.transform.position = new Vector2(4, 1); target.AddComponent<BoxCollider2D>().size = new Vector2(1, 2);
            var ward = target.AddComponent<PlayerWardRuntime>(); ward.Initialize(Track(ScriptableObject.CreateInstance<WardDefinitionSO>())); ward.Arm(-1);
            Physics2D.SyncTransforms(); brain.Tick(.25f); brain.TickPhysics(.02f); brain.Tick(0);
            Assert.AreEqual(EnemyAttackPayloadKind.Beam, brain.CurrentAttack.Kind);
            brain.Tick(.8f); brain.TickPhysics(.02f); Assert.AreEqual(GargoyleState.Staggered, brain.CurrentState);
            Assert.IsTrue(policy.HeadExposed); Assert.AreEqual(5, target.GetComponent<SimpleHealth>().CurrentHealth);
            Assert.IsFalse(ward.IsActive); brain.Tick(0); Assert.AreEqual(GargoyleState.Staggered, brain.CurrentState);
        }

        private sealed class FakeMotor : IEnemyPatrolMotor2D
        {
            public Vector2 Position => Vector2.zero;
            public int FacingDirection { get; private set; } = 1;
            public EnemyPatrolMoveResult Result = EnemyPatrolMoveResult.Arrived;
            public EnemyPatrolMoveResult MoveTowards(Vector2 target, float speed, float arrivalDistance, float fixedDeltaTime) => Result;
            public void Stop() { }
            public void SetFacing(int direction) { if (direction != 0) FacingDirection = direction > 0 ? 1 : -1; }
        }
    }
}
