using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TicGame.Architecture;
using UnityEngine;

namespace TicGame.Architecture.Tests
{
    public sealed class EnemyBaselineTests
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
        public void Definition_ClampsMaximumHealth_AndFallsBackToAssetName()
        {
            var definition = CreateDefinition("Training Dummy", maximumHealth: -10f);

            Assert.AreEqual("Training Dummy", definition.Id);
            Assert.AreEqual("Training Dummy", definition.DisplayName);
            Assert.AreEqual(1f, definition.MaxHealth);
        }

        [Test]
        public void Health_InitializesIdempotentlyAtFullHealth()
        {
            var health = CreateHealth();

            health.Initialize(maximumHealth: 12f);
            ApplyDamage(health, amount: 3f);
            health.Initialize(maximumHealth: 12f);

            Assert.AreEqual(12f, health.MaximumHealth);
            Assert.AreEqual(12f, health.CurrentHealth);
            Assert.False(health.IsDefeated);
        }

        [Test]
        public void Health_DamageDefeatAndRestore_EmitExpectedEvents()
        {
            var health = CreateHealth();
            health.Initialize(maximumHealth: 5f);
            var healthChanges = 0;
            var damageEvents = 0;
            var defeatEvents = 0;
            var restoreEvents = 0;
            health.HealthChanged += _ => healthChanges++;
            health.Damaged += _ => damageEvents++;
            health.Defeated += _ => defeatEvents++;
            health.Restored += _ => restoreEvents++;

            var lethalResult = ApplyDamage(health, amount: 8f);
            var rejectedResult = ApplyDamage(health, amount: 1f);
            health.Restore(amount: 20f);

            Assert.True(lethalResult.Accepted);
            Assert.True(lethalResult.Killed);
            Assert.AreEqual(5f, lethalResult.AppliedAmount);
            Assert.False(rejectedResult.Accepted);
            Assert.AreEqual(5f, health.CurrentHealth);
            Assert.AreEqual(2, healthChanges);
            Assert.AreEqual(1, damageEvents);
            Assert.AreEqual(1, defeatEvents);
            Assert.AreEqual(1, restoreEvents);
        }

        [Test]
        public void Actor_UsesDefinitionIdentityAndHealth_ThenResetsAfterDefeat()
        {
            var owner = CreateObject("Fallback Enemy");
            var health = owner.AddComponent<EnemyHealth>();
            var actor = owner.AddComponent<EnemyActor>();
            var definition = CreateDefinition("dummy-id", maximumHealth: 9f);
            SetField(definition, "displayName", "Review Dummy");
            actor.SetDefinition(definition);

            actor.Initialize();
            ApplyDamage(health, amount: 9f);

            Assert.AreEqual("dummy-id", actor.Id);
            Assert.AreEqual("Review Dummy", actor.DisplayName);
            Assert.True(actor.IsDefeated);
            Assert.False(actor.IsOperational);

            actor.ResetActor();

            Assert.AreEqual(9f, health.CurrentHealth);
            Assert.True(actor.IsOperational);
        }

        [Test]
        public void ContactAttack_DamagesPlayerFlashesSpriteAndRespectsCooldown()
        {
            var enemy = CreateObject("Contact Enemy");
            var enemyHealth = enemy.AddComponent<EnemyHealth>();
            var actor = enemy.AddComponent<EnemyActor>();
            actor.SetDefinition(CreateDefinition("contact-enemy", maximumHealth: 5f));
            actor.Initialize();
            Assert.NotNull(enemyHealth);

            var visual = CreateObject("Enemy Visual");
            visual.transform.SetParent(enemy.transform);
            var renderer = visual.AddComponent<SpriteRenderer>();
            renderer.color = Color.white;
            var contactAttack = enemy.AddComponent<EnemyContactAttack2D>();
            contactAttack.Configure(actor, renderer);
            contactAttack.ConfigureDamage(amount: 1f, cooldown: 1f, flashDuration: 0.2f);

            var player = CreateObject("Player");
            var playerHealth = player.AddComponent<SimpleHealth>();
            playerHealth.Initialize();

            Assert.True(contactAttack.TryAttack(player));
            Assert.AreEqual(4f, playerHealth.CurrentHealth);
            Assert.AreEqual(Color.red, renderer.color);
            Assert.True(contactAttack.IsFlashing);

            Assert.False(contactAttack.TryAttack(player));
            Assert.AreEqual(4f, playerHealth.CurrentHealth);

            contactAttack.Tick(deltaTime: 0.2f);
            Assert.AreEqual(Color.white, renderer.color);
            Assert.False(contactAttack.IsFlashing);

            contactAttack.Tick(deltaTime: 0.8f);
            Assert.True(contactAttack.TryAttack(player));
            Assert.AreEqual(3f, playerHealth.CurrentHealth);
        }

