using NUnit.Framework;
using UnityEngine;

namespace TicGame.Architecture.Tests.Audio
{
    public sealed class AudioServiceTests
    {
        [Test]
        public void InvalidRequests_AreRejectedWithoutAllocatingVoices()
        {
            var owner = new GameObject("audio owner");
            var cue = ScriptableObject.CreateInstance<SoundCueSO>();
            try
            {
                var audio = owner.AddComponent<AudioService>();
                audio.Initialize();
                Assert.IsFalse(audio.TryPlayWorld(cue, Vector3.zero, owner));
                Assert.IsFalse(audio.TryPlayWorld(cue, new Vector3(float.NaN, 0, 0), owner));
                Assert.IsFalse(audio.TryPlayUi(cue, null));
                Assert.IsFalse(audio.SetVolume(AudioCategory.Sfx, float.NaN));
                Assert.IsFalse(audio.SetVolume(AudioCategory.Sfx, float.PositiveInfinity));
                Assert.AreEqual(1f, audio.GetVolume(AudioCategory.Sfx));
                Assert.AreEqual(0, audio.ActiveVoiceCount);
                audio.Shutdown();
                Assert.IsFalse(audio.IsInitialized);
            }
            finally { Object.DestroyImmediate(cue); Object.DestroyImmediate(owner); }
        }
    }
}
