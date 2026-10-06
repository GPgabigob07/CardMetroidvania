using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace TicGame.Architecture.Tests
{
    public sealed class HitRippleLabPlayModeTests
    {
        private HitRippleTestController controller;
        private Camera camera;
        [UnitySetUp]
        public IEnumerator LoadLab()
        {
#if UNITY_EDITOR
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/Test_HitRipple.unity", new LoadSceneParameters(LoadSceneMode.Single));
#else
            Assert.Ignore("The diagnostic authoring scene runs in the Editor.");
#endif
            yield return null;
            controller = Object.FindFirstObjectByType<HitRippleTestController>();
            camera = Camera.main; Physics2D.SyncTransforms();
            Assert.That(controller, Is.Not.Null); Assert.That(camera, Is.Not.Null);
        }
        [UnityTearDown]
        public IEnumerator UnloadLab()
        {
            var scene = controller != null ? controller.gameObject.scene : default;
            var empty = SceneManager.CreateScene("Ripple lab fixture cleanup"); SceneManager.SetActiveScene(empty);
            if (scene.IsValid() && scene.isLoaded) yield return SceneManager.UnloadSceneAsync(scene);
        }
        [UnityTest]
        public IEnumerator ActualScreenHitDetection_PreservesDistinctPoints_AndAllThreeOutcomes()
        {
            var target = Object.FindObjectsByType<HitRippleTestTarget>(FindObjectsSortMode.None).Single(value => value.Owner.name.StartsWith("Small"));
            var presenter = target.Owner.GetComponent<EnemyHitRipplePresenter>();
            var offsets = new[] { new Vector2(-.5f, .3f), new Vector2(.5f, -.3f), new Vector2(0, .7f) };
            var kinds = new[] { HitRippleKind.Damage, HitRippleKind.Rejected, HitRippleKind.Fatal };
            for (var index = 0; index < 3; index++)
            {
                var point = (Vector2)target.Owner.transform.position + offsets[index];
                var report = controller.HitScreenPoint(camera.WorldToScreenPoint(point), kinds[index]);
                Assert.That(report, Is.Not.Null); Assert.That(report.TargetResults.Count, Is.EqualTo(1));
                Assert.That(Vector2.Distance(report.TargetResults[0].Context.HitPoint, point), Is.LessThan(.0001));
                Assert.That(HitRippleOutcome.Classify(report.TargetResults[0].Result), Is.EqualTo(kinds[index]));
            }
            Assert.That(presenter.ActiveCount, Is.EqualTo(3));
            var block = new MaterialPropertyBlock(); presenter.Visuals[0].GetPropertyBlock(block);
            for (var index = 0; index < 3; index++)
            {
                var wave = block.GetVector("_RippleOriginRadius" + index);
                Assert.That(Vector2.Distance(new Vector2(wave.x, wave.y), offsets[index]), Is.LessThan(.0001));
            }
            controller.SetPaused(true); presenter.Tick(.05f, false); presenter.Visuals[0].GetPropertyBlock(block);
            var radius = block.GetVector("_RippleOriginRadius0").z;
            yield return null; yield return null;
            presenter.Visuals[0].GetPropertyBlock(block);
            Assert.That(block.GetVector("_RippleOriginRadius0").z, Is.EqualTo(radius));
            Assert.That(target.Owner.Health.IsDefeated, Is.False, "Simulated fatal feedback must not hide the diagnostic sprite.");
        }
        [UnityTest]
        public IEnumerator ThreeSingleSpriteTargets_RenderOffCenterContacts_IncludingOffsetPivot()
        {
            var texture = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32); texture.Create();
            camera.targetTexture = texture;
            var targets = Object.FindObjectsByType<HitRippleTestTarget>(FindObjectsSortMode.None).OrderBy(value => value.Owner.transform.position.x).ToArray();
            foreach (var target in targets)
            {
                var point = (Vector2)target.Owner.transform.position + new Vector2(-.5f, .3f);
                var report = controller.HitScreenPoint(camera.WorldToScreenPoint(point), HitRippleKind.Damage);
                Assert.That(report, Is.Not.Null);
                target.Owner.GetComponent<EnemyHitRipplePresenter>().Tick(.1f, false);
            }
            controller.SetPaused(true);
            yield return null; yield return null;
            var previous = RenderTexture.active; RenderTexture.active = texture;
            var capture = new Texture2D(1280, 720); capture.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); capture.Apply();
            RenderTexture.active = previous;
            try
            {
                foreach (var target in targets)
                {
                    var presenter = target.Owner.GetComponent<EnemyHitRipplePresenter>();
                    var visual = presenter.Visuals[0];
                    var block = new MaterialPropertyBlock(); visual.GetPropertyBlock(block);
                    var wave = block.GetVector("_RippleOriginRadius0");
                    var world = target.Owner.transform.TransformPoint(new Vector3(wave.x + wave.z - .05f, wave.y, 0));
                    var screen = camera.WorldToScreenPoint(world);
                    var color = capture.GetPixel(Mathf.RoundToInt(screen.x), Mathf.RoundToInt(screen.y));
                    Assert.That(color.b, Is.GreaterThan(.7f), target.Owner.name + " must render a bright band around its off-center hit.");
                }
                var outputFolder = Path.GetFullPath(Path.Combine(Application.dataPath, "../../HitRippleLabWork"));
                Directory.CreateDirectory(outputFolder); File.WriteAllBytes(Path.Combine(outputFolder, "lab-preview.png"), capture.EncodeToPNG());
            }
            finally { Object.Destroy(capture); camera.targetTexture = null; texture.Release(); Object.Destroy(texture); }
        }
    }
}
