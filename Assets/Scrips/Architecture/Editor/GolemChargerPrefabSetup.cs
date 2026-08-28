using TicGame.Architecture;
using UnityEditor;
using UnityEngine;

namespace TicGame.Architecture.EditorTools
{
    public static class GolemChargerPrefabSetup
    {
        private const string EnemyLayerName = "Enemy";
        private const string PlayerLayerName = "PlayerHitbox";
        private const string EnvironmentLayerName = "Environment";
        private const string DefinitionFolder = "Assets/Data/Enemies";
        private const string TagFolder = "Assets/Data/Tags/Damage";
        private const string DamageProfileFolder = "Assets/Data/Damage";
        private const string ArtFolder = "Assets/Art/Enemies/GolemCharger";
        private const string PrefabFolder = "Assets/Prefabs/Enemies";
        private const string DefinitionPath = DefinitionFolder + "/Enemy_GolemCharger.asset";
        private const string ImpactTagPath = TagFolder + "/Damage_Impact.asset";
        private const string CardTagPath = TagFolder + "/Damage_Card.asset";
        private const string BasicTagPath = TagFolder + "/Damage_Basic.asset";
        private const string CardImpactProfilePath = DamageProfileFolder + "/Damage_CardImpact_Overcharge.asset";
        private const string BaselineSpriteSourcePath = "Assets/basic_golen_sptire_baseline.png";
        private const string BaselineSpritePath = ArtFolder + "/golem-charger-idle-baseline.png";
        private const string PrefabPath = PrefabFolder + "/GolemCharger.prefab";

        [MenuItem("TIC/Setup/Create Or Update Golem Charger")]
        public static void CreateOrUpdateGolemCharger()
        {
            EnsureFolder(DefinitionFolder);
            EnsureFolder(TagFolder);
            EnsureFolder(DamageProfileFolder);
            EnsureFolder(ArtFolder);
            EnsureFolder(PrefabFolder);

            var enemyLayer = EnsureLayer(EnemyLayerName);
            var environmentLayer = EnsureLayer(EnvironmentLayerName);
            if (enemyLayer < 0 || environmentLayer < 0)
            {
                Debug.LogError("Golem Charger setup requires free project layers for Enemy and Environment.");
                return;
            }

            var definition = CreateOrLoadDefinition();
            var impactTag = CreateOrLoadTag(ImpactTagPath, "Damage.Impact", "Impact");
            var cardTag = CreateOrLoadTag(CardTagPath, "Damage.Card", "Card");
            CreateOrLoadTag(BasicTagPath, "Damage.Basic", "Basic");
            CreateOrLoadCardImpactProfile(cardTag, impactTag);
            var baselineSprite = MoveAndConfigureBaselineSprite();
            CreateOrUpdatePrefab(definition, impactTag, cardTag, baselineSprite, enemyLayer, environmentLayer);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Created or updated the Golem Charger definition, damage tags, and prefab.");
        }

        private static EnemyDefinitionSO CreateOrLoadDefinition()
        {
            var definition = AssetDatabase.LoadAssetAtPath<EnemyDefinitionSO>(DefinitionPath);
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<EnemyDefinitionSO>();
                AssetDatabase.CreateAsset(definition, DefinitionPath);
            }

            definition.name = "Golem Charger";
            var serialized = new SerializedObject(definition);
            serialized.FindProperty("id").stringValue = "enemy.golem_charger";
            serialized.FindProperty("displayName").stringValue = "Golem Charger";
            serialized.FindProperty("maxHealth").floatValue = 12f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return definition;
        }

        private static GameplayTagSO CreateOrLoadTag(string path, string id, string displayName)
        {
            var tag = AssetDatabase.LoadAssetAtPath<GameplayTagSO>(path);
            if (tag == null)
            {
                tag = ScriptableObject.CreateInstance<GameplayTagSO>();
                AssetDatabase.CreateAsset(tag, path);
            }

            tag.name = displayName;
            var serialized = new SerializedObject(tag);
            serialized.FindProperty("id").stringValue = id;
            serialized.FindProperty("displayName").stringValue = displayName;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return tag;
        }

