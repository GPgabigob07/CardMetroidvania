using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace TicGame.Architecture.Tests
{
    public sealed class EnemyKnockbackPrefabTests
    {
        [TestCase("BatMachine", 1f)]
        [TestCase("BatMachine", -1f)]
        [TestCase("GolemCharger", 1f)]
        [TestCase("GolemCharger", -1f)]
        public void AcceptedPlayerMeleeThroughChildHurtbox_DisplacesNeutralEnemy(string name, float direction)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Prefabs/Enemies/{name}.prefab");
            var enemy = Object.Instantiate(prefab, new Vector3(10000f, 10000f), Quaternion.identity);
            var player = new GameObject("Knockback source");
            var previousMode = Physics2D.simulationMode;
            try
            {
                Physics2D.simulationMode = SimulationMode2D.Script;
                enemy.GetComponent<EnemyActor>().Initialize();
                var body = enemy.GetComponent<Rigidbody2D>();
                var bat = enemy.GetComponent<BatMachineBrain>();
                var golem = enemy.GetComponent<GolemChargerBrain>();
                if (bat != null)
                {
                    enemy.GetComponent<AerialSteeringMotor2D>().SetBody(body);
                    bat.Initialize();
                }
                if (golem != null)
                {
                    golem.Initialize();
                    golem.Tick(0f);
                }
                body.gravityScale = 0f;
                body.linearVelocity = Vector2.zero;
                var recipient = bat != null
                    ? enemy.GetComponentInChildren<BatMachineHurtbox>().gameObject
                    : enemy.GetComponentsInChildren<EnemyHurtboxRegion>()
                        .First(region => region.Region == EnemyHurtboxRegionType.Body).gameObject;
                var effects = player.AddComponent<PlayerCombatEffects>();
                var instance = effects.BuildPrimaryDamageInstance("hit", "attack", 1f, 1f, 1f, 1);
                var report = DamageResolver.Resolve(new DamageRequest(instance, new[] { recipient }, body.position, Vector2.right * direction));
                Assert.AreEqual(1, report.EffectiveHitCount);
                var startPosition = body.position;
                for (var step = 0; step < 3; step++)
                {
                    bat?.FixedTick(0.02f);
                    golem?.FixedTick(0.02f);
                    Physics2D.Simulate(0.02f);
                    bat?.Tick(0.02f);
                    golem?.Tick(0.02f);
                }
                Assert.Greater((body.position.x - startPosition.x) * direction, 0.04f);
            }
            finally
            {
                Physics2D.simulationMode = previousMode;
                Object.DestroyImmediate(enemy);
                Object.DestroyImmediate(player);
            }
        }

        [TestCase("BatMachine")]
        [TestCase("GolemCharger")]
        public void CombatEnemy_ExposesKnockbackReceiverOnDynamicBody(string name)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Prefabs/Enemies/{name}.prefab");
            Assert.NotNull(prefab);
            Assert.NotNull(prefab.GetComponent<EnemyKnockbackReceiver>(), "Accepted melee hits need a root receiver.");
            Assert.AreEqual(RigidbodyType2D.Dynamic, prefab.GetComponent<Rigidbody2D>().bodyType);
        }
    }
}
