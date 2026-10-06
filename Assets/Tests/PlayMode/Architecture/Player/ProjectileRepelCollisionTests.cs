using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace TicGame.Architecture.PlayModeTests
{
    public sealed class ProjectileRepelCollisionTests
    {
        private readonly List<Object> objects = new();
        private SimulationMode2D previousMode;
        private GameObject player;
        private SimpleHealth health;
        private PlayerAttackHitDetector2D detector;
        private PlayerRepelRuntime repel;
        private EnemyProjectile2D shot;

        [SetUp]
        public void SetUp()
        {
            Time.timeScale = 1;
            previousMode = Physics2D.simulationMode; Physics2D.simulationMode = SimulationMode2D.Script;
            player = Track(new GameObject("Repel physics player")); player.SetActive(false);
            player.layer = LayerMask.NameToLayer("PlayerHitbox");
            player.AddComponent<Rigidbody2D>().gravityScale = 0f;
            health = player.AddComponent<SimpleHealth>();
            player.AddComponent<BoxCollider2D>();
            detector = player.AddComponent<PlayerAttackHitDetector2D>();
            repel = player.AddComponent<PlayerRepelRuntime>();
            var controller = player.AddComponent<PlayerController>(); controller.enabled = false;
            Set(controller, "movementConfig", Track(ScriptableObject.CreateInstance<PlayerMovementConfigSO>()));
            Set(controller, "dashDefinition", Track(ScriptableObject.CreateInstance<PlayerDashDefinitionSO>()));
            Set(controller, "attackDefinition", Track(ScriptableObject.CreateInstance<PlayerAttackDefinitionSO>()));
            player.SetActive(true); player.GetComponent<Rigidbody2D>().gravityScale = 0;
            controller.ActionRunner.TryStartAction(controller.Context, new MeleeAction());
            detector.Initialize(controller);
            var owner = Track(new GameObject("Hostile projectile")); owner.SetActive(false);
            owner.layer = LayerMask.NameToLayer("Enemy"); owner.transform.position = new Vector2(.55f, 0);
            owner.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
            var shotCollider = owner.AddComponent<CircleCollider2D>(); shotCollider.isTrigger = true; shotCollider.radius = .12f;
            shot = owner.AddComponent<EnemyProjectile2D>();
            var profile = Track(ScriptableObject.CreateInstance<DamageProfileSO>());
            JsonUtility.FromJsonOverwrite("{\"baseDamage\":2}", profile); Set(shot, "damageProfile", profile);
            shot.Launch(Vector2.left, Track(new GameObject("Enemy source")), 1);
            Physics2D.SyncTransforms();
        }

        [TearDown]
        public void TearDown()
        {
            Physics2D.simulationMode = previousMode; Time.timeScale = 1;
            for (var index = objects.Count - 1; index >= 0; index--) Object.DestroyImmediate(objects[index]);
            objects.Clear();
        }
        private T Track<T>(T value) where T : Object { objects.Add(value); return value; }
        private static void Set(object target, string field, object value) => target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        private void Resolve() => typeof(PlayerAttackHitDetector2D).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(detector, null);
        private void Simulate() { shot.FixedTick(.02f); Physics2D.Simulate(.02f); }

        [Test]
        public void MeleeInterceptionBeforeContact_TransfersOwnershipAndPreventsPlayerDamage()
        {
            repel.Arm(3); Resolve(); Simulate();
            Assert.AreEqual(player, shot.SourceObject); Assert.AreEqual(5, health.CurrentHealth);
        }

        [Test]
        public void MissedInterception_AllowsRealProjectileContactDamage()
        {
            Simulate(); Assert.AreEqual(3, health.CurrentHealth); Assert.IsFalse(shot.IsLaunched);
        }

        [Test]
        public void DamageAcceptedBeforeMelee_IsNotRefunded()
        {
            Simulate(); repel.Arm(3); Resolve(); Assert.AreEqual(3, health.CurrentHealth);
        }

        private EnemyHealth EnemyAt(Vector2 position, out EnemyPoise poise)
        {
            var enemy = Track(new GameObject("Enemy collider")); enemy.layer = LayerMask.NameToLayer("Enemy"); enemy.transform.position = position;
            var enemyHealth = enemy.AddComponent<EnemyHealth>();
            var actor = enemy.AddComponent<EnemyActor>(); var definition = Track(ScriptableObject.CreateInstance<EnemyDefinitionSO>());
            JsonUtility.FromJsonOverwrite("{\"id\":\"test-enemy\",\"maxHealth\":10}", definition); actor.SetDefinition(definition); actor.Initialize();
            poise = enemy.AddComponent<EnemyPoise>(); poise.Initialize(30, 0);
            var policy = enemy.AddComponent<BatMachineDamagePolicy>(); policy.SetDependencies(enemyHealth, poise, null);
            enemy.AddComponent<BoxCollider2D>();
            var hurtbox = new GameObject("Bat hurtbox"); hurtbox.layer = enemy.layer; hurtbox.transform.SetParent(enemy.transform, false);
            hurtbox.AddComponent<BoxCollider2D>().isTrigger = true; hurtbox.AddComponent<BatMachineHurtbox>().Configure(policy);
            Physics2D.SyncTransforms(); return enemyHealth;
        }

        [Test]
        public void ConvertedShot_RealEnemyContact_AppliesDocumentedHealthAndPoiseDamage()
        {
            var enemyHealth = EnemyAt(new Vector2(1.15f, 0), out var poise);
            repel.Arm(3); Resolve(); Assert.AreEqual(9, enemyHealth.CurrentHealth); Simulate();
            Assert.AreEqual(7, enemyHealth.CurrentHealth); Assert.AreEqual(20, poise.CurrentPoise); Assert.IsFalse(shot.IsLaunched);
        }

        [Test]
        public void UnconvertedShot_EnemyContactDoesNotDamageEnemy()
        {
            player.transform.position = Vector2.left * 4;
            var enemyHealth = EnemyAt(new Vector2(1.15f, 0), out var poise);
            Simulate(); Assert.AreEqual(10, enemyHealth.CurrentHealth); Assert.AreEqual(30, poise.CurrentPoise);
        }

        [Test]
        public void ProjectileSourceChildCollider_CannotReceiveItsOwnProjectile()
        {
            var sourceChild = Track(new GameObject("Source child")); sourceChild.transform.SetParent(shot.SourceObject.transform);
            var childHealth = sourceChild.AddComponent<SimpleHealth>(); childHealth.Initialize();
            Assert.IsFalse(shot.TryResolveHit(sourceChild)); Assert.AreEqual(5, childHealth.CurrentHealth);
        }

        [Test]
        public void ConvertedShot_PhysicalBodyCannotBypassTheBatHurtboxPolicy()
        {
            var enemyHealth = EnemyAt(new Vector2(3, 0), out var poise);
            shot.Deflect(player);
            Assert.IsFalse(shot.TryResolveCollision(enemyHealth.gameObject));
            Assert.IsTrue(shot.TryResolveCollision(enemyHealth.GetComponentInChildren<BatMachineHurtbox>().gameObject));
            Assert.AreEqual(8, enemyHealth.CurrentHealth); Assert.AreEqual(20, poise.CurrentPoise);
        }

        private sealed class MeleeAction : IPlayerAction, IPlayerActionAnimationSource
        {
            public PlayerActionState State => PlayerActionState.Attack1;
            public bool IsComplete => false;
            public PlayerActionPhase AnimationPhase => PlayerActionPhase.Execution;
            public PlayerCardTimeState AnimationCardTime => PlayerCardTimeState.None;
            public void Enter(PlayerContext context) { }
            public void Tick(PlayerContext context, float deltaTime) { }
            public void FixedTick(PlayerContext context, float fixedDeltaTime) { }
            public void Exit(PlayerContext context) { }
        }
    }
}
