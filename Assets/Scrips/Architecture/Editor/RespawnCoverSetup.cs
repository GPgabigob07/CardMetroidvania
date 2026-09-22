using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TicGame.Architecture.EditorTools
{
    public static class RespawnCoverSetup
    {
        private const string GameplayScenePath = "Assets/Scenes/Gameplay.unity";
        private const string CompositionName = "[Gameplay Composition]";
        private const string HudName = "[Player HUD]";
        private const string CoverName = "Respawn Cover";
        private const string ImageName = "Black Screen";

        [MenuItem("TIC/Setup/Create Or Update Respawn Cover")]
        private static void CreateOrUpdate()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Respawn cover setup is unavailable in Play Mode.");
                return;
            }

            if (HasDirtyOpenScenes())
            {
                Debug.LogWarning("Respawn cover setup aborted because an open scene has unsaved changes.");
                return;
            }

            var gameplay = SceneManager.GetSceneByPath(GameplayScenePath);
            if (!gameplay.IsValid() || !gameplay.isLoaded)
            {
                gameplay = EditorSceneManager.OpenScene(GameplayScenePath, OpenSceneMode.Additive);
            }

            try
            {
                Configure(gameplay);
                if (!EditorSceneManager.SaveScene(gameplay, GameplayScenePath))
                {
                    Debug.LogError("Respawn cover setup could not save Gameplay.");
                    return;
                }

                Debug.Log("Respawn cover setup saved Gameplay.");
            }
            catch (Exception exception)
            {
                Debug.LogError($"Respawn cover setup failed: {exception.Message}");
            }
        }

        private static void Configure(Scene gameplay)
        {
            var composition = FindSingleRoot(gameplay, CompositionName);
            if (composition == null)
            {
                throw new InvalidOperationException($"Gameplay requires one {CompositionName} root.");
            }

            var root = composition.GetComponent<GameplaySceneRoot>();
            if (root == null)
            {
                throw new InvalidOperationException($"{CompositionName} requires GameplaySceneRoot.");
            }

            var cover = FindOrCreateCover(composition.transform);
            var canvas = cover.GetComponent<Canvas>() ?? Undo.AddComponent<Canvas>(cover);
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = FindHudSortingOrder(gameplay) + 1;

            var group = cover.GetComponent<CanvasGroup>() ?? Undo.AddComponent<CanvasGroup>(cover);
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;

            var presenter = cover.GetComponent<RespawnCoverUI>() ?? Undo.AddComponent<RespawnCoverUI>(cover);
            AssignGroup(presenter, group);
            ConfigureBlackScreen(cover.transform);
            AssignCover(root, presenter);
            EditorUtility.SetDirty(cover);
            EditorUtility.SetDirty(root);
        }

        private static bool HasDirtyOpenScenes()
        {
            for (var index = 0; index < SceneManager.sceneCount; index++)
            {
                if (SceneManager.GetSceneAt(index).isDirty)
                {
                    return true;
                }
            }

            return false;
        }

        private static GameObject FindOrCreateCover(Transform composition)
        {
            var matches = composition.Cast<Transform>().Where(child => child.name == CoverName).ToArray();
            if (matches.Length > 1)
            {
                throw new InvalidOperationException($"{CompositionName} has more than one {CoverName} child.");
            }

            if (matches.Length == 1)
            {
                return matches[0].gameObject;
            }

            var cover = new GameObject(CoverName, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(cover, "Create Respawn Cover");
            cover.transform.SetParent(composition, worldPositionStays: false);
            return cover;
        }

        private static void ConfigureBlackScreen(Transform cover)
        {
            var matches = cover.Cast<Transform>().Where(child => child.name == ImageName).ToArray();
            if (matches.Length > 1)
            {
                throw new InvalidOperationException($"{CoverName} has more than one {ImageName} child.");
            }

            var imageObject = matches.Length == 1
                ? matches[0].gameObject
                : CreateImageObject(cover);
            var rect = imageObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var image = imageObject.GetComponent<Image>() ?? Undo.AddComponent<Image>(imageObject);
            image.color = Color.black;
            image.raycastTarget = false;
            EditorUtility.SetDirty(imageObject);
        }

        private static GameObject CreateImageObject(Transform cover)
        {
            var image = new GameObject(ImageName, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(image, "Create Respawn Cover Image");
            image.transform.SetParent(cover, worldPositionStays: false);
            return image;
        }

        private static GameObject FindSingleRoot(Scene scene, string name)
        {
            var matches = scene.GetRootGameObjects().Where(root => root.name == name).ToArray();
            if (matches.Length > 1)
            {
                throw new InvalidOperationException($"Gameplay has more than one {name} root.");
            }

            return matches.SingleOrDefault();
        }

        private static int FindHudSortingOrder(Scene scene)
        {
            var hud = FindSingleRoot(scene, HudName);
            if (hud == null)
            {
                return 0;
            }

            return hud.GetComponentsInChildren<Canvas>(includeInactive: true)
                .Select(canvas => canvas.sortingOrder)
                .DefaultIfEmpty(0)
                .Max();
        }

        private static void AssignCover(GameplaySceneRoot root, RespawnCoverUI cover)
        {
            var serializedRoot = new SerializedObject(root);
            serializedRoot.FindProperty("respawnCover").objectReferenceValue = cover;
            serializedRoot.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AssignGroup(RespawnCoverUI presenter, CanvasGroup group)
        {
            var serializedPresenter = new SerializedObject(presenter);
            serializedPresenter.FindProperty("group").objectReferenceValue = group;
            serializedPresenter.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
