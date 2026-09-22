using System;
using System.Collections;
using System.Reflection;
using System.Threading.Tasks;
using NUnit.Framework;

namespace TicGame.Architecture.Tests
{
    public sealed class DirectionalSceneStreamingTests
    {
        [Test]
        public void FirstContactAndRepeatedContactDoNotRequestAnOperation()
        {
            var tracker = new SceneCrossingTracker();
            Assert.AreEqual(SceneFlowAction.None, tracker.Enter(SceneTriggerSide.A, SceneFlowAction.Unload, SceneFlowAction.Load));
            Assert.AreEqual(SceneFlowAction.None, tracker.Enter(SceneTriggerSide.A, SceneFlowAction.Unload, SceneFlowAction.Load));
            Assert.AreEqual(SceneFlowAction.Unload, tracker.Enter(SceneTriggerSide.B, SceneFlowAction.Unload, SceneFlowAction.Load));
            Assert.AreEqual(SceneFlowAction.Load, tracker.Enter(SceneTriggerSide.A, SceneFlowAction.Unload, SceneFlowAction.Load));
        }

        [Test]
        public void ReverseEntryAndNoneActionAreSupported()
        {
            var tracker = new SceneCrossingTracker();
            Assert.AreEqual(SceneFlowAction.None, tracker.Enter(SceneTriggerSide.B, SceneFlowAction.None, SceneFlowAction.Load));
            Assert.AreEqual(SceneFlowAction.Load, tracker.Enter(SceneTriggerSide.A, SceneFlowAction.None, SceneFlowAction.Load));
            Assert.AreEqual(SceneFlowAction.None, tracker.Enter(SceneTriggerSide.B, SceneFlowAction.None, SceneFlowAction.Load));
        }

        [Test]
        public void ReversalWaitsForLoadThenUnloadsWithoutConcurrentOperations()
        {
            var loaded = false;
            var count = 0;
            var requestedLoad = false;
            Action<Exception> complete = null;
            var request = new SceneLoadRequest(() => loaded, (load, callback) =>
            {
                count++;
                requestedLoad = load;
                complete = callback;
            });
            request.Request(false);
            Assert.AreEqual(0, count);
            request.Request(true);
            request.Request(true);
            request.Request(false);
            Assert.AreEqual(1, count);
            loaded = true;
            complete(null);
            Assert.AreEqual(2, count);
            Assert.IsFalse(requestedLoad);
            loaded = false;
            complete(null);
            Assert.IsFalse(request.IsBusy);
        }

        [Test]
        public void FailureStopsRetryLoopAndAllowsExplicitRetry()
        {
            var count = 0;
            var request = new SceneLoadRequest(() => false, (_, complete) =>
            {
                count++;
                complete(new InvalidOperationException("unavailable"));
            });
            request.Request(true);
            Assert.AreEqual(1, count);
            Assert.IsFalse(request.IsBusy);
            Assert.AreEqual("unavailable", request.LastError);
            request.Request(true);
            Assert.AreEqual(2, count);
        }

        [Test]
        public void OppositeRequestSupersedesCallerUntilThePhysicalOperationCanReverse()
        {
            var loaded = false;
            Action<Exception> complete = null;
            var request = new SceneLoadRequest(() => loaded, (_, callback) => complete = callback);

            var first = request.RequestAsync(true);
            var reverse = request.RequestAsync(false);

            Assert.AreEqual(SceneRequestOutcome.Superseded, first.Result.Outcome);
            Assert.IsFalse(reverse.IsCompleted);
            loaded = true;
            complete(null);
            loaded = false;
            complete(null);
            Assert.AreEqual(SceneRequestOutcome.Succeeded, reverse.Result.Outcome);
        }

