using System;
using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace TicGame.Architecture.Tests.Settings
{
    public sealed class SettingsPersistenceTests
    {
        [Test]
        public void PlayerPrefsAdapter_RoundTripsOnlyItsUniqueKey()
        {
            var key = "TIC.Settings.Test." + Guid.NewGuid();
            try
            {
                var store = new PlayerPrefsSettingsStore();
                Assert.IsFalse(store.TryReadFloat(key, out _));
                store.WriteFloat(key, .37f);
                Assert.IsTrue(store.Flush());
                Assert.IsTrue(new PlayerPrefsSettingsStore().TryReadFloat(key, out var loaded));
                Assert.AreEqual(.37f, loaded);
            }
            finally { PlayerPrefs.DeleteKey(key); PlayerPrefs.Save(); }
        }

        [UnityTest]
        public IEnumerator DomainReloadDisabled_TwoSessionsHaveOneSettingsOwner()
        {
            var wasEnabled = EditorSettings.enterPlayModeOptionsEnabled;
            var previousOptions = EditorSettings.enterPlayModeOptions;
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
            try
            {
                for (var run = 0; run < 2; run++)
                {
                    yield return new EnterPlayMode(expectDomainReload: false);
                    yield return null;
                    var roots = UnityEngine.Object.FindObjectsByType<GameplayServicesRoot>(FindObjectsSortMode.None);
                    Assert.AreEqual(1, roots.Length);
                    var settings = UnityEngine.Object.FindObjectsByType<UserSettingsService>(FindObjectsSortMode.None);
                    Assert.AreEqual(1, settings.Length);
                    Assert.AreSame(settings[0], roots[0].Settings);
                    Assert.IsTrue(settings[0].IsInitialized);
                    Assert.IsFalse(settings[0].IsDirty);
                    Assert.IsTrue(roots[0].GetComponent<AudioService>().IsReady);
                    yield return new ExitPlayMode();
                    Assert.IsTrue(settings[0] == null);
                }
            }
            finally
            {
                EditorSettings.enterPlayModeOptions = previousOptions;
                EditorSettings.enterPlayModeOptionsEnabled = wasEnabled;
            }
        }
    }
}
