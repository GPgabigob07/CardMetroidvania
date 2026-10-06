using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TicGame.Architecture.Tests.Audio
{
    public sealed class AudioPlaybackTests
    {
        [UnityTest]
        public IEnumerator ConfiguredBootstrap_DuplicateRootDoesNotCreateAnotherAudioPool()
        {
            var root = Object.FindFirstObjectByType<GameplayServicesRoot>();
            Assert.NotNull(root);
            Assert.IsTrue(root.IsInitialized);
            Assert.NotNull(root.Audio);
            var audio = root.GetComponent<AudioService>();
            Assert.NotNull(audio);
            Assert.IsTrue(audio.IsInitialized);
            var duplicate = Object.Instantiate(Resources.Load<GameObject>("Runtime/GameplayServices"));
            yield return null;
            Assert.IsTrue(duplicate == null);
            Assert.AreEqual(1, Object.FindObjectsByType<GameplayServicesRoot>(FindObjectsSortMode.None).Length);
            Assert.AreEqual(24, audio.GetComponentsInChildren<AudioSource>().Length);
        }

#if UNITY_EDITOR
        [UnityTest]
        public IEnumerator NativeRandomContainer_PlaysOnceAtInvocationPosition_ThenReleases()
        {
            var root = new GameObject("native audio test");
            var owner = new GameObject("native invoker");
            var original = UnityEditor.AssetDatabase.LoadAssetAtPath<SoundCueSO>("Assets/Data/Audio/Cues/Sound_Swing.asset");
            Assert.NotNull(original);
            var cue = Object.Instantiate(original);
            try
            {
                var mixer = Resources.Load<AudioMixer>("AudioTestMixer");
                cue.Configure(cue.Resource, mixer.FindMatchingGroups("Master")[0]);
                var audio = root.AddComponent<AudioService>();
                audio.Initialize();
                Assert.IsTrue(audio.TryPlayWorld(cue, new Vector3(3, 4, 0), owner));
                var source = root.GetComponentInChildren<AudioSource>();
                Assert.AreSame(cue.Resource, source.resource);
                Assert.AreEqual(new Vector3(3, 4, 0), source.transform.position);
                yield return new WaitForSecondsRealtime(0.03f);
                Assert.IsTrue(source.isPlaying, "Native container must actually play a seeded clip.");
                yield return new WaitForSecondsRealtime(3f);
                Assert.AreEqual(0, audio.ActiveVoiceCount, "Manual single-clip container must complete and release its voice.");
            }
            finally { Object.DestroyImmediate(root); Object.DestroyImmediate(owner); Object.DestroyImmediate(cue); }
        }
#endif
        [UnityTest]
        public IEnumerator FixedPositions_PauseAndReuse_KeepVoicesIndependent()
        {
            var root = new GameObject("test audio service");
            var owner = new GameObject("invoker");
            var cue = ScriptableObject.CreateInstance<SoundCueSO>();
            var clip = AudioClip.Create("long test tone", 44100 * 5, 1, 44100, false);
            try
            {
                var mixer = Resources.Load<AudioMixer>("AudioTestMixer");
                Assert.NotNull(mixer);
                cue.Configure(clip, mixer.FindMatchingGroups("Master")[0]);
                var audio = root.AddComponent<AudioService>();
                audio.Configure(null, 2);
                audio.Initialize();
                Assert.IsTrue(audio.TryPlayWorld(cue, new Vector3(2, 3, 0), owner));
                Assert.IsTrue(audio.TryPlayWorld(cue, new Vector3(-4, 1, 0), owner));
                var sources = root.GetComponentsInChildren<AudioSource>();
                Assert.AreEqual(new Vector3(2, 3, 0), sources[0].transform.position);
                Assert.AreEqual(new Vector3(-4, 1, 0), sources[1].transform.position);
                owner.transform.position = Vector3.one * 30;
                audio.SetWorldPaused(true);
                yield return null;
                yield return null;
                yield return null;
                Assert.AreEqual(2, audio.ActiveVoiceCount);
                Assert.AreEqual(new Vector3(2, 3, 0), sources[0].transform.position);
                Assert.IsFalse(audio.TryPlayWorld(cue, Vector3.zero, owner));
                cue.ConfigurePolicy(10, 4, 0);
                Assert.IsTrue(audio.TryPlayUi(cue, owner));
                Assert.AreEqual(0, sources[0].spatialBlend);
                Assert.AreEqual(Vector3.zero, sources[0].transform.position);
                audio.SetWorldPaused(false);
                cue.ConfigurePolicy(0, 4, 0);
                Assert.IsTrue(audio.TryPlayWorld(cue, new Vector3(7, 8, 0), owner));
                Assert.That(sources, Has.Some.Matches<AudioSource>(source => source.transform.position == new Vector3(7, 8, 0) && source.spatialBlend == 1));
                Assert.That(sources, Has.All.Matches<AudioSource>(source => source.dopplerLevel == 0 && source.pitch == 1));
                Object.DestroyImmediate(owner);
                audio.ReclaimFinishedVoices();
                Assert.AreEqual(0, audio.ActiveVoiceCount);
            }
            finally { Object.DestroyImmediate(root); if (owner != null) Object.DestroyImmediate(owner); Object.DestroyImmediate(cue); Object.DestroyImmediate(clip); }
        }

        [UnityTest]
        public IEnumerator SceneUnload_ReleasesOnlyTheUnloadedScenesVoice()
        {
            var root = new GameObject("test audio service");
            var owner = new GameObject("surviving owner");
            var other = new GameObject("scene owner");
            var scene = SceneManager.CreateScene("AudioOwnerTest");
            SceneManager.MoveGameObjectToScene(other, scene);
            var cue = ScriptableObject.CreateInstance<SoundCueSO>();
            var clip = AudioClip.Create("test sound", 44100 * 5, 1, 44100, false);
            try
            {
                cue.Configure(clip, Resources.Load<AudioMixer>("AudioTestMixer").FindMatchingGroups("Master")[0]);
                var audio = root.AddComponent<AudioService>();
                audio.Initialize();
                Assert.IsTrue(audio.TryPlayWorld(cue, Vector3.left, owner));
                Assert.IsTrue(audio.TryPlayWorld(cue, Vector3.right, other));
                audio.SetWorldPaused(true);
                yield return SceneManager.UnloadSceneAsync(scene);
                Assert.AreEqual(1, audio.ActiveVoiceCount);
                audio.Shutdown();
                audio.Initialize();
                Assert.AreEqual(24, root.GetComponentsInChildren<AudioSource>().Length);
            }
            finally { Object.DestroyImmediate(root); Object.DestroyImmediate(owner); if (other != null) Object.DestroyImmediate(other); Object.DestroyImmediate(cue); Object.DestroyImmediate(clip); }
        }
    }
}