        [Test]
        public void ThreeDirectionChangesSupersedeEarlierCallersAndFinishTheLatestRequest()
        {
            var loaded = false;
            var count = 0;
            Action<Exception> complete = null;
            var request = new SceneLoadRequest(() => loaded, (_, callback) =>
            {
                count++;
                complete = callback;
            });

            var first = request.RequestAsync(true);
            var second = request.RequestAsync(false);
            var third = request.RequestAsync(true);
            var fourth = request.RequestAsync(false);

            Assert.AreEqual(SceneRequestOutcome.Superseded, first.Result.Outcome);
            Assert.AreEqual(SceneRequestOutcome.Superseded, second.Result.Outcome);
            Assert.AreEqual(SceneRequestOutcome.Superseded, third.Result.Outcome);
            Assert.AreEqual(1, count);
            loaded = true;
            complete(null);
            Assert.AreEqual(2, count);
            loaded = false;
            complete(null);
            Assert.AreEqual(SceneRequestOutcome.Succeeded, fourth.Result.Outcome);
        }

        [Test]
        public void MatchingRequestsJoinOneOperationAndCompleteTogether()
        {
            var loaded = false;
            var count = 0;
            Action<Exception> complete = null;
            var request = new SceneLoadRequest(() => loaded, (_, callback) =>
            {
                count++;
                complete = callback;
            });

            var first = request.RequestAsync(true);
            var second = request.RequestAsync(true);

            Assert.AreEqual(1, count);
            loaded = true;
            complete(null);
            Assert.AreEqual(SceneRequestOutcome.Succeeded, first.Result.Outcome);
            Assert.AreEqual(SceneRequestOutcome.Succeeded, second.Result.Outcome);
        }

        [Test]
        public void ReentrantRequestAfterSupersessionKeepsTheNewestTarget()
        {
            var loaded = false;
            Action<Exception> complete = null;
            var request = new SceneLoadRequest(() => loaded, (_, callback) => complete = callback);
            var first = request.RequestAsync(true);
            Task<SceneRequestResult> reentrant = null;
            first.ContinueWith(_ => reentrant = request.RequestAsync(true), TaskContinuationOptions.ExecuteSynchronously);

            var reverse = request.RequestAsync(false);

            Assert.AreEqual(SceneRequestOutcome.Superseded, first.Result.Outcome);
            Assert.AreEqual(SceneRequestOutcome.Superseded, reverse.Result.Outcome);
            loaded = true;
            complete(null);
            Assert.AreEqual(SceneRequestOutcome.Succeeded, reentrant.Result.Outcome);
        }

        [Test]
        public void SynchronousFailureReturnsFailedAndAnExplicitRetryCanSucceed()
        {
            var loaded = false;
            var attempts = 0;
            var request = new SceneLoadRequest(() => loaded, (_, complete) =>
            {
                attempts++;
                if (attempts == 1) throw new InvalidOperationException("unavailable");
                loaded = true;
                complete(null);
            });

            var failed = request.RequestAsync(true);
            var retry = request.RequestAsync(true);

            Assert.AreEqual(SceneRequestOutcome.Failed, failed.Result.Outcome);
            Assert.AreEqual("unavailable", failed.Result.Error);
            Assert.AreEqual(SceneRequestOutcome.Succeeded, retry.Result.Outcome);
            Assert.AreEqual(2, attempts);
        }

        [Test]
        public void IdleWaitIncludesTheOperationQueuedByTheCurrentCompletion()
        {
            var loaded = false;
            Action<Exception> complete = null;
            var request = new SceneLoadRequest(() => loaded, (_, callback) => complete = callback);

            request.RequestAsync(true);
            var idle = request.WaitForIdleAsync();
            request.RequestAsync(false);
            loaded = true;
            complete(null);

            Assert.IsFalse(idle.IsCompleted);
            loaded = false;
            complete(null);
            Assert.IsTrue(idle.IsCompleted);
        }

