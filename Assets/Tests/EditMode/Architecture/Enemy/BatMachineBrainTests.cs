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
        public void PatrolRandom_AllCandidatesRejected_RetainsPositionWithoutMovingIntoObstacle()
        {
            var obstacle = CreateObject("Patrol Bounds Obstacle");
            var obstacleCollider = obstacle.AddComponent<BoxCollider2D>();
            obstacleCollider.size = new Vector2(10f, 10f);
            Physics2D.SyncTransforms();
            var rig = CreateBat(
                withTarget: false,
                configureBrain: brain => SetField(brain, "patrolObstacleLayers", (LayerMask)(1 << obstacle.layer)));

            rig.Brain.FixedTick(0.1f);

            Assert.AreEqual(rig.Body.position, rig.Brain.CurrentPatrolWaypoint);
            Assert.AreEqual(Vector2.zero, rig.Body.linearVelocity);
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
        public void WindupFire_OffsetLauncherSpawn_LocksDirectionFromEffectiveSpawnPosition()
        {
            var rig = CreateBat();
            var spawn = CreateObject("Offset Projectile Spawn");
            spawn.transform.position = new Vector3(0f, 2f, 0f);
            rig.Launcher.Configure(null, spawn.transform, rig.Root, projectileSpeed: 6f);
            EnterEngage(rig);

            rig.Brain.Tick(0f);
            rig.Brain.Tick(0.2f);

            Assert.AreEqual(new Vector2(1f, -2f).normalized, rig.Brain.CurrentFirePlan.LockedDirection);
        }

        [Test]
        public void Engage_RepeatedTicksAfterFailedDodge_RollsOnlyOnceForThreatWindow()
        {
            var rig = CreateBat();
            EnterEngage(rig);
            rig.Brain.Tick(0f);
            Assert.AreEqual(BatMachineState.WindupFire, rig.Brain.CurrentState);
            rig.Brain.Tick(0.2f);
            Assert.AreEqual(BatMachineState.Engage, rig.Brain.CurrentState);
            var approachingTarget = CreateObject("Approaching Target");
            approachingTarget.transform.position = Vector3.right * 3f;
            var targetBody = approachingTarget.AddComponent<Rigidbody2D>();
            targetBody.gravityScale = 0f;
            targetBody.linearVelocity = Vector2.left * 4f;
            rig.Brain.SetTarget(approachingTarget.transform, targetBody);
            var rolls = new CountingRollSource(0.99f);
            rig.Brain.SetDodgeRollSource(rolls);

            rig.Brain.Tick(0.01f);
            rig.Brain.Tick(0.01f);

            Assert.AreEqual(BatMachineState.Engage, rig.Brain.CurrentState);
            Assert.AreEqual(1, rolls.CallCount);
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
        public void Heal_WhileStunnedFall_DoesNotRestoreFlightOrChangeState()
        {
            var rig = CreateBat();
            rig.Health.ApplyDamage(CreateContext(rig.Root, amount: 2f));
            EnterStunnedFall(rig, descendingSpeed: 5f);

            rig.Health.Restore(1f);

            Assert.AreEqual(BatMachineState.StunnedFall, rig.Brain.CurrentState);
            Assert.AreEqual(0f, rig.Poise.CurrentPoise);
            Assert.Greater(rig.Body.gravityScale, 0f);
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

        [Test]
        public void Heal_WhileGroundedRecovery_DoesNotBypassRecoveryTimer()
        {
            var rig = CreateBat();
            rig.Health.ApplyDamage(CreateContext(rig.Root, amount: 2f));
            EnterStunnedFall(rig, descendingSpeed: 5f);
            rig.Brain.ReportTerrainLanding(isTerrain: true);

            rig.Health.Restore(1f);

            Assert.AreEqual(BatMachineState.GroundedRecovery, rig.Brain.CurrentState);
            Assert.AreEqual(0f, rig.Poise.CurrentPoise);
            Assert.Greater(rig.Body.gravityScale, 0f);
        }

        [Test]
        public void Heal_AfterDefeat_ResurrectsIntoPatrolWithFlightAndPoise()
        {
            var rig = CreateBat();
            rig.Health.ApplyDamage(CreateContext(rig.Root, amount: 12f));
            Assert.AreEqual(BatMachineState.Dead, rig.Brain.CurrentState);

            rig.Health.Restore(1f);

            Assert.AreEqual(BatMachineState.PatrolRandom, rig.Brain.CurrentState);
            Assert.AreEqual(rig.Poise.MaximumPoise, rig.Poise.CurrentPoise);
            Assert.AreEqual(0f, rig.Body.gravityScale);
        }

        private BatRig CreateBat(bool withTarget = true, System.Action<BatMachineBrain> configureBrain = null)
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
            configureBrain?.Invoke(brain);
            if (withTarget)
            {
                var target = CreateObject("Player Target");
                target.transform.position = Vector3.right;
                brain.SetTarget(target.transform, null);
            }

            brain.Initialize();

            return new BatRig(root, body, health, poise, launcher, brain);
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
                BatProjectileLauncher launcher,
                BatMachineBrain brain)
            {
                Root = root;
                Body = body;
                Health = health;
                Poise = poise;
                Launcher = launcher;
                Brain = brain;
            }

            public GameObject Root { get; }
            public Rigidbody2D Body { get; }
            public EnemyHealth Health { get; }
            public EnemyPoise Poise { get; }
            public BatProjectileLauncher Launcher { get; }
            public BatMachineBrain Brain { get; }
        }

        private sealed class CountingRollSource : IRandomRollSource
        {
            private readonly float value;

            public CountingRollSource(float value)
            {
                this.value = value;
            }

            public int CallCount { get; private set; }

            public float NextNormalized()
            {
                CallCount++;
                return value;
            }
        }
    }
}
