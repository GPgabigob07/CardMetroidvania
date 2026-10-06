using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace TicGame.Architecture.Tests
{
    public sealed class HitRippleRendererTests
    {
        [Test]
        public void ShaderExists_AndHasNoImportErrors()
        {
            var shader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Shaders/EnemyHitRipple.shader");
            Assert.That(shader, Is.Not.Null, "URP enemy ripple shader is missing.");
            Assert.That(ShaderUtil.ShaderHasError(shader), Is.False);
            var material = new Material(shader);
            try
            {
                Assert.That(material.HasProperty("_MainTex"), Is.True);
                Assert.That(material.passCount, Is.EqualTo(3));
            }
            finally { Object.DestroyImmediate(material); }
        }

        [Test]
        public void NegativeScaleFlipAndPivot_KeepWaveInOwnerSpace()
        {
            var root = new GameObject("Owner");
            var texture = new Texture2D(16, 16);
            var sprite = Sprite.Create(texture, new Rect(0, 0, 16, 16), new Vector2(.25f, .5f), 100);
            var profile = ScriptableObject.CreateInstance<HitRippleProfileSO>();
            try
            {
                var actor = root.AddComponent<EnemyActor>();
                var visual = new GameObject("Body"); visual.transform.SetParent(root.transform, false);
                visual.transform.localPosition = new Vector3(2, 3, 0); visual.transform.localScale = new Vector3(-2, 3, 1);
                var renderer = visual.AddComponent<SpriteRenderer>(); renderer.sprite = sprite;
                var presenter = root.AddComponent<EnemyHitRipplePresenter>(); presenter.Configure(actor, profile, new[] { renderer });
                presenter.Present(new DamageContext(null, root, null, 1, Vector2.one, Vector2.right), new DamageResult(true, false, 1, 1));
                var block = new MaterialPropertyBlock(); renderer.GetPropertyBlock(block);
                var wave = block.GetVector("_RippleOriginRadius0");
                Assert.That(new Vector2(wave.x, wave.y), Is.EqualTo(Vector2.one));
                Assert.That(block.GetMatrix("_RippleVisualToOwner").MultiplyPoint3x4(Vector3.zero), Is.EqualTo(new Vector3(2, 3, 0)));
                Assert.That(block.GetVector("_RipplePixelGridOrigin").x, Is.EqualTo(-.04f).Within(.00001));
                renderer.flipX = true; presenter.Tick(0, false); renderer.GetPropertyBlock(block);
                Assert.That(block.GetVector("_RipplePixelGridOrigin").x, Is.EqualTo(.04f).Within(.00001));
                Assert.That(block.GetVector("_RippleOriginRadius0"), Is.EqualTo(wave));
                var inverse = block.GetMatrix("_RippleOwnerToVisual");
                Assert.That(inverse.MultiplyVector(Vector3.right).magnitude, Is.EqualTo(.5f).Within(.00001));
            }
            finally { Object.DestroyImmediate(root); Object.DestroyImmediate(sprite); Object.DestroyImmediate(texture); Object.DestroyImmediate(profile); }
        }
    }
}
