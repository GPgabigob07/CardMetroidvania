using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace TicGame.Architecture.Tests.Settings
{
    public sealed class SettingsReadinessTests
    {
        private sealed class Store : ISettingsStore
        {
            public bool TryReadFloat(string key, out float value) { value = key.EndsWith(".Sfx") ? .25f : 1; return true; }
            public void WriteFloat(string key, float value) { }
            public bool Flush() => true;
        }
        private sealed class Audio : IAudioSettingsService
        {
            public bool IsReady { get; private set; }
            public event Action Ready;
            public bool Fail;
            public int Writes;
            public readonly Dictionary<AudioCategory, float> Values = new();
            public bool SetVolume(AudioCategory category, float gain) { Writes++; if (Fail) return false; Values[category] = gain; return true; }
            public float GetVolume(AudioCategory category) => Values.TryGetValue(category, out var gain) ? gain : 1;
            public void MakeReady() { IsReady = true; Ready?.Invoke(); }
        }

        [Test]
        public void LateReadiness_AppliesSavedPreferencesOnce_AndChangesAreImmediate()
        {
            var owner = new GameObject("settings ready");
            try
            {
                var audio = new Audio();
                var settings = owner.AddComponent<UserSettingsService>();
                settings.Configure(audio, new Store());
                settings.Initialize();
                Assert.AreEqual(0, audio.Writes);
                audio.MakeReady();
                Assert.AreEqual(5, audio.Writes);
                Assert.AreEqual(.25f, audio.GetVolume(AudioCategory.Sfx));
                Assert.IsFalse(settings.IsDirty);
                settings.TrySetAudioVolume(AudioCategory.Sfx, .5f);
                Assert.AreEqual(.5f, audio.GetVolume(AudioCategory.Sfx));
                Assert.AreEqual(6, audio.Writes);
                settings.Shutdown();
                audio.MakeReady();
                Assert.AreEqual(6, audio.Writes);
            }
            finally { UnityEngine.Object.DestroyImmediate(owner); }
        }

        [Test]
        public void AlreadyReady_FailedApplicationRetainsDesiredValuesForRetry()
        {
            var owner = new GameObject("settings ready");
            try
            {
                var audio = new Audio { Fail = true };
                audio.MakeReady();
                var settings = owner.AddComponent<UserSettingsService>();
                settings.Configure(audio, new Store());
                settings.Initialize();
                Assert.AreEqual(5, audio.Writes);
                Assert.AreEqual(.25f, settings.GetAudioVolume(AudioCategory.Sfx));
                Assert.IsFalse(settings.IsDirty);
                audio.Fail = false;
                Assert.IsTrue(settings.ApplyAudioSettings());
                Assert.AreEqual(.25f, audio.GetVolume(AudioCategory.Sfx));
            }
            finally { UnityEngine.Object.DestroyImmediate(owner); }
        }
    }
}
