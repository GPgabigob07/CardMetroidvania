using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace TicGame.Architecture.Tests.Audio
{
    public sealed class AudioReloadTests
    {
        [UnityTest]
        public IEnumerator DomainReloadDisabled_TwoPlaySessionsHaveOneFreshAudioService()
        {
            var wasEnabled = EditorSettings.enterPlayModeOptionsEnabled;
            var previousOptions = EditorSettings.enterPlayModeOptions;
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
            try
            {
                for (var session = 0; session < 2; session++)
                {
                    yield return new EnterPlayMode(expectDomainReload: false);
                    var roots = Object.FindObjectsByType<GameplayServicesRoot>(FindObjectsSortMode.None);
                    Assert.AreEqual(1, roots.Length);
                    Assert.IsTrue(roots[0].IsInitialized);
                    Assert.NotNull(roots[0].Audio);
                    var audio = roots[0].GetComponent<AudioService>();
                    Assert.IsTrue(audio.IsInitialized);
                    Assert.AreEqual(24, audio.GetComponentsInChildren<AudioSource>().Length);
                    yield return new ExitPlayMode();
                    Assert.IsTrue(audio == null);
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
