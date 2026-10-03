using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TicGame.Architecture.Tests
{
    public sealed class PlayerWardRuntimeTests
    {
        private GameObject player;
        private PlayerWardRuntime ward;
        private WardDefinitionSO definition;
        private SimpleHealth health;
        [SetUp]
        public void SetUp()
        {
            player = new GameObject("Player"); health = player.AddComponent<SimpleHealth>(); health.Initialize();
            player.AddComponent<BoxCollider2D>().size = new Vector2(1, 2);
            definition = ScriptableObject.CreateInstance<WardDefinitionSO>();
            ward = player.AddComponent<PlayerWardRuntime>(); ward.Initialize(definition); Physics2D.SyncTransforms();
        }
        [TearDown] public void TearDown() { Time.timeScale = 1; Object.DestroyImmediate(player); Object.DestroyImmediate(definition); }
        private BeamGuardQuery Query(float y = 0, float thickness = .3f, bool opening = true, long token = 1, float endX = -4)
            => new BeamGuardQuery(token, new Vector2(4, y), new Vector2(endX, y), thickness, new Vector2(4, y), opening);

        [Test]
        public void ArmLocksFacing_RejectsStacking_AndOpeningConsumesOnce()
        {
            Assert.IsTrue(ward.CanArm); Assert.IsTrue(ward.Arm(1)); Assert.IsFalse(ward.Arm(-1));
            Assert.AreEqual(1, ward.FacingSign); Assert.AreEqual(WardInterceptionKind.Countered, ward.TryIntercept(Query()).Kind);
            Assert.IsFalse(ward.IsActive); Assert.AreEqual(WardInterceptionKind.None, ward.TryIntercept(Query()).Kind);
            Assert.IsTrue(ward.CanArm);
        }

        [Test]
        public void ConsumedCastCannotConsumeNewWard_ButAnotherCastCan()
        {
            ward.Arm(1); ward.TryIntercept(Query()); ward.Arm(1);
            Assert.AreEqual(WardInterceptionKind.None, ward.TryIntercept(Query()).Kind); Assert.IsTrue(ward.IsActive);
            Assert.AreEqual(WardInterceptionKind.Countered, ward.TryIntercept(Query(token: 2)).Kind);
        }

        [Test]
        public void LateClipPreservesGuard_UntilLiveDurationExpires()
        {
            ward.Arm(1); var result = ward.TryIntercept(Query(opening: false));
            Assert.AreEqual(WardInterceptionKind.Clipped, result.Kind); Assert.Greater(result.ClipPosition.x, .6f);
            Assert.IsTrue(ward.IsActive); ward.Tick(.59f); Assert.IsTrue(ward.IsActive); ward.Tick(.01f); Assert.IsFalse(ward.IsActive);
        }

        [Test]
        public void BackEmitterCannotIntercept_EvenWhenSegmentCrossesGuard()
        {
            ward.Arm(1);
            var query = new BeamGuardQuery(1, new Vector2(-4, 0), new Vector2(4, 0), .3f, new Vector2(-4, 0), true);
            Assert.AreEqual(WardInterceptionKind.None, ward.TryIntercept(query).Kind); Assert.IsTrue(ward.IsActive);
        }

        [TestCase(.85f, .2f, true)] [TestCase(.851f, .2f, false)]
        public void TangentUsesRealBeamThickness(float y, float thickness, bool intersects)
        {
            // No body collider: this isolates the tall guard's geometric tangent from player bounds.
            Object.DestroyImmediate(player.GetComponent<BoxCollider2D>());
            ward.Arm(1); var result = ward.TryIntercept(Query(y, thickness));
            Assert.AreEqual(intersects ? WardInterceptionKind.Countered : WardInterceptionKind.None, result.Kind);
        }

        [Test]
        public void TerrainClippedSegmentThatStopsBeforeGuard_CannotCounter()
        {
            ward.Arm(1); Assert.AreEqual(WardInterceptionKind.None, ward.TryIntercept(Query(endX: 1)).Kind);
            Assert.IsTrue(ward.IsActive);
        }

        [Test]
        public void BeamAlreadyAtPlayerBeforeGuard_CannotCounterRetroactively()
        {
            ward.Arm(1);
            var query = new BeamGuardQuery(1, Vector2.zero, new Vector2(4, 0), .3f, new Vector2(4, 0), true);
            Assert.AreEqual(WardInterceptionKind.None, ward.TryIntercept(query).Kind);
        }

        [Test]
        public void AirborneMovingRootCarriesGuard_WithCommittedFacingAndLiveGeometry()
        {
            ward.Arm(-1); player.transform.position = new Vector2(2, 5);
            JsonUtility.FromJsonOverwrite("{\"forwardOffset\":1,\"height\":2}", definition);
            Physics2D.SyncTransforms(); Assert.AreEqual(new Vector2(1, 5), ward.GuardCenter); Assert.AreEqual(-1, ward.FacingSign);
            var query = new BeamGuardQuery(1, new Vector2(-4, 5), new Vector2(4, 5), .3f, new Vector2(-4, 5), true);
            Assert.AreEqual(WardInterceptionKind.Countered, ward.TryIntercept(query).Kind);
        }

        [Test]
        public void DurationEditsPreserveElapsed_AndCannotRearmExpiredWard()
        {
            ward.Arm(1); ward.Tick(.4f); JsonUtility.FromJsonOverwrite("{\"duration\":1}", definition); ward.Tick(.2f);
            Assert.IsTrue(ward.IsActive); Assert.AreEqual(.6f, ward.Elapsed, .001f);
            JsonUtility.FromJsonOverwrite("{\"duration\":0.5}", definition); ward.Tick(0); Assert.IsFalse(ward.IsActive);
            JsonUtility.FromJsonOverwrite("{\"duration\":2}", definition); ward.Tick(.1f); Assert.IsFalse(ward.IsActive);
        }

        [Test]
        public void InvalidRevisionRetainsWholeLastValidConfiguration_AndNeverWritesAsset()
        {
            ward.Arm(1); JsonUtility.FromJsonOverwrite("{\"duration\":-1,\"forwardOffset\":9}", definition);
            var authored = JsonUtility.ToJson(definition);
            LogAssert.Expect(LogType.Warning, "Invalid live Ward configuration; retaining the last valid settings.");
            ward.Tick(.2f); Assert.AreEqual(new Vector2(.6f, 0), ward.GuardCenter); ward.Tick(.2f);
            Assert.AreEqual(authored, JsonUtility.ToJson(definition)); Assert.IsTrue(ward.IsActive);
        }

        [Test]
        public void PauseAndHoldFreezeTimerAndSuppressCounter_DeathClearsGuard()
        {
            ward.Arm(1); Time.timeScale = 0; ward.Tick(1); Assert.AreEqual(0, ward.Elapsed);
            Assert.AreEqual(WardInterceptionKind.None, ward.TryIntercept(Query()).Kind); Time.timeScale = 1;
            using (player.AddComponent<PlayerWorldHold>().Acquire())
            { ward.Tick(1); Assert.AreEqual(0, ward.Elapsed); Assert.AreEqual(WardInterceptionKind.None, ward.TryIntercept(Query()).Kind); }
            health.ApplyDamage(new DamageContext(null, player, null, 5, Vector2.zero, Vector2.right));
            ward.Tick(0); Assert.IsFalse(ward.IsActive); Assert.IsFalse(ward.CanArm);
        }

        [Test]
        public void ClearResetsGuardAndOldCastMemory_ForExplicitAreaOrRunReset()
        {
            ward.Arm(1); ward.TryIntercept(Query()); ward.Arm(1); ward.Clear(); Assert.IsFalse(ward.IsActive);
            ward.Arm(1); Assert.AreEqual(WardInterceptionKind.Countered, ward.TryIntercept(Query()).Kind);
        }
    }
}
