using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace TicGame.Architecture.Tests.Settings
{
    public sealed class SettingsSetupTests
    {
        [Test]
        public void Upgrade_IsIdempotentAndPreservesArtworkControlsAndBuildScenes()
        {
            var type = Type.GetType("TicGame.Architecture.EditorTools.SettingsFrontendSetup, TicGame.Architecture.Editor");
            Assert.NotNull(type);
            var setup = type.GetMethod("Setup", BindingFlags.Public | BindingFlags.Static);
            var prefabPath = "Assets/Resources/Runtime/PlaytestSession.prefab";
            var before = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            var viewBefore = new SerializedObject(before.GetComponent<PlaytestMenuView>());
            var controls = viewBefore.FindProperty("controls").stringValue;
            var artworkName = viewBefore.FindProperty("artwork").objectReferenceValue.name;
            var scenes = string.Join("|", Array.ConvertAll(EditorBuildSettings.scenes, scene => scene.path + scene.enabled));
            setup.Invoke(null, null);
            var text = File.ReadAllText(prefabPath);
            setup.Invoke(null, null);
            Assert.AreEqual(text, File.ReadAllText(prefabPath));
            var root = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            var view = new SerializedObject(root.GetComponent<PlaytestMenuView>());
            Assert.AreEqual(controls, view.FindProperty("controls").stringValue);
            Assert.AreEqual(artworkName, view.FindProperty("artwork").objectReferenceValue.name);
            Assert.AreEqual(scenes, string.Join("|", Array.ConvertAll(EditorBuildSettings.scenes, scene => scene.path + scene.enabled)));
            Assert.AreEqual(5, view.FindProperty("buttons").arraySize);
            var panel = root.GetComponentInChildren<AudioSettingsPanel>(true);
            Assert.NotNull(panel);
            Assert.AreEqual(5, panel.Sliders.Length);
            foreach (var slider in panel.Sliders)
            { Assert.AreEqual(0, slider.minValue); Assert.AreEqual(100, slider.maxValue); Assert.IsTrue(slider.wholeNumbers); Assert.AreEqual(Navigation.Mode.Explicit, slider.navigation.mode); }
            var services = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Runtime/GameplayServices.prefab");
            Assert.AreEqual(1, services.GetComponents<UserSettingsService>().Length);
            var dependency = new SerializedObject(services.GetComponent<UserSettingsService>()).FindProperty("audioSettingsSource");
            Assert.AreSame(services.GetComponent<AudioService>(), dependency.objectReferenceValue);
            var modules = new SerializedObject(services.GetComponent<GameplayServicesRoot>()).FindProperty("moduleComponents");
            var count = 0;
            for (var index = 0; index < modules.arraySize; index++) if (modules.GetArrayElementAtIndex(index).objectReferenceValue is UserSettingsService) count++;
            Assert.AreEqual(1, count);
        }
    }
}
