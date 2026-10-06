using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace TicGame.Architecture.Tests.Settings
{
    public sealed class AudioSettingsPresenterTests
    {
        private sealed class Settings : IUserSettingsService
        {
            public readonly float[] Gains = { 1, .25f, 1, 1, 1 };
            public int Writes;
            public int Flushes;
            public bool IsDirty { get; private set; }
            public event Action Changed;
            public float GetAudioVolume(AudioCategory category) => Gains[(int)category];
            public bool TrySetAudioVolume(AudioCategory category, float gain) { Gains[(int)category] = gain; Writes++; IsDirty = true; Changed?.Invoke(); return true; }
            public void ResetAudioDefaults() { for (var index = 0; index < 5; index++) Gains[index] = 1; IsDirty = true; Changed?.Invoke(); }
            public bool Save() { if (IsDirty) Flushes++; IsDirty = false; return true; }
        }

        [Test]
        public void OpenRefreshAndReopen_DoNotDuplicateCallbacks_AndCloseSaves()
        {
            var root = new GameObject("audio settings panel", typeof(RectTransform));
            try
            {
                var sliders = new Slider[5];
                var labels = new Text[5];
                for (var index = 0; index < 5; index++)
                {
                    var row = new GameObject("row", typeof(RectTransform)); row.transform.SetParent(root.transform);
                    sliders[index] = row.AddComponent<Slider>(); sliders[index].maxValue = 100; sliders[index].wholeNumbers = true;
                    labels[index] = new GameObject("value", typeof(RectTransform)).AddComponent<Text>(); labels[index].transform.SetParent(root.transform);
                }
                var reset = new GameObject("reset", typeof(RectTransform)).AddComponent<Button>(); reset.transform.SetParent(root.transform);
                var back = new GameObject("back", typeof(RectTransform)).AddComponent<Button>(); back.transform.SetParent(root.transform);
                var panel = root.AddComponent<AudioSettingsPanel>(); panel.Configure(sliders, labels, reset, back);
                var presenter = root.AddComponent<AudioSettingsPresenter>(); presenter.Configure(panel);
                var settings = new Settings(); presenter.Bind(settings); presenter.Open();
                Assert.AreEqual(25, sliders[1].value);
                Assert.AreEqual("25%", labels[1].text);
                Assert.AreEqual(0, settings.Writes);
                presenter.Open();
                sliders[1].value = 50;
                Assert.AreEqual(1, settings.Writes);
                Assert.AreEqual(.5f, settings.Gains[1]);
                Assert.AreEqual("50%", labels[1].text);
                Assert.AreEqual(0, settings.Flushes);
                presenter.Close();
                Assert.AreEqual(1, settings.Flushes);
                presenter.Open();
                sliders[1].value = 75;
                Assert.AreEqual(2, settings.Writes);
                reset.onClick.Invoke();
                foreach (var label in labels) Assert.AreEqual("100%", label.text);
                presenter.Close(); presenter.Close();
                Assert.AreEqual(2, settings.Flushes);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }
    }
}
