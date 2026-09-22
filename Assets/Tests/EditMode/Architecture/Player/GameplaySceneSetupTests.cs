using NUnit.Framework;
using System.Linq;
using TicGame.Architecture;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace TicGame.Architecture.Tests
{
    public sealed class GameplaySceneSetupTests
    {
        [Test]
        public void SceneSetupRoundTripPreservesLoadedAndActiveFlags()
        {
            var source = new[]
            {
                new SceneSetup { path = "Assets/Scenes/Blue|Area.unity", isLoaded = true, isActive = false },
                new SceneSetup { path = "Assets/Scenes/Gameplay.unity", isLoaded = true, isActive = true }
            };

            var state = source.Select(item => new GameplaySceneSetupState(item.path, item.isLoaded, item.isActive));
            var restored = GameplayScenePlayState.Deserialize(GameplayScenePlayState.Serialize(state));

            Assert.That(restored, Has.Length.EqualTo(2));
            Assert.That(restored[0].Path, Is.EqualTo(source[0].path));
            Assert.That(restored[0].IsLoaded, Is.True);
            Assert.That(restored[1].IsActive, Is.True);
        }

        [Test]
        public void EmptySceneSetupDoesNotRestoreAnything()
        {
            Assert.That(GameplayScenePlayState.Deserialize(string.Empty), Is.Empty);
        }

        [Test]
        public void StartupOverrideIsConsumedExactlyOnce()
        {
            GameplayStartupOverride.Set("pink", "pink-start");

            Assert.That(GameplayStartupOverride.TryConsume(out var area, out var spawn), Is.True);
            Assert.That(area, Is.EqualTo("pink"));
            Assert.That(spawn, Is.EqualTo("pink-start"));
            Assert.That(GameplayStartupOverride.TryConsume(out _, out _), Is.False);
        }

        [Test]
        public void TemporaryFixtureSceneCanBeCreatedAndRemovedWithoutProjectAssets()
        {
            var priorSetup = EditorSceneManager.GetSceneManagerSetup();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            try
            {
                Assert.That(scene.IsValid(), Is.True);
                Assert.That(scene.path, Is.Empty);
            }
            finally
            {
                EditorSceneManager.RestoreSceneManagerSetup(priorSetup);
            }
        }
    }
}
