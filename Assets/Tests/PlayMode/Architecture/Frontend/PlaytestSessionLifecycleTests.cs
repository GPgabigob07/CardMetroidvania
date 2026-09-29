using System.Collections;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TicGame.Architecture.Tests
{
    public sealed class PlaytestSessionLifecycleTests
    {
        [UnityTest]
        public IEnumerator Title_StartPauseReturn_ThreeRunsHaveFreshProgress()
        {
            yield return SceneManager.LoadSceneAsync(PlaytestSessionController.TitleScene);
            yield return null;
            var session = PlaytestSessionController.Instance;
            Assert.NotNull(session);
            Assert.IsTrue(session.IsTitle);
            yield return Capture("title");
            foreach (var button in session.GetComponentsInChildren<Button>())
                if (button.GetComponentInChildren<Text>().text == "Controls") button.onClick.Invoke();
            yield return null;
            yield return Capture("controls");
            session.GetComponent<PlaytestMenuView>().Back();
            for (var run = 0; run < 3; run++)
            {
                var start = session.StartPlaytestAsync();
                var duplicate = session.StartPlaytestAsync();
                Assert.IsTrue(duplicate.IsCompleted);
                Assert.IsFalse(duplicate.Result);
                yield return Await(start);
                Assert.IsTrue(start.Result, session.LastError);
                Assert.IsTrue(session.Gameplay.IsReady);
                Assert.AreEqual(1, Object.FindObjectsByType<PlayerController>(FindObjectsSortMode.None).Length);
                Assert.AreEqual(1, Object.FindObjectsByType<GameplayServicesRoot>(FindObjectsSortMode.None).Length);
                Assert.IsFalse(session.Gameplay.Progress.IsGateOpen("blue", "test-gate"));
                session.Gameplay.Progress.OpenGate("blue", "test-gate");
                yield return null;
                var pause = session.GetComponent<PlaytestPauseController>();
                var body = session.Gameplay.Player.GetComponent<Rigidbody2D>();
                body.linearVelocity = new Vector2(4, 7);
                Assert.IsTrue(pause.TryPause());
                yield return new WaitForSecondsRealtime(.1f);
                Assert.AreEqual(0f, Time.timeScale);
                Assert.AreEqual(new Vector2(4, 7), body.linearVelocity);
                if (run == 0) yield return Capture("pause");
                var cardTime = session.Services.GetComponent<CardTimeSessionController>();
                var source = cardTime.RegisterPlayerSource(session.Gameplay.Player);
                Assert.NotNull(source, "Reuse the registered player source, not a second owner.");
                source.PublishAvailability(new CardTimeOpportunity(PlayerCardTimeState.Chain, 1));
                Assert.AreEqual(CardTimeActivationRequestResult.Activated, source.RequestActivation());
                var remaining = cardTime.Current.ActiveRemaining;
                yield return new WaitForSecondsRealtime(.15f);
                Assert.AreEqual(remaining, cardTime.Current.ActiveRemaining, "Card Time expired behind pause.");
                pause.Resume();
                Assert.Less(Time.timeScale, 1f, "Resume removed the Card Time modifier.");
                source.Cancel();
                Assert.AreEqual(1f, Time.timeScale);
                var back = session.ReturnToTitleAsync();
                yield return Await(back);
                Assert.IsTrue(back.Result, session.LastError);
                Assert.IsTrue(session.IsTitle);
                Assert.AreEqual(0, Object.FindObjectsByType<PlayerController>(FindObjectsSortMode.None).Length);
                Assert.AreEqual(1f, Time.timeScale);
                Assert.IsFalse(SceneManager.GetSceneByPath("Assets/Scenes/BlueArea_Tutorial.unity").isLoaded);
                Assert.IsFalse(SceneManager.GetSceneByPath("Assets/Scenes/PinkArea_Perimeters.unity").isLoaded);
            }
        }

        [UnityTest]
        public IEnumerator DirectGameplayEntry_ReturnDuringStreaming_SettlesAndResets()
        {
            yield return SceneManager.LoadSceneAsync(PlaytestSessionController.GameplayScene);
            var deadline = Time.realtimeSinceStartup + 20;
            while (PlaytestSessionController.Instance?.Gameplay?.IsReady != true && Time.realtimeSinceStartup < deadline)
                yield return null;
            var session = PlaytestSessionController.Instance;
            Assert.IsTrue(session.Gameplay.IsReady, session.LastError);
            var loading = SceneStreamingService.RequestAsync("Assets/Scenes/PinkArea_Perimeters.unity", true);
            var back = session.ReturnToTitleAsync();
            yield return Await(back);
            Assert.IsTrue(loading.IsCompleted);
            Assert.IsTrue(back.Result, session.LastError);
            Assert.IsFalse(SceneManager.GetSceneByPath("Assets/Scenes/PinkArea_Perimeters.unity").isLoaded);
        }

        private static IEnumerator Await(Task<bool> task)
        {
            var deadline = Time.realtimeSinceStartup + 25;
            while (!task.IsCompleted && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.IsTrue(task.IsCompleted, "Transition exceeded test timeout.");
        }

        private static IEnumerator Capture(string name)
        {
            yield return null;
            yield return null;
            if (SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null)
            {
                var canvas = PlaytestSessionController.Instance.GetComponentInChildren<Canvas>();
                var camera = Object.FindFirstObjectByType<Camera>();
                var target = new RenderTexture(1280, 720, 24);
                var previousMode = canvas.renderMode;
                var previousCamera = canvas.worldCamera;
                var previousTarget = camera.targetTexture;
                var previousActive = RenderTexture.active;
                camera.targetTexture = target;
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 1;
                Canvas.ForceUpdateCanvases();
                camera.Render();
                RenderTexture.active = target;
                var image = new Texture2D(1280, 720, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
                image.Apply();
                System.IO.File.WriteAllBytes(System.IO.Path.GetFullPath(".utmp/playtest/" + name + ".png"), image.EncodeToPNG());
                canvas.renderMode = previousMode;
                canvas.worldCamera = previousCamera;
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                Object.Destroy(image);
                Object.Destroy(target);
            }
        }
    }
}
