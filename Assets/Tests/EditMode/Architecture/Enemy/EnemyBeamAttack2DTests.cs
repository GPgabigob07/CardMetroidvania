using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace TicGame.Architecture.Tests
{
    public sealed class EnemyBeamAttack2DTests
    {
        private readonly List<Object> objects = new();
        private EnemyActor actor;
        private EnemyBeamAttack2D beam;
        private EnemyAttackDefinitionSO definition;
        private EnemyAttackStep step;
        private DamageProfileSO profile;
        private SimpleHealth health;
        private PlayerWardRuntime ward;
        private WardDefinitionSO wardDefinition;
        [SetUp]
        public void SetUp()
        {
            var root = Track(new GameObject("Gargoyle")); actor = root.AddComponent<EnemyActor>();
            var identity = Track(ScriptableObject.CreateInstance<EnemyDefinitionSO>()); actor.SetDefinition(identity); actor.Initialize();
            var tuning = Track(ScriptableObject.CreateInstance<GargoyleTuningSO>());
            typeof(GargoyleTuningSO).GetField("environmentLayer", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(tuning, (LayerMask)(1 << 8));
            beam = root.AddComponent<EnemyBeamAttack2D>(); beam.Initialize(actor, tuning);
            profile = Track(ScriptableObject.CreateInstance<DamageProfileSO>()); JsonUtility.FromJsonOverwrite("{\"baseDamage\":1}", profile);
            step = new EnemyAttackStep("beam", .8f, .7f, .75f, profile);
            JsonUtility.FromJsonOverwrite("{\"payload\":{\"kind\":2,\"lockedAimDuration\":0.3}}", step);
            definition = Track(ScriptableObject.CreateInstance<EnemyAttackDefinitionSO>()); definition.SetSteps(new[] { step });
            var player = Track(new GameObject("Player")); player.transform.position = new Vector2(4, 0);
            health = player.AddComponent<SimpleHealth>(); health.Initialize(); player.AddComponent<BoxCollider2D>().size = new Vector2(1, 2);
            wardDefinition = Track(ScriptableObject.CreateInstance<WardDefinitionSO>()); ward = player.AddComponent<PlayerWardRuntime>(); ward.Initialize(wardDefinition);
            Physics2D.SyncTransforms();
        }
        [TearDown]
        public void TearDown() { Time.timeScale = 1; for (var i = objects.Count - 1; i >= 0; i--) Object.DestroyImmediate(objects[i]); objects.Clear(); }
        private T Track<T>(T value) where T : Object { objects.Add(value); return value; }
        private void Fire(float elapsed = 0) { beam.Begin(1, definition); beam.Sample(Vector2.zero, Vector2.right, elapsed); }

        [Test]
        public void HealthDamageIsOncePerCast_WithMultiplePlayerColliders()
        {
            var child = Track(new GameObject("PlayerCollider")); child.transform.SetParent(health.transform, false); child.AddComponent<BoxCollider2D>(); Physics2D.SyncTransforms();
            Fire(); beam.Sample(Vector2.zero, Vector2.right, .1f); beam.Sample(Vector2.zero, Vector2.right, .6f);
            Assert.AreEqual(4, health.CurrentHealth);
        }
        [Test]
        public void OpeningWardCancelsBeforeDamage_AndNotifiesOnce()
        {
            ward.Arm(-1); var counters = 0; beam.Countered += _ => counters++;
            Fire(); beam.Sample(Vector2.zero, Vector2.right, .1f);
            Assert.AreEqual(5, health.CurrentHealth); Assert.IsFalse(ward.IsActive); Assert.IsFalse(beam.IsActive); Assert.AreEqual(1, counters);
        }
        [TestCase(0f, true)]
        [TestCase(.3f, false)]
        public void WardInterceptionDoesNotRequireBodyOverlap(float elapsed, bool opening)
        {
            health.transform.position = new Vector2(4, 1.3f);
            JsonUtility.FromJsonOverwrite("{\"height\":3}", wardDefinition);
            Physics2D.SyncTransforms(); ward.Arm(-1);
            var counters = 0; beam.Countered += _ => counters++;
            Fire(elapsed);
            Assert.That(health.CurrentHealth, Is.EqualTo(5));
            Assert.That(beam.EndPoint.x, Is.LessThan(4));
            Assert.That(ward.IsActive, Is.EqualTo(!opening));
            Assert.That(beam.IsActive, Is.EqualTo(!opening));
            Assert.That(counters, Is.EqualTo(opening ? 1 : 0));
        }
        [Test]
        public void LateWardClipsWithoutConsumption_ThenExpiredGuardAllowsTheUnspentHit()
        {
            ward.Arm(-1); Fire(.3f); Assert.AreEqual(5, health.CurrentHealth); Assert.IsTrue(ward.IsActive); Assert.Less(beam.EndPoint.x, 4);
            ward.Tick(.6f); beam.Sample(Vector2.zero, Vector2.right, .65f); Assert.AreEqual(4, health.CurrentHealth);
        }
        [Test]
        public void BackFacingWardCannotBlock()
        { ward.Arm(1); Fire(); Assert.AreEqual(4, health.CurrentHealth); Assert.IsTrue(ward.IsActive); }
        [Test]
        public void TerrainClipsThickSegmentBeforeWardAndPlayer()
        {
            var wall = Track(new GameObject("Wall")); wall.layer = 8; wall.transform.position = new Vector2(2, 0); wall.AddComponent<BoxCollider2D>();
            Physics2D.SyncTransforms(); ward.Arm(-1); Fire();
            Assert.AreEqual(5, health.CurrentHealth); Assert.IsTrue(ward.IsActive); Assert.Less(beam.EndPoint.x, 2);
        }
        [Test]
        public void EmitterInsideTerrainCannotDamageOrConsumeWard()
        {
            var wall = Track(new GameObject("Wall")); wall.layer = 8; wall.AddComponent<BoxCollider2D>(); Physics2D.SyncTransforms();
            ward.Arm(-1); Fire(); Assert.AreEqual(5, health.CurrentHealth); Assert.IsTrue(ward.IsActive);
        }
        [Test]
        public void SynchronousEnemyDeathInCounterCallbackWinsOverStaggerNotification()
        {
            var counters = 0; beam.Countered += _ => counters++;
            ward.Countered += _ => actor.Health.ApplyDamage(new DamageContext(null, actor.gameObject, null, actor.Health.MaximumHealth, Vector2.zero, Vector2.right));
            ward.Arm(-1); Fire(); Assert.IsTrue(actor.IsDefeated); Assert.AreEqual(0, counters); Assert.AreEqual(5, health.CurrentHealth);
        }
        [Test]
        public void OpeningWindowAndThicknessUseLiveValues_WithoutReplayingSpentDamage()
        {
            ward.Arm(-1); beam.Begin(1, definition);
            JsonUtility.FromJsonOverwrite("{\"payload\":{\"beamOpeningDuration\":0.4,\"beamThickness\":0.6}}", step);
            beam.Sample(Vector2.zero, Vector2.right, .3f); Assert.IsFalse(beam.IsActive); Assert.AreEqual(5, health.CurrentHealth);
            beam.Begin(2, definition); JsonUtility.FromJsonOverwrite("{\"baseDamage\":2}", profile);
            beam.Sample(Vector2.zero, Vector2.right, 0); JsonUtility.FromJsonOverwrite("{\"baseDamage\":3}", profile);
            beam.Sample(Vector2.zero, Vector2.right, .1f); Assert.AreEqual(3, health.CurrentHealth);
        }
        [Test]
        public void PauseAndHoldSuppressAllBeamQueries_EvenAtZeroElapsed()
        {
            beam.Begin(1, definition); ward.Arm(-1); Time.timeScale = 0; beam.Sample(Vector2.zero, Vector2.right, 0);
            Assert.AreEqual(5, health.CurrentHealth); Assert.IsTrue(ward.IsActive); Time.timeScale = 1;
            using (health.gameObject.AddComponent<PlayerWorldHold>().Acquire()) beam.Sample(Vector2.zero, Vector2.right, 0);
            Assert.AreEqual(5, health.CurrentHealth); Assert.IsTrue(ward.IsActive);
        }
        [Test]
        public void CancelRejectsOldSamplesAndReusedCastTokens()
        {
            beam.Begin(1, definition); beam.Cancel(); beam.Cancel(); beam.Sample(Vector2.zero, Vector2.right, 0);
            beam.Begin(1, definition); beam.Sample(Vector2.zero, Vector2.right, 0); Assert.AreEqual(5, health.CurrentHealth);
            beam.Begin(2, definition); beam.Sample(Vector2.zero, Vector2.right, 0); Assert.AreEqual(4, health.CurrentHealth);
        }
        [Test]
        public void WiderLiveBeamTurnsAnActualNearMissIntoOneHealthHit()
        {
            health.transform.position = new Vector2(4, 1.3f); Physics2D.SyncTransforms(); Fire(); Assert.AreEqual(5, health.CurrentHealth);
            JsonUtility.FromJsonOverwrite("{\"payload\":{\"beamThickness\":0.8}}", step);
            beam.Sample(Vector2.zero, Vector2.right, .1f); beam.Sample(Vector2.zero, Vector2.right, .2f); Assert.AreEqual(4, health.CurrentHealth);
        }
    }
}
