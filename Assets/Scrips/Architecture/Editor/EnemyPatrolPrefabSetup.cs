using TicGame.Architecture;
using UnityEditor;
using UnityEngine;

namespace TicGame.Architecture.EditorTools
{
    public static class EnemyPatrolPrefabSetup
    {
        private const string EnemyLayerName = "Enemy";
        private const string PlayerLayerName = "PlayerHitbox";
        private const string EnvironmentLayerName = "Environment";
        private const string DefinitionFolder = "Assets/Data/Enemies";
        private const string PrefabFolder = "Assets/Prefabs/Enemies";
        private const string DefinitionPath = DefinitionFolder + "/Enemy_Patrol.asset";
        private const string GroundedPrefabPath = PrefabFolder + "/GroundedPatrolEnemy.prefab";
        private const string AerialPrefabPath = PrefabFolder + "/AerialPatrolEnemy.prefab";

        [MenuItem("TIC/Setup/Create Or Update Patrol Enemy Prefabs")]
        public static void CreateOrUpdatePatrolEnemyPrefabs()
        {
            EnsureFolder(DefinitionFolder);
            EnsureFolder(PrefabFolder);

            var enemyLayer = EnsureLayer(EnemyLayerName);
            var environmentLayer = EnsureLayer(EnvironmentLayerName);
            if (enemyLayer < 0 || environmentLayer < 0)
            {
                Debug.LogError("Patrol enemy setup requires free project layers for Enemy and Environment.");
                return;
            }

            var definition = CreateOrLoadDefinition();
            CreateOrUpdateGroundedPrefab(definition, enemyLayer, environmentLayer);
            CreateOrUpdateAerialPrefab(definition, enemyLayer);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Created or updated grounded and aerial patrol enemy prefabs.");
        }

        private static EnemyDefinitionSO CreateOrLoadDefinition()
        {
            var definition = AssetDatabase.LoadAssetAtPath<EnemyDefinitionSO>(DefinitionPath);
            if (definition != null)
            {
                return definition;
            }

            definition = ScriptableObject.CreateInstance<EnemyDefinitionSO>();
            definition.name = "Patrol Enemy";
            AssetDatabase.CreateAsset(definition, DefinitionPath);
            return definition;
        }

        private static void CreateOrUpdateGroundedPrefab(
            EnemyDefinitionSO definition,
            int enemyLayer,
            int environmentLayer)
        {
            var loadedPrefabContents = AssetDatabase.LoadAssetAtPath<GameObject>(GroundedPrefabPath) != null;
            var root = LoadOrCreatePrefabRoot(GroundedPrefabPath, "Grounded Patrol Enemy");
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
                bodyCollider.size = new Vector2(x: 0.8f, y: 1f);

                RemoveComponent<AerialEnemyPatrolMotor2D>(root);
                var motor = GetOrAddComponent<GroundedEnemyPatrolMotor2D>(root);
                var knockback = GetOrAddComponent<EnemyKnockbackReceiver>(root);
                knockback.SetBody(body);
                motor.SetBody(body);
                motor.ConfigureProbes(
                    layer: 1 << environmentLayer,
                    ledgeOffset: new Vector2(x: 0.5f, y: -0.6f),
                    wallOffset: new Vector2(x: 0.55f, y: 0f),
                    radius: 0.08f);

                ConfigureSharedEnemy(root, definition, motor);
                ConfigureHurtbox(root, enemyLayer);
                PrefabUtility.SaveAsPrefabAsset(root, GroundedPrefabPath);
            }
            finally
            {
                ReleasePrefabRoot(root, loadedPrefabContents);
            }
        }

