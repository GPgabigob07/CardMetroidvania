using System.Collections.Generic;
using System.Linq;
using TicGame.Architecture;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TicGame.Architecture.EditorTools
{
    public static class BatMachinePrefabSetup
    {
        private const string EnemyLayerName = "Enemy";
        private const string EnvironmentLayerName = "Environment";
        private const string PlayerLayerName = "PlayerHitbox";
        private const string EnemyDefinitionPath = "Assets/Data/Enemies/Enemy_BatMachine.asset";
        private const string ProjectileDamagePath = "Assets/Data/Damage/Damage_BatProjectile.asset";
        private const string BatSheetDirectory = "Assets/Art/Enemies/BatMachine/";
        private const string ProjectileSpritePath = BatSheetDirectory + "BatMachineProjectile-v2.png";
        private const string ProjectileSpinPath = BatSheetDirectory + "BatMachineProjectile_Spin.png";
        private const string PrefabPath = "Assets/Prefabs/Enemies/BatMachine.prefab";
        private const string ScenePath = "Assets/Scenes/Test_BatMachine.unity";
        private const int BatFrameWidth = 64;
        private const int BatFrameHeight = 64;
        private const int ProjectileFrameWidth = 32;
        private const int ProjectileFrameHeight = 32;
        private const int BatFrameCount = 7;
        private const int ProjectileFrameCount = 5;
        private const float SpritePixelsPerUnit = 64f;

        private static readonly Dictionary<BatMachineState, string> BatSheetPaths = new()
        {
            { BatMachineState.PatrolRandom, BatSheetDirectory + "BatMachine_PatrolRandom.png" },
            { BatMachineState.Engage, BatSheetDirectory + "BatMachine_Engage.png" },
            { BatMachineState.WindupFire, BatSheetDirectory + "BatMachine_WindupFire.png" },
            { BatMachineState.Evade, BatSheetDirectory + "BatMachine_Evade.png" },
            { BatMachineState.StunnedFall, BatSheetDirectory + "BatMachine_StunnedFall.png" },
            { BatMachineState.GroundedRecovery, BatSheetDirectory + "BatMachine_GroundedRecovery.png" },
            { BatMachineState.Dead, BatSheetDirectory + "BatMachine_Dead.png" }
        };

        [MenuItem("TIC/Setup/Create Or Update Bat Machine")]
        public static void CreateOrUpdateBatMachine()
        {
            EnsureFolder("Assets/Data/Enemies");
            EnsureFolder("Assets/Data/Damage");
            EnsureFolder("Assets/Prefabs/Enemies");

            var enemyLayer = EnsureLayer(EnemyLayerName);
            var environmentLayer = EnsureLayer(EnvironmentLayerName);
            var playerLayer = EnsureLayer(PlayerLayerName);
            if (enemyLayer < 0 || environmentLayer < 0 || playerLayer < 0)
            {
                Debug.LogError("Bat Machine setup requires Enemy, Environment, and PlayerHitbox layers.");
                return;
            }

            var definition = CreateOrLoadEnemyDefinition();
            var projectileDamage = CreateOrLoadProjectileDamage();
            var legacyProjectileSprite = ConfigureLegacyProjectileSprite();
            var batFramesByState = ConfigureBatVisualSheets();
            var projectileFrames = ConfigureSpriteSheet(
                ProjectileSpinPath,
                ProjectileFrameWidth,
                ProjectileFrameHeight,
                SpritePixelsPerUnit);
            if (legacyProjectileSprite == null
                || !HasExpectedFrameCount(batFramesByState.Values, BatFrameCount)
                || projectileFrames.Count != ProjectileFrameCount)
            {
                Debug.LogError("Bat Machine setup requires seven frames for each Bat sheet and five projectile spin frames.");
                return;
            }

            CreateOrUpdatePrefab(
                definition,
                projectileDamage,
                batFramesByState,
                projectileFrames,
                enemyLayer,
                environmentLayer,
                playerLayer);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Created or updated the Bat Machine authored data and prefab.");
        }

        [MenuItem("TIC/Setup/Create Or Update Bat Machine Test Scene")]
        public static void CreateOrUpdateBatMachineTestScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            var batPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (batPrefab == null)
            {
                Debug.LogError("Run 'TIC/Setup/Create Or Update Bat Machine' before creating the test scene.");
                return;
            }

            var environmentLayer = LayerMask.NameToLayer(EnvironmentLayerName);
            if (environmentLayer < 0)
            {
                Debug.LogError("The Bat Machine test scene requires the Environment layer.");
                return;
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateCamera(scene);
            CreateArena(scene, environmentLayer);
            var player = CreatePlayerReference(scene);
            var bat = PrefabUtility.InstantiatePrefab(batPrefab, scene) as GameObject;
            bat.name = "BatMachine Training";
            bat.transform.position = new Vector3(3f, 2.5f, 0f);
            ConfigureSceneBat(bat, player, environmentLayer);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Created or updated Test_BatMachine.unity.");
        }

        private static EnemyDefinitionSO CreateOrLoadEnemyDefinition()
        {
            var definition = AssetDatabase.LoadAssetAtPath<EnemyDefinitionSO>(EnemyDefinitionPath);
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<EnemyDefinitionSO>();
                AssetDatabase.CreateAsset(definition, EnemyDefinitionPath);
            }

            definition.name = "Bat Machine";
            var serialized = new SerializedObject(definition);
            SetString(serialized, "id", "enemy.bat_machine");
            SetString(serialized, "displayName", "Bat Machine");
            SetFloat(serialized, "maxHealth", 10f);
            SetFloat(serialized, "defeatEnergyReward", 2f);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return definition;
        }

        private static DamageProfileSO CreateOrLoadProjectileDamage()
        {
            var profile = AssetDatabase.LoadAssetAtPath<DamageProfileSO>(ProjectileDamagePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<DamageProfileSO>();
                AssetDatabase.CreateAsset(profile, ProjectileDamagePath);
            }

            profile.name = "Bat Projectile";
            var serialized = new SerializedObject(profile);
            SetString(serialized, "id", "damage.bat_projectile");
            SetString(serialized, "displayName", "Bat Projectile");
            SetString(serialized, "description", "A non-homing projectile fired by the Bat Machine.");
            SetFloat(serialized, "baseDamage", 1f);
            SetFloat(serialized, "hitStopSeconds", 0.05f);
            SetFloat(serialized, "knockbackForce", 0f);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return profile;
        }

        private static Dictionary<BatMachineState, Sprite[]> ConfigureBatVisualSheets()
        {
            var framesByState = new Dictionary<BatMachineState, Sprite[]>();
            foreach (var pair in BatSheetPaths)
            {
                var frames = ConfigureSpriteSheet(pair.Value, BatFrameWidth, BatFrameHeight, SpritePixelsPerUnit);
                framesByState[pair.Key] = frames.ToArray();
            }

            return framesByState;
        }

        private static Sprite ConfigureLegacyProjectileSprite()
        {
            AssetDatabase.Refresh();
            AssetDatabase.ImportAsset(ProjectileSpritePath, ImportAssetOptions.ForceUpdate);
            var importer = AssetImporter.GetAtPath(ProjectileSpritePath) as TextureImporter;
            if (importer == null)
            {
                Debug.LogError($"Missing legacy Bat Machine projectile sprite at {ProjectileSpritePath}.");
                return null;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = SpritePixelsPerUnit;
            importer.maxTextureSize = ProjectileFrameWidth;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(ProjectileSpritePath);
        }

        private static List<Sprite> ConfigureSpriteSheet(
            string path,
            int frameWidth,
            int frameHeight,
            float pixelsPerUnit)
        {
            AssetDatabase.Refresh();
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                Debug.LogError($"Missing Bat Machine sprite sheet at {path}.");
                return new List<Sprite>();
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = pixelsPerUnit;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();

            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture == null
                || texture.width % frameWidth != 0
                || texture.height != frameHeight)
            {
                Debug.LogError($"Bat Machine sprite sheet at {path} must be a single row of {frameWidth} by {frameHeight} frames.");
                return new List<Sprite>();
            }

            var frameCount = texture.width / frameWidth;
            var spriteRects = new SpriteRect[frameCount];
            var sheetName = System.IO.Path.GetFileNameWithoutExtension(path);
            for (var index = 0; index < frameCount; index++)
            {
                spriteRects[index] = new SpriteRect
                {
                    name = $"{sheetName}_{index}",
                    alignment = SpriteAlignment.Custom,
                    pivot = new Vector2(0.5f, 0.5f),
                    spriteID = new GUID(Hash128.Compute($"{path}:{index}").ToString()),
                    rect = new Rect(index * frameWidth, 0f, frameWidth, frameHeight)
                };
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
            return AssetDatabase.LoadAllAssetsAtPath(path)
                .OfType<Sprite>()
                .OrderBy(sprite => sprite.rect.x)
                .ToList();
        }

        private static bool HasExpectedFrameCount(IEnumerable<Sprite[]> clips, int expectedFrameCount)
        {
            return clips.All(clip => clip != null
                && clip.Length == expectedFrameCount
                && clip.All(sprite => sprite != null));
        }

        private static void CreateOrUpdatePrefab(
            EnemyDefinitionSO definition,
            DamageProfileSO projectileDamage,
            IReadOnlyDictionary<BatMachineState, Sprite[]> batFramesByState,
            IReadOnlyList<Sprite> projectileFrames,
            int enemyLayer,
            int environmentLayer,
            int playerLayer)
        {
            var exists = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null;
            var root = exists ? PrefabUtility.LoadPrefabContents(PrefabPath) : new GameObject("BatMachine");
            try
            {
                root.name = "BatMachine";
                root.layer = enemyLayer;
                var body = GetOrAddComponent<Rigidbody2D>(root);
                body.bodyType = RigidbodyType2D.Dynamic;
                body.gravityScale = 1f;
                body.freezeRotation = true;
                body.interpolation = RigidbodyInterpolation2D.Interpolate;

                var bodyCollider = GetOrAddComponent<BoxCollider2D>(root);
                bodyCollider.isTrigger = false;
                bodyCollider.size = new Vector2(0.9f, 0.5f);

                var health = GetOrAddComponent<EnemyHealth>(root);
                var poise = GetOrAddComponent<EnemyPoise>(root);
                var actor = GetOrAddComponent<EnemyActor>(root);
                actor.SetDefinition(definition);
                var motor = GetOrAddComponent<AerialSteeringMotor2D>(root);
                var monitor = GetOrAddComponent<BatThreatMonitor>(root);
                var policy = GetOrAddComponent<BatMachineDamagePolicy>(root);
                var brain = GetOrAddComponent<BatMachineBrain>(root);
                var launcher = GetOrAddComponent<BatProjectileLauncher>(root);

                var hurtbox = GetOrCreateChild(root, "BatMachineHurtbox", enemyLayer);
                hurtbox.transform.localPosition = Vector3.zero;
                var hurtboxCollider = GetOrAddComponent<BoxCollider2D>(hurtbox);
                hurtboxCollider.isTrigger = true;
                hurtboxCollider.size = new Vector2(1f, 0.6f);
                var hurtboxRouter = GetOrAddComponent<BatMachineHurtbox>(hurtbox);
                hurtboxRouter.Configure(policy);

                var spawn = GetOrCreateChild(root, "ProjectileSpawn", enemyLayer);
                spawn.transform.localPosition = new Vector3(0.6f, 0f, 0f);
                var projectileTemplate = GetOrCreateChild(root, "ProjectileTemplate", enemyLayer);
                projectileTemplate.transform.localPosition = new Vector3(0.7f, 0f, 0f);
                var projectileBody = GetOrAddComponent<Rigidbody2D>(projectileTemplate);
                projectileBody.bodyType = RigidbodyType2D.Kinematic;
                projectileBody.gravityScale = 0f;
                var projectileCollider = GetOrAddComponent<CircleCollider2D>(projectileTemplate);
                projectileCollider.isTrigger = true;
                projectileCollider.radius = 0.12f;
                var projectile = GetOrAddComponent<EnemyProjectile2D>(projectileTemplate);
                var projectileRenderer = GetOrAddComponent<SpriteRenderer>(projectileTemplate);
                projectileRenderer.sprite = projectileFrames[0];
                projectileRenderer.sortingOrder = 2;
                var projectileSpinVisual = GetOrAddComponent<ProjectileSpinVisual>(projectileTemplate);
                projectileSpinVisual.Configure(projectile, projectileRenderer, projectileFrames.ToArray());
                projectileTemplate.SetActive(false);

                var visual = GetOrCreateChild(root, "VisualRoot", enemyLayer);
                visual.transform.localPosition = Vector3.zero;
                var renderer = GetOrAddComponent<SpriteRenderer>(visual);
                renderer.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
                renderer.color = new Color(0.45f, 0.75f, 0.95f, 1f);
                renderer.sortingOrder = 1;
                var visualController = GetOrAddComponent<BatMachineVisualController>(visual);
                visualController.Configure(brain, body, renderer, batFramesByState);

                ConfigureComponents(
                    actor, health, poise, body, motor, monitor, policy, brain, launcher, projectile,
                    spawn.transform, root, projectileDamage, environmentLayer, playerLayer, enemyLayer);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                if (exists)
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
                else
                {
                    Object.DestroyImmediate(root);
                }
            }
        }

        private static void ConfigureComponents(
            EnemyActor actor, EnemyHealth health, EnemyPoise poise, Rigidbody2D body,
            AerialSteeringMotor2D motor, BatThreatMonitor monitor, BatMachineDamagePolicy policy,
            BatMachineBrain brain, BatProjectileLauncher launcher, EnemyProjectile2D projectile,
            Transform spawn, GameObject root, DamageProfileSO projectileDamage,
            int environmentLayer, int playerLayer, int enemyLayer)
        {
            var poiseSerialized = new SerializedObject(poise);
            SetFloat(poiseSerialized, "fallbackMaximumPoise", 30f);
            SetFloat(poiseSerialized, "fallbackRegenerationPerSecond", 0.33f);
            poiseSerialized.ApplyModifiedPropertiesWithoutUndo();

            var motorSerialized = new SerializedObject(motor);
            SetObject(motorSerialized, "body", body);
            motorSerialized.ApplyModifiedPropertiesWithoutUndo();

            var monitorSerialized = new SerializedObject(monitor);
            SetFloat(monitorSerialized, "monitorRadius", 8f);
            SetFloat(monitorSerialized, "baseMeleeReach", 2f);
            SetFloat(monitorSerialized, "outerBandThickness", 0.5f);
            monitorSerialized.ApplyModifiedPropertiesWithoutUndo();

            var policySerialized = new SerializedObject(policy);
            SetObject(policySerialized, "health", health);
            SetObject(policySerialized, "poise", poise);
            SetObject(policySerialized, "brain", brain);
            policySerialized.ApplyModifiedPropertiesWithoutUndo();

            var projectileSerialized = new SerializedObject(projectile);
            SetObject(projectileSerialized, "body", projectile.GetComponent<Rigidbody2D>());
            SetFloat(projectileSerialized, "lifetimeSeconds", 3f);
            SetObject(projectileSerialized, "damageProfile", projectileDamage);
            SetLayerMask(projectileSerialized, "targetLayers", (1 << playerLayer) | (1 << enemyLayer));
            projectileSerialized.ApplyModifiedPropertiesWithoutUndo();

            var launcherSerialized = new SerializedObject(launcher);
            SetObject(launcherSerialized, "projectilePrefab", projectile);
            SetObject(launcherSerialized, "spawnPoint", spawn);
            SetObject(launcherSerialized, "sourceObject", root);
            SetFloat(launcherSerialized, "projectileSpeed", 6f);
            launcherSerialized.ApplyModifiedPropertiesWithoutUndo();

            var brainSerialized = new SerializedObject(brain);
            SetObject(brainSerialized, "actor", actor);
            SetObject(brainSerialized, "health", health);
            SetObject(brainSerialized, "poise", poise);
            SetObject(brainSerialized, "body", body);
            SetObject(brainSerialized, "motor", motor);
            SetObject(brainSerialized, "threatMonitor", monitor);
            SetObject(brainSerialized, "launcher", launcher);
            SetObject(brainSerialized, "damagePolicy", policy);
            SetVector2(brainSerialized, "patrolHalfExtents", new Vector2(2.5f, 1.25f));
            SetFloat(brainSerialized, "minimumPatrolWaypointDistance", 0.5f);
            SetFloat(brainSerialized, "patrolArrivalDistance", 0.15f);
            SetFloat(brainSerialized, "patrolWaitSeconds", 0.3f);
            SetInt(brainSerialized, "patrolSeed", 1729);
            SetLayerMask(brainSerialized, "patrolObstacleLayers", 1 << environmentLayer);
            SetFloat(brainSerialized, "normalFireCooldown", 4f);
            SetFloat(brainSerialized, "shortFireCooldown", 2f);
            SetFloat(brainSerialized, "fireWindupSeconds", 0.2f);
            SetFloat(brainSerialized, "interShotDelay", 0.1f);
            SetFloat(brainSerialized, "flightSpeed", 5f);
            SetFloat(brainSerialized, "flightAcceleration", 12f);
            SetVector2(brainSerialized, "engageOffset", new Vector2(-2f, 1f));
            SetFloat(brainSerialized, "evadeSeconds", 0.25f);
            SetFloat(brainSerialized, "evadeCooldownSeconds", 1f);
            SetFloat(brainSerialized, "safeImpactSpeed", 4f);
            SetFloat(brainSerialized, "damagePerSpeedUnit", 1f);
            SetFloat(brainSerialized, "groundedRecoverySeconds", 0.5f);
            SetFloat(brainSerialized, "postStunPoiseRestore", 12f);
            brainSerialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GameObject CreatePlayerReference(Scene scene)
        {
            var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player/Player.prefab");
            var player = playerPrefab != null
                ? PrefabUtility.InstantiatePrefab(playerPrefab, scene) as GameObject
                : new GameObject("Player Reference");
            player.name = "Player";
            player.transform.position = new Vector3(-2f, 0f, 0f);
            return player;
        }

        private static void ConfigureSceneBat(GameObject bat, GameObject player, int environmentLayer)
        {
            var brain = bat.GetComponent<BatMachineBrain>();
            var targetBody = player.GetComponent<Rigidbody2D>();
            var serialized = new SerializedObject(brain);
            SetObject(serialized, "target", player.transform);
            SetObject(serialized, "targetBody", targetBody);
            SetLayerMask(serialized, "patrolObstacleLayers", 1 << environmentLayer);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateCamera(Scene scene)
        {
            var cameraObject = new GameObject("Main Camera");
            SceneManager.MoveGameObjectToScene(cameraObject, scene);
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 2f, -10f);
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            camera.backgroundColor = new Color(0.08f, 0.12f, 0.2f, 1f);
            cameraObject.AddComponent<AudioListener>();
        }

        private static void CreateArena(Scene scene, int environmentLayer)
        {
            CreateArenaBlock(scene, "Floor", new Vector3(0f, -1f, 0f), new Vector2(16f, 1f), environmentLayer);
            CreateArenaBlock(scene, "Left Wall", new Vector3(-8f, 2f, 0f), new Vector2(0.5f, 6f), environmentLayer);
            CreateArenaBlock(scene, "Right Wall", new Vector3(8f, 2f, 0f), new Vector2(0.5f, 6f), environmentLayer);
        }

        private static void CreateArenaBlock(Scene scene, string name, Vector3 position, Vector2 size, int layer)
        {
            var block = new GameObject(name);
            block.layer = layer;
            block.transform.position = position;
            SceneManager.MoveGameObjectToScene(block, scene);
            var collider = block.AddComponent<BoxCollider2D>();
            collider.size = size;
        }

        private static GameObject GetOrCreateChild(GameObject root, string name, int layer)
        {
            var child = root.transform.Find(name);
            var result = child != null ? child.gameObject : new GameObject(name);
            result.layer = layer;
            result.transform.SetParent(root.transform, false);
            return result;
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

        private static int EnsureLayer(string layerName)
        {
            var existingLayer = LayerMask.NameToLayer(layerName);
            if (existingLayer >= 0)
            {
                return existingLayer;
            }

            var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (assets.Length == 0)
            {
                return -1;
            }

            var serialized = new SerializedObject(assets[0]);
            var layers = serialized.FindProperty("layers");
            for (var index = 8; index < layers.arraySize; index++)
            {
                var layer = layers.GetArrayElementAtIndex(index);
                if (!string.IsNullOrEmpty(layer.stringValue))
                {
                    continue;
                }

                layer.stringValue = layerName;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                return index;
            }

            return -1;
        }

        private static void SetObject(SerializedObject serialized, string propertyName, Object value)
        {
            serialized.FindProperty(propertyName).objectReferenceValue = value;
        }

        private static void SetString(SerializedObject serialized, string propertyName, string value)
        {
            serialized.FindProperty(propertyName).stringValue = value;
        }

        private static void SetFloat(SerializedObject serialized, string propertyName, float value)
        {
            serialized.FindProperty(propertyName).floatValue = value;
        }

        private static void SetInt(SerializedObject serialized, string propertyName, int value)
        {
            serialized.FindProperty(propertyName).intValue = value;
        }

        private static void SetVector2(SerializedObject serialized, string propertyName, Vector2 value)
        {
            serialized.FindProperty(propertyName).vector2Value = value;
        }

        private static void SetLayerMask(SerializedObject serialized, string propertyName, int value)
        {
            serialized.FindProperty(propertyName).intValue = value;
        }
    }
}
