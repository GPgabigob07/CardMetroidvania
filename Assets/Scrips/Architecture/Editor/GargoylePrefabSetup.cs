using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TicGame.Architecture.EditorTools
{
    public static class GargoylePrefabSetup
    {
        public const string DataFolder = "Assets/Data/Enemies/GargoyleSentinel";
        public const string PrefabPath = "Assets/Prefabs/Enemies/GargoyleSentinel.prefab";
        public const string ScenePath = "Assets/Scenes/Test_GargoyleSentinel.unity";
        private const string SpritePath = "Assets/Art/Enemies/GargoyleSentinel/GargoyleIdle.png";

        [MenuItem("TIC/Setup/Create Or Update Gargoyle Sentinel")]
        public static void CreateOrUpdate()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Apply asset/import setup outside Play Mode.");
            Folder(DataFolder); Folder("Assets/Art/Enemies/GargoyleSentinel"); Folder("Assets/Prefabs/Enemies");
            var environment = LayerMask.NameToLayer("Environment");
            var enemy = LayerMask.NameToLayer("Enemy");
            if (environment < 0 || enemy < 0) throw new InvalidOperationException("Existing Environment and Enemy layers are required.");
            var presentation = Asset<GargoylePresentationSO>("GargoylePresentation", null);
            ImportSprite(presentation);
            var singleDamage = Asset<DamageProfileSO>("Damage_Sentinel", a => JsonUtility.FromJsonOverwrite("{\"baseDamage\":1,\"hitStopSeconds\":0}", a));
            var novaDamage = Asset<DamageProfileSO>("Damage_Nova", a => JsonUtility.FromJsonOverwrite("{\"baseDamage\":2,\"hitStopSeconds\":0}", a));
            var basic = Attack("Basic", singleDamage, ("claw-left", .35f, .12f, .18f), ("claw-right", .30f, .12f, .20f), ("claw-heavy", .55f, .15f, .45f));
            var wing = Attack("Wingbreaker", singleDamage, ("wing-advance", .45f, .20f, .20f), ("wing-slam", .30f, .15f, .40f));
            var volley = Attack("Volley", singleDamage, ("volley", .45f, .1f, .40f));
            var beam = Attack("Beam", singleDamage, ("beam", .8f, .7f, .75f));
            var nova = Attack("Nova", novaDamage, ("nova", 2.4f, .15f, .9f));
            var feint = Attack("FeintClaw", singleDamage, ("delayed-claw", .35f, .12f, .45f));
            var tuning = Asset<GargoyleTuningSO>("GargoyleTuning", a =>
            {
                Assign(a, "basicAttack", basic); Assign(a, "wingbreakerAttack", wing); Assign(a, "volleyAttack", volley);
                Assign(a, "beamAttack", beam); Assign(a, "novaAttack", nova); Assign(a, "feintAttack", feint); Assign(a, "presentation", presentation);
                Edit(a, s => s.FindProperty("environmentLayer").intValue = 1 << environment);
            });
            var identity = Asset<EnemyDefinitionSO>("Enemy_GargoyleSentinel", a => JsonUtility.FromJsonOverwrite("{\"id\":\"gargoyle-sentinel\",\"displayName\":\"Gargoyle Sentinel\",\"maxHealth\":45}", a));
            var profile = CreateProfile(PrototypeCardAssetSetup.CreateOrUpdateWardAssets());
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null) CreatePrefab(identity, tuning, enemy);
            EnsurePrefabPresentation(tuning);
            AssetDatabase.SaveAssets();
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null) CreateScene(profile, environment);
            EnsureArenaPresentation();
            Debug.Log("Gargoyle Sentinel setup saved. Single-frame draft: artist animation and body-mask acceptance pending.");
        }

        private static EnemyAttackDefinitionSO Attack(string name, DamageProfileSO damage, params (string id, float windup, float active, float recovery)[] timings)
        {
            return Asset<EnemyAttackDefinitionSO>("Attack_" + name, definition =>
            {
                var steps = timings.Select(t => new EnemyAttackStep(t.id, t.windup, t.active, t.recovery, damage)).ToArray();
                foreach (var step in steps)
                {
                    var kind = name == "Volley" ? 1 : name == "Beam" ? 2 : name == "Nova" ? 3 : 0;
                    JsonUtility.FromJsonOverwrite("{\"payload\":{\"kind\":" + kind + "}}", step);
                    if (name == "Beam") JsonUtility.FromJsonOverwrite("{\"payload\":{\"lockedAimDuration\":0.3}}", step);
                    if (name == "Volley") JsonUtility.FromJsonOverwrite("{\"payload\":{\"lockedAimDuration\":0.15}}", step);
                    if (step.Id == "wing-advance") JsonUtility.FromJsonOverwrite("{\"payload\":{\"advanceDistance\":0.8}}", step);
                    if (kind == 0) AuthorInitialMeleeGeometry(step);
                }
                definition.SetSteps(steps);
                if (name == "Volley")
                {
                    definition.SetPatterns(new[] { new EnemyProjectilePattern(1, .45f, new[] { 0f }), new EnemyProjectilePattern(3, .55f, new[] { -12f, 0, 12f }), new EnemyProjectilePattern(5, .65f, new[] { -30f, -15f, 0, 15f, 30f }) });
                    Assign(definition, "projectilePrefab", CreateProjectile());
                }
            });
        }

        // Explicit authoring action used once for the initial draft; normal setup preserves live tuning.
        public static void ApplyInitialVolleyAimLock()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Author defaults outside Play Mode.");
            var definition = AssetDatabase.LoadAssetAtPath<EnemyAttackDefinitionSO>(DataFolder + "/Attack_Volley.asset");
            foreach (var step in definition.Steps) JsonUtility.FromJsonOverwrite("{\"payload\":{\"lockedAimDuration\":0.15}}", step);
            EditorUtility.SetDirty(definition); AssetDatabase.SaveAssets();
        }

        [MenuItem("TIC/Setup/Apply Initial Gargoyle Melee Footprints")]
        public static void ApplyInitialMeleeGeometry()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Author defaults outside Play Mode.");
            foreach (var name in new[] { "Basic", "Wingbreaker", "FeintClaw" })
            {
                var definition = AssetDatabase.LoadAssetAtPath<EnemyAttackDefinitionSO>(DataFolder + "/Attack_" + name + ".asset");
                if (definition == null) throw new InvalidOperationException("Create the Gargoyle assets before applying initial melee footprints.");
                Undo.RecordObject(definition, "Apply initial Gargoyle melee footprints");
                foreach (var step in definition.Steps) AuthorInitialMeleeGeometry(step);
                EditorUtility.SetDirty(definition);
            }
            AssetDatabase.SaveAssets();
        }

        private static void AuthorInitialMeleeGeometry(EnemyAttackStep step)
        {
            // Initial authoring only: all runtime geometry is subsequently owned by these SO payloads.
            var geometry = step.Id switch
            {
                "claw-left" => "{\"hitboxSize\":{\"x\":1.5,\"y\":1},\"offset\":{\"x\":1,\"y\":1},\"hitboxAngle\":0}",
                "claw-right" => "{\"hitboxSize\":{\"x\":2.1,\"y\":0.8},\"offset\":{\"x\":1,\"y\":1.2},\"hitboxAngle\":-10}",
                "claw-heavy" => "{\"hitboxSize\":{\"x\":1.1,\"y\":1.8},\"offset\":{\"x\":0.95,\"y\":0.7},\"hitboxAngle\":-15}",
                "wing-advance" => "{\"hitboxSize\":{\"x\":2,\"y\":1.2},\"offset\":{\"x\":1.05,\"y\":1.1},\"hitboxAngle\":0}",
                "wing-slam" => "{\"hitboxSize\":{\"x\":1.2,\"y\":2},\"offset\":{\"x\":0.85,\"y\":1.65},\"hitboxAngle\":15}",
                "delayed-claw" => "{\"hitboxSize\":{\"x\":1.2,\"y\":0.9},\"offset\":{\"x\":1,\"y\":1.05},\"hitboxAngle\":0}",
                _ => null
            };
            if (geometry != null) JsonUtility.FromJsonOverwrite("{\"payload\":" + geometry + "}", step);
        }

        private static EnemyProjectile2D CreateProjectile()
        {
            var path = "Assets/Prefabs/Enemies/GargoyleProjectile.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing.GetComponent<EnemyProjectile2D>();
            var root = new GameObject("Gargoyle Projectile");
            try
            {
                var body = root.AddComponent<Rigidbody2D>(); body.bodyType = RigidbodyType2D.Kinematic; body.gravityScale = 0;
                var collider = root.AddComponent<CircleCollider2D>(); collider.radius = .10f; collider.isTrigger = true;
                var renderer = root.AddComponent<SpriteRenderer>(); renderer.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"); renderer.color = Color.cyan;
                root.transform.localScale = new Vector3(.2f, .2f, 1);
                root.AddComponent<EnemyProjectile2D>();
                return PrefabUtility.SaveAsPrefabAsset(root, path).GetComponent<EnemyProjectile2D>();
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        private static void ImportSprite(GargoylePresentationSO presentation)
        {
            if (!File.Exists(SpritePath)) File.Copy("gdd/art-references/gargoyle-idle-draft-200x200-20261003.png", SpritePath);
            AssetDatabase.ImportAsset(SpritePath);
            var importer = (TextureImporter)AssetImporter.GetAtPath(SpritePath);
            var pivot = presentation.NormalizedPivot;
            if (importer.textureType != TextureImporterType.Sprite || importer.spritePixelsPerUnit != presentation.PixelsPerUnit || importer.spritePivot != pivot || !importer.isReadable
                || importer.filterMode != FilterMode.Point || importer.textureCompression != TextureImporterCompression.Uncompressed)
            {
                importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = presentation.PixelsPerUnit;
                var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
                settings.spriteAlignment = (int)SpriteAlignment.Custom; settings.spritePivot = pivot; settings.spriteMeshType = SpriteMeshType.FullRect;
                importer.SetTextureSettings(settings);
                importer.filterMode = FilterMode.Point; importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.mipmapEnabled = false; importer.isReadable = true; importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }
            if (presentation.IdleSprite == null) Assign(presentation, "idleSprite", AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath));
        }

        private static PlayerCardInventoryProfileSO CreateProfile(CardDefinitionSO ward)
        {
            return Asset<PlayerCardInventoryProfileSO>("GargoyleCardInventory", profile =>
            {
                var source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player/Player.prefab");
                var ordinary = new SerializedObject(source.GetComponent<PlayerController>()).FindProperty("cardInventoryProfile").objectReferenceValue as PlayerCardInventoryProfileSO;
                EditorUtility.CopySerialized(ordinary, profile); profile.name = "GargoyleCardInventory";
                foreach (var card in profile.GetEquippedCards(PlayerCardTimeState.Neutral).Where(c => c.Id.Contains("jump-boost")).ToArray()) profile.TryUnequip(card);
                profile.TryAddOwnedCard(ward);
                if (!profile.TryEquip(ward)) throw new InvalidOperationException("Ward needs a free Neutral slot; replace Jump Boost only.");
            });
        }

        private static void CreatePrefab(EnemyDefinitionSO identity, GargoyleTuningSO tuning, int enemyLayer)
        {
            var root = new GameObject("Gargoyle Sentinel"); root.layer = enemyLayer;
            try
            {
                var body = root.AddComponent<Rigidbody2D>(); body.gravityScale = 0; body.bodyType = RigidbodyType2D.Kinematic; body.constraints = RigidbodyConstraints2D.FreezeRotation;
                var collider = root.AddComponent<BoxCollider2D>(); collider.size = tuning.Presentation.BodySize; collider.offset = tuning.Presentation.BodyRegion.Offset;
                var actor = root.AddComponent<EnemyActor>(); actor.SetDefinition(identity);
                root.AddComponent<EnemyPoise>(); root.AddComponent<GroundedEnemyPatrolMotor2D>();
                root.AddComponent<EnemyMeleeAttack2D>(); root.AddComponent<EnemyProjectilePatternLauncher>(); root.AddComponent<EnemyBeamAttack2D>(); root.AddComponent<EnemyNovaAttack2D>();
                var policy = root.AddComponent<GargoyleDamagePolicy>(); Assign(policy, "tuning", tuning);
                var brain = root.AddComponent<GargoyleBrain>(); Assign(brain, "tuning", tuning);
                foreach (var kind in new[] { GargoyleRegionKind.Body, GargoyleRegionKind.Core, GargoyleRegionKind.Head })
                {
                    var child = new GameObject(kind.ToString()); child.layer = enemyLayer; child.transform.SetParent(root.transform, false);
                    var shape = child.AddComponent<BoxCollider2D>(); shape.isTrigger = true;
                    var hurtbox = child.AddComponent<GargoyleHurtbox>();
                    Edit(hurtbox, s => { s.FindProperty("policy").objectReferenceValue = policy; s.FindProperty("region").enumValueIndex = (int)kind; s.FindProperty("shape").objectReferenceValue = shape; });
                }
                var visual = new GameObject("Draft Sprite - artist animations pending"); visual.transform.SetParent(root.transform, false);
                var renderer = visual.AddComponent<SpriteRenderer>(); renderer.sprite = tuning.Presentation.IdleSprite;
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        private static void CreateScene(PlayerCardInventoryProfileSO profile, int environment)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cameraRoot = new GameObject("Main Camera"); cameraRoot.tag = "MainCamera"; cameraRoot.transform.position = new Vector3(0, 2, -10);
            var camera = cameraRoot.AddComponent<Camera>(); camera.orthographic = true; camera.orthographicSize = 6; camera.backgroundColor = new Color(.08f, .1f, .13f); cameraRoot.AddComponent<AudioListener>();
            Block("Arena Floor", new Vector2(0, -.5f), new Vector2(24, 1), environment);
            Block("Left Wall", new Vector2(-12, 3), new Vector2(1, 8), environment); Block("Right Wall", new Vector2(12, 3), new Vector2(1, 8), environment);
            var player = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player/Player.prefab"), scene);
            player.name = "Player"; player.transform.position = new Vector3(-3, 1.1f, 0);
            Assign(player.GetComponent<PlayerController>(), "cardInventoryProfile", profile); Assign(player.GetComponent<PlayerCardInventoryRuntime>(), "profile", profile);
            var ward = player.AddComponent<PlayerWardRuntime>(); Assign(ward, "definition", AssetDatabase.LoadAssetAtPath<WardDefinitionSO>("Assets/Data/Cards/Effects/WardDefinition.asset"));
            Assign(player.GetComponent<PlayerCardRuntime>(), "ward", ward);
            Edit(player.GetComponent<PlayerDeathRespawn>(), s => s.FindProperty("fallbackRespawnPosition").vector3Value = player.transform.position);
            var gargoyle = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath), scene);
            gargoyle.transform.position = new Vector3(3, 0, 0); Assign(gargoyle.GetComponent<GargoyleBrain>(), "target", player);
            EditorSceneManager.SaveScene(scene, ScenePath);
            PlayerHudSetup.CreateOrUpdateHud(scene);
            EditorSceneManager.SaveScene(scene, ScenePath); AssetDatabase.SaveAssets();
        }

        private static void Block(string name, Vector2 position, Vector2 size, int layer)
        {
            var root = new GameObject(name); root.layer = layer; root.transform.position = position;
            root.AddComponent<BoxCollider2D>().size = size;
            var visual = new GameObject("Surface"); visual.transform.SetParent(root.transform, false); visual.transform.localScale = size;
            var renderer = visual.AddComponent<SpriteRenderer>(); renderer.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"); renderer.color = Color.gray;
        }

        private static void EnsurePrefabPresentation(GargoyleTuningSO tuning)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab.GetComponent<GargoyleAnimationPresenter>() != null) return;
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                var presenter = root.AddComponent<GargoyleAnimationPresenter>();
                presenter.Configure(root.GetComponent<GargoyleBrain>(), tuning.Presentation, root.GetComponentInChildren<SpriteRenderer>());
                Assign(presenter, "attackCue", CreateLine(root.transform, "Attack Telegraph")); Assign(presenter, "coreCue", CreateLine(root.transform, "Exposed Core"));
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        private static void EnsureArenaPresentation()
        {
            var scene = SceneManager.GetSceneByPath(ScenePath);
            if (!scene.IsValid() || !scene.isLoaded) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var roots = scene.GetRootGameObjects(); var brain = roots.SelectMany(r => r.GetComponentsInChildren<GargoyleBrain>()).Single();
            var player = roots.SelectMany(r => r.GetComponentsInChildren<PlayerController>()).Single().gameObject; var changed = false;
            if (player.GetComponent<PlayerWardPresenter>() == null)
            {
                var definition = AssetDatabase.LoadAssetAtPath<WardDefinitionSO>("Assets/Data/Cards/Effects/WardDefinition.asset");
                player.AddComponent<PlayerWardPresenter>().Configure(player.GetComponent<PlayerWardRuntime>(), definition, CreateLine(player.transform, "Directional Ward")); changed = true;
            }
            if (brain.GetComponent<GargoyleDebugPresenter>() == null) { brain.gameObject.AddComponent<GargoyleDebugPresenter>().Configure(brain, player); changed = true; }
            if (changed) { EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets(); }
        }
        private static LineRenderer CreateLine(Transform parent, string name)
        {
            var path = DataFolder + "/TelegraphMaterial.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null) { material = new Material(Shader.Find("Sprites/Default")); AssetDatabase.CreateAsset(material, path); }
            var root = new GameObject(name); root.transform.SetParent(parent, false);
            var line = root.AddComponent<LineRenderer>(); line.sharedMaterial = material; line.sortingOrder = 10; line.enabled = false; return line;
        }

        private static T Asset<T>(string name, Action<T> initialize) where T : ScriptableObject
        {
            var path = DataFolder + "/" + name + ".asset";
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>(); asset.name = name; AssetDatabase.CreateAsset(asset, path);
            initialize?.Invoke(asset); EditorUtility.SetDirty(asset); return asset;
        }
        private static void Assign(UnityEngine.Object target, string property, UnityEngine.Object value) => Edit(target, s => s.FindProperty(property).objectReferenceValue = value);
        private static void Edit(UnityEngine.Object target, Action<SerializedObject> edit)
        {
            var serialized = new SerializedObject(target); edit(serialized); serialized.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(target);
        }
        private static void Folder(string path)
        {
            var parts = path.Split('/'); var current = parts[0];
            foreach (var part in parts.Skip(1)) { var next = current + "/" + part; if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, part); current = next; }
        }
    }
}