        [Test]
        public void FailedOperationSettlesIdleWithoutStartingAnAutomaticRetry()
        {
            var attempts = 0;
            var request = new SceneLoadRequest(() => false, (_, complete) =>
            {
                attempts++;
                complete(new InvalidOperationException("unavailable"));
            });

            var result = request.RequestAsync(true);
            var idle = request.WaitForIdleAsync();

            Assert.AreEqual(SceneRequestOutcome.Failed, result.Result.Outcome);
            Assert.IsTrue(idle.IsCompleted);
            Assert.AreEqual(1, attempts);
        }

        [Test]
        public void AlreadySatisfiedRequestCompletesWithoutStartingAnOperation()
        {
            var count = 0;
            var request = new SceneLoadRequest(() => false, (_, _) => count++);

            var result = request.RequestAsync(false);

            Assert.IsTrue(result.IsCompleted);
            Assert.AreEqual(SceneRequestOutcome.Succeeded, result.Result.Outcome);
            Assert.AreEqual(0, count);
        }

        [Test]
        public void ProtectedSceneRequiresEveryLeaseToReleaseAndResetsForTheNextSession()
        {
            const string path = "Assets/Scenes/Gameplay.unity";
            ResetSceneStreamingService();
            var first = SceneStreamingService.ProtectScene(path);
            var second = SceneStreamingService.ProtectScene(path);

            Assert.AreEqual(SceneRequestOutcome.Failed, SceneStreamingService.RequestAsync(path, false).Result.Outcome);
            first.Dispose();
            Assert.AreEqual(SceneRequestOutcome.Failed, SceneStreamingService.RequestAsync(path, false).Result.Outcome);
            second.Dispose();
            Assert.IsFalse(SceneStreamingService.IsSceneProtected(path));

            SceneStreamingService.ProtectScene(path);
            ResetSceneStreamingService();
            Assert.IsFalse(SceneStreamingService.IsSceneProtected(path));
        }

        [Test]
        public void GlobalIdleWaitIncludesAPathRegisteredByACompletionContinuation()
        {
            ResetSceneStreamingService();
            var firstLoaded = false;
            var secondLoaded = false;
            Action<Exception> completeFirst = null;
            Action<Exception> completeSecond = null;
            var notifyIdle = GlobalIdleNotifier();
            var first = new SceneLoadRequest(() => firstLoaded, (_, complete) => completeFirst = complete, notifyIdle);
            RegisterRequest("Assets/Scenes/First.unity", first);
            var firstResult = first.RequestAsync(true);
            Task<SceneRequestResult> secondResult = null;

            firstResult.ContinueWith(_ =>
            {
                var second = new SceneLoadRequest(() => secondLoaded, (_, complete) => completeSecond = complete, notifyIdle);
                RegisterRequest("Assets/Scenes/Second.unity", second);
                secondResult = second.RequestAsync(true);
            }, TaskContinuationOptions.ExecuteSynchronously);
            var idle = SceneStreamingService.WaitForIdleAsync();

            firstLoaded = true;
            completeFirst(null);

            Assert.IsFalse(idle.IsCompleted);
            secondLoaded = true;
            completeSecond(null);
            Assert.IsTrue(secondResult.IsCompleted);
            Assert.IsTrue(idle.IsCompleted);
        }

        private static void ResetSceneStreamingService()
        {
            var reset = typeof(SceneStreamingService).GetMethod("Reset", BindingFlags.NonPublic | BindingFlags.Static);
            reset.Invoke(null, null);
        }

        private static Action GlobalIdleNotifier()
        {
            var complete = typeof(SceneStreamingService).GetMethod("CompleteGlobalIdleWaiters", BindingFlags.NonPublic | BindingFlags.Static);
            return () => complete.Invoke(null, null);
        }

        private static void RegisterRequest(string path, SceneLoadRequest request)
        {
            var field = typeof(SceneStreamingService).GetField("requests", BindingFlags.NonPublic | BindingFlags.Static);
            var requests = (IDictionary)field.GetValue(null);
            requests.Add(path, request);
        }
    }
}
