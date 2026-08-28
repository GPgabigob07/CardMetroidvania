using System;
using System.Collections.Generic;
using UnityEngine;

namespace TicGame.Architecture
{
    [Serializable]
    internal struct PlayerAnimationBinding
    {
        [Tooltip(tooltip: "Semantic animation state produced by the mapper.")]
        [SerializeField] private PlayerAnimationState state;

        [Tooltip(tooltip: "Animator state name played for this semantic state.")]
        [SerializeField] private string animatorStateName;

        public PlayerAnimationState State => state;
        public string AnimatorStateName => animatorStateName;
    }

    [RequireComponent(requiredComponent: typeof(Animator))]
    [RequireComponent(requiredComponent: typeof(SpriteRenderer))]
    public sealed class PlayerAnimationDriver : MonoBehaviour
    {
        [Header(header: "Components")]
        [Tooltip(tooltip: "Animator controlled exclusively by this driver.")]
        [SerializeField] private Animator animator;

        [Tooltip(tooltip: "Player controller that publishes resolved animation snapshots.")]
        [SerializeField] private PlayerController playerController;

        [Header(header: "Mapping")]
        [Min(min: 0f)]
        [Tooltip(tooltip: "Downward speed that selects the hard landing presentation.")]
        [SerializeField] private float hardLandingSpeed = 14f;

        [Min(min: 0f)]
        [Tooltip(tooltip: "Default cross-fade duration used by mapped commands.")]
        [SerializeField] private float crossFadeDuration;

        [Tooltip(tooltip: "Bindings from semantic animation states to Animator state names.")]
        [SerializeField] private List<PlayerAnimationBinding> bindings = new();

        private readonly Dictionary<PlayerAnimationState, int> stateHashes = new();
        private readonly HashSet<PlayerAnimationState> missingBindingWarnings = new();

        private IPlayerAnimationMapper mapper;
        private bool isSubscribed;
        private bool hasCurrentState;
        private PlayerAnimationState currentState;
        private bool hasPendingFallback;
        private PlayerAnimationState pendingFallback;
        private int pendingTransientHash;

        SpriteRenderer spriteRenderer;
        
        private void Awake()
        {
            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }

            if (playerController == null)
            {
                playerController = GetComponentInParent<PlayerController>();
            }
            
            spriteRenderer = GetComponent<SpriteRenderer>();

            mapper = new PlayerAnimationMapper(
                hardLandingSpeed: hardLandingSpeed,
                crossFadeDuration: crossFadeDuration);
            BuildBindingLookup();
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void Start()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
            hasPendingFallback = false;
        }

        private void Update()
        {
            if (!hasPendingFallback || animator == null)
            {
                return;
            }

            var stateInfo = animator.GetCurrentAnimatorStateInfo(layerIndex: 0);
            if (stateHashes.TryGetValue(key: pendingFallback, value: out var fallbackHash)
                && stateInfo.shortNameHash == fallbackHash)
            {
                hasPendingFallback = false;
                hasCurrentState = true;
                currentState = pendingFallback;
                return;
            }

            if (stateInfo.shortNameHash != pendingTransientHash || stateInfo.normalizedTime < 1f)
            {
                return;
            }

            hasPendingFallback = false;
            PlayState(
                state: pendingFallback,
                crossFade: crossFadeDuration,
                restart: false);
        }

        private void Subscribe()
        {
            if (isSubscribed
                || playerController == null
                || playerController.AnimationSnapshots == null)
            {
                return;
            }

            playerController.AnimationSnapshots.Changed += HandleSnapshotChanged;
            isSubscribed = true;

            if (playerController.AnimationSnapshots.HasCurrent)
            {
                HandleSnapshotChanged(new PlayerAnimationTransition(
                    previous: default,
                    current: playerController.AnimationSnapshots.Current,
                    hasPrevious: false));
            }
        }

        private void Unsubscribe()
        {
            if (!isSubscribed || playerController?.AnimationSnapshots == null)
            {
                return;
            }

            playerController.AnimationSnapshots.Changed -= HandleSnapshotChanged;
            isSubscribed = false;
        }

        private void HandleSnapshotChanged(PlayerAnimationTransition transition)
        {
            var command = mapper.Map(transition: transition);
            if (!PlayState(
                    state: command.State,
                    crossFade: command.CrossFadeDuration,
                    restart: command.Restart))
            {
                return;
            }

            hasPendingFallback = command.HasFallback;
            pendingFallback = command.FallbackState;
            pendingTransientHash = stateHashes[command.State];
        }

        private bool PlayState(
            PlayerAnimationState state,
            float crossFade,
            bool restart)
        {
            if (animator == null || !stateHashes.TryGetValue(key: state, value: out var stateHash))
            {
                WarnMissingBinding(state: state);
                return false;
            }

            if (hasCurrentState && currentState == state && !restart)
            {
                return true;
            }

            hasCurrentState = true;
            currentState = state;
            hasPendingFallback = false;

            if (crossFade > 0f)
            {
                animator.CrossFade(
                    stateHashName: stateHash,
                    normalizedTransitionDuration: crossFade,
                    layer: 0,
                    normalizedTimeOffset: 0f);
            }
            else
            {
                animator.Play(
                    stateNameHash: stateHash,
                    layer: 0,
                    normalizedTime: 0f);
            }

            return true;
        }

        private void BuildBindingLookup()
        {
            stateHashes.Clear();

            foreach (var binding in bindings)
            {
                if (string.IsNullOrWhiteSpace(value: binding.AnimatorStateName))
                {
                    continue;
                }

                stateHashes[binding.State] = Animator.StringToHash(name: binding.AnimatorStateName);
            }
        }

        private void WarnMissingBinding(PlayerAnimationState state)
        {
            if (!missingBindingWarnings.Add(item: state))
            {
                return;
            }

            Debug.LogWarning(
                message: $"No Animator binding is configured for player animation state '{state}'.",
                context: this);
        }
    }
}