        private static void CreateOrUpdateAerialPrefab(
            EnemyDefinitionSO definition,
            int enemyLayer)
        {
            var loadedPrefabContents = AssetDatabase.LoadAssetAtPath<GameObject>(AerialPrefabPath) != null;
            var root = LoadOrCreatePrefabRoot(AerialPrefabPath, "Aerial Patrol Enemy");
            try
            {
                root.layer = enemyLayer;
                var body = GetOrAddComponent<Rigidbody2D>(root);
                body.bodyType = RigidbodyType2D.Dynamic;
                body.gravityScale = 0f;
                body.freezeRotation = true;
                body.interpolation = RigidbodyInterpolation2D.Interpolate;

                var bodyCollider = GetOrAddComponent<BoxCollider2D>(root);
                bodyCollider.isTrigger = true;
                bodyCollider.size = new Vector2(x: 0.8f, y: 0.8f);

                RemoveComponent<GroundedEnemyPatrolMotor2D>(root);
                var motor = GetOrAddComponent<AerialEnemyPatrolMotor2D>(root);
                motor.SetBody(body);
                var knockback = GetOrAddComponent<EnemyKnockbackReceiver>(root);
                knockback.SetBody(body);

                ConfigureSharedEnemy(root, definition, motor);
                var brain = root.GetComponent<EnemyPatrolBrain>();
                brain.ConfigureRoute(
                    firstOffset: new Vector2(x: -2f, y: 0f),
                    secondOffset: new Vector2(x: 2f, y: 1f));
                ConfigureHurtbox(root, enemyLayer);
                PrefabUtility.SaveAsPrefabAsset(root, AerialPrefabPath);
            }
            finally
            {
                ReleasePrefabRoot(root, loadedPrefabContents);
            }
        }

        private static void ConfigureSharedEnemy(
            GameObject root,
            EnemyDefinitionSO definition,
            MonoBehaviour motor)
        {
            GetOrAddComponent<EnemyHealth>(root);
            var actor = GetOrAddComponent<EnemyActor>(root);
            actor.SetDefinition(value: definition);
            var contactAttack = GetOrAddComponent<EnemyContactAttack2D>(root);
            contactAttack.Configure(actor, root.GetComponentInChildren<SpriteRenderer>(includeInactive: true));
            contactAttack.ConfigureDamage(amount: 1f, cooldown: 0.75f, flashDuration: 0.15f);
            contactAttack.ConfigureOverlap(
                root.GetComponent<Collider2D>() ?? root.GetComponentInChildren<Collider2D>(includeInactive: true),
                GetTargetLayerMask());

            var brain = GetOrAddComponent<EnemyPatrolBrain>(root);
            brain.ConfigureRoute(
                firstOffset: new Vector2(x: -2f, y: 0f),
                secondOffset: new Vector2(x: 2f, y: 0f));
            brain.ConfigureMovement(speed: 2f, delay: 0.2f, distance: 0.05f);

            var serializedBrain = new SerializedObject(brain);
            serializedBrain.FindProperty("actor").objectReferenceValue = actor;
            serializedBrain.FindProperty("motorComponent").objectReferenceValue = motor;
            serializedBrain.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureHurtbox(GameObject root, int enemyLayer)
        {
            var hurtboxTransform = root.transform.Find("Hurtbox");
            var hurtbox = hurtboxTransform != null
                ? hurtboxTransform.gameObject
                : new GameObject(name: "Hurtbox");
            hurtbox.layer = enemyLayer;
            hurtbox.transform.SetParent(parent: root.transform, worldPositionStays: false);
            hurtbox.transform.localPosition = Vector3.zero;

            var collider = GetOrAddComponent<BoxCollider2D>(hurtbox);
            collider.isTrigger = true;
            collider.size = new Vector2(x: 0.9f, y: 1.1f);
        }

        private static GameObject LoadOrCreatePrefabRoot(string path, string objectName)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
            {
                return PrefabUtility.LoadPrefabContents(path);
            }

            return new GameObject(name: objectName);
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
                AssetDatabase.SaveAssets();
                return index;
            }

            return -1;
        }

        private static LayerMask GetTargetLayerMask()
        {
            var playerLayer = LayerMask.NameToLayer(PlayerLayerName);
            return playerLayer >= 0 ? 1 << playerLayer : ~0;
        }
    }
}
