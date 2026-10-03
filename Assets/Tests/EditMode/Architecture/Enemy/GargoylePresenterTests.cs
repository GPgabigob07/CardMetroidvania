using NUnit.Framework;
using UnityEngine;

namespace TicGame.Architecture.Tests
{
    public sealed class GargoylePresenterTests
    {
        [Test] public void DraftPresenterUsesAssetSpriteAndLiveColorWithoutMutatingAsset()
        {
            var root = new GameObject("Presenter"); var definition = ScriptableObject.CreateInstance<GargoylePresentationSO>();
            var texture = new Texture2D(2, 2); var sprite = Sprite.Create(texture, new Rect(0, 0, 2, 2), Vector2.zero);
            try
            {
                var serialized = new UnityEditor.SerializedObject(definition); serialized.FindProperty("idleSprite").objectReferenceValue = sprite; serialized.ApplyModifiedPropertiesWithoutUndo();
                var renderer = root.AddComponent<SpriteRenderer>(); var presenter = root.AddComponent<GargoyleAnimationPresenter>();
                presenter.Configure(null, definition, renderer); presenter.RefreshVisuals(0);
                Assert.That(renderer.sprite, Is.SameAs(sprite));
                JsonUtility.FromJsonOverwrite("{\"normalColor\":{\"r\":0.2,\"g\":0.3,\"b\":0.4,\"a\":1}}", definition);
                var before = JsonUtility.ToJson(definition); presenter.RefreshVisuals(.2f);
                Assert.That(renderer.color, Is.EqualTo(definition.NormalColor)); Assert.That(JsonUtility.ToJson(definition), Is.EqualTo(before));
            }
            finally { Object.DestroyImmediate(root); Object.DestroyImmediate(sprite); Object.DestroyImmediate(texture); Object.DestroyImmediate(definition); }
        }
        [Test] public void WardVisualUsesActualGuardGeometryAndHidesAfterClear()
        {
            var root = new GameObject("Player"); var definition = ScriptableObject.CreateInstance<WardDefinitionSO>();
            try
            {
                root.AddComponent<SimpleHealth>().Initialize(); root.AddComponent<BoxCollider2D>();
                var runtime = root.AddComponent<PlayerWardRuntime>(); runtime.Initialize(definition); runtime.Arm(-1);
                var line = root.AddComponent<LineRenderer>(); var presenter = root.AddComponent<PlayerWardPresenter>(); presenter.Configure(runtime, definition, line); presenter.RefreshVisuals();
                Assert.That(line.enabled, Is.True); Assert.That(line.positionCount, Is.EqualTo(2));
                Assert.That((line.GetPosition(1) - line.GetPosition(0)).magnitude, Is.EqualTo(runtime.Configuration.Height).Within(.0001f));
                Assert.That(line.GetPosition(0).x, Is.EqualTo(runtime.GuardCenter.x).Within(.0001f));
                runtime.Clear(); presenter.RefreshVisuals(); Assert.That(line.enabled, Is.False);
            }
            finally { Object.DestroyImmediate(root); Object.DestroyImmediate(definition); }
        }
    }
}
