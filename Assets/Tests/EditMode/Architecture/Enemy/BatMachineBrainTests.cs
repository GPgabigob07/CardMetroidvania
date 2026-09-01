using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace TicGame.Architecture.Tests
{
    public sealed class BatMachineBrainTests
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
        public void PoiseDepleted_WhileEngaging_EntersStunnedFallAndEnablesGravity()
        {
            var rig = CreateBat();
            EnterEngage(rig);

            rig.Poise.ApplyPoiseDamage(30f);

            Assert.AreEqual(BatMachineState.StunnedFall, rig.Brain.CurrentState);
            Assert.Greater(rig.Body.gravityScale, 0f);
        }

        [Test]
        public void PatrolRandom_InitializationSelectsDeterministicBoundedWaypointAndRequestsMovement()
        {
            var first = CreateBat(withTarget: false);
            var second = CreateBat(withTarget: false);

            Assert.AreEqual(first.Brain.CurrentPatrolWaypoint, second.Brain.CurrentPatrolWaypoint);
            Assert.LessOrEqual(Mathf.Abs(first.Brain.CurrentPatrolWaypoint.x), 2f);
            Assert.LessOrEqual(Mathf.Abs(first.Brain.CurrentPatrolWaypoint.y), 1f);
            Assert.GreaterOrEqual(Vector2.Distance(Vector2.zero, first.Brain.CurrentPatrolWaypoint), 0.5f);

            first.Brain.FixedTick(0.1f);

            Assert.Greater(first.Body.linearVelocity.sqrMagnitude, 0f);
        }

        [Test]
        public void PatrolRandom_ArrivalWaitsBeforeSelectingNextWaypoint()
        {
            var rig = CreateBat(withTarget: false);
            var firstWaypoint = rig.Brain.CurrentPatrolWaypoint;
            rig.Body.position = firstWaypoint;

            rig.Brain.FixedTick(0.02f);
            rig.Brain.Tick(0.29f);

            Assert.AreEqual(firstWaypoint, rig.Brain.CurrentPatrolWaypoint);

            rig.Brain.Tick(0.01f);

            Assert.AreNotEqual(firstWaypoint, rig.Brain.CurrentPatrolWaypoint);
        }

        [Test]
        public void FirePlan_LowHealthAndLowPoise_UsesShortCooldownAndTwoShots()
        {
            var rig = CreateBat();

            rig.Health.ApplyDamage(CreateContext(rig.Root, amount: 6f));
            rig.Poise.ApplyPoiseDamage(16f);

            Assert.AreEqual(2, rig.Brain.CurrentFirePlan.ProjectileCount);
            Assert.Less(rig.Brain.CurrentFirePlan.Cooldown, rig.Brain.NormalFireCooldown);
        }

        [Test]
        public void StunnedFall_WithoutTerrainLanding_DoesNotRecoverInAir()
        {
            var rig = CreateBat();
            EnterEngage(rig);
            rig.Poise.ApplyPoiseDamage(30f);

            rig.Brain.Tick(30f);

            Assert.AreEqual(BatMachineState.StunnedFall, rig.Brain.CurrentState);
            Assert.AreEqual(0f, rig.Poise.CurrentPoise);
        }

        [Test]
        public void Landing_NonLethalFall_EntersGroundedRecoveryRatherThanFlyingRecovery()
        {
            var rig = CreateBat();
            EnterStunnedFall(rig, descendingSpeed: 5f);

            rig.Brain.ReportTerrainLanding(isTerrain: true);

            Assert.AreEqual(BatMachineState.GroundedRecovery, rig.Brain.CurrentState);
            Assert.AreEqual(11f, rig.Health.CurrentHealth);
            Assert.Greater(rig.Body.gravityScale, 0f);
        }

        [Test]
        public void Landing_LethalRecordedFallSpeed_EntersDeadEvenIfBodyHasStopped()
        {
            var rig = CreateBat();
            EnterStunnedFall(rig, descendingSpeed: 30f);
            rig.Body.linearVelocity = Vector2.zero;

            rig.Brain.ReportTerrainLanding(isTerrain: true);

            Assert.AreEqual(BatMachineState.Dead, rig.Brain.CurrentState);
            Assert.IsTrue(rig.Health.IsDefeated);
        }

        [Test]
        public void Landing_NonTerrainContact_DoesNotEndStunnedFall()
        {
            var rig = CreateBat();
            EnterStunnedFall(rig, descendingSpeed: 5f);

            rig.Brain.ReportTerrainLanding(isTerrain: false);

            Assert.AreEqual(BatMachineState.StunnedFall, rig.Brain.CurrentState);
            Assert.AreEqual(12f, rig.Health.CurrentHealth);
        }

        [Test]
        public void GroundedRecovery_RestoresPositivePoiseOnlyAfterTimerThenResumesEngage()
        {
            var rig = CreateBat();
            EnterStunnedFall(rig, descendingSpeed: 5f);
            rig.Brain.ReportTerrainLanding(isTerrain: true);

            rig.Brain.Tick(0.49f);

            Assert.AreEqual(BatMachineState.GroundedRecovery, rig.Brain.CurrentState);
            Assert.AreEqual(0f, rig.Poise.CurrentPoise);

            rig.Brain.Tick(0.01f);

            Assert.AreEqual(BatMachineState.Engage, rig.Brain.CurrentState);
            Assert.AreEqual(12f, rig.Poise.CurrentPoise);
            Assert.AreEqual(0f, rig.Body.gravityScale);
        }

        private BatRig CreateBat(bool withTarget = true)
        {
            var root = CreateObject("Bat Machine");
            var body = root.AddComponent<Rigidbody2D>();
            body.gravityScale = 2.5f;
            var health = root.AddComponent<EnemyHealth>();
            var actor = root.AddComponent<EnemyActor>();
            actor.SetDefinition(CreateDefinition("bat-machine", maximumHealth: 12f));
            actor.Initialize();
            var poise = root.AddComponent<EnemyPoise>();
            poise.Initialize(maximum: 30f, regenerationPerSecond: 0.33f);
            var motor = root.AddComponent<AerialSteeringMotor2D>();
            motor.SetBody(body);
            var monitor = root.AddComponent<BatThreatMonitor>();
            monitor.Configure(monitorRadius: 8f, baseMeleeReach: 2f, outerBandThickness: 0.5f);
            var launcher = root.AddComponent<BatProjectileLauncher>();
            var brain = root.AddComponent<BatMachineBrain>();
            var policy = root.AddComponent<BatMachineDamagePolicy>();
            policy.SetDependencies(health, poise, brain);
            brain.SetDependencies(actor, health, poise, body, motor, monitor, launcher, policy);
            brain.ConfigureFire(normalCooldown: 4f, shortCooldown: 2f, windup: 0.2f, interShotDelay: 0.1f);
            brain.ConfigureRecovery(safeImpactSpeed: 4f, damagePerSpeedUnit: 1f, recoveryDuration: 0.5f, restoredPoise: 12f);
            if (withTarget)
            {
                var target = CreateObject("Player Target");
                target.transform.position = Vector3.right;
                brain.SetTarget(target.transform, null);
            }

            brain.Initialize();

            return new BatRig(root, body, health, poise, brain);
        }

        private static void EnterEngage(BatRig rig)
        {
            rig.Brain.Tick(0f);
            Assert.AreEqual(BatMachineState.Engage, rig.Brain.CurrentState);
        }

        private static void EnterStunnedFall(BatRig rig, float descendingSpeed)
        {
            EnterEngage(rig);
            rig.Body.linearVelocity = Vector2.down * descendingSpeed;
            rig.Poise.ApplyPoiseDamage(30f);
            Assert.AreEqual(BatMachineState.StunnedFall, rig.Brain.CurrentState);
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

        private static DamageContext CreateContext(GameObject target, float amount, float poiseDamage = 0f)
        {
            return new DamageContext(
                source: null,
                target: target,
                profile: null,
                amount: amount,
                hitPoint: Vector2.zero,
                direction: Vector2.right,
                poiseDamage: poiseDamage);
        }

        private static void SetField<T>(object target, string fieldName, T value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(field, $"Expected private field '{fieldName}' on {target.GetType().Name}.");
            field.SetValue(target, value);
        }

        private sealed class BatRig
        {
            public BatRig(
                GameObject root,
                Rigidbody2D body,
                EnemyHealth health,
                EnemyPoise poise,
                BatMachineBrain brain)
            {
                Root = root;
                Body = body;
                Health = health;
                Poise = poise;
                Brain = brain;
            }

            public GameObject Root { get; }
            public Rigidbody2D Body { get; }
            public EnemyHealth Health { get; }
            public EnemyPoise Poise { get; }
            public BatMachineBrain Brain { get; }
        }
    }
}