        private static DamageProfileSO CreateOrLoadCardImpactProfile(
            GameplayTagSO cardTag,
            GameplayTagSO impactTag)
        {
            var profile = AssetDatabase.LoadAssetAtPath<DamageProfileSO>(CardImpactProfilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<DamageProfileSO>();
                AssetDatabase.CreateAsset(profile, CardImpactProfilePath);
            }

            profile.name = "Card Impact Overcharge";
            var serialized = new SerializedObject(profile);
            serialized.FindProperty("id").stringValue = "damage.card-impact.overcharge";
            serialized.FindProperty("displayName").stringValue = "Card Impact Overcharge";
            serialized.FindProperty("description").stringValue = "Tags an armed Overcharge attack as Card and Impact.";
            serialized.FindProperty("baseDamage").floatValue = 0f;
            serialized.FindProperty("hitStopSeconds").floatValue = 0.1f;
            var tags = serialized.FindProperty("damageTags").FindPropertyRelative("tags");
            tags.arraySize = 2;
            tags.GetArrayElementAtIndex(0).objectReferenceValue = cardTag;
            tags.GetArrayElementAtIndex(1).objectReferenceValue = impactTag;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return profile;
        }

        private static void CreateOrUpdatePrefab(
            EnemyDefinitionSO definition,
            GameplayTagSO impactTag,
            GameplayTagSO cardTag,
            Sprite baselineSprite,
            int enemyLayer,
            int environmentLayer)
        {
            var loadedPrefabContents = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null;
            var root = LoadOrCreatePrefabRoot(PrefabPath, "Golem Charger");
            try
            {
                root.layer = enemyLayer;
                var body = GetOrAddComponent<Rigidbody2D>(root);
                body.bodyType = RigidbodyType2D.Dynamic;
                body.gravityScale = 1f;
                body.freezeRotation = true;
                body.interpolation = RigidbodyInterpolation2D.Interpolate;

                var bodyCollider = GetOrAddComponent<BoxCollider2D>(root);
                bodyCollider.isTrigger = false;
                bodyCollider.offset = new Vector2(x: 0f, y: 0.32f);
                bodyCollider.size = new Vector2(x: 0.62f, y: 0.64f);

                RemoveComponent<EnemyContactAttack2D>(root);
                var health = GetOrAddComponent<EnemyHealth>(root);
                var actor = GetOrAddComponent<EnemyActor>(root);
                actor.SetDefinition(definition);
                var chargeAttack = GetOrAddComponent<GolemChargeAttack2D>(root);
                var brain = GetOrAddComponent<GolemChargerBrain>(root);
                var damagePolicy = GetOrAddComponent<GolemChargerDamagePolicy>(root);

                var bodyHurtbox = GetOrCreateChild(root, "BodyHurtbox", enemyLayer);
                bodyHurtbox.transform.localPosition = new Vector3(x: 0f, y: 0.32f, z: 0f);
                var bodyHurtboxCollider = GetOrAddComponent<BoxCollider2D>(bodyHurtbox);
                bodyHurtboxCollider.isTrigger = true;
                bodyHurtboxCollider.size = new Vector2(x: 0.68f, y: 0.63f);
                var bodyRegion = GetOrAddComponent<EnemyHurtboxRegion>(bodyHurtbox);
                bodyRegion.Configure(damagePolicy, EnemyHurtboxRegionType.Body);

                var headHurtbox = GetOrCreateChild(root, "HeadWeakPointHurtbox", enemyLayer);
                headHurtbox.transform.localPosition = new Vector3(x: 0f, y: 0.54f, z: 0f);
                var headHurtboxCollider = GetOrAddComponent<CircleCollider2D>(headHurtbox);
                headHurtboxCollider.isTrigger = true;
                headHurtboxCollider.radius = 0.14f;
                var headRegion = GetOrAddComponent<EnemyHurtboxRegion>(headHurtbox);
                headRegion.Configure(damagePolicy, EnemyHurtboxRegionType.HeadWeakPoint);

                var chargeHitbox = GetOrCreateChild(root, "ChargeHitbox", enemyLayer);
                chargeHitbox.transform.localPosition = new Vector3(x: 0.43f, y: 0.3f, z: 0f);
                var chargeCollider = GetOrAddComponent<BoxCollider2D>(chargeHitbox);
                chargeCollider.isTrigger = true;
                chargeCollider.size = new Vector2(x: 0.58f, y: 0.45f);
                chargeCollider.enabled = false;

                ConfigureVisual(root, baselineSprite, enemyLayer);

                ConfigureComponents(
                    actor,
                    health,
                    brain,
                    chargeAttack,
                    damagePolicy,
                    body,
                    chargeCollider,
                    impactTag,
                    cardTag,
                    environmentLayer);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                ReleasePrefabRoot(root, loadedPrefabContents);
            }
        }

