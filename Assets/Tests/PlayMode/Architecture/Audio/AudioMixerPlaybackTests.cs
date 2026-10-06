#if UNITY_EDITOR
using System;
using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.TestTools;
namespace TicGame.Architecture.Tests.Audio
{
    public sealed class AudioMixerPlaybackTests
    {
        [UnityTest]
        public IEnumerator ConfiguredMixer_ExposesIndependentGains_AndRestoresAuthoredLevels()
        {
            var mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>("Assets/Data/Audio/PrototypeAudio.mixer");
            Assert.NotNull(mixer);
            if (mixer.FindMatchingGroups("").Length < 7) Assert.Ignore("Native mixer handoff is not saved yet.");
            var root = new GameObject("mixer settings test");
            var audio = root.AddComponent<AudioService>();
            audio.Configure(mixer);
            audio.Initialize();
            yield return null;
            var names = new[] { "MasterVolume", "SfxVolume", "UiVolume", "MusicVolume", "AmbienceVolume" };
            try
            {
                foreach (AudioCategory category in Enum.GetValues(typeof(AudioCategory))) Assert.IsTrue(audio.SetVolume(category, 1));
                Assert.IsTrue(audio.SetVolume(AudioCategory.Sfx, 0.1f));
                Assert.IsTrue(mixer.GetFloat("SfxVolume", out var sfx));
                Assert.That(sfx, Is.EqualTo(-20).Within(0.001));
                foreach (var name in new[] { "UiVolume", "MusicVolume", "AmbienceVolume", "MasterVolume" })
                { Assert.IsTrue(mixer.GetFloat(name, out var other)); Assert.AreEqual(0, other); }
                Assert.IsTrue(audio.SetVolume(AudioCategory.Master, 0));
                Assert.IsTrue(mixer.GetFloat("MasterVolume", out var master));
                Assert.AreEqual(-80, master);
                Assert.AreEqual(0.1f, audio.GetVolume(AudioCategory.Sfx));
                Assert.IsTrue(audio.SetVolume(AudioCategory.Sfx, 1));
                Assert.IsTrue(audio.SetVolume(AudioCategory.Master, 1));
                foreach (var name in names) { Assert.IsTrue(mixer.GetFloat(name, out var restored)); Assert.AreEqual(0, restored); }
            }
            finally { foreach (var name in names) mixer.ClearFloat(name); UnityEngine.Object.DestroyImmediate(root); }
        }

    }
}
#endif

