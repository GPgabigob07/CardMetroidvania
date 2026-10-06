using UnityEngine;

namespace TicGame.Architecture
{
    public sealed class TrainingDummyReviewBootstrap : MonoBehaviour
    {
        private const string GroundDummyName = "Review Dummy - Ground";
        private const string AirDummyName = "Review Dummy - Air";

        private EnemyDefinitionSO runtimeDefinition;
        private Sprite runtimeSprite;
        private Texture2D runtimeTexture;
        [Header("Hit Feedback")]
        [SerializeField] private HitRippleProfileSO hitRippleProfile;
        [SerializeField] private Material hitRippleMaterial;

        public bool ConfigureFeedback(HitRippleProfileSO profile, Material material)
        {
            if (hitRippleProfile == profile && hitRippleMaterial == material) return false;
            hitRippleProfile = profile; hitRippleMaterial = material; return true;
        }

        private void Start()
        {
            runtimeDefinition = ScriptableObject.CreateInstance<EnemyDefinitionSO>();
            runtimeDefinition.name = "Review Training Dummy";

            CreateDummy(
                objectName: GroundDummyName,
                position: transform.position + new Vector3(x: 3f, y: 0f, z: 0f),
                color: new Color(r: 0.85f, g: 0.2f, b: 0.3f, a: 1f));
            CreateDummy(
                objectName: AirDummyName,
                position: transform.position + new Vector3(x: 4.5f, y: 2.5f, z: 0f),
                color: new Color(r: 0.2f, g: 0.75f, b: 1f, a: 1f));
        }

        private void OnDestroy()
        {
            if (runtimeSprite != null) Destroy(runtimeSprite);
            if (runtimeTexture != null) Destroy(runtimeTexture);
            if (runtimeDefinition != null)
            {
                Destroy(obj: runtimeDefinition);
            }
        }

        private void CreateDummy(string objectName, Vector3 position, Color color)
        {
            if (GameObject.Find(name: objectName) != null)
            {
                return;
            }

            var dummyObject = new GameObject(name: objectName);
            dummyObject.transform.position = position;

            dummyObject.layer = LayerMask.NameToLayer("Enemy");

            var renderer = dummyObject.AddComponent<SpriteRenderer>();
            renderer.sprite = CreateSquareSprite();
            renderer.color = color;
            renderer.sortingOrder = 2;
            dummyObject.transform.localScale = new Vector3(x: 1f, y: 2f, z: 1f);

            var collider = dummyObject.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;

            var health = dummyObject.AddComponent<EnemyHealth>();
            var actor = dummyObject.AddComponent<EnemyActor>();
            actor.SetDefinition(value: runtimeDefinition);
            actor.Initialize();

            dummyObject.AddComponent<TrainingDummy>();
            var debug = dummyObject.AddComponent<EnemyDebugPresentation>();
            if (hitRippleProfile != null && hitRippleMaterial != null)
            {
                renderer.sharedMaterial = hitRippleMaterial;
                var presenter = dummyObject.AddComponent<EnemyHitRipplePresenter>();
                presenter.Configure(actor, hitRippleProfile, new[] { renderer });
                dummyObject.AddComponent<EnemyHitRippleDamageListener>().Configure(presenter);
                debug.SetHitFlashEnabled(false);
            }
        }

        private Sprite CreateSquareSprite()
        {
            if (runtimeSprite != null) return runtimeSprite;
            // Same unit square as the original dummy, with enough source pixels to review the band.
            runtimeTexture = new Texture2D(32, 32) { filterMode = FilterMode.Point };
            var pixels = new Color32[32 * 32];
            System.Array.Fill(pixels, new Color32(255, 255, 255, 255));
            runtimeTexture.SetPixels32(pixels); runtimeTexture.Apply();
            runtimeSprite = Sprite.Create(
                texture: runtimeTexture,
                rect: new Rect(x: 0f, y: 0f, width: 32f, height: 32f),
                pivot: new Vector2(x: 0.5f, y: 0.5f),
                pixelsPerUnit: 32f);
            return runtimeSprite;
        }
    }
}
