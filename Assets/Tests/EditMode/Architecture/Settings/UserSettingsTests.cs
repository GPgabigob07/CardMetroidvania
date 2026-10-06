using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace TicGame.Architecture.Tests.Settings
{
    public sealed class UserSettingsTests
    {
        private sealed class Store : ISettingsStore
        {
            public readonly Dictionary<string, float> Values = new();
            public bool Fail; public bool ThrowWrite;
            public int Flushes;
            public bool TryReadFloat(string key, out float value) => Values.TryGetValue(key, out value);
            public void WriteFloat(string key, float value) { if (ThrowWrite) throw new System.IO.IOException("store unavailable"); Values[key] = value; }
            public bool Flush() { Flushes++; return !Fail; }
        }

        [Test]
        public void WriteFailureDuringShutdown_KeepsDesiredValuesUntilRetry()
        {
            var owner = new GameObject("settings write failure");
            var store = new Store { ThrowWrite = true };
            try
            {
                var settings = owner.AddComponent<UserSettingsService>();
                settings.Configure(null, store);
                settings.Initialize();
                settings.TrySetAudioVolume(AudioCategory.Music, .28f);
                settings.Shutdown();
                Assert.IsTrue(settings.IsDirty);
                settings.Initialize();
                Assert.AreEqual(.28f, settings.GetAudioVolume(AudioCategory.Music));
                store.ThrowWrite = false;
                Assert.IsTrue(settings.Save());
                Assert.AreEqual(.28f, store.Values["TIC.Settings.Audio.v1.Music"]);
            }
            finally { Object.DestroyImmediate(owner); }
        }

        [Test]
        public void Load_ValidatesValuesAndUsesDefaultsWithoutDirtying()
        {
            var owner = new GameObject("settings");
            try
            {
                var store = new Store();
                store.Values["TIC.Settings.Audio.v1.Master"] = float.NaN;
                store.Values["TIC.Settings.Audio.v1.Sfx"] = -1;
                store.Values["TIC.Settings.Audio.v1.Ui"] = 2;
                store.Values["TIC.Settings.Audio.v1.Music"] = float.PositiveInfinity;
                var settings = owner.AddComponent<UserSettingsService>();
                settings.Configure(null, store);
                settings.Initialize();
                Assert.AreEqual(1, settings.GetAudioVolume(AudioCategory.Master));
                Assert.AreEqual(0, settings.GetAudioVolume(AudioCategory.Sfx));
                Assert.AreEqual(1, settings.GetAudioVolume(AudioCategory.Ui));
                Assert.AreEqual(1, settings.GetAudioVolume(AudioCategory.Music));
                Assert.AreEqual(1, settings.GetAudioVolume(AudioCategory.Ambience));
                Assert.IsFalse(settings.IsDirty);
                Assert.IsFalse(settings.TrySetAudioVolume(AudioCategory.Sfx, float.NaN));
                Assert.IsFalse(settings.TrySetAudioVolume((AudioCategory)100, .5f));
                Assert.IsTrue(settings.TrySetAudioVolume(AudioCategory.Ui, 1));
                Assert.IsFalse(settings.IsDirty);
            }
            finally { Object.DestroyImmediate(owner); }
        }

        [Test]
        public void Changes_ResetAndSave_NotifyOnceAndPreserveUnrelatedKeys()
        {
            var owner = new GameObject("settings");
            try
            {
                var store = new Store();
                store.Values["unrelated"] = 42;
                var settings = owner.AddComponent<UserSettingsService>();
                settings.Configure(null, store);
                settings.Initialize();
                var notifications = 0;
                settings.Changed += () => notifications++;
                Assert.IsTrue(settings.TrySetAudioVolume(AudioCategory.Sfx, .25f));
                Assert.AreEqual(1, notifications);
                Assert.AreEqual(0, store.Flushes);
                Assert.IsTrue(settings.Save());
                Assert.AreEqual(1, store.Flushes);
                Assert.IsFalse(settings.IsDirty);
                Assert.AreEqual(.25f, store.Values["TIC.Settings.Audio.v1.Sfx"]);
                Assert.AreEqual(42, store.Values["unrelated"]);
                Assert.AreEqual(6, store.Values.Count);
                settings.ResetAudioDefaults();
                Assert.AreEqual(2, notifications);
                Assert.IsTrue(settings.IsDirty);
                Assert.AreEqual(1, settings.GetAudioVolume(AudioCategory.Sfx));
            }
            finally { Object.DestroyImmediate(owner); }
        }

        [Test]
        public void FailedSave_RetainsDirtyStateAndSupportsRetry()
        {
            var owner = new GameObject("settings");
            try
            {
                var store = new Store { Fail = true };
                var settings = owner.AddComponent<UserSettingsService>();
                settings.Configure(null, store);
                settings.Initialize();
                settings.TrySetAudioVolume(AudioCategory.Master, .4f);
                Assert.IsFalse(settings.Save());
                Assert.IsTrue(settings.IsDirty);
                Assert.AreEqual(.4f, settings.GetAudioVolume(AudioCategory.Master));
                store.Fail = false;
                Assert.IsTrue(settings.Save());
                Assert.IsFalse(settings.IsDirty);
                settings.Shutdown();
                settings.Initialize();
                Assert.AreEqual(.4f, settings.GetAudioVolume(AudioCategory.Master));
            }
            finally { Object.DestroyImmediate(owner); }
        }
    }
}



