using System.Collections.Generic;
using UnityEngine;

namespace TicGame.Architecture
{
    [RequireComponent(requiredComponent: typeof(Animator))]
    public sealed class PlayerAttackAnimationDriver : MonoBehaviour
    {
        [Header(header: "Components")]
        [Tooltip(tooltip: "Attack rig Animator controlled by this driver.")]
        [SerializeField] private Animator animator;

        [Tooltip(tooltip: "Player controller that publishes resolved animation snapshots.")]
        [SerializeField] private PlayerController playerController;

        [Header(header: "Playback")]
        [Min(min: 0f)]
        [Tooltip(tooltip: "Cross-fade duration used when changing attack presentation states.")]
        [SerializeField] private float crossFadeDuration;

        [Tooltip(tooltip: "Mirrors the rig on X when the player faces right.")]
        [SerializeField] private bool mirrorWhenFacingRight = true;

        private readonly Dictionary<PlayerAnimationState, int> stateHashes = new();
        private readonly HashSet<PlayerAnimationState> missingStateWarnings = new();
        private readonly PlayerAttackAnimationMapper mapper = new();

        private bool isSubscribed;
        private bool hasCurrentState;
        private PlayerAnimationState currentState;
        private float scaleMagnitudeX;

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

            scaleMagnitudeX = Mathf.Abs(f: transform.localScale.x);
            BuildStateLookup();
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
                ApplySnapshot(snapshot: playerController.AnimationSnapshots.Current);
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
            ApplySnapshot(snapshot: transition.Current);
        }

        private void ApplySnapshot(PlayerAnimationSnapshot snapshot)
        {
            ApplyFacing(direction: snapshot.FacingDirection);
            PlayState(state: mapper.Map(snapshot: snapshot));
        }

        private void ApplyFacing(int direction)
        {
            var rightFacingSign = mirrorWhenFacingRight ? -1f : 1f;
            var facingSign = direction < 0 ? -rightFacingSign : rightFacingSign;
            var scale = transform.localScale;
            scale.x = scaleMagnitudeX * facingSign;
            transform.localScale = scale;
        }

        private void PlayState(PlayerAnimationState state)
        {
            if (animator == null || !stateHashes.TryGetValue(key: state, value: out var stateHash))
            {
                return;
            }

            if (!animator.HasState(layerIndex: 0, stateID: stateHash))
            {
                WarnMissingState(state: state);
                return;
            }

            if (hasCurrentState && currentState == state)
            {
                return;
            }

            hasCurrentState = true;
            currentState = state;

            if (crossFadeDuration > 0f)
            {
                animator.CrossFade(
                    stateHashName: stateHash,
                    normalizedTransitionDuration: crossFadeDuration,
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
        }

        private void BuildStateLookup()
        {
            stateHashes.Clear();
            AddState(state: PlayerAnimationState.Idle, animatorStateName: "Idling");
            AddState(state: PlayerAnimationState.Attack1Reading, animatorStateName: "Attack1_WindUp");
            AddState(state: PlayerAnimationState.Attack1Execution, animatorStateName: "Attack1_Execution");
            AddState(state: PlayerAnimationState.Attack1Recovery, animatorStateName: "Attack1_Recovery");
            AddState(state: PlayerAnimationState.Attack2Reading, animatorStateName: "Attack2_WindUp");
            AddState(state: PlayerAnimationState.Attack2Execution, animatorStateName: "Attack2_Execution");
            AddState(state: PlayerAnimationState.Attack2Recovery, animatorStateName: "Attack2_Recovery");
            AddState(state: PlayerAnimationState.Attack3Reading, animatorStateName: "Attack3_WindUp");
            AddState(state: PlayerAnimationState.Attack3Execution, animatorStateName: "Attack3_Execution");
            AddState(state: PlayerAnimationState.Attack3Recovery, animatorStateName: "Attack3_Recovery");
        }

        private void AddState(PlayerAnimationState state, string animatorStateName)
        {
            stateHashes[state] = Animator.StringToHash(name: $"Base Layer.{animatorStateName}");
        }

        private void WarnMissingState(PlayerAnimationState state)
        {
            if (!missingStateWarnings.Add(item: state))
            {
                return;
            }

            Debug.LogError(
                message: $"Attack Animator has no state configured for '{state}'.",
                context: this);
        }
    }
}