        [Test]
        public void ContactAttack_OverlapCheck_DamagesPlayerInsideAttackCollider()
        {
            var enemy = CreateObject("Overlap Enemy");
            enemy.transform.position = Vector3.zero;
            var enemyCollider = enemy.AddComponent<BoxCollider2D>();
            enemyCollider.size = Vector2.one;
            var actor = enemy.AddComponent<EnemyActor>();
            actor.SetDefinition(CreateDefinition("overlap-enemy", maximumHealth: 5f));
            actor.Initialize();

            var contactAttack = enemy.AddComponent<EnemyContactAttack2D>();
            contactAttack.Configure(actor, renderer: null);
            contactAttack.ConfigureDamage(amount: 1f, cooldown: 1f, flashDuration: 0.2f);
            contactAttack.ConfigureOverlap(enemyCollider, ~0);

            var player = CreateObject("Player");
            player.transform.position = Vector3.zero;
            player.AddComponent<BoxCollider2D>().size = Vector2.one;
            var playerHealth = player.AddComponent<SimpleHealth>();
            playerHealth.Initialize();
            Physics2D.SyncTransforms();

            Assert.True(contactAttack.TryAttackOverlaps());
            Assert.AreEqual(4f, playerHealth.CurrentHealth);
        }

        [Test]
        public void Dummy_RegenerationWaitsAndAdditionalDamageRestartsDelay()
        {
            var owner = CreateObject("Dummy");
            var health = owner.AddComponent<EnemyHealth>();
            owner.AddComponent<EnemyActor>();
            var dummy = owner.AddComponent<TrainingDummy>();
            health.Initialize(maximumHealth: 10f);
            dummy.ConfigureRegeneration(delay: 1f, healthPerSecond: 2f, restoreImmediatelyOnDefeat: false);

            ApplyDamage(health, amount: 4f);
            dummy.TickRegeneration(deltaTime: 0.75f);
            ApplyDamage(health, amount: 1f);
            dummy.TickRegeneration(deltaTime: 0.75f);

            Assert.AreEqual(5f, health.CurrentHealth);

            dummy.TickRegeneration(deltaTime: 0.5f);

            Assert.AreEqual(6f, health.CurrentHealth);
            Assert.AreEqual(2, dummy.AcceptedHitCount);
            Assert.AreEqual(5f, dummy.TotalDamageReceived);
        }

        [Test]
        public void Dummy_ImmediateDefeatRestore_UsesConfiguredFeedbackDelay()
        {
            var owner = CreateObject("Dummy");
            var health = owner.AddComponent<EnemyHealth>();
            owner.AddComponent<EnemyActor>();
            var dummy = owner.AddComponent<TrainingDummy>();
            health.Initialize(maximumHealth: 5f);
            dummy.ConfigureRegeneration(
                delay: 10f,
                healthPerSecond: 0f,
                restoreImmediatelyOnDefeat: true,
                defeatDelay: 0.25f);

            ApplyDamage(health, amount: 5f);
            dummy.TickRegeneration(deltaTime: 0.2f);

            Assert.True(health.IsDefeated);

            dummy.TickRegeneration(deltaTime: 0.05f);

            Assert.AreEqual(5f, health.CurrentHealth);
            Assert.False(health.IsDefeated);
        }

        [Test]
        public void Dummy_DefeatCanRecoverThroughGradualRegeneration()
        {
            var owner = CreateObject("Dummy");
            var health = owner.AddComponent<EnemyHealth>();
            owner.AddComponent<EnemyActor>();
            var dummy = owner.AddComponent<TrainingDummy>();
            health.Initialize(maximumHealth: 5f);
            dummy.ConfigureRegeneration(delay: 0.5f, healthPerSecond: 2f, restoreImmediatelyOnDefeat: false);

            ApplyDamage(health, amount: 5f);
            dummy.TickRegeneration(deltaTime: 0.5f);

            Assert.AreEqual(1f, health.CurrentHealth);
            Assert.False(health.IsDefeated);
        }

        private EnemyDefinitionSO CreateDefinition(string name, float maximumHealth)
        {
            var definition = ScriptableObject.CreateInstance<EnemyDefinitionSO>();
            definition.name = name;
            SetField(definition, "maxHealth", maximumHealth);
            objectsToDestroy.Add(definition);
            return definition;
        }

        private EnemyHealth CreateHealth()
        {
            return CreateObject("Enemy").AddComponent<EnemyHealth>();
        }

        private GameObject CreateObject(string name)
        {
            var instance = new GameObject(name);
            objectsToDestroy.Add(instance);
            return instance;
        }

        private static DamageResult ApplyDamage(EnemyHealth health, float amount)
        {
            var context = new DamageContext(
                source: null,
                target: health.gameObject,
                profile: null,
                amount: amount,
                hitPoint: Vector2.zero,
                direction: Vector2.right);
            return health.ApplyDamage(context);
        }

        private static void SetField<T>(object target, string fieldName, T value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(field, $"Expected private field '{fieldName}' on {target.GetType().Name}.");
            field.SetValue(target, value);
        }
    }
}
