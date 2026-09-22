using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;

namespace TicGame.Architecture.Tests
{
    public sealed class GameplayRespawnTests
    {
        private readonly List<UnityEngine.Object> createdObjects = new();

        [TearDown]
        public void TearDown()
        {
            foreach (var created in createdObjects)
            {
                if (created != null)
                {
                    UnityEngine.Object.DestroyImmediate(created);
                }
            }

            createdObjects.Clear();
            ResetSceneStreamingService();
        }

        [Test]
        public async Task RespawnSequenceRunsTheWorldReconciliationInOrder()
        {
            var trace = new List<string>();
            var sequence = new GameplayAreaCoordinator.RespawnSequence(
                hold: () => trace.Add("hold"),
                suspendTriggers: () => new TraceLease(trace, "suspend-triggers", "resume-triggers"),
                settleRequests: () => Complete(trace, "settle-requests"),
                unloadOtherAreas: () => Complete(trace, "unload-other-areas"),
                loadDestination: () => Complete(trace, "load-destination"),
                restoreProgress: () => Complete(trace, "restore-progress"),
                resolveSpawn: () => Complete(trace, "resolve-spawn"),
                teleport: () => trace.Add("teleport"),
                restoreHealth: () => trace.Add("restore-health"),
                resetCrossings: () => trace.Add("reset-crossings"),
                releaseHold: () => trace.Add("release-hold"));

            var respawned = await sequence.RunAsync();

            Assert.IsTrue(respawned);
            CollectionAssert.AreEqual(new[]
            {
                "hold", "suspend-triggers", "settle-requests", "unload-other-areas",
                "load-destination", "restore-progress", "resolve-spawn", "teleport",
                "restore-health", "reset-crossings", "resume-triggers", "release-hold"
            }, trace);
        }

        [Test]
        public async Task RespawnSequenceKeepsThePlayerHeldWhenLoadingFails()
        {
            var trace = new List<string>();
            var sequence = new GameplayAreaCoordinator.RespawnSequence(
                hold: () => trace.Add("hold"),
                suspendTriggers: () => new TraceLease(trace, "suspend-triggers", "resume-triggers"),
                settleRequests: () => Complete(trace, "settle-requests"),
                unloadOtherAreas: () => Complete(trace, "unload-other-areas"),
                loadDestination: () => Fail(trace, "load-destination"),
                restoreProgress: () => Complete(trace, "restore-progress"),
                resolveSpawn: () => Complete(trace, "resolve-spawn"),
                teleport: () => trace.Add("teleport"),
                restoreHealth: () => trace.Add("restore-health"),
                resetCrossings: () => trace.Add("reset-crossings"),
                releaseHold: () => trace.Add("release-hold"));

            var respawned = await sequence.RunAsync();

            Assert.IsFalse(respawned);
            CollectionAssert.AreEqual(new[]
            {
                "hold", "suspend-triggers", "settle-requests", "unload-other-areas",
                "load-destination", "resume-triggers"
            }, trace);
        }

        [Test]
        public void DirectionalRequestsAreRejectedOnlyWhileSuspended()
        {
            ResetSceneStreamingService();
            Assert.IsTrue(SceneStreamingService.DirectionalRequestsAllowed);

            var first = SceneStreamingService.SuspendDirectionalRequests();
            var second = SceneStreamingService.SuspendDirectionalRequests();
            Assert.IsFalse(SceneStreamingService.DirectionalRequestsAllowed);

            first.Dispose();
            Assert.IsFalse(SceneStreamingService.DirectionalRequestsAllowed);
            second.Dispose();
            Assert.IsTrue(SceneStreamingService.DirectionalRequestsAllowed);
        }

