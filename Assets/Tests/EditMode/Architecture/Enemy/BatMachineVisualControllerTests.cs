using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using TicGame.Architecture;
using UnityEngine;
using UnityEngine.TestTools;

namespace TicGame.Architecture.Tests
{
    public sealed class BatMachineVisualControllerTests
    {
        private readonly List<Object> objectsToDestroy = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (var instance in objectsToDestroy)
            {
                Object.DestroyImmediate(instance);
            }

            objectsToDestroy.Clear();
        }

        [Test]
        public void Tick_EngageStateAndPositiveVelocity_SelectsEngageFramesAndFacesRight()
        {
            var rig = CreateRig(BatMachineState.Engage, new Vector2(3f, 0f));

            rig.Controller.Tick(0.2f);

            Assert.AreSame(rig.EngageFrames[1], rig.Renderer.sprite);
            Assert.False(rig.Renderer.flipX);
        }

        [Test]
        public void Tick_NearZeroVelocity_PreservesLastFacingDirection()
        {
            var rig = CreateRig(BatMachineState.PatrolRandom, Vector2.left);
            rig.Controller.Tick(0.1f);
            rig.Body.linearVelocity = Vector2.zero;

            rig.Controller.Tick(0.1f);

            Assert.True(rig.Renderer.flipX);
        }

        [Test]
        public void Tick_EachState_SelectsItsOwnSevenFrameBinding()
        {
            foreach (BatMachineState state in System.Enum.GetValues(typeof(BatMachineState)))
            {
                var rig = CreateRig(state, Vector2.zero);

                rig.Controller.Tick(0f);

                Assert.AreSame(rig.FramesByState[state][0], rig.Renderer.sprite, state.ToString());
            }
        }

        [TestCase(BatMachineState.WindupFire)]
        [TestCase(BatMachineState.Evade)]
        [TestCase(BatMachineState.StunnedFall)]
        [TestCase(BatMachineState.GroundedRecovery)]
        [TestCase(BatMachineState.Dead)]
        public void Tick_NonFlightState_UsesSerializedBasePlaybackRateRegardlessOfBodySpeed(BatMachineState state)
        {
            var rig = CreateRig(state, Vector2.right * 5f);
            SetField(rig.Controller, "basePlaybackRate", 0.5f);
            SetField(rig.Controller, "minimumVelocityPlaybackRate", 2f);
            SetField(rig.Controller, "maximumVelocityPlaybackRate", 3f);

            rig.Controller.Tick(0.5f);

            Assert.AreSame(rig.FramesByState[state][2], rig.Renderer.sprite);
        }

        [TestCase(BatMachineState.PatrolRandom)]
        [TestCase(BatMachineState.Engage)]
        public void Tick_FlightStatePlayback_ChangesWithBodySpeed(BatMachineState state)
        {
            var stationaryRig = CreateRig(state, Vector2.zero);
            var movingRig = CreateRig(state, Vector2.right * 5f);

            stationaryRig.Controller.Tick(0.5f);
            movingRig.Controller.Tick(0.5f);

            Assert.AreSame(stationaryRig.FramesByState[state][3], stationaryRig.Renderer.sprite);
            Assert.AreSame(movingRig.FramesByState[state][4], movingRig.Renderer.sprite);
        }

        [Test]
        public void Tick_BindingWithNullFrame_PreservesCurrentSpriteAndLogsOneError()
        {
            var rig = CreateRig(BatMachineState.Engage, Vector2.right);
            var existingSprite = rig.FramesByState[BatMachineState.PatrolRandom][0];
            rig.Renderer.sprite = existingSprite;
            rig.EngageFrames[3] = null;
            LogAssert.Expect(LogType.Error, new Regex("Bat Machine visual binding for 'Engage' must contain exactly 7 frames."));

            rig.Controller.Tick(0.2f);
            rig.Controller.Tick(0.2f);

            Assert.AreSame(existingSprite, rig.Renderer.sprite);
        }

        [Test]
        public void Tick_DoesNotMutateBrainStateOrBodyVelocity()
        {
            var velocity = new Vector2(-3f, 2f);
            var rig = CreateRig(BatMachineState.Evade, velocity);

            rig.Controller.Tick(0.2f);

            Assert.AreEqual(BatMachineState.Evade, rig.Brain.CurrentState);
            Assert.AreEqual(velocity, rig.Body.linearVelocity);
        }

        private BatVisualRig CreateRig(BatMachineState state, Vector2 velocity)
        {
            var root = CreateObject("Bat Machine");
            var body = root.AddComponent<Rigidbody2D>();
            body.linearVelocity = velocity;
            var brain = root.AddComponent<BatMachineBrain>();
            var rendererObject = CreateObject("Visual Root");
            rendererObject.transform.SetParent(root.transform);
            var renderer = rendererObject.AddComponent<SpriteRenderer>();
            var controller = rendererObject.AddComponent<BatMachineVisualController>();
            var framesByState = CreateFrames();
            controller.Configure(brain, body, renderer, framesByState);
            SetCurrentState(brain, state);

            return new BatVisualRig(brain, body, renderer, controller, framesByState);
        }

        private Dictionary<BatMachineState, Sprite[]> CreateFrames()
        {
            var framesByState = new Dictionary<BatMachineState, Sprite[]>();
            foreach (BatMachineState state in System.Enum.GetValues(typeof(BatMachineState)))
            {
                var frames = new Sprite[7];
                for (var index = 0; index < frames.Length; index++)
                {
                    var texture = new Texture2D(1, 1);
                    var sprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f));
                    frames[index] = sprite;
                    objectsToDestroy.Add(sprite);
                    objectsToDestroy.Add(texture);
                }

                framesByState.Add(state, frames);
            }

            return framesByState;
        }

        private static void SetCurrentState(BatMachineBrain brain, BatMachineState state)
        {
            var stateMachineField = typeof(BatMachineBrain).GetField(
                "stateMachine",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(stateMachineField);
            var stateMachine = stateMachineField.GetValue(brain);
            var currentStateField = stateMachine.GetType().GetField(
                "<CurrentState>k__BackingField",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(currentStateField);
            currentStateField.SetValue(stateMachine, new TestState(state));
        }

        private static void SetField<T>(object target, string fieldName, T value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(field, $"Expected private field '{fieldName}' on {target.GetType().Name}.");
            field.SetValue(target, value);
        }

        private GameObject CreateObject(string name)
        {
            var instance = new GameObject(name);
            objectsToDestroy.Add(instance);
            return instance;
        }

        private sealed class BatVisualRig
        {
            public BatVisualRig(
                BatMachineBrain brain,
                Rigidbody2D body,
                SpriteRenderer renderer,
                BatMachineVisualController controller,
                Dictionary<BatMachineState, Sprite[]> framesByState)
            {
                Brain = brain;
                Body = body;
                Renderer = renderer;
                Controller = controller;
                FramesByState = framesByState;
            }

            public BatMachineBrain Brain { get; }
            public Rigidbody2D Body { get; }
            public SpriteRenderer Renderer { get; }
            public BatMachineVisualController Controller { get; }
            public Dictionary<BatMachineState, Sprite[]> FramesByState { get; }
            public Sprite[] EngageFrames => FramesByState[BatMachineState.Engage];
        }

        private sealed class TestState : IState<BatMachineState>
        {
            public TestState(BatMachineState id)
            {
                Id = id;
            }

            public BatMachineState Id { get; }

            public void Enter()
            {
            }

            public void Exit()
            {
            }

            public void FixedTick(float fixedDeltaTime)
            {
            }

            public void Tick(float deltaTime)
            {
            }
        }
    }
}
