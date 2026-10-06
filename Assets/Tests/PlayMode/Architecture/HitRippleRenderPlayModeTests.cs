using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace TicGame.Architecture.Tests
{
    public sealed class HitRippleRenderPlayModeTests
    {
        private GameObject root;
        private Material material;
        private Texture2D texture;
        private Sprite sprite;
        private RenderTexture target;
        private SpriteRenderer renderer;

        private void Setup()
        {
            Assert.That(SystemInfo.graphicsDeviceType, Is.Not.EqualTo(UnityEngine.Rendering.GraphicsDeviceType.Null), "Rendering checks require graphics.");
            root = new GameObject("Ripple GPU validation");
            var cameraObject = new GameObject("Validation camera"); cameraObject.transform.SetParent(root.transform);
            var camera = cameraObject.AddComponent<Camera>(); camera.orthographic = true; camera.orthographicSize = .6f;
            camera.transform.position = new Vector3(1000, 1000, -10); camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.clear; camera.allowHDR = false; camera.allowMSAA = false;
            var cameraDataType = Type.GetType("UnityEngine.Rendering.Universal.UniversalAdditionalCameraData, Unity.RenderPipelines.Universal.Runtime");
            if (cameraDataType != null) cameraObject.AddComponent(cameraDataType);
            target = new RenderTexture(128, 128, 24, RenderTextureFormat.ARGB32); target.Create(); camera.targetTexture = target;
            var lightType = Type.GetType("UnityEngine.Rendering.Universal.Light2D, Unity.RenderPipelines.Universal.2D.Runtime");
            Assert.That(lightType, Is.Not.Null, "Use the installed URP 2D lighting assembly for rendering checks.");
            if (lightType != null)
            {
                var lightObject = new GameObject("Validation light"); lightObject.transform.SetParent(root.transform);
                var light = lightObject.AddComponent(lightType);
                var property = lightType.GetProperty("lightType"); property.SetValue(light, Enum.Parse(property.PropertyType, "Global"));
                lightType.GetProperty("intensity").SetValue(light, 1f);
            }
            texture = new Texture2D(64, 64) { filterMode = FilterMode.Point };
            var pixels = new Color32[64 * 64];
            for (var y = 0; y < 64; y++) for (var x = 0; x < 64; x++)
                pixels[y * 64 + x] = x > 48 && y > 48 || x >= 17 && x <= 20 && y >= 31 && y <= 33
                    ? new Color32(0, 0, 0, 0) : new Color32(255, 0, 0, 255);
            texture.SetPixels32(pixels); texture.Apply();
            sprite = Sprite.Create(texture, new Rect(0, 0, 64, 64), new Vector2(.5f, .5f), 64);
            var visual = new GameObject("Sprite"); visual.transform.SetParent(root.transform); visual.transform.position = new Vector3(1000, 1000, 0);
            renderer = visual.AddComponent<SpriteRenderer>(); renderer.sprite = sprite;
            material = new Material(Shader.Find("TicGame/2D/Enemy Hit Ripple")); renderer.sharedMaterial = material;
        }
        [UnityTest]
        public IEnumerator PlayerDamage_RendersPureRedBand_OnBlueSprite()
        {
            Setup();
            var pixels = texture.GetPixels32();
            for (var index = 0; index < pixels.Length; index++) if (pixels[index].a > 0) pixels[index] = new Color32(0, 0, 255, 255);
            texture.SetPixels32(pixels); texture.Apply();
            var profile = ScriptableObject.CreateInstance<HitRippleProfileSO>();
            JsonUtility.FromJsonOverwrite("{\"blueColor\":{\"r\":1,\"g\":0,\"b\":0,\"a\":1}}", profile);
            var health = renderer.gameObject.AddComponent<SimpleHealth>(); health.Initialize();
            var presenter = renderer.gameObject.AddComponent<PlayerHitRipplePresenter>();
            presenter.Configure(health, profile, new[] { renderer }); presenter.SetManualPlayback(true);
            health.ApplyDamage(new DamageContext(null, renderer.gameObject, null, 1, new Vector2(1000, 1000), Vector2.right));
            presenter.Tick(profile.TraversalSeconds * .25f / (Mathf.Sqrt(.5f) + 4f / 64f), false);
            yield return null; yield return null;
            var result = Read();
            try
            {
                var band = result.GetPixel(87, 64);
                Assert.That(band.r, Is.GreaterThan(.8f)); Assert.That(band.g, Is.LessThan(.2f)); Assert.That(band.b, Is.LessThan(.2f));
                Assert.That(result.GetPixel(64, 64).b, Is.GreaterThan(.8f));
            }
            finally { Object.Destroy(result); Object.Destroy(profile); }
        }
        private void Upload(int count, Color leading, Color trailing, float strength)
        {
            var block = new MaterialPropertyBlock();
            block.SetFloat("_RippleCount", count); block.SetFloat("_RipplePixelsPerUnit", 64);
            block.SetVector("_RipplePixelGridOrigin", new Vector4(-.5f, -.5f, 0, 0));
            block.SetMatrix("_RippleVisualToOwner", Matrix4x4.identity); block.SetMatrix("_RippleOwnerToVisual", Matrix4x4.identity);
            for (var index = 0; index < 3; index++)
            {
                block.SetVector("_RippleOriginRadius" + index, new Vector4(0, 0, .25f, 4f / 64));
                block.SetColor("_RippleLeadingColor" + index, leading); block.SetColor("_RippleTrailingColor" + index, trailing);
                block.SetFloat("_RippleStrength" + index, strength);
            }
            renderer.SetPropertyBlock(block);
        }
        private Texture2D Read()
        {
            var previous = RenderTexture.active; RenderTexture.active = target;
            var result = new Texture2D(128, 128); result.ReadPixels(new Rect(0, 0, 128, 128), 0, 0); result.Apply();
            RenderTexture.active = previous; return result;
        }
        [UnityTest]
        public IEnumerator FullCircle_ColorsOnlyBand_AndPreservesTransparentPixels()
        {
            Setup(); Upload(1, Color.blue, Color.blue, 1);
            yield return null; yield return null;
            var result = Read();
            try
            {
                var right = result.GetPixel(87, 64); var up = result.GetPixel(64, 87); var center = result.GetPixel(64, 64);
                Assert.That(right.b, Is.GreaterThan(.8f)); Assert.That(right.r, Is.LessThan(.2f));
                Assert.That(up.b, Is.GreaterThan(.8f), "Full circle must travel vertically as well as horizontally.");
                Assert.That(center.r, Is.GreaterThan(.8f)); Assert.That(center.b, Is.LessThan(.2f));
                Assert.That(result.GetPixel(105, 105).a, Is.LessThan(.1f));
                Assert.That(result.GetPixel(41, 64).a, Is.LessThan(.1f), "The band must preserve transparent pixels inside its radius.");
            }
            finally { Object.Destroy(result); }
        }
        [UnityTest]
        public IEnumerator RejectedWhite_AndNewestEqualStrengthOverlapAreReadable()
        {
            Setup(); Upload(1, Color.white, Color.white, 1);
            yield return null; yield return null;
            var result = Read();
            try { var band = result.GetPixel(87, 64); Assert.That(band.r, Is.GreaterThan(.8f)); Assert.That(band.g, Is.GreaterThan(.8f)); Assert.That(band.b, Is.GreaterThan(.8f)); }
            finally { Object.Destroy(result); }
            Upload(3, Color.blue, Color.blue, 1);
            var block = new MaterialPropertyBlock(); renderer.GetPropertyBlock(block);
            block.SetColor("_RippleLeadingColor2", Color.green); block.SetColor("_RippleTrailingColor2", Color.green); renderer.SetPropertyBlock(block);
            yield return null; yield return null;
            result = Read();
            try { var band = result.GetPixel(87, 64); Assert.That(band.g, Is.GreaterThan(.8f)); Assert.That(band.b, Is.LessThan(.2f)); }
            finally { Object.Destroy(result); }
        }
        [UnityTest]
        public IEnumerator FatalHasBlueLeadingRedTrailing_AndThreeOverlapsDoNotWhiten()
        {
            Setup(); Upload(1, Color.blue, Color.red, 1);
            yield return null; yield return null;
            var result = Read();
            try
            {
                var inner = result.GetPixel(84, 64); var outer = result.GetPixel(90, 64);
                Assert.That(inner.r, Is.GreaterThan(inner.b)); Assert.That(outer.b, Is.GreaterThan(outer.r));
            }
            finally { Object.Destroy(result); }
            Upload(3, Color.blue, Color.blue, .8f);
            yield return null; yield return null;
            result = Read();
            try { var band = result.GetPixel(87, 64); Assert.That(band.g, Is.LessThan(.1f)); Assert.That(band.b, Is.GreaterThan(.7f)); }
            finally { Object.Destroy(result); }
        }
        [UnityTest]
        public IEnumerator DamageResolution_UsesOffCenterContact_InActualPresenterRendering()
        {
            Setup();
            var enemy = new GameObject("Offset contact enemy"); enemy.transform.SetParent(root.transform);
            enemy.transform.position = new Vector3(1000, 1000, 0);
            renderer.transform.SetParent(enemy.transform, true);
            var definition = ScriptableObject.CreateInstance<EnemyDefinitionSO>();
            var profile = ScriptableObject.CreateInstance<HitRippleProfileSO>();
            JsonUtility.FromJsonOverwrite("{\"traversalSeconds\":100,\"strength\":1,\"fadeOutFraction\":0,\"blueColor\":{\"r\":0,\"g\":0,\"b\":1,\"a\":1}}", profile);
            try
            {
                var actor = enemy.AddComponent<EnemyActor>(); actor.SetDefinition(definition); actor.Initialize();
                var presenter = enemy.AddComponent<EnemyHitRipplePresenter>(); presenter.Configure(actor, profile, new[] { renderer });
                enemy.AddComponent<EnemyHitRippleDamageListener>().Configure(presenter);
                var point = new Vector2(999.7f, 1000);
                var instance = new DamageInstance("off-center-render", null, null, new DamageFormulaValues(1, 1, 0, 0, 0, 0, 1));
                DamageResolver.Resolve(new DamageRequest(instance, new[] { enemy }, point, Vector2.right));
                var block = new MaterialPropertyBlock(); renderer.GetPropertyBlock(block);
                Assert.That(block.GetVector("_RippleOriginRadius0").x, Is.EqualTo(-.3f).Within(.0001));
                var distance = Mathf.Sqrt(.8f * .8f + .5f * .5f) + 4f / 64;
                presenter.Tick(100 * .12f / distance, false);
                yield return null; yield return null;
                var result = Read();
                try
                {
                    var displacedBand = result.GetPixel(32, 75);
                    Assert.That(displacedBand.b, Is.GreaterThan(.8f), "A hit away from the pivot must render its ring around the reported hit point.");
                    Assert.That(result.GetPixel(64, 64).b, Is.LessThan(.2f), "The sprite pivot must not become the hit origin.");
                }
                finally { Object.Destroy(result); }
            }
            finally { Object.Destroy(profile); Object.Destroy(definition); }
        }

        [TearDown]
        public void Cleanup()
        {
            Object.DestroyImmediate(root); Object.DestroyImmediate(material); Object.DestroyImmediate(sprite); Object.DestroyImmediate(texture);
            if (target != null) { target.Release(); Object.DestroyImmediate(target); }
        }
    }
}
