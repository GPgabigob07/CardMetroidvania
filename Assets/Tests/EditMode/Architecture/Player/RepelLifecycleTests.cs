using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace TicGame.Architecture.Tests
{
    public sealed class RepelLifecycleTests
    {
        private GameObject player;
        private PlayerController controller;
        private PlayerRepelRuntime repel;
        [SetUp]
        public void SetUp()
        {
            player = new GameObject("Repel lifecycle player"); player.AddComponent<SimpleHealth>().Initialize();
            player.AddComponent<PlayerAttackHitDetector2D>();
            controller = player.AddComponent<PlayerController>();
            var context = new PlayerContext(player.GetComponent<PlayerMotor2D>(), player.GetComponent<PlayerSensors2D>(), null, null, null);
            var runner = new PlayerActionRunner(); context.AttachRuntime(null, runner);
            typeof(PlayerController).GetProperty("Context").SetValue(controller, context);
            typeof(PlayerController).GetProperty("ActionRunner").SetValue(controller, runner);
            repel = player.AddComponent<PlayerRepelRuntime>(); repel.Arm(3);
        }
        [TearDown] public void TearDown() { Time.timeScale = 1; Object.DestroyImmediate(player); }
        [Test] public void TransientReset_ClearsRepel() { controller.ResetTransientState(); Assert.IsFalse(repel.IsActive); }
        [Test] public void NewRunReset_ClearsRepel() { controller.ResetCardTimeForFullRun(); Assert.IsFalse(repel.IsActive); }
        [Test] public void CardEffectsClear_ClearsRepel() { player.AddComponent<PlayerCardRuntime>().ClearNewCardEffects(); Assert.IsFalse(repel.IsActive); }
        [Test]
        public void WorldHold_PreservesEffectButFreezesItsTimer()
        {
            var hold = player.AddComponent<PlayerWorldHold>();
            typeof(PlayerWorldHold).GetField("player", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(hold, controller);
            using (hold.Acquire()) { repel.Tick(4); Assert.IsTrue(repel.IsActive); Assert.AreEqual(3, repel.RemainingSeconds); }
            repel.Tick(1); Assert.AreEqual(2, repel.RemainingSeconds);
        }
        [Test]
        public void OrdinaryTicks_DoNotRequireComboOrCardTimeToRemainOpen()
        {
            repel.Tick(1); Assert.IsTrue(repel.IsActive); Assert.AreEqual(2, repel.RemainingSeconds);
        }
    }
}