        private static Sprite MoveAndConfigureBaselineSprite()
        {
            if (AssetDatabase.LoadAssetAtPath<Sprite>(BaselineSpritePath) == null
                && AssetDatabase.LoadAssetAtPath<Texture2D>(BaselineSpriteSourcePath) != null)
            {
                var error = AssetDatabase.MoveAsset(BaselineSpriteSourcePath, BaselineSpritePath);
                if (!string.IsNullOrEmpty(error))
                {
                    Debug.LogError($"Could not move the Golem Charger baseline sprite: {error}");
                    return null;
                }
            }

            var importer = AssetImporter.GetAtPath(BaselineSpritePath) as TextureImporter;
            if (importer == null)
            {
                return null;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 128f;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            var importerSettings = new TextureImporterSettings();
            importer.ReadTextureSettings(importerSettings);
            importerSettings.spriteAlignment = (int)SpriteAlignment.BottomCenter;
            importerSettings.spritePivot = new Vector2(x: 0.5f, y: 0f);
            importer.SetTextureSettings(importerSettings);
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(BaselineSpritePath);
        }

        private static void ConfigureVisual(GameObject root, Sprite sprite, int enemyLayer)
        {
            var visual = GetOrCreateChild(root, "VisualRoot", enemyLayer);
            visual.transform.localPosition = Vector3.zero;
            var renderer = GetOrAddComponent<SpriteRenderer>(visual);
            renderer.sprite = sprite;
            renderer.sortingOrder = 0;
        }

        private static void ConfigureComponents(
            EnemyActor actor,
            EnemyHealth health,
            GolemChargerBrain brain,
            GolemChargeAttack2D chargeAttack,
            GolemChargerDamagePolicy damagePolicy,
            Rigidbody2D body,
            Collider2D chargeHitbox,
            GameplayTagSO impactTag,
            GameplayTagSO cardTag,
            int environmentLayer)
        {
            var playerLayers = GetTargetLayerMask();

            var attackSerialized = new SerializedObject(chargeAttack);
            attackSerialized.FindProperty("actor").objectReferenceValue = actor;
            attackSerialized.FindProperty("body").objectReferenceValue = body;
            attackSerialized.FindProperty("chargeHitbox").objectReferenceValue = chargeHitbox;
            attackSerialized.FindProperty("chargeSpeed").floatValue = 8f;
            attackSerialized.FindProperty("blockingLayers").intValue = 1 << environmentLayer;
            attackSerialized.FindProperty("damageAmount").floatValue = 1f;
            attackSerialized.FindProperty("targetLayers").intValue = playerLayers;
            attackSerialized.ApplyModifiedPropertiesWithoutUndo();

            var brainSerialized = new SerializedObject(brain);
            brainSerialized.FindProperty("actor").objectReferenceValue = actor;
            brainSerialized.FindProperty("chargeAttack").objectReferenceValue = chargeAttack;
            brainSerialized.FindProperty("body").objectReferenceValue = body;
            brainSerialized.FindProperty("targetLayers").intValue = playerLayers;
            brainSerialized.FindProperty("detectionRange").floatValue = 4f;
            brainSerialized.FindProperty("patrolSpeed").floatValue = 0.75f;
            brainSerialized.FindProperty("patrolHalfDistance").floatValue = 2f;
            brainSerialized.FindProperty("windupSeconds").floatValue = 0.75f;
            brainSerialized.FindProperty("chargeSeconds").floatValue = 0.45f;
            brainSerialized.FindProperty("interruptedSeconds").floatValue = 1.25f;
            brainSerialized.FindProperty("recoverySeconds").floatValue = 0.35f;
            brainSerialized.ApplyModifiedPropertiesWithoutUndo();

            var policySerialized = new SerializedObject(damagePolicy);
            policySerialized.FindProperty("health").objectReferenceValue = health;
            policySerialized.FindProperty("brain").objectReferenceValue = brain;
            policySerialized.FindProperty("impactTag").objectReferenceValue = impactTag;
            policySerialized.FindProperty("cardTag").objectReferenceValue = cardTag;
            policySerialized.FindProperty("idleDamageMultiplier").floatValue = 0.15f;
            policySerialized.FindProperty("windupDamageMultiplier").floatValue = 1f;
            policySerialized.FindProperty("chargeInterruptDamageMultiplier").floatValue = 1f;
            policySerialized.FindProperty("interruptedBodyDamageMultiplier").floatValue = 1.5f;
            policySerialized.FindProperty("interruptedHeadDamageMultiplier").floatValue = 3f;
            policySerialized.FindProperty("recoveryDamageMultiplier").floatValue = 1f;
            policySerialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GameObject LoadOrCreatePrefabRoot(string path, string objectName)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
            {
                return PrefabUtility.LoadPrefabContents(path);
            }

            return new GameObject(objectName);
        }

        private static void ReleasePrefabRoot(GameObject root, bool loadedPrefabContents)
        {
            if (loadedPrefabContents)
            {
                PrefabUtility.UnloadPrefabContents(root);
                return;
            }

            Object.DestroyImmediate(root);
        }

        private static GameObject GetOrCreateChild(GameObject root, string objectName, int layer)
        {
            var child = root.transform.Find(objectName);
            var result = child != null ? child.gameObject : new GameObject(objectName);
            result.layer = layer;
            result.transform.SetParent(root.transform, worldPositionStays: false);
            return result;
        }

        private static T GetOrAddComponent<T>(GameObject owner) where T : Component
        {
            var component = owner.GetComponent<T>();
            return component != null ? component : owner.AddComponent<T>();
        }

        private static void RemoveComponent<T>(GameObject owner) where T : Component
        {
            var component = owner.GetComponent<T>();
            if (component != null)
            {
                Object.DestroyImmediate(component);
            }
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

            var tagManagerAssets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (tagManagerAssets.Length == 0)
            {
                return -1;
            }

            var tagManager = new SerializedObject(tagManagerAssets[0]);
            var layers = tagManager.FindProperty("layers");
            for (var index = 8; index < layers.arraySize; index++)
            {
                var layer = layers.GetArrayElementAtIndex(index);
                if (!string.IsNullOrEmpty(layer.stringValue))
                {
                    continue;
                }

                layer.stringValue = layerName;
                tagManager.ApplyModifiedPropertiesWithoutUndo();
                return index;
            }

            return -1;
        }

        private static int GetTargetLayerMask()
        {
            var playerLayer = LayerMask.NameToLayer(PlayerLayerName);
            return playerLayer >= 0 ? 1 << playerLayer : ~0;
        }
    }
}