        [Test]
        public void SuspendedTriggerCrossingDoesNotSeedDirectionHistory()
        {
            var triggerObject = CreateObject("Trigger");
            var trigger = triggerObject.AddComponent<DirectionalSceneTrigger>();
            var volumeA = CreateObject("Volume A").AddComponent<SceneTriggerVolume>();
            var volumeB = CreateObject("Volume B").AddComponent<SceneTriggerVolume>();
            trigger.ConfigureVolumes(volumeA, volumeB);
            var player = CreateObject("Player").AddComponent<PlayerController>();

            using (SceneStreamingService.SuspendDirectionalRequests())
            {
                trigger.Enter(volumeA, player);
            }

            var crossings = (System.Collections.IDictionary)typeof(DirectionalSceneTrigger)
                .GetField("crossings", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(trigger);
            Assert.AreEqual(0, crossings.Count);

            trigger.Enter(volumeA, player);
            Assert.AreEqual(1, crossings.Count);
        }

        [Test]
        public void ResetCrossingClearsEveryColliderContactForThePlayer()
        {
            var triggerObject = CreateObject("Trigger");
            var trigger = triggerObject.AddComponent<DirectionalSceneTrigger>();
            var volumeA = CreateObject("Volume A").AddComponent<SceneTriggerVolume>();
            var volumeB = CreateObject("Volume B").AddComponent<SceneTriggerVolume>();
            trigger.ConfigureVolumes(volumeA, volumeB);
            var playerObject = CreateObject("Player");
            playerObject.AddComponent<PlayerController>();
            var firstCollider = playerObject.AddComponent<BoxCollider2D>();
            var secondCollider = playerObject.AddComponent<CircleCollider2D>();

            InvokeTriggerEnter(volumeA, firstCollider);
            InvokeTriggerEnter(volumeA, secondCollider);
            InvokeTriggerEnter(volumeB, firstCollider);
            InvokeTriggerEnter(volumeB, secondCollider);
            trigger.ResetCrossing();

            Assert.AreEqual(0, GetContactCount(volumeA));
            Assert.AreEqual(0, GetContactCount(volumeB));
        }

        [Test]
        public void CancelAndReconfigureFenceAPendingRespawnFromTheNextSession()
        {
            var coordinator = CreateObject("Coordinator").AddComponent<GameplayAreaCoordinator>();
            var pending = new TaskCompletionSource<bool>();
            SetPrivateField(coordinator, "sessionGeneration", 4);
            SetPrivateField(coordinator, "respawnTask", pending.Task);
            SetPrivateField(coordinator, "respawnTaskGeneration", 4);

            coordinator.CancelSession();
            coordinator.Configure(null, null, null, null, null, null);
            var next = coordinator.RespawnAsync();

            Assert.AreNotSame(pending.Task, next);
        }

        [Test]
        public void ConcurrentRespawnCallsInTheSameSessionJoinThePendingOperation()
        {
            var coordinator = CreateObject("Coordinator").AddComponent<GameplayAreaCoordinator>();
            var pending = new TaskCompletionSource<bool>();
            SetPrivateField(coordinator, "sessionGeneration", 4);
            SetPrivateField(coordinator, "respawnTask", pending.Task);
            SetPrivateField(coordinator, "respawnTaskGeneration", 4);

            Assert.AreSame(pending.Task, coordinator.RespawnAsync());
            Assert.AreSame(pending.Task, coordinator.RespawnAsync());
        }

        [Test]
        public async Task FailedRespawnCanBeRetriedWithoutTeleportingOnTheFailure()
        {
            var trace = new List<string>();
            var failed = CreateSequence(trace, () => Fail(trace, "resolve-spawn"));

            Assert.IsFalse(await failed.RunAsync());
            CollectionAssert.DoesNotContain(trace, "teleport");
            CollectionAssert.DoesNotContain(trace, "release-hold");

            trace.Clear();
            var retried = CreateSequence(trace, () => Complete(trace, "resolve-spawn"));
            Assert.IsTrue(await retried.RunAsync());
            CollectionAssert.Contains(trace, "teleport");
            CollectionAssert.Contains(trace, "release-hold");
        }

        [Test]
        public async Task RespawnSequenceDoesNotTeleportWhenTheSpawnDisappearsAfterProgressRestoration()
        {
            var trace = new List<string>();
            var spawnStillExists = true;
            var sequence = new GameplayAreaCoordinator.RespawnSequence(
                () => trace.Add("hold"),
                () => new TraceLease(trace, "suspend-triggers", "resume-triggers"),
                () => Complete(trace, "settle-requests"),
                () => Complete(trace, "unload-other-areas"),
                () => Complete(trace, "load-destination"),
                () =>
                {
                    trace.Add("restore-progress");
                    spawnStillExists = false;
                    return Task.FromResult(true);
                },
                () => Task.FromResult(spawnStillExists),
                () => trace.Add("teleport"),
                () => trace.Add("restore-health"),
                () => trace.Add("reset-crossings"),
                () => trace.Add("release-hold"));

            Assert.IsFalse(await sequence.RunAsync());
            CollectionAssert.DoesNotContain(trace, "teleport");
            CollectionAssert.DoesNotContain(trace, "release-hold");
        }

        private static GameplayAreaCoordinator.RespawnSequence CreateSequence(
            ICollection<string> trace,
            Func<Task<bool>> resolveSpawn)
        {
            return new GameplayAreaCoordinator.RespawnSequence(
                () => trace.Add("hold"),
                () => new TraceLease(trace, "suspend-triggers", "resume-triggers"),
                () => Complete(trace, "settle-requests"),
                () => Complete(trace, "unload-other-areas"),
                () => Complete(trace, "load-destination"),
                () => Complete(trace, "restore-progress"),
                resolveSpawn,
                () => trace.Add("teleport"),
                () => trace.Add("restore-health"),
                () => trace.Add("reset-crossings"),
                () => trace.Add("release-hold"));
        }

        private GameObject CreateObject(string name)
        {
            var gameObject = new GameObject(name);
            createdObjects.Add(gameObject);
            return gameObject;
        }

        private static void InvokeTriggerEnter(SceneTriggerVolume volume, Collider2D collider)
        {
            typeof(SceneTriggerVolume).GetMethod("OnTriggerEnter2D", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(volume, new object[] { collider });
        }

        private static int GetContactCount(SceneTriggerVolume volume)
        {
            return ((System.Collections.IDictionary)typeof(SceneTriggerVolume)
                .GetField("contacts", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(volume)).Count;
        }

        private static void SetPrivateField(GameplayAreaCoordinator coordinator, string name, object value)
        {
            typeof(GameplayAreaCoordinator).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(coordinator, value);
        }

        private static Task<bool> Complete(ICollection<string> trace, string step)
        {
            trace.Add(step);
            return Task.FromResult(true);
        }

        private static Task<bool> Fail(ICollection<string> trace, string step)
        {
            trace.Add(step);
            return Task.FromResult(false);
        }

        private static void ResetSceneStreamingService()
        {
            typeof(SceneStreamingService).GetMethod("Reset", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                .Invoke(null, null);
        }

        private sealed class TraceLease : IDisposable
        {
            private readonly ICollection<string> trace;
            private readonly string release;

            public TraceLease(ICollection<string> trace, string acquire, string release)
            {
                this.trace = trace;
                this.release = release;
                trace.Add(acquire);
            }

            public void Dispose() => trace.Add(release);
        }
    }
}
