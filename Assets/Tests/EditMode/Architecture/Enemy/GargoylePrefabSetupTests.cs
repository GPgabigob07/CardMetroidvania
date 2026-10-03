using System;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TicGame.Architecture.Tests
{
    public sealed class GargoylePrefabSetupTests
    {
        private const string Folder = "Assets/Data/Enemies/GargoyleSentinel/";
        private static void Setup() => Type.GetType("TicGame.Architecture.EditorTools.GargoylePrefabSetup, TicGame.Architecture.Editor")
            .GetMethod("CreateOrUpdate", BindingFlags.Public | BindingFlags.Static).Invoke(null, null);
        [OneTimeSetUp] public void Author() => Setup();
        [OneTimeTearDown] public void CloseArena() => EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        [Test] public void TuningReferencesAllValidAttacksAndPresentation()
        {
            var tuning = AssetDatabase.LoadAssetAtPath<GargoyleTuningSO>(Folder + "GargoyleTuning.asset");
            Assert.That(tuning, Is.Not.Null);
            Assert.That(tuning.GetValidationErrors(), Is.Empty);
            Assert.That(tuning.GetAttackDefinition(GargoyleAttackFamily.Volley).Steps.Single().Payload.LockedAimDuration, Is.EqualTo(.15f));
            foreach (var attack in new[] { tuning.BasicAttack, tuning.FeintAttack, tuning.NovaAttack }.Concat(tuning.Families.Select(tuning.GetAttackDefinition)))
                Assert.That(attack.GetValidationErrors(), Is.Empty, attack.name);
        }
        [Test] public void ImportedSpriteEnforcesRealPixelsPivotAndScale()
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Enemies/GargoyleSentinel/GargoyleIdle.png");
            Assert.That(sprite, Is.Not.Null);
            Assert.That(sprite.rect.size, Is.EqualTo(new Vector2(200, 200)));
            Assert.That(sprite.pivot, Is.EqualTo(new Vector2(100, 36)));
            Assert.That(sprite.pixelsPerUnit, Is.EqualTo(48));
            var pixels = sprite.texture.GetPixels32();
            var occupied = Enumerable.Range(0, pixels.Length).Where(i => pixels[i].a != 0).ToArray();
            Assert.That(occupied.Max(i => i % 200) - occupied.Min(i => i % 200) + 1, Is.LessThanOrEqualTo(128));
            Assert.That(occupied.Max(i => i / 200) - occupied.Min(i => i / 200) + 1, Is.LessThanOrEqualTo(128));
        }
        [Test] public void PrefabHasAllCapabilitiesAndNoContactDamage()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/GargoyleSentinel.prefab");
            Assert.That(prefab, Is.Not.Null);
            Assert.That(prefab.transform.localScale, Is.EqualTo(Vector3.one));
            foreach (var type in new[] { typeof(EnemyActor), typeof(EnemyPoise), typeof(GargoyleBrain), typeof(GargoyleDamagePolicy), typeof(EnemyMeleeAttack2D), typeof(EnemyBeamAttack2D), typeof(EnemyNovaAttack2D), typeof(EnemyProjectilePatternLauncher) })
                Assert.That(prefab.GetComponent(type), Is.Not.Null, type.Name);
            Assert.That(prefab.GetComponentsInChildren<GargoyleHurtbox>(true).Length, Is.EqualTo(3));
            Assert.That(prefab.GetComponent<GargoyleAnimationPresenter>(), Is.Not.Null);
            Assert.That(prefab.GetComponents<Component>().Any(c => c != null && c.GetType().Name.Contains("ContactDamage")), Is.False);
        }
        [Test] public void SeparateWardProfilePreservesRecoveryAndMovementWithoutChangingOrdinaryProfile()
        {
            var profile = AssetDatabase.LoadAssetAtPath<PlayerCardInventoryProfileSO>(Folder + "GargoyleCardInventory.asset");
            Assert.That(profile, Is.Not.Null);
            var neutral = profile.GetEquippedCards(PlayerCardTimeState.Neutral).Select(c => c.Id).ToArray();
            Assert.That(neutral, Does.Contain("ward"));
            Assert.That(neutral, Does.Contain("blood-charge").And.Contain("reconstitute").And.Contain("mend"));
            Assert.That(neutral.Any(id => id.Contains("jump-boost")), Is.False);
            Assert.That(profile.GetEquippedCards(PlayerCardTimeState.Chain).Any(c => c.Id.Contains("poise")), Is.True);
            var ordinary = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player/Player.prefab");
            var binding = new SerializedObject(ordinary.GetComponent<PlayerController>()).FindProperty("cardInventoryProfile").objectReferenceValue;
            Assert.That(binding, Is.Not.SameAs(profile));
        }
        [Test] public void ArenaIsSoloWithRoomToEscapeNovaAndWardBindings()
        {
            Assert.That(File.Exists("Assets/Scenes/Test_GargoyleSentinel.unity"), Is.True);
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Test_GargoyleSentinel.unity");
            var roots = scene.GetRootGameObjects();
            Assert.That(roots.SelectMany(r => r.GetComponentsInChildren<EnemyActor>()).Count(), Is.EqualTo(1));
            var player = roots.SelectMany(r => r.GetComponentsInChildren<PlayerController>()).Single();
            Assert.That(player.GetComponent<PlayerWardRuntime>(), Is.Not.Null);
            Assert.That(player.GetComponent<PlayerWardPresenter>(), Is.Not.Null);
            Assert.That(roots.SelectMany(r => r.GetComponentsInChildren<GargoyleDebugPresenter>()).Count(), Is.EqualTo(1));
            Assert.That(roots.SelectMany(r => r.GetComponentsInChildren<PlayerHudUI>()).Count(), Is.EqualTo(1));
            Assert.That(roots.Single(r => r.name == "Arena Floor").GetComponent<BoxCollider2D>().size.x, Is.GreaterThanOrEqualTo(20));
        }
        [Test] public void WardCardDefaultsAndCatalogPreserveRecoveryReadiness()
        {
            var card = AssetDatabase.LoadAssetAtPath<CardDefinitionSO>("Assets/Data/Cards/Definitions/Neutral_Ward.asset");
            Assert.That(card, Is.Not.Null); Assert.That(card.GetValidationErrors(), Is.Empty);
            Assert.That(card.Category, Is.EqualTo(PlayerCardTimeState.Neutral)); Assert.That(card.ConsumptionPolicy, Is.EqualTo(CardConsumptionPolicy.Reusable));
            Assert.That(card.FixedCosts.Single().Amount, Is.EqualTo(20));
            var definition = card.Effect.CommitOperations.Single().Ward; Assert.That(definition.TryRead(out var values), Is.True);
            Assert.That(values.Duration, Is.EqualTo(.6f)); Assert.That(values.Height, Is.EqualTo(1.5f)); Assert.That(values.ForwardOffset, Is.EqualTo(.6f));
            var catalog = AssetDatabase.LoadAssetAtPath<CardCatalogSO>("Assets/Data/Cards/Inventory/TestCardCatalog.asset");
            Assert.That(catalog.Cards, Does.Contain(card)); Assert.That(catalog.Cards.Any(c => c.Id == "mend"), Is.True);
        }
        [Test] public void RepeatedSetupPreservesSerializedAssetsAndExistingTuning()
        {
            Assert.That(Directory.Exists(Folder), Is.True);
            var files = Directory.GetFiles(Folder, "*", SearchOption.AllDirectories)
                .Concat(new[] { "Assets/Prefabs/Enemies/GargoyleSentinel.prefab", "Assets/Scenes/Test_GargoyleSentinel.unity" }).ToArray();
            var before = files.ToDictionary(path => path, File.ReadAllBytes);
            Setup();
            foreach (var entry in before) Assert.That(File.ReadAllBytes(entry.Key), Is.EqualTo(entry.Value), entry.Key);
        }
    }
}
