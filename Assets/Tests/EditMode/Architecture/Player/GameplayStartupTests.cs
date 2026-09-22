using NUnit.Framework;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TicGame.Architecture.Tests
{
    public sealed class GameplayStartupTests
    {
        private GameObject player;
        private GameObject gameplay;
        private GameObject markerOne;
        private GameObject markerTwo;
        private AreaDefinition definition;

        [TearDown]
        public void TearDown()
        {
            ResetSceneStreamingService();
            Object.DestroyImmediate(markerTwo);
            Object.DestroyImmediate(markerOne);
            Object.DestroyImmediate(definition);
            Object.DestroyImmediate(gameplay);
            Object.DestroyImmediate(player);
        }

        [Test]
        public void WorldHold_NestedLeasesKeepPhysicsDisabledUntilTheFinalLeaseIsReleased()
        {
            player = new GameObject("Player");
            var body = player.AddComponent<Rigidbody2D>();
            body.simulated = true;
            var hold = player.AddComponent<PlayerWorldHold>();

            var first = hold.Acquire();
            var second = hold.Acquire();

            Assert.IsTrue(player.activeSelf);
            Assert.IsFalse(body.simulated);
            first.Dispose();
            Assert.IsFalse(body.simulated);
            second.Dispose();
            Assert.IsTrue(body.simulated);
        }

        [Test]
        public void WorldHold_RestoresAnInitiallyDisabledBodyAfterRelease()
        {
            player = new GameObject("Player");
            var body = player.AddComponent<Rigidbody2D>();
            body.simulated = false;
            var hold = player.AddComponent<PlayerWorldHold>();

            using (hold.Acquire())
            {
                Assert.IsFalse(body.simulated);
            }

            Assert.IsFalse(body.simulated);
        }

        [Test]
        public void SceneRoot_InvalidInitialAddressKeepsTheConfiguredPlayerHeldAndReportsARetryableError()
        {
            player = new GameObject("Player");
            var body = player.AddComponent<Rigidbody2D>();
            body.simulated = true;
            var controller = player.AddComponent<PlayerController>();
            player.AddComponent<PlayerWorldHold>();

            gameplay = new GameObject("Gameplay");
            var guide = gameplay.AddComponent<CardTimeGuideUI>();
            var coordinator = gameplay.AddComponent<GameplayAreaCoordinator>();
            var root = gameplay.AddComponent<GameplaySceneRoot>();
            SetField(root, "player", controller);
            SetField(root, "guide", guide);
            SetField(root, "coordinator", coordinator);
            SetField(root, "initialAreaId", string.Empty);
            SetField(root, "initialSpawnId", "start");

            InvokeStart(root);

            Assert.IsFalse(body.simulated);
            Assert.That(root.LastError, Does.Contain("initial area"));
        }

        [Test]
        public void SceneRoot_BindsTheGuideToTheRunProgressWhenItsSessionIsInitialized()
        {
            gameplay = new GameObject("Gameplay");
            var guide = gameplay.AddComponent<CardTimeGuideUI>();
            var root = gameplay.AddComponent<GameplaySceneRoot>();
            SetField(root, "guide", guide);

            typeof(GameplaySceneRoot).GetMethod("InitializeRunProgress", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(root, new object[] { new SpawnAddress("blue", "start") });
            guide.Discover("Guide");

            Assert.IsTrue(root.Progress.GuideDiscovered);
        }

        [Test]
        public void SceneRoot_RetainsTheConsumedPinkAddressForTheStartupSession()
        {
            gameplay = new GameObject("Gameplay");
            var root = gameplay.AddComponent<GameplaySceneRoot>();
            SetField(root, "initialAreaId", "blue");
            SetField(root, "initialSpawnId", "start");
            GameplayStartupOverride.Set("pink", "pink-start");

            var method = typeof(GameplaySceneRoot).GetMethod("TryCreateInitialAddress", BindingFlags.Instance | BindingFlags.NonPublic);
            var firstArguments = new object[] { null, null };
            Assert.IsTrue((bool)method.Invoke(root, firstArguments));
            Assert.AreEqual(new SpawnAddress("pink", "pink-start"), firstArguments[0]);
            typeof(GameplaySceneRoot).GetMethod("InitializeRunProgress", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(root, new[] { firstArguments[0] });

            var retryArguments = new object[] { null, null };
            Assert.IsTrue((bool)method.Invoke(root, retryArguments));
            Assert.AreEqual(new SpawnAddress("pink", "pink-start"), retryArguments[0]);
            Assert.AreEqual(new SpawnAddress("pink", "pink-start"), root.Progress.Respawn);
        }

        [Test]
        public void SpawnValidation_ReportsAMissingMarkerWithoutSelectingAnotherSpawn()
        {
            var address = new SpawnAddress("blue", "missing-marker-" + System.Guid.NewGuid());

            var result = TryFindSpawn(SceneManager.GetActiveScene(), address, out var spawn, out var error);

            Assert.IsFalse(result);
            Assert.IsNull(spawn);
            Assert.That(error, Does.Contain("has no spawn marker"));
        }

        [Test]
        public void SpawnValidation_RejectsDuplicateMarkers()
        {
            var id = "duplicate-marker-" + System.Guid.NewGuid();
            markerOne = new GameObject("Marker One");
            markerOne.AddComponent<AreaSpawnPoint>().Configure(id);
            markerTwo = new GameObject("Marker Two");
            markerTwo.AddComponent<AreaSpawnPoint>().Configure(id);

            var result = TryFindSpawn(SceneManager.GetActiveScene(), new SpawnAddress("blue", id), out var spawn, out var error);

            Assert.IsFalse(result);
            Assert.IsNull(spawn);
            Assert.That(error, Does.Contain("exactly one is required"));
        }

        [Test]
        public void Coordinator_LoadedAreaBindingRestoresGatesAndConnectsTutorialZonesToTheGameplayGuide()
        {
            const string gateId = "opened-gate";
            var progress = new RunProgress(new SpawnAddress("blue", "start"));
            progress.OpenGate("blue", gateId);
            definition = ScriptableObject.CreateInstance<AreaDefinition>();
            definition.Configure("blue", "Assets/Scenes/Binding.unity", "start");

            gameplay = new GameObject("Gameplay");
            var guide = gameplay.AddComponent<CardTimeGuideUI>();
            guide.Bind(progress);
            var coordinator = gameplay.AddComponent<GameplayAreaCoordinator>();
            coordinator.Configure(null, null, guide, new[] { definition }, progress, null);

            markerOne = new GameObject("Gate");
            var barrier = markerOne.AddComponent<BoxCollider2D>();
            var gate = markerOne.AddComponent<CardMeleeGate>();
            gate.Configure(barrier, null, gateId);
            markerTwo = new GameObject("Tutorial Zone");
            markerTwo.AddComponent<BoxCollider2D>().isTrigger = true;
            var zone = markerTwo.AddComponent<CardTimeTutorialZone>();

            typeof(GameplayAreaCoordinator).GetMethod("BindLoadedArea", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(coordinator, new object[] { SceneManager.GetActiveScene(), definition });
            var resolvedGuide = typeof(CardTimeTutorialZone).GetMethod("ResolveGuide", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(zone, null);

            Assert.IsTrue(gate.IsOpen);
            Assert.IsFalse(barrier.enabled);
            Assert.AreSame(guide, resolvedGuide);
        }

        [Test]
        public async Task Coordinator_MissingSceneKeepsThePlayerHeldAndAllowsAnExplicitRetry()
        {
            const string path = "Assets/Scenes/MissingGameplayStartupTest.unity";
            var address = new SpawnAddress("missing", "start");
            var coordinator = CreateCoordinator(address, path, out var hold, out var body);
            var error = $"Scene '{path}' is not available. Add it to the enabled Build Profile scene list.";

            LogAssert.Expect(LogType.Error, error);
            LogAssert.Expect(LogType.Error, error);
            var first = await coordinator.StartAtAsync(address);
            LogAssert.Expect(LogType.Error, error);
            LogAssert.Expect(LogType.Error, error);
            var retry = await coordinator.RetryAsync();

            Assert.IsFalse(first);
            Assert.IsFalse(retry);
            Assert.IsTrue(hold.IsHeld);
            Assert.IsFalse(body.simulated);
            Assert.That(coordinator.LastError, Does.Contain("not available"));
        }

        [Test]
        public async Task Coordinator_AlreadyLoadedAreaWithReadyServicesTeleportsAndReleasesTheHold()
        {
            var activeScene = RequirePathBackedActiveScene();
            var address = new SpawnAddress("active", "start");
            var coordinator = CreateCoordinator(address, activeScene.path, out var hold, out var body);
            markerOne = new GameObject("Start Marker");
            markerOne.transform.position = new Vector3(7f, 9f, 0f);
            markerOne.AddComponent<AreaSpawnPoint>().Configure(address.SpawnId);
            BindReadyServices();

            var started = await coordinator.StartAtAsync(address);

            Assert.IsTrue(started);
            Assert.IsTrue(coordinator.IsReady);
            Assert.AreEqual(markerOne.transform.position, player.transform.position);
            Assert.IsFalse(hold.IsHeld);
            Assert.IsTrue(body.simulated);
        }

        [Test]
        public async Task Coordinator_NewestStartWinsWhenAnEarlierStartIsWaitingForServices()
        {
            var activeScene = RequirePathBackedActiveScene();
            var firstAddress = new SpawnAddress("active", "first");
            var secondAddress = new SpawnAddress("active", "second");
            var coordinator = CreateCoordinator(firstAddress, activeScene.path, out var hold, out _);
            markerOne = new GameObject("First Marker");
            markerOne.transform.position = new Vector3(3f, 4f, 0f);
            markerOne.AddComponent<AreaSpawnPoint>().Configure(firstAddress.SpawnId);
            markerTwo = new GameObject("Second Marker");
            markerTwo.transform.position = new Vector3(12f, 15f, 0f);
            markerTwo.AddComponent<AreaSpawnPoint>().Configure(secondAddress.SpawnId);

            var first = coordinator.StartAtAsync(firstAddress);
            var second = coordinator.StartAtAsync(secondAddress);
            BindReadyServices();
            var firstResult = await first;
            var secondResult = await second;

            Assert.IsFalse(firstResult);
            Assert.IsTrue(secondResult);
            Assert.AreEqual(markerTwo.transform.position, player.transform.position);
            Assert.IsTrue(coordinator.IsReady);
            Assert.IsFalse(hold.IsHeld);
        }

        [Test]
        public async Task Coordinator_CancelDuringServiceWaitReturnsFalseWithoutTeleportingOrReleasingHold()
        {
            var activeScene = SceneManager.GetActiveScene();
            if (string.IsNullOrWhiteSpace(activeScene.path))
            {
                Assert.Ignore("The Unity test runner did not provide a path-backed active scene for already-loaded startup coverage.");
            }

            var address = new SpawnAddress("active", "start");
            var coordinator = CreateCoordinator(address, activeScene.path, out var hold, out var body);
            SetCoordinatorTimeout(coordinator, 0.1f);
            player.transform.position = new Vector3(12f, 34f, 0f);

            var starting = coordinator.StartAtAsync(address);
            coordinator.CancelSession();
            var started = await starting;

            Assert.IsFalse(started);
            Assert.AreEqual(new Vector3(12f, 34f, 0f), player.transform.position);
            Assert.IsTrue(hold.IsHeld);
            Assert.IsFalse(body.simulated);
        }

        private static void SetField(GameplaySceneRoot target, string name, object value)
        {
            typeof(GameplaySceneRoot).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(target, value);
        }

        private static void InvokeStart(GameplaySceneRoot root)
        {
            typeof(GameplaySceneRoot).GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(root, null);
        }

        private static bool TryFindSpawn(Scene scene, SpawnAddress address, out AreaSpawnPoint spawn, out string error)
        {
            var arguments = new object[] { scene, address, null, null };
            var result = (bool)typeof(GameplayAreaCoordinator)
                .GetMethod("TryFindSpawn", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, arguments);
            spawn = (AreaSpawnPoint)arguments[2];
            error = (string)arguments[3];
            return result;
        }

        private GameplayAreaCoordinator CreateCoordinator(
            SpawnAddress address,
            string scenePath,
            out PlayerWorldHold hold,
            out Rigidbody2D body)
        {
            player = new GameObject("Player");
            body = player.AddComponent<Rigidbody2D>();
            player.AddComponent<PlayerAttackHitDetector2D>();
            var controller = player.AddComponent<PlayerController>();
            hold = player.AddComponent<PlayerWorldHold>();
            definition = ScriptableObject.CreateInstance<AreaDefinition>();
            definition.Configure(address.AreaId, scenePath, address.SpawnId);
            gameplay = new GameObject("Gameplay");
            var coordinator = gameplay.AddComponent<GameplayAreaCoordinator>();
            coordinator.Configure(
                controller,
                hold,
                null,
                new[] { definition },
                new RunProgress(address),
                null);
            return coordinator;
        }

        private static void SetCoordinatorTimeout(GameplayAreaCoordinator coordinator, float seconds)
        {
            typeof(GameplayAreaCoordinator).GetField(
                    "serviceReadyTimeoutSeconds",
                    BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(coordinator, seconds);
        }

        private static void ResetSceneStreamingService()
        {
            typeof(SceneStreamingService).GetMethod("Reset", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, null);
        }

        private Scene RequirePathBackedActiveScene()
        {
            var scene = SceneManager.GetActiveScene();
            if (string.IsNullOrWhiteSpace(scene.path))
            {
                Assert.Ignore("The Unity test runner did not provide a path-backed active scene for already-loaded startup coverage.");
            }

            return scene;
        }

        private void BindReadyServices()
        {
            var controller = player.GetComponent<PlayerController>();
            controller.BindGameplayServices(new TestGameplayServices());
            controller.BindPlayerCardTimeSource(new TestPlayerCardTimeSource());
        }

        private sealed class TestGameplayServices : IGameplayServices
        {
            public IGameplayTimeService Time => null;
            public IHitStopService HitStop => null;
            public HitStopRequestEventChannelSO HitStopRequests => null;
            public ICardTimeSession CardTime => null;
            public CardTimeSessionEventChannelSO CardTimeTransitions => null;
            public ICardFeedbackService CardFeedback => null;
            public IGameStateService GameState => null;
        }

        private sealed class TestPlayerCardTimeSource : IPlayerCardTimeSource
        {
            public PlayerCardTimeConfigSO Configuration => null;

            public void PublishAvailability(PlayerCardTimeState state) { }
            public CardTimeActivationRequestResult RequestActivation() => CardTimeActivationRequestResult.Rejected;
            public bool TryCommit() => false;
            public bool TryCommit(ICardCommitTransaction transaction) => false;
            public bool Cancel() => false;
            public void Unregister() { }
        }
    }
}
