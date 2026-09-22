using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TicGame.Architecture.Tests
{
    public sealed class AreaProgressBindingTests
    {
        private readonly List<GameObject> objects = new();

        [TearDown]
        public void TearDown()
        {
            foreach (var value in objects) Object.DestroyImmediate(value);
            objects.Clear();
        }

        [Test]
        public void GateRestoresOpenedProgressBeforeAnyHit()
        {
            var progress = new RunProgress(new SpawnAddress("blue", "start"));
            progress.OpenGate("blue", "seal-1");
            var door = Create("Door");
            var barrier = door.AddComponent<BoxCollider2D>();
            var renderer = door.AddComponent<SpriteRenderer>();
            var gate = door.AddComponent<CardMeleeGate>();
            gate.Configure(barrier, renderer, "seal-1");

            gate.BindProgress(progress, "blue");

            Assert.IsTrue(gate.IsOpen);
            Assert.IsFalse(barrier.enabled);
            Assert.IsFalse(renderer.enabled);
        }

        [Test]
        public void GateProgressIsScopedToAreaAndOnlyPublishedByQualifyingDamage()
        {
            var progress = new RunProgress(new SpawnAddress("blue", "start"));
            var door = Create("Door");
            var barrier = door.AddComponent<BoxCollider2D>();
            var gate = door.AddComponent<CardMeleeGate>();
            gate.Configure(barrier, null, "seal-1");
            gate.BindProgress(progress, "blue");

            gate.ApplyDamage(new DamageContext(null, door, null, 10, Vector2.zero, Vector2.right));
            Assert.IsFalse(progress.IsGateOpen("blue", "seal-1"));
            gate.ApplyDamage(new DamageContext(null, door, null, 10, Vector2.zero, Vector2.right,
                isCardEnhancedMelee: true));
            Assert.IsTrue(progress.IsGateOpen("blue", "seal-1"));
            Assert.IsFalse(progress.IsGateOpen("pink", "seal-1"));
        }

        [Test]
        public void GuideDiscoverySurvivesZoneReloadAndTriggerDestruction()
        {
            var progress = new RunProgress(new SpawnAddress("blue", "start"));
            var owner = Create("Guide");
            var guide = owner.AddComponent<CardTimeGuideUI>();
            guide.Bind(progress);
            guide.Discover("actual guide");
            Assert.IsTrue(guide.IsDiscovered);
            Assert.IsTrue(guide.IsVisible);
            guide.Discover("actual guide");
            Assert.IsTrue(guide.IsVisible);

            var trigger = Create("Tutorial Trigger");
            trigger.AddComponent<BoxCollider2D>().isTrigger = true;
            var zone = trigger.AddComponent<CardTimeTutorialZone>();
            zone.BindGuide(guide);
            Object.DestroyImmediate(trigger);

            var reloaded = Create("Guide Reload").AddComponent<CardTimeGuideUI>();
            reloaded.Bind(progress);

            Assert.IsTrue(progress.GuideDiscovered);
            Assert.IsTrue(guide.IsDiscovered);
            Assert.IsFalse(reloaded.IsVisible);
        }

        [Test]
        public void StreamedZoneDoesNotUseSerializedLocalGuideWithoutBinding()
        {
            var trigger = Create("Streamed Tutorial Trigger");
            trigger.AddComponent<BoxCollider2D>().isTrigger = true;
            trigger.AddComponent<CardTimeGuideUI>();
            var zone = trigger.AddComponent<CardTimeTutorialZone>();
            zone.ConfigureForStreamedComposition();

            var resolved = typeof(CardTimeTutorialZone).GetMethod("ResolveGuide", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(zone, null);

            Assert.IsNull(resolved);
        }

        [Test]
        public void CheckpointActivationUpdatesRespawnAddressAndIsIdempotent()
        {
            var progress = new RunProgress(new SpawnAddress("blue", "start"));
            var player = Create("Player").AddComponent<PlayerController>();
            var volume = Create("Checkpoint");
            volume.AddComponent<BoxCollider2D>().isTrigger = true;
            var checkpoint = volume.AddComponent<AreaRespawnCheckpoint>();
            var markerObject = Create("Pink Spawn");
            var marker = markerObject.AddComponent<AreaSpawnPoint>();
            marker.Configure("pink-corridor-begin");
            checkpoint.Configure(marker);
            checkpoint.Bind(progress, "pink", player);

            Assert.IsTrue(checkpoint.TryActivate(player));
            Assert.AreEqual(new SpawnAddress("pink", "pink-corridor-begin"), progress.Respawn);
            Assert.IsTrue(checkpoint.TryActivate(player));
            Assert.AreEqual(new SpawnAddress("pink", "pink-corridor-begin"), progress.Respawn);
        }

        [Test]
        public void CheckpointRejectsForeignPlayerWithoutChangingRespawnAddress()
        {
            var progress = new RunProgress(new SpawnAddress("blue", "start"));
            var player = Create("Player").AddComponent<PlayerController>();
            var foreignPlayer = Create("Foreign Player").AddComponent<PlayerController>();
            var volume = Create("Checkpoint");
            volume.AddComponent<BoxCollider2D>().isTrigger = true;
            var checkpoint = volume.AddComponent<AreaRespawnCheckpoint>();
            var marker = Create("Pink Spawn").AddComponent<AreaSpawnPoint>();
            marker.Configure("pink-corridor-begin");
            checkpoint.Configure(marker);
            checkpoint.Bind(progress, "pink", player);

            Assert.IsFalse(checkpoint.TryActivate(foreignPlayer));
            Assert.AreEqual(new SpawnAddress("blue", "start"), progress.Respawn);
        }

        [Test]
        public void CheckpointRejectsMarkerFromAnotherSceneWithoutChangingRespawnAddress()
        {
            var progress = new RunProgress(new SpawnAddress("blue", "start"));
            var player = Create("Player").AddComponent<PlayerController>();
            var volume = Create("Checkpoint");
            volume.AddComponent<BoxCollider2D>().isTrigger = true;
            var checkpoint = volume.AddComponent<AreaRespawnCheckpoint>();
            var markerObject = Create("Pink Spawn");
            var marker = markerObject.AddComponent<AreaSpawnPoint>();
            marker.Configure("pink-corridor-begin");
            var foreignScene = SceneManager.CreateScene("Checkpoint Foreign Scene");
            SceneManager.MoveGameObjectToScene(markerObject, foreignScene);
            checkpoint.Configure(marker);
            checkpoint.Bind(progress, "pink", player);

            Assert.IsFalse(checkpoint.TryActivate(player));
            Assert.AreEqual(new SpawnAddress("blue", "start"), progress.Respawn);

            SceneManager.UnloadSceneAsync(foreignScene);
        }

        [Test]
        public void CoordinatorBindsCheckpointInLoadedAreaToSessionPlayerAndProgress()
        {
            var activeScene = SceneManager.GetActiveScene();
            var progress = new RunProgress(new SpawnAddress("blue", "start"));
            var player = Create("Player").AddComponent<PlayerController>();
            var composition = Create("Gameplay");
            var coordinator = composition.AddComponent<GameplayAreaCoordinator>();
            var definition = ScriptableObject.CreateInstance<AreaDefinition>();
            definition.Configure("pink", "Assets/Scenes/PinkArea_Perimeters.unity", "pink-start");
            var volume = Create("Pink Checkpoint");
            volume.AddComponent<BoxCollider2D>().isTrigger = true;
            var checkpoint = volume.AddComponent<AreaRespawnCheckpoint>();
            var marker = Create("Pink Spawn").AddComponent<AreaSpawnPoint>();
            marker.Configure("pink-corridor-begin");
            checkpoint.Configure(marker);

            try
            {
                coordinator.Configure(player, null, null, new[] { definition }, progress, null);
                typeof(GameplayAreaCoordinator).GetMethod("BindLoadedArea", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(coordinator, new object[] { activeScene, definition });

                Assert.IsTrue(checkpoint.TryActivate(player));
                Assert.AreEqual(new SpawnAddress("pink", "pink-corridor-begin"), progress.Respawn);
            }
            finally
            {
                Object.DestroyImmediate(definition);
            }
        }

        private GameObject Create(string name)
        {
            var value = new GameObject(name);
            objects.Add(value);
            return value;
        }
    }
}
