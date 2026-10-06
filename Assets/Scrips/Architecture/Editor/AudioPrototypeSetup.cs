using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;

namespace TicGame.Architecture.EditorTools
{
    public static class AudioPrototypeSetup
    {
        public const string MixerPath = "Assets/Data/Audio/PrototypeAudio.mixer";
        private const string AudioRoot = "Assets/Data/Audio";
        private const string VendorRoot = "Assets/ThirdParty/Kenney/Audio/";
        private static readonly string[] CueNames = { "Swing", "Hit", "Jump", "Landing", "Dash", "Hurt", "CardTimeEnter", "CardTimeExit", "CardSelect", "CardConfirm" };
        private static readonly string[] Routes = { "Combat", "Combat", "Movement", "Movement", "Movement", "Combat", "Combat", "Combat", "UI", "UI" };
        private static readonly string[][] Clips =
        {
            new[] { "RpgAudio/Audio/knifeSlice.ogg", "RpgAudio/Audio/knifeSlice2.ogg" },
            new[] { "ImpactSounds/Audio/impactPunch_medium_000.ogg", "ImpactSounds/Audio/impactPunch_medium_001.ogg", "ImpactSounds/Audio/impactPunch_medium_002.ogg" },
            new[] { "RpgAudio/Audio/cloth1.ogg", "RpgAudio/Audio/cloth2.ogg" },
            new[] { "ImpactSounds/Audio/footstep_concrete_000.ogg", "ImpactSounds/Audio/footstep_concrete_001.ogg", "ImpactSounds/Audio/footstep_concrete_002.ogg" },
            new[] { "RpgAudio/Audio/cloth3.ogg", "RpgAudio/Audio/cloth4.ogg" },
            new[] { "ImpactSounds/Audio/impactPunch_heavy_000.ogg", "ImpactSounds/Audio/impactPunch_heavy_001.ogg" },
            new[] { "InterfaceSounds/Audio/open_001.ogg", "InterfaceSounds/Audio/open_002.ogg" },
            new[] { "InterfaceSounds/Audio/close_001.ogg", "InterfaceSounds/Audio/close_002.ogg" },
            new[] { "CasinoAudio/Audio/card-shove-1.ogg", "CasinoAudio/Audio/card-shove-2.ogg" },
            new[] { "InterfaceSounds/Audio/confirmation_002.ogg", "InterfaceSounds/Audio/confirmation_003.ogg" }
        };

        [MenuItem("TIC/Audio/Prepare Native Assets")]
        public static void PrepareAssets()
        {
            EnsureFolder(AudioRoot);
            EnsureFolder(AudioRoot + "/Cues");
            EnsureFolder(AudioRoot + "/Containers");
            if (AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath) == null)
                CreateThroughNativeMenu("Assets/Create/Audio/Audio Mixer", "t:AudioMixer", MixerPath, Array.Empty<UnityEngine.Object>());
            for (var index = 0; index < CueNames.Length; index++)
            {
                var path = AudioRoot + "/Containers/Audio_" + CueNames[index] + ".asset";
                var resource = AssetDatabase.LoadAssetAtPath<AudioResource>(path);
                if (resource == null)
                {
                    var selectedClips = Clips[index].Select(clipPath => AssetDatabase.LoadAssetAtPath<AudioClip>(VendorRoot + clipPath)).ToArray();
                    if (selectedClips.Any(clip => clip == null)) throw new InvalidOperationException("Missing Kenney source clips for " + CueNames[index]);
                    resource = CreateThroughNativeMenu("Assets/Create/Audio/Audio Random Container", "t:AudioRandomContainer", path, selectedClips) as AudioResource;
                    var serialized = new SerializedObject(resource);
                    // Guard the Unity 6.3 serialization schema; no internal authoring APIs.
                    SetInt(serialized, "m_TriggerMode", 0); // Manual
                    SetInt(serialized, "m_PlaybackMode", 1); // Shuffle
                    SetInt(serialized, "m_AvoidRepeatingLast", 1);
                    SetInt(serialized, "m_LoopMode", 1); // Clips
                    SetInt(serialized, "m_LoopCount", 1);
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(resource);
                }
                var cuePath = AudioRoot + "/Cues/Sound_" + CueNames[index] + ".asset";
                if (AssetDatabase.LoadAssetAtPath<SoundCueSO>(cuePath) != null) continue;
                var cue = ScriptableObject.CreateInstance<SoundCueSO>();
                cue.Configure(resource, null, index == 8 ? 0.25f : 0.45f);
                AssetDatabase.CreateAsset(cue, cuePath);
            }
            AssetDatabase.SaveAssets();
        }

