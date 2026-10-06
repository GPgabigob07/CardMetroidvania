using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TicGame.Architecture.Tests.Settings
{
    public sealed class SettingsFrontendTests
    {
        [UnityTest]
        public IEnumerator TitleAndPause_ApplySaveResetAndBackPreserveParent()
        {
            yield return SceneManager.LoadSceneAsync(PlaytestSessionController.TitleScene);
            yield return null;
            yield return null;
            var session = PlaytestSessionController.Instance;
            var menu = session.GetComponent<PlaytestMenuView>();
            var service = session.Services.GetComponent<UserSettingsService>();
            var audio = session.Services.GetComponent<AudioService>();
            var store = new Store();
            service.Shutdown();
            service.Configure(audio, store);
            service.Initialize();
            try
            {
                var entry = FindSettingsButton(session);
                EventSystem.current.SetSelectedGameObject(entry.gameObject);
                menu.OpenSettings();
                yield return null;
                yield return null;
                var panel = session.GetComponentInChildren<AudioSettingsPanel>();
                Assert.NotNull(panel);
                Assert.AreEqual(panel.Sliders[0].gameObject, EventSystem.current.currentSelectedGameObject);
                panel.Sliders[0].value = 63;
                Assert.AreEqual(.63f, service.GetAudioVolume(AudioCategory.Master), .00001f);
                Assert.IsTrue(service.IsDirty);
#if UNITY_EDITOR
                var mixer = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Audio.AudioMixer>("Assets/Data/Audio/PrototypeAudio.mixer");
                Assert.IsTrue(mixer.GetFloat("MasterVolume", out var decibels));
                Assert.AreEqual(AudioVolumeMath.ToDecibels(.63f), decibels, .001f);
#endif
                Assert.AreEqual(0, store.Flushes);
                var move = new AxisEventData(EventSystem.current) { moveDir = MoveDirection.Left };
                ExecuteEvents.Execute(panel.Sliders[0].gameObject, move, ExecuteEvents.moveHandler);
                Assert.AreEqual(62, panel.Sliders[0].value, "Native slider navigation must change by one percent.");
                var previousBackground = InputSystem.settings.backgroundBehavior;
                var previousEditorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
                InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
                InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                var keyboard = InputSystem.AddDevice<Keyboard>();
                var gamepad = InputSystem.AddDevice<Gamepad>();
                try
                {
                    // Allow the UI actions to resolve newly added devices before the first press.
                    yield return null;
                    var module = EventSystem.current.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
                    Assert.IsTrue(module.move.action.enabled);
                    Assert.IsNotEmpty(module.move.action.controls);
                    InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.RightArrow));
                    InputSystem.Update();
                    module.Process();
                    yield return null;
                    yield return null;
                    InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                    yield return null;
                    Assert.AreEqual(63, panel.Sliders[0].value, "Keyboard right must move by one percent. Focus=" + EventSystem.current.isFocused + " selected=" + EventSystem.current.currentSelectedGameObject?.name + " action=" + EventSystem.current.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>().move?.action?.ReadValue<Vector2>());
                    InputSystem.QueueStateEvent(gamepad, new GamepadState { leftStick = Vector2.left }); InputSystem.Update(); EventSystem.current.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>().Process();
                    yield return null;
                    yield return null;
                    InputSystem.QueueStateEvent(gamepad, new GamepadState());
                    yield return null;
                    Assert.AreEqual(62, panel.Sliders[0].value, "Gamepad left must move by one percent.");
                    // Native pointer handling is used by the slider, including click-to-seek.
                    Canvas.ForceUpdateCanvases();
                    var rect = (RectTransform)panel.Sliders[1].transform;
                    var corners = new Vector3[4];
                    rect.GetWorldCorners(corners);
                    var pointer = new PointerEventData(EventSystem.current)
                    {
                        position = RectTransformUtility.WorldToScreenPoint(null, (corners[0] + corners[2]) * .5f),
                        button = PointerEventData.InputButton.Left
                    };
                    ExecuteEvents.Execute(panel.Sliders[1].gameObject, pointer, ExecuteEvents.pointerDownHandler);
                    ExecuteEvents.Execute(panel.Sliders[1].gameObject, pointer, ExecuteEvents.pointerUpHandler);
                    Assert.AreEqual(50, panel.Sliders[1].value, 1, "Mouse click at midpoint must seek halfway.");
                }
                finally
                {
                    InputSystem.RemoveDevice(keyboard);
                    InputSystem.RemoveDevice(gamepad);
                    InputSystem.settings.backgroundBehavior = previousBackground;
                    InputSystem.settings.editorInputBehaviorInPlayMode = previousEditorInput;
                }
                yield return Capture(panel, 1280, 720);
                yield return Capture(panel, 1920, 1080);
                menu.Back();
                yield return null;
                yield return null;
                Assert.IsFalse(menu.IsSettingsOpen);
                Assert.AreEqual(1, store.Flushes);
                Assert.AreEqual(entry.gameObject, EventSystem.current.currentSelectedGameObject);
                service.Shutdown();
                service.Initialize();
                Assert.AreEqual(.62f, service.GetAudioVolume(AudioCategory.Master), .00001f);
                var start = session.StartPlaytestAsync();
                yield return Await(start);
                Assert.IsTrue(start.Result, session.LastError);
                yield return null;
                var pause = session.GetComponent<PlaytestPauseController>();
                Assert.IsTrue(pause.TryPause());
                yield return null;
                yield return null;
                entry = FindSettingsButton(session);
                EventSystem.current.SetSelectedGameObject(entry.gameObject);
                menu.OpenSettings();
                yield return null;
                yield return null;
                panel = session.GetComponentInChildren<AudioSettingsPanel>();
                Assert.AreEqual(62, panel.Sliders[0].value);
                panel.ResetButton.onClick.Invoke();
                foreach (var slider in panel.Sliders) Assert.AreEqual(100, slider.value);
                panel.BackButton.onClick.Invoke();
                menu.Back(); // Duplicate cancel delivery in one frame cannot resume gameplay.
                Assert.IsTrue(pause.IsPaused);
                Assert.AreEqual(0, Time.timeScale);
                yield return null;
                yield return null;
                Assert.AreEqual(entry.gameObject, EventSystem.current.currentSelectedGameObject);
                Assert.AreEqual(2, store.Flushes);
                menu.Back();
                Assert.IsFalse(pause.IsPaused);
                var back = session.ReturnToTitleAsync();
                yield return Await(back);
                Assert.IsTrue(back.Result, session.LastError);
                service.TrySetAudioVolume(AudioCategory.Ui, .41f);
                service.SendMessage("OnApplicationPause", true);
                Assert.AreEqual(3, store.Flushes);
                service.SendMessage("OnApplicationPause", true);
                Assert.AreEqual(3, store.Flushes, "Clean lifecycle events must not flush again.");
                service.TrySetAudioVolume(AudioCategory.Ui, .52f);
                service.SendMessage("OnApplicationFocus", false);
                Assert.AreEqual(4, store.Flushes);
                service.TrySetAudioVolume(AudioCategory.Ui, .61f);
                service.SendMessage("OnApplicationQuit");
                Assert.AreEqual(5, store.Flushes);
                Assert.IsFalse(service.IsDirty);
            }
            finally
            {
                service.Shutdown();
                service.Configure(audio, new PlayerPrefsSettingsStore());
                service.Initialize();
            }
        }

        private static Button FindSettingsButton(PlaytestSessionController session)
        {
            foreach (var button in session.GetComponentsInChildren<Button>())
                if (button.GetComponentInChildren<Text>()?.text == "Settings") return button;
            Assert.Fail("Settings entry missing.");
            return null;
        }

        private static IEnumerator Await(Task<bool> task)
        {
            var deadline = Time.realtimeSinceStartup + 25;
            while (!task.IsCompleted && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.IsTrue(task.IsCompleted, "Transition timed out.");
        }

        private static IEnumerator Capture(AudioSettingsPanel panel, int width, int height)
        {
            yield return null;
            var directory = System.Environment.GetEnvironmentVariable("TIC_SETTINGS_CAPTURE_DIR");
            if (string.IsNullOrEmpty(directory) || SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null) yield break;
            var canvas = panel.GetComponentInParent<Canvas>();
            var camera = Object.FindFirstObjectByType<Camera>();
            var target = new RenderTexture(width, height, 24);
            var previousMode = canvas.renderMode;
            var previousCamera = canvas.worldCamera;
            var previousDistance = canvas.planeDistance;
            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;
            var image = new Texture2D(width, height, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = target;
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 1;
                Canvas.ForceUpdateCanvases();
                camera.Render();
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                image.Apply();
                System.IO.Directory.CreateDirectory(directory);
                System.IO.File.WriteAllBytes(System.IO.Path.Combine(directory, "settings-" + width + ".png"), image.EncodeToPNG());
            }
            finally
            {
                canvas.renderMode = previousMode;
                canvas.worldCamera = previousCamera;
                canvas.planeDistance = previousDistance;
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                Object.Destroy(image);
                Object.Destroy(target);
            }
        }

        private sealed class Store : ISettingsStore
        {
            private readonly Dictionary<string, float> values = new();
            public int Flushes;
            public bool TryReadFloat(string key, out float value) => values.TryGetValue(key, out value);
            public void WriteFloat(string key, float value) => values[key] = value;
            public bool Flush() { Flushes++; return true; }
        }
    }
}








