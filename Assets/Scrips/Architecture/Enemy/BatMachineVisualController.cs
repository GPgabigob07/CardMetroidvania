using System;
using System.Collections.Generic;
using UnityEngine;

namespace TicGame.Architecture
{
    [Serializable]
    public sealed class BatMachineVisualStateBinding
    {
        [Tooltip(tooltip: "Bat gameplay state represented by this seven-frame visual clip.")]
        [SerializeField] private BatMachineState state;

        [Tooltip(tooltip: "Exactly seven right-facing frames for this state.")]
        [SerializeField] private Sprite[] frames = new Sprite[7];

        public BatMachineState State => state;
        public IReadOnlyList<Sprite> Frames => frames;

        public void Configure(BatMachineState value, Sprite[] valueFrames)
        {
            state = value;
            frames = valueFrames ?? Array.Empty<Sprite>();
        }
    }

    [RequireComponent(requiredComponent: typeof(SpriteRenderer))]
    public sealed class BatMachineVisualController : MonoBehaviour
    {
        private const int RequiredFrameCount = 7;

        [Header(header: "Dependencies")]
        [Tooltip(tooltip: "Gameplay state source observed by this presentation-only controller.")]
        [SerializeField] private BatMachineBrain brain;

        [Tooltip(tooltip: "Physics body observed for travel direction and aerial speed.")]
        [SerializeField] private Rigidbody2D body;

        [Tooltip(tooltip: "Visual renderer that receives frames and horizontal mirroring.")]
        [SerializeField] private SpriteRenderer renderer;

        [Header(header: "State Frames")]
        [Tooltip(tooltip: "One seven-frame binding for every Bat Machine state.")]
        [SerializeField] private BatMachineVisualStateBinding[] stateBindings = Array.Empty<BatMachineVisualStateBinding>();

        [Header(header: "Playback")]
        [Min(min: 0f)]
        [Tooltip(tooltip: "Base animation frames per second for every Bat visual state.")]
        [SerializeField] private float framesPerSecond = 8f;

        [Min(min: 0f)]
        [Tooltip(tooltip: "Base playback multiplier used by non-flight visual states.")]
        [SerializeField] private float basePlaybackRate = 1f;

        [Min(min: 0f)]
        [Tooltip(tooltip: "Aerial speed at which Patrol and Engage reach their fastest playback rate.")]
        [SerializeField] private float maximumAerialSpeed = 5f;

        [Min(min: 0f)]
        [Tooltip(tooltip: "Slowest playback multiplier used by Patrol and Engage.")]
        [SerializeField] private float minimumVelocityPlaybackRate = 0.8f;

        [Min(min: 0f)]
        [Tooltip(tooltip: "Fastest playback multiplier used by Patrol and Engage.")]
        [SerializeField] private float maximumVelocityPlaybackRate = 1.2f;

        [Header(header: "Facing")]
        [Min(min: 0f)]
        [Tooltip(tooltip: "Horizontal velocity magnitude required before the visible sprite changes facing direction.")]
        [SerializeField] private float facingDeadZone = 0.01f;

        private readonly HashSet<BatMachineState> missingBindingWarnings = new HashSet<BatMachineState>();
        private float elapsed;
        private int facingDirection = 1;

        private void Awake()
        {
            ResolveDependencies();
        }

        private void Update()
        {
            if (brain == null || renderer == null)
            {
                return;
            }

            Tick(Time.deltaTime);
        }

        public void Configure(
            BatMachineBrain targetBrain,
            Rigidbody2D targetBody,
            SpriteRenderer targetRenderer,
            IReadOnlyDictionary<BatMachineState, Sprite[]> framesByState)
        {
            brain = targetBrain;
            body = targetBody;
            renderer = targetRenderer;
            stateBindings = BuildBindings(framesByState);
            elapsed = 0f;
            facingDirection = renderer != null && renderer.flipX ? -1 : 1;
            missingBindingWarnings.Clear();
        }

        public void Tick(float deltaTime)
        {
            if (brain == null || renderer == null)
            {
                return;
            }

            var state = brain.CurrentState;
            var frames = ResolveFrames(state);
            if (!HasValidFrames(frames))
            {
                WarnMissingBinding(state);
                return;
            }

            elapsed += Mathf.Max(0f, deltaTime) * ResolvePlaybackRate(state);
            renderer.sprite = frames[(int)(elapsed * Mathf.Max(0f, framesPerSecond)) % frames.Length];
            UpdateFacing(body != null ? body.linearVelocity.x : 0f);
        }

        public IReadOnlyList<Sprite> GetFrames(BatMachineState state)
        {
            return ResolveFrames(state) ?? Array.Empty<Sprite>();
        }

        private Sprite[] ResolveFrames(BatMachineState state)
        {
            foreach (var binding in stateBindings)
            {
                if (binding != null && binding.State == state)
                {
                    return binding.Frames as Sprite[];
                }
            }

            return null;
        }

        private float ResolvePlaybackRate(BatMachineState state)
        {
            if (state != BatMachineState.PatrolRandom && state != BatMachineState.Engage)
            {
                return Mathf.Max(0f, basePlaybackRate);
            }

            var speedFraction = maximumAerialSpeed <= 0f || body == null
                ? 1f
                : Mathf.Clamp01(body.linearVelocity.magnitude / maximumAerialSpeed);
            return Mathf.Lerp(
                Mathf.Max(0f, minimumVelocityPlaybackRate),
                Mathf.Max(0f, maximumVelocityPlaybackRate),
                speedFraction);
        }

        private static bool HasValidFrames(Sprite[] frames)
        {
            if (frames == null || frames.Length != RequiredFrameCount)
            {
                return false;
            }

            foreach (var frame in frames)
            {
                if (frame == null)
                {
                    return false;
                }
            }

            return true;
        }

        private void UpdateFacing(float horizontalVelocity)
        {
            if (Mathf.Abs(horizontalVelocity) <= facingDeadZone)
            {
                return;
            }

            facingDirection = horizontalVelocity > 0f ? 1 : -1;
            renderer.flipX = facingDirection < 0;
        }

        private void ResolveDependencies()
        {
            if (brain == null)
            {
                brain = GetComponentInParent<BatMachineBrain>();
            }

            if (body == null && brain != null)
            {
                body = brain.GetComponent<Rigidbody2D>();
            }

            if (renderer == null)
            {
                renderer = GetComponent<SpriteRenderer>();
            }
        }

        private void WarnMissingBinding(BatMachineState state)
        {
            if (missingBindingWarnings.Add(state))
            {
                Debug.LogError($"Bat Machine visual binding for '{state}' must contain exactly {RequiredFrameCount} frames.", this);
            }
        }

        private static BatMachineVisualStateBinding[] BuildBindings(
            IReadOnlyDictionary<BatMachineState, Sprite[]> framesByState)
        {
            if (framesByState == null)
            {
                return Array.Empty<BatMachineVisualStateBinding>();
            }

            var bindings = new BatMachineVisualStateBinding[framesByState.Count];
            var index = 0;
            foreach (var pair in framesByState)
            {
                var binding = new BatMachineVisualStateBinding();
                binding.Configure(pair.Key, pair.Value);
                bindings[index++] = binding;
            }

            return bindings;
        }
    }
}