        [MenuItem("TIC/Audio/Setup Prototype Audio")]
        public static void Setup()
        {
            PrepareAssets();
            var mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);
            var groups = mixer.FindMatchingGroups("");
            foreach (var name in new[] { "Master", "SFX", "Combat", "Movement", "UI", "Music", "Ambience" })
                if (groups.Count(group => group.name == name) != 1) throw new InvalidOperationException("Create exactly one mixer group named " + name + " in " + MixerPath + ", then rerun setup.");
            foreach (var name in new[] { "MasterVolume", "SfxVolume", "UiVolume", "MusicVolume", "AmbienceVolume" })
                if (!mixer.GetFloat(name, out _)) throw new InvalidOperationException("Expose and name the mixer volume parameter " + name + ", then rerun setup.");
            RequireChildren(groups, "Master", "SFX", "UI", "Music", "Ambience");
            RequireChildren(groups, "SFX", "Combat", "Movement");
            for (var index = 0; index < CueNames.Length; index++)
            {
                var cue = Cue(CueNames[index]);
                if (cue.MixerGroup != null) continue;
                cue.Configure(cue.Resource, groups.Single(group => group.name == Routes[index]), cue.Gain);
                EditorUtility.SetDirty(cue);
            }
            WirePrefabs(mixer);
            AssetDatabase.SaveAssets();
            Debug.Log("Prototype audio is wired. Verify position, attenuation, pause/UI, and levels in Play Mode.");
        }

        private static void WirePrefabs(AudioMixer mixer)
        {
            EditPrefab("Assets/Resources/Runtime/GameplayServices.prefab", root =>
            {
                var service = root.GetComponent<AudioService>() ?? root.AddComponent<AudioService>();
                FillReference(service, "mixer", mixer);
                var composition = new SerializedObject(root.GetComponent<GameplayServicesRoot>());
                var modules = composition.FindProperty("moduleComponents");
                var exists = false;
                for (var index = 0; index < modules.arraySize; index++) if (modules.GetArrayElementAtIndex(index).objectReferenceValue == service) exists = true;
                if (!exists) { var index = modules.arraySize; modules.InsertArrayElementAtIndex(index); modules.GetArrayElementAtIndex(index).objectReferenceValue = service; composition.ApplyModifiedPropertiesWithoutUndo(); }
            });
            EditPrefab("Assets/Prefabs/Player/Player.prefab", root =>
            {
                var player = root.GetComponent<PlayerAudioPresenter>() ?? root.AddComponent<PlayerAudioPresenter>();
                FillReference(player, "swing", Cue("Swing")); FillReference(player, "dash", Cue("Dash"));
                FillReference(player, "jump", Cue("Jump")); FillReference(player, "landing", Cue("Landing"));
                var cardTime = root.GetComponent<CardTimeAudioPresenter>() ?? root.AddComponent<CardTimeAudioPresenter>();
                FillReference(cardTime, "enter", Cue("CardTimeEnter")); FillReference(cardTime, "exit", Cue("CardTimeExit"));
                var ui = root.GetComponent<UiAudioPresenter>() ?? root.AddComponent<UiAudioPresenter>();
                FillReference(ui, "selection", Cue("CardSelect")); FillReference(ui, "confirmation", Cue("CardConfirm"));
                var damage = root.GetComponent<DamageAudioPresenter>() ?? root.AddComponent<DamageAudioPresenter>();
                FillReference(damage, "hit", Cue("Hurt"));
            });
            foreach (var path in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/Enemies" }).Select(AssetDatabase.GUIDToAssetPath))
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab.GetComponent<EnemyActor>() == null) continue;
                EditPrefab(path, root =>
                {
                    WireDamageRecipients(root, Cue("Hit"));
                });
            }
        }

        public static void WireDamageRecipients(GameObject root, SoundCueSO cue)
        {
            var actor = root.GetComponent<EnemyActor>();
            foreach (var recipient in root.GetComponentsInChildren<MonoBehaviour>(true)
                         .Where(component => component is IDamageable
                             && component.GetComponentInParent<EnemyActor>(true) == actor
                             && component.GetComponentInParent<EnemyProjectile2D>(true) == null)
                         .Select(component => component.gameObject).Distinct())
            {
                var damage = recipient.GetComponent<DamageAudioPresenter>() ?? recipient.AddComponent<DamageAudioPresenter>();
                FillReference(damage, "hit", cue);
            }
        }

        private static void RequireChildren(AudioMixerGroup[] groups, string parent, params string[] children)
        {
            var serialized = new SerializedObject(groups.Single(group => group.name == parent));
            var property = serialized.FindProperty("m_Children");
            if (property == null) throw new InvalidOperationException("Mixer hierarchy schema changed; inspect in Editor.");
            var actual = new HashSet<UnityEngine.Object>();
            for (var index = 0; index < property.arraySize; index++) actual.Add(property.GetArrayElementAtIndex(index).objectReferenceValue);
            foreach (var child in children) if (!actual.Contains(groups.Single(group => group.name == child))) throw new InvalidOperationException(child + " must be a child of " + parent);
        }

        private static SoundCueSO Cue(string name) => AssetDatabase.LoadAssetAtPath<SoundCueSO>(AudioRoot + "/Cues/Sound_" + name + ".asset");

        private static void FillReference(UnityEngine.Object target, string propertyName, UnityEngine.Object value)
        {
            var serialized = new SerializedObject(target);
            var property = serialized.FindProperty(propertyName);
            if (property.objectReferenceValue != null) return;
            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EditPrefab(string path, Action<GameObject> edit)
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            try { edit(root); PrefabUtility.SaveAsPrefabAsset(root, path); }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        private static UnityEngine.Object CreateThroughNativeMenu(string menu, string filter, string destination, UnityEngine.Object[] selection)
        {
            if (!Application.isBatchMode) throw new InvalidOperationException("Native asset missing: " + destination + ". Create it with " + menu + " or use the prepared assets before rerunning setup.");
            var previousSelection = Selection.objects;
            var before = new HashSet<string>(AssetDatabase.FindAssets(filter));
            try
            {
                Selection.objects = selection;
                if (!EditorApplication.ExecuteMenuItem(menu)) throw new InvalidOperationException("Native creation menu unavailable: " + menu);
                AssetDatabase.SaveAssets();
                var added = AssetDatabase.FindAssets(filter).Where(guid => !before.Contains(guid)).ToArray();
                if (added.Length != 1) throw new InvalidOperationException("Native menu did not create exactly one asset: " + menu);
                var sourcePath = AssetDatabase.GUIDToAssetPath(added[0]);
                var error = AssetDatabase.MoveAsset(sourcePath, destination);
                if (!string.IsNullOrEmpty(error)) throw new InvalidOperationException(error);
                var result = AssetDatabase.LoadMainAssetAtPath(destination);
                result.name = Path.GetFileNameWithoutExtension(destination);
                EditorUtility.SetDirty(result);
                return result;
            }
            finally { Selection.objects = previousSelection; }
        }

        private static void SetInt(SerializedObject serialized, string name, int value)
        {
            var property = serialized.FindProperty(name);
            if (property == null) throw new InvalidOperationException("Native container schema changed: " + name);
            property.intValue = value;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
