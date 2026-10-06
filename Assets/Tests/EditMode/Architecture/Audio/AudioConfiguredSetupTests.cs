using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;

namespace TicGame.Architecture.Tests.Audio
{
    public sealed class AudioConfiguredSetupTests
    {
        [Test]
        public void Setup_RerunPreservesPrefabTopologyAndCueTuning()
        {
            var mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>("Assets/Data/Audio/PrototypeAudio.mixer");
            if (mixer.FindMatchingGroups("").Length < 7) Assert.Ignore("Native mixer handoff is not saved yet.");
            var type = Type.GetType("TicGame.Architecture.EditorTools.AudioPrototypeSetup, TicGame.Architecture.Editor");
            var setup = type.GetMethod("Setup", BindingFlags.Public | BindingFlags.Static);
            setup.Invoke(null, null);
            var servicesPath = "Assets/Resources/Runtime/GameplayServices.prefab";
            var playerPath = "Assets/Prefabs/Player/Player.prefab";
            var beforeServices = File.ReadAllText(servicesPath);
            var beforePlayer = File.ReadAllText(playerPath);
            var cue = AssetDatabase.LoadAssetAtPath<SoundCueSO>("Assets/Data/Audio/Cues/Sound_Hit.asset");
            var priority = cue.Priority;
            var gain = cue.Gain;
            setup.Invoke(null, null);
            Assert.AreEqual(beforeServices, File.ReadAllText(servicesPath));
            Assert.AreEqual(beforePlayer, File.ReadAllText(playerPath));
            Assert.AreEqual(priority, cue.Priority);
            Assert.AreEqual(gain, cue.Gain);
            var services = AssetDatabase.LoadAssetAtPath<GameObject>(servicesPath);
            Assert.AreEqual(1, services.GetComponents<AudioService>().Length);
            var modules = new SerializedObject(services.GetComponent<GameplayServicesRoot>()).FindProperty("moduleComponents");
            var audioModules = 0;
            for (var index = 0; index < modules.arraySize; index++) if (modules.GetArrayElementAtIndex(index).objectReferenceValue is AudioService) audioModules++;
            Assert.AreEqual(1, audioModules);
            foreach (var name in new[] { "Swing", "Hit", "Jump", "Landing", "Dash", "Hurt", "CardTimeEnter", "CardTimeExit", "CardSelect", "CardConfirm" })
            {
                var sound = AssetDatabase.LoadAssetAtPath<SoundCueSO>("Assets/Data/Audio/Cues/Sound_" + name + ".asset");
                Assert.NotNull(sound.Resource);
                Assert.NotNull(sound.MixerGroup);
                Assert.AreSame(mixer, sound.MixerGroup.audioMixer);
            }
        }
    }
}

