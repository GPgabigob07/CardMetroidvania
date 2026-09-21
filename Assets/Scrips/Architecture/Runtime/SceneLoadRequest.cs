using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TicGame.Architecture
{
    // Serializes operations for one scene; the latest request wins after an in-flight operation.
    public sealed class SceneLoadRequest
    {
        private readonly Func<bool> isLoaded;
        private readonly Action<bool, Action<Exception>> beginOperation;
        private readonly Action idleReached;
        private readonly List<TaskCompletionSource<SceneRequestResult>> requestWaiters = new();
        private readonly List<TaskCompletionSource<object>> idleWaiters = new();
        private bool desiredLoaded;

        public bool IsBusy { get; private set; }
        public string LastError { get; private set; }

        public SceneLoadRequest(
            Func<bool> isLoaded,
            Action<bool, Action<Exception>> beginOperation,
            Action idleReached = null)
        {
            this.isLoaded = isLoaded;
            this.beginOperation = beginOperation;
            this.idleReached = idleReached;
        }

        public void Request(bool loaded)
        {
            RequestAsync(loaded);
        }

        public Task<SceneRequestResult> RequestAsync(bool loaded)
        {
            var waiter = new TaskCompletionSource<SceneRequestResult>();
            TaskCompletionSource<SceneRequestResult>[] superseded = null;
            if (IsBusy && desiredLoaded != loaded)
            {
                superseded = requestWaiters.ToArray();
                requestWaiters.Clear();
            }
            desiredLoaded = loaded;
            LastError = null;
            requestWaiters.Add(waiter);
            CompleteWaiters(superseded, SceneRequestOutcome.Superseded, null);
            Reconcile();
            return waiter.Task;
        }

        public Task WaitForIdleAsync()
        {
            if (!IsBusy) return Task.CompletedTask;
            var waiter = new TaskCompletionSource<object>();
            idleWaiters.Add(waiter);
            return waiter.Task;
        }

        private void Reconcile()
        {
            if (IsBusy) return;
            if (isLoaded() == desiredLoaded)
            {
                CompleteRequests(SceneRequestOutcome.Succeeded, null);
                CompleteIdleWaiters();
                return;
            }
            IsBusy = true;
            try
            {
                beginOperation(desiredLoaded, Complete);
            }
            catch (Exception error)
            {
                Complete(error);
            }
        }

        private void Complete(Exception error)
        {
            IsBusy = false;
            if (error != null)
            {
                LastError = error.Message;
                CompleteRequests(SceneRequestOutcome.Failed, LastError);
                CompleteIdleWaiters();
                return;
            }
            Reconcile();
            if (!IsBusy) CompleteIdleWaiters();
        }

        private void CompleteRequests(SceneRequestOutcome outcome, string error)
        {
            if (requestWaiters.Count == 0) return;
            var waiters = requestWaiters.ToArray();
            requestWaiters.Clear();
            CompleteWaiters(waiters, outcome, error);
        }

        private static void CompleteWaiters(
            IEnumerable<TaskCompletionSource<SceneRequestResult>> waiters,
            SceneRequestOutcome outcome,
            string error)
        {
            if (waiters == null) return;
            var result = new SceneRequestResult(outcome, error);
            foreach (var waiter in waiters) waiter.TrySetResult(result);
        }

        private void CompleteIdleWaiters()
        {
            if (IsBusy) return;
            if (idleWaiters.Count > 0)
            {
                var waiters = idleWaiters.ToArray();
                idleWaiters.Clear();
                foreach (var waiter in waiters) waiter.TrySetResult(null);
            }
            idleReached?.Invoke();
        }
    }
}
