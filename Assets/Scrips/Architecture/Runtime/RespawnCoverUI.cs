using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace TicGame.Architecture
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class RespawnCoverUI : MonoBehaviour
    {
        [Header("Presentation")]
        [Tooltip("Canvas group that fades the full-screen respawn cover.")]
        [SerializeField] private CanvasGroup group;

        [Tooltip("Unscaled duration of each fade direction.")]
        [SerializeField, Min(0f)] private float fadeSeconds = 0.2f;

        private readonly List<PendingWait> waits = new();
        private TaskCompletionSource<bool> fadeCompletion;
        private float fadeTarget;

        public float FadeSeconds => fadeSeconds;
        public bool IsOpaque => group != null && group.alpha >= 1f;

        private void Awake()
        {
            group ??= GetComponent<CanvasGroup>();
            SetAlpha(0f);
        }

        private void Update()
        {
            UpdateFade();
            UpdateWaits();
        }

        private void OnDisable()
        {
            CompletePendingOperations();
        }

        private void OnDestroy()
        {
            CompletePendingOperations();
        }

        public Task FadeToOpaqueAsync() => FadeToAsync(1f);

        public Task FadeToClearAsync() => FadeToAsync(0f);

        public Task WaitUnscaledAsync(float seconds)
        {
            if (seconds <= 0f)
            {
                return Task.CompletedTask;
            }

            var completion = new TaskCompletionSource<bool>();
            waits.Add(new PendingWait(seconds, completion));
            return completion.Task;
        }

        private Task FadeToAsync(float target)
        {
            group ??= GetComponent<CanvasGroup>();
            if (group == null)
            {
                return Task.CompletedTask;
            }

            fadeCompletion?.TrySetResult(true);
            fadeCompletion = null;
            fadeTarget = target;
            if (Mathf.Approximately(group.alpha, fadeTarget) || fadeSeconds <= 0f)
            {
                SetAlpha(fadeTarget);
                return Task.CompletedTask;
            }

            fadeCompletion = new TaskCompletionSource<bool>();
            return fadeCompletion.Task;
        }

        private void UpdateFade()
        {
            if (fadeCompletion == null || group == null)
            {
                return;
            }

            var step = Time.unscaledDeltaTime / fadeSeconds;
            group.alpha = Mathf.MoveTowards(group.alpha, fadeTarget, step);
            if (!Mathf.Approximately(group.alpha, fadeTarget))
            {
                return;
            }

            var completion = fadeCompletion;
            fadeCompletion = null;
            completion.TrySetResult(true);
        }

        private void UpdateWaits()
        {
            for (var index = waits.Count - 1; index >= 0; index--)
            {
                var pending = waits[index];
                pending.RemainingSeconds -= Time.unscaledDeltaTime;
                if (pending.RemainingSeconds > 0f)
                {
                    waits[index] = pending;
                    continue;
                }

                waits.RemoveAt(index);
                pending.Completion.TrySetResult(true);
            }
        }

        private void CompletePendingOperations()
        {
            fadeCompletion?.TrySetResult(true);
            fadeCompletion = null;
            foreach (var pending in waits)
            {
                pending.Completion.TrySetResult(true);
            }

            waits.Clear();
        }

        private void SetAlpha(float alpha)
        {
            if (group != null)
            {
                group.alpha = alpha;
            }
        }

        private struct PendingWait
        {
            public PendingWait(float remainingSeconds, TaskCompletionSource<bool> completion)
            {
                RemainingSeconds = remainingSeconds;
                Completion = completion;
            }

            public float RemainingSeconds { get; set; }
            public TaskCompletionSource<bool> Completion { get; }
        }
    }
}
