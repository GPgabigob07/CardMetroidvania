using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TicGame.Architecture.EditorTools
{
    public static class HitRippleLabSetup
    {
        public const string ScenePath = "Assets/Scenes/Test_HitRipple.unity";
        private const string Folder = "Assets/Data/Feedback/HitRippleLab";
        private const string RootName = "Hit Ripple Lab";

        [MenuItem("TicGame/Feedback/Create or Update Hit Ripple Test Scene")]
        public static void CreateOrUpdate()
        {
            var activeScene = SceneManager.GetActiveScene();
            if (string.IsNullOrEmpty(activeScene.path))
                throw new InvalidOperationException("Save or open an existing scene before creating the ripple lab. Your current unsaved scene is preserved.");
            var profile = AssetDatabase.LoadAssetAtPath<HitRippleProfileSO>(EnemyHitRippleSetup.ProfilePath);
            var material = AssetDatabase.LoadAssetAtPath<Material>(EnemyHitRippleSetup.MaterialPath);
            if (profile == null || material == null) throw new InvalidOperationException("Run Setup Enemy Hit Ripples first.");
            if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/Data/Feedback", "HitRippleLab");
            var definitionPath = Folder + "/Enemy_RippleTest.asset";
            var definition = AssetDatabase.LoadAssetAtPath<EnemyDefinitionSO>(definitionPath);
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<EnemyDefinitionSO>();
                AssetDatabase.CreateAsset(definition, definitionPath);
            }
            var small = CreateSprite("SmallGrid", 64, 64, new Vector2(.5f, .5f), false);
            var large = CreateSprite("LargeGrid", 128, 96, new Vector2(.5f, .5f), false);
            var offset = CreateSprite("OffsetPivotCircle", 96, 96, new Vector2(.2f, .75f), true);
            var scene = SceneManager.GetSceneByPath(ScenePath);
            var alreadyLoaded = scene.IsValid() && scene.isLoaded;
            if (alreadyLoaded && scene.isDirty) throw new InvalidOperationException("Save the ripple test scene before regenerating it.");
            if (!alreadyLoaded) scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null
                ? EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive)
                : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            try
            {
                var previous = scene.GetRootGameObjects().FirstOrDefault(root => root.name == RootName && root.GetComponent<HitRippleTestController>() != null);
                if (previous != null) UnityEngine.Object.DestroyImmediate(previous);
                var root = new GameObject(RootName);
                var controller = root.AddComponent<HitRippleTestController>();
                var cameraObject = Child(root, "Main Camera"); cameraObject.tag = "MainCamera";
                cameraObject.transform.position = new Vector3(0, 0, -10);
                var camera = cameraObject.AddComponent<Camera>(); camera.orthographic = true; camera.orthographicSize = 4.5f;
                camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.025f, .035f, .055f);
                camera.allowHDR = false; camera.allowMSAA = false;
                var cameraData = Type.GetType("UnityEngine.Rendering.Universal.UniversalAdditionalCameraData, Unity.RenderPipelines.Universal.Runtime");
                if (cameraData != null) cameraObject.AddComponent(cameraData);
                CreateLight(root);
                var enemyLayer = LayerMask.NameToLayer("Enemy");
                if (enemyLayer < 0) throw new InvalidOperationException("Enemy layer is missing.");
                var targets = new[]
                {
                    CreateTarget(root, "Small · centered pivot", new Vector2(-5, 0), small, false, definition, profile, material, enemyLayer),
                    CreateTarget(root, "Large · stacking test", Vector2.zero, large, false, definition, profile, material, enemyLayer),
                    CreateTarget(root, "Circle · offset pivot / flipped", new Vector2(5, 0), offset, true, definition, profile, material, enemyLayer)
                };
                controller.Configure(camera, targets, 1 << enemyLayer);
                EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene, ScenePath);
                AssetDatabase.SaveAssets();
                Debug.Log("Saved Test_HitRipple. In Play Mode, click/hold any target; right click rejected, middle click fatal. B bursts, P freezes, R clears.");
            }
            finally
            {
                if (activeScene.IsValid() && activeScene.isLoaded) SceneManager.SetActiveScene(activeScene);
                if (!alreadyLoaded) EditorSceneManager.CloseScene(scene, true);
            }
        }
        private static GameObject Child(GameObject parent, string name)
        { var child = new GameObject(name); child.transform.SetParent(parent.transform, false); return child; }

        private static HitRippleTestTarget CreateTarget(GameObject parent, string name, Vector2 position, Sprite sprite,
            bool flipped, EnemyDefinitionSO definition, HitRippleProfileSO profile, Material material, int layer)
        {
            var root = Child(parent, name); root.transform.localPosition = position; root.layer = layer;
            var actor = root.AddComponent<EnemyActor>(); actor.SetDefinition(definition);
            var visualObject = Child(root, "Sprite"); visualObject.layer = layer;
            var visual = visualObject.AddComponent<SpriteRenderer>(); visual.sprite = sprite; visual.sharedMaterial = material;
            visual.sortingOrder = 1; visual.flipX = flipped;
            var center = sprite.bounds.center; if (flipped) center.x = -center.x;
            visualObject.transform.localPosition = -center;
            var presenter = root.AddComponent<EnemyHitRipplePresenter>(); presenter.Configure(actor, profile, new[] { visual });
            presenter.SetManualPlayback(true);
            var receiver = Child(root, "Hit Detection"); receiver.layer = layer;
            Collider2D shape;
            if (flipped)
            {
                var circle = receiver.AddComponent<CircleCollider2D>(); circle.radius = sprite.rect.width / sprite.pixelsPerUnit * .5f; shape = circle;
            }
            else
            {
                var box = receiver.AddComponent<BoxCollider2D>(); box.size = sprite.rect.size / sprite.pixelsPerUnit; shape = box;
            }
            shape.isTrigger = true;
            var target = receiver.AddComponent<HitRippleTestTarget>(); target.Configure(actor, shape);
            receiver.AddComponent<EnemyHitRippleDamageListener>().Configure(presenter);
            return target;
        }
        private static void CreateLight(GameObject parent)
        {
            var type = Type.GetType("UnityEngine.Rendering.Universal.Light2D, Unity.RenderPipelines.Universal.2D.Runtime");
            if (type == null) throw new InvalidOperationException("URP Light2D is required for the ripple lab.");
            var light = Child(parent, "Global 2D Light").AddComponent(type);
            var kind = type.GetProperty("lightType"); kind.SetValue(light, Enum.Parse(kind.PropertyType, "Global"));
            type.GetProperty("intensity").SetValue(light, .7f);
        }
        private static Sprite CreateSprite(string name, int width, int height, Vector2 pivot, bool circle)
        {
            var path = Folder + "/" + name + ".png";
            if (!File.Exists(path))
            {
                var texture = new Texture2D(width, height);
                try
                {
                    var pixels = new Color32[width * height];
                    for (var y = 0; y < height; y++) for (var x = 0; x < width; x++)
                    {
                        var unit = new Vector2((x + .5f) / width * 2 - 1, (y + .5f) / height * 2 - 1);
                        var visible = !circle || unit.sqrMagnitude <= 1;
                        var grid = x % 8 == 0 || y % 8 == 0;
                        var shade = (byte)(grid ? 65 : (x / 8 + y / 8) % 2 == 0 ? 40 : 48);
                        pixels[y * width + x] = new Color32(shade, shade, shade, visible ? (byte)255 : (byte)0);
                    }
                    texture.SetPixels32(pixels); texture.Apply(); File.WriteAllBytes(path, texture.EncodeToPNG());
                }
                finally { UnityEngine.Object.DestroyImmediate(texture); }
            }
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 32;
            var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
            settings.spriteAlignment = 9; settings.spritePivot = pivot; settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings); importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false; importer.alphaIsTransparency = true; importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}
