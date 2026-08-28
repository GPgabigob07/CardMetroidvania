using System.Collections.Generic;
using System.Linq;
using TicGame.Architecture;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace TicGame.Architecture.EditorTools
{
    public static class GolemChargerAnimationSetup
    {
        private const string SheetPath =
            "Assets/Art/Enemies/GolemCharger/golem-charger-animation-concept-sheet.png";
        private const string AnimationFolder = "Assets/Art/Enemies/GolemCharger/Animations";
        private const string ControllerPath = AnimationFolder + "/GolemCharger.controller";
        private const string PrefabPath = "Assets/Prefabs/Enemies/GolemCharger.prefab";
        private const int Columns = 4;
        private const int Rows = 4;
        private const float PixelsPerUnit = 320f;
        private const string StateParameter = "State";

        [MenuItem("TIC/Setup/Create Or Update Golem Charger Animations")]
        public static void CreateOrUpdateGolemChargerAnimations()
        {
            EnsureFolder(AnimationFolder);
            var sprites = ConfigureAndLoadSprites();
            if (sprites.Count != Columns * Rows)
            {
                Debug.LogError("The Golem Charger animation sheet did not produce the expected 16 sprites.");
                return;
            }

            var patrol = CreateOrUpdateClip("GolemCharger_Patrol", sprites, new[] { 0, 1, 2, 3 }, 5f, true);
            var windup = CreateOrUpdateClip("GolemCharger_Windup", sprites, new[] { 4, 5, 6 }, 6f, true);
            var charge = CreateOrUpdateClip("GolemCharger_Charge", sprites, new[] { 7, 8 }, 12f, true);
            var recovery = CreateOrUpdateClip("GolemCharger_Recovery", sprites, new[] { 9, 10, 11 }, 6f, false);
            var death = CreateOrUpdateClip("GolemCharger_Death", sprites, new[] { 12, 13, 14, 15 }, 6f, false);
            var controller = CreateOrUpdateController(patrol, windup, charge, recovery, death);
            ConfigurePrefab(controller);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Created or updated Golem Charger sprite slicing, animation clips, controller, and prefab bindings.");
        }

        private static List<Sprite> ConfigureAndLoadSprites()
        {
            var importer = AssetImporter.GetAtPath(SheetPath) as TextureImporter;
            if (importer == null)
            {
                Debug.LogError($"Missing Golem Charger animation sheet at {SheetPath}.");
                return new List<Sprite>();
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();

            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(SheetPath);
            if (texture == null || texture.width < Columns || texture.height < Rows)
            {
                Debug.LogError("The Golem Charger animation sheet must be large enough for a 4 by 4 grid.");
                return new List<Sprite>();
            }

            var cellWidth = Mathf.FloorToInt(texture.width / (float)Columns);
            var cellHeight = Mathf.FloorToInt(texture.height / (float)Rows);
            var spriteRects = new SpriteRect[Columns * Rows];
            for (var row = 0; row < Rows; row++)
            {
                for (var column = 0; column < Columns; column++)
                {
                    var index = row * Columns + column;
                    spriteRects[index] = new SpriteRect
                    {
                        name = $"GolemCharger_Frame_{index:00}",
                        alignment = SpriteAlignment.Custom,
                        pivot = new Vector2(x: 0.5f, y: 0f),
                        spriteID = GUID.Generate(),
                        rect = new Rect(
                            x: column * cellWidth,
                            y: texture.height - ((row + 1) * cellHeight),
                            width: cellWidth,
                            height: cellHeight)
                    };
                }
            }

            var dataProviderFactories = new SpriteDataProviderFactories();
            dataProviderFactories.Init();
            var dataProvider = dataProviderFactories.GetSpriteEditorDataProviderFromObject(importer);
            dataProvider.InitSpriteEditorDataProvider();
            dataProvider.SetSpriteRects(spriteRects);
            if (dataProvider is ISpriteNameFileIdDataProvider nameFileIdDataProvider)
            {
                nameFileIdDataProvider.SetNameFileIdPairs(spriteRects
                    .Select(spriteRect => new SpriteNameFileIdPair(spriteRect.name, spriteRect.spriteID))
                    .ToArray());
            }

            dataProvider.Apply();
            importer.SaveAndReimport();
            return AssetDatabase.LoadAllAssetsAtPath(SheetPath)
                .OfType<Sprite>()
                .OrderBy(sprite => sprite.name)
                .ToList();
        }

        private static AnimationClip CreateOrUpdateClip(
            string clipName,
            IReadOnlyList<Sprite> sprites,
            IReadOnlyList<int> frameIndices,
            float framesPerSecond,
            bool loop)
        {
            var path = $"{AnimationFolder}/{clipName}.anim";
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip == null)
            {
                clip = new AnimationClip { name = clipName };
                AssetDatabase.CreateAsset(clip, path);
            }

            clip.frameRate = framesPerSecond;
            var binding = new EditorCurveBinding
            {
                type = typeof(SpriteRenderer),
                path = "VisualRoot",
                propertyName = "m_Sprite"
            };
            var keys = frameIndices
                .Select((index, frame) => new ObjectReferenceKeyframe
                {
                    time = frame / framesPerSecond,
                    value = sprites[index]
                })
                .ToArray();
            AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            EditorUtility.SetDirty(clip);
            return clip;
        }

        private static AnimatorController CreateOrUpdateController(
            AnimationClip patrol,
            AnimationClip windup,
            AnimationClip charge,
            AnimationClip recovery,
            AnimationClip death)
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            }

            for (var index = controller.parameters.Length - 1; index >= 0; index--)
            {
                controller.RemoveParameter(index);
            }

            controller.AddParameter(StateParameter, AnimatorControllerParameterType.Int);
            var stateMachine = controller.layers[0].stateMachine;
            foreach (var transition in stateMachine.anyStateTransitions)
            {
                stateMachine.RemoveAnyStateTransition(transition);
            }

            foreach (var childState in stateMachine.states)
            {
                stateMachine.RemoveState(childState.state);
            }

            var patrolState = stateMachine.AddState("Patrol");
            patrolState.motion = patrol;
            var windupState = stateMachine.AddState("Windup");
            windupState.motion = windup;
            var chargeState = stateMachine.AddState("Charge");
            chargeState.motion = charge;
            var recoveryState = stateMachine.AddState("Recovery");
            recoveryState.motion = recovery;
            var deathState = stateMachine.AddState("Death");
            deathState.motion = death;
            stateMachine.defaultState = patrolState;

            AddAnyStateTransition(stateMachine, patrolState, GolemChargerState.Patrol);
            AddAnyStateTransition(stateMachine, windupState, GolemChargerState.Windup);
            AddAnyStateTransition(stateMachine, chargeState, GolemChargerState.Charge);
            AddAnyStateTransition(stateMachine, recoveryState, GolemChargerState.Recovery);
            AddAnyStateTransition(stateMachine, deathState, GolemChargerState.Dead);
            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static void AddAnyStateTransition(
            AnimatorStateMachine stateMachine,
            AnimatorState destination,
            GolemChargerState expectedState)
        {
            var transition = stateMachine.AddAnyStateTransition(destination);
            transition.hasExitTime = false;
            transition.duration = 0f;
            transition.canTransitionToSelf = false;
            transition.AddCondition(
                AnimatorConditionMode.Equals,
                threshold: (int)expectedState,
                parameter: StateParameter);
        }

        private static void ConfigurePrefab(AnimatorController controller)
        {
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                var brain = root.GetComponent<GolemChargerBrain>();
                var animator = GetOrAddComponent<Animator>(root);
                animator.runtimeAnimatorController = controller;
                var presenter = GetOrAddComponent<GolemChargerAnimationPresenter>(root);
                presenter.Configure(
                    brain,
                    animator,
                    root.GetComponent<Rigidbody2D>(),
                    root.transform.Find("VisualRoot")?.GetComponent<SpriteRenderer>());
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static T GetOrAddComponent<T>(GameObject owner) where T : Component
        {
            var component = owner.GetComponent<T>();
            return component != null ? component : owner.AddComponent<T>();
        }

        private static void EnsureFolder(string path)
        {
            var segments = path.Split('/');
            var current = segments[0];
            for (var index = 1; index < segments.Length; index++)
            {
                var next = $"{current}/{segments[index]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, segments[index]);
                }

                current = next;
            }
        }
    }
}
