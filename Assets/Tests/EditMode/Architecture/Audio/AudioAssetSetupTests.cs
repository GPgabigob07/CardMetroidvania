using System;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;

namespace TicGame.Architecture.Tests.Audio
{
    public sealed class AudioAssetSetupTests
    {
        private sealed class RecordingAudio : IAudioService
        {
            public int Hits;
            public Vector3 Position;
            public bool TryPlayWorld(SoundCueSO cue, Vector3 position, GameObject owner) { Hits++; Position = position; return true; }
            public bool TryPlayUi(SoundCueSO cue, GameObject owner) => false;
            public void SetWorldPaused(bool paused) { }
        }

        [Test]
        public void EnemyChildHurtbox_ResolvedDamageEmitsExactlyOneImpactCue()
        {
            var type = Type.GetType("TicGame.Architecture.EditorTools.AudioPrototypeSetup, TicGame.Architecture.Editor");
            var wire = type.GetMethod("WireDamageRecipients", BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(wire);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/GolemCharger.prefab");
            var root = UnityEngine.Object.Instantiate(prefab);
            var cue = ScriptableObject.CreateInstance<SoundCueSO>();
            try
            {
                root.GetComponent<EnemyActor>().Initialize();
                var audio = new RecordingAudio();
                wire.Invoke(null, new object[] { root, cue });
                foreach (var presenter in root.GetComponentsInChildren<DamageAudioPresenter>(true)) presenter.BindAudio(audio);
                var region = root.GetComponentInChildren<EnemyHurtboxRegion>(true);
                Assert.NotNull(region.GetComponent<DamageAudioPresenter>());
                var instance = new DamageInstance("audio-hit", null, null, new DamageFormulaValues { FlatDamage = 1, CritValue = 1 });
                var request = new DamageRequest(instance, new[] { region.gameObject }, new Vector2(2, 3), Vector2.right);
                DamageResolver.Resolve(request);
                Assert.AreEqual(1, audio.Hits);
                Assert.AreEqual(new Vector3(2, 3, region.transform.position.z), audio.Position);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); UnityEngine.Object.DestroyImmediate(cue); }
        }

        [Test]
        public void NativeAssets_AreSeededSinglePlayContainers_AndSetupIsIdempotent()
        {
            var type = Type.GetType("TicGame.Architecture.EditorTools.AudioPrototypeSetup, TicGame.Architecture.Editor");
            Assert.NotNull(type, "Audio setup tool must exist.");
            var prepare = type.GetMethod("PrepareAssets", BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(prepare);
            prepare.Invoke(null, null);
            var cue = AssetDatabase.LoadAssetAtPath<SoundCueSO>("Assets/Data/Audio/Cues/Sound_Swing.asset");
            Assert.NotNull(cue);
            Assert.NotNull(cue.Resource);
            var container = new SerializedObject(cue.Resource);
            Assert.GreaterOrEqual(container.FindProperty("m_Elements").arraySize, 2);
            Assert.AreEqual(0, container.FindProperty("m_TriggerMode").intValue, "Manual trigger");
            Assert.AreEqual(1, container.FindProperty("m_LoopCount").intValue);
            var guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(cue));
            cue.ConfigurePolicy(37, 2, 0.25f);
            prepare.Invoke(null, null);
            Assert.AreEqual(guid, AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(cue)));
            Assert.AreEqual(37, cue.Priority);
            Assert.AreEqual(0.25f, cue.Cooldown);
            cue.ConfigurePolicy(128, 4, 0);
            EditorUtility.SetDirty(cue);
            AssetDatabase.SaveAssets();
            Assert.NotNull(AssetDatabase.LoadAssetAtPath<AudioMixer>("Assets/Data/Audio/PrototypeAudio.mixer"));
        }
    }
}
