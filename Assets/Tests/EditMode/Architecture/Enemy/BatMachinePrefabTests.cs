using System.Linq;
using NUnit.Framework;
using TicGame.Architecture;
using UnityEditor;
using UnityEngine;

public sealed class BatMachinePrefabTests
{
    private const string BatSheetDirectory = "Assets/Art/Enemies/BatMachine/";
    private const string ProjectileSpritePath = "Assets/Art/Enemies/BatMachine/BatMachineProjectile-v2.png";
    private const string ProjectileSpinPath = "Assets/Art/Enemies/BatMachine/BatMachineProjectile_Spin.png";

    [Test]
    public void BatMachinePrefab_HasRequiredCombatComposition()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/BatMachine.prefab");

        Assert.NotNull(prefab, "The authored Bat Machine prefab must exist.");
        Assert.NotNull(prefab.GetComponent<EnemyActor>());
        Assert.NotNull(prefab.GetComponent<EnemyPoise>());
        Assert.NotNull(prefab.GetComponent<BatMachineBrain>());
        Assert.NotNull(prefab.GetComponent<BatProjectileLauncher>());
    }

    [Test]
    public void BatMachinePrefab_VisualRootConfiguresControllerDependencies()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/BatMachine.prefab");

        Assert.NotNull(prefab, "The authored Bat Machine prefab must exist.");
        var visualRoot = prefab.transform.Find("VisualRoot");
        Assert.NotNull(visualRoot, "The Bat Machine must have a VisualRoot child.");
        var controller = visualRoot.GetComponent<BatMachineVisualController>();
        Assert.NotNull(controller, "VisualRoot must use the Bat Machine visual controller.");
        var serialized = new SerializedObject(controller);
        Assert.AreEqual(prefab.GetComponent<BatMachineBrain>(), serialized.FindProperty("brain").objectReferenceValue);
        Assert.AreEqual(prefab.GetComponent<Rigidbody2D>(), serialized.FindProperty("body").objectReferenceValue);
        Assert.AreEqual(visualRoot.GetComponent<SpriteRenderer>(), serialized.FindProperty("renderer").objectReferenceValue);
    }

    [TestCase(BatMachineState.PatrolRandom, "BatMachine_PatrolRandom.png")]
    [TestCase(BatMachineState.Engage, "BatMachine_Engage.png")]
    [TestCase(BatMachineState.WindupFire, "BatMachine_WindupFire.png")]
    [TestCase(BatMachineState.Evade, "BatMachine_Evade.png")]
    [TestCase(BatMachineState.StunnedFall, "BatMachine_StunnedFall.png")]
    [TestCase(BatMachineState.GroundedRecovery, "BatMachine_GroundedRecovery.png")]
    [TestCase(BatMachineState.Dead, "BatMachine_Dead.png")]
    public void BatMachinePrefab_VisualRootConfiguresSevenFramesFromTheStateSheet(
        BatMachineState state,
        string sheetName)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/BatMachine.prefab");
        var controller = prefab.transform.Find("VisualRoot").GetComponent<BatMachineVisualController>();
        var frames = controller.GetFrames(state);

        Assert.AreEqual(7, frames.Count, $"{state} must have exactly seven configured frames.");
        foreach (var frame in frames)
        {
            Assert.NotNull(frame, $"{state} must not contain an empty visual frame.");
            Assert.AreEqual(BatSheetDirectory + sheetName, AssetDatabase.GetAssetPath(frame));
        }
    }

    [Test]
    public void BatMachinePrefab_ProjectileTemplateConfiguresFiveSpinFrames()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/BatMachine.prefab");
        var importedFrames = AssetDatabase.LoadAllAssetsAtPath(ProjectileSpinPath);

        Assert.NotNull(prefab, "The authored Bat Machine prefab must exist.");
        var template = prefab.transform.Find("ProjectileTemplate");
        Assert.NotNull(template, "The projectile template child must exist.");
        var spinVisual = template.GetComponent<ProjectileSpinVisual>();
        Assert.NotNull(spinVisual, "ProjectileTemplate must use the projectile spin visual.");
        var renderer = template.GetComponent<SpriteRenderer>();
        Assert.NotNull(renderer, "The projectile template must render its launched projectile.");
        var spinSerialized = new SerializedObject(spinVisual);
        Assert.AreEqual(template.GetComponent<EnemyProjectile2D>(), spinSerialized.FindProperty("projectile").objectReferenceValue);
        Assert.AreEqual(renderer, spinSerialized.FindProperty("renderer").objectReferenceValue);
        var frames = spinSerialized.FindProperty("frames");
        Assert.NotNull(frames, "ProjectileSpinVisual must serialize its configured frames.");
        Assert.AreEqual(5, frames.arraySize, "ProjectileTemplate must configure exactly five spin frames.");
        for (var index = 0; index < frames.arraySize; index++)
        {
            var frame = frames.GetArrayElementAtIndex(index).objectReferenceValue as Sprite;
            Assert.NotNull(frame, "ProjectileTemplate must not contain an empty spin frame.");
            Assert.AreEqual(ProjectileSpinPath, AssetDatabase.GetAssetPath(frame));
        }

        var sprites = System.Array.FindAll(importedFrames, asset => asset is Sprite)
            .Cast<Sprite>()
            .OrderBy(sprite => sprite.rect.x)
            .ToArray();
        Assert.AreEqual(5, sprites.Length, "The projectile spin sheet must import exactly five sprites.");
        for (var index = 0; index < sprites.Length; index++)
        {
            Assert.AreEqual(new Rect(index * 32f, 0f, 32f, 32f), sprites[index].rect);
        }

        AssertSpriteSheetImportSettings(ProjectileSpinPath, SpriteImportMode.Multiple);
        Assert.AreEqual(sprites[0], renderer.sprite, "ProjectileTemplate must render the first spin frame before launch.");
    }

    [TestCase(BatMachineState.PatrolRandom, "BatMachine_PatrolRandom.png")]
    [TestCase(BatMachineState.Engage, "BatMachine_Engage.png")]
    [TestCase(BatMachineState.WindupFire, "BatMachine_WindupFire.png")]
    [TestCase(BatMachineState.Evade, "BatMachine_Evade.png")]
    [TestCase(BatMachineState.StunnedFall, "BatMachine_StunnedFall.png")]
    [TestCase(BatMachineState.GroundedRecovery, "BatMachine_GroundedRecovery.png")]
    [TestCase(BatMachineState.Dead, "BatMachine_Dead.png")]
    public void BatMachineVisualSheet_ImportsExactlySevenSprites(BatMachineState state, string sheetName)
    {
        var assets = AssetDatabase.LoadAllAssetsAtPath(BatSheetDirectory + sheetName);
        var sprites = System.Array.FindAll(assets, asset => asset is Sprite)
            .Cast<Sprite>()
            .OrderBy(sprite => sprite.rect.x)
            .ToArray();

        Assert.AreEqual(7, sprites.Length, $"{state} sheet must import exactly seven sprites.");
        for (var index = 0; index < sprites.Length; index++)
        {
            Assert.AreEqual(new Rect(index * 64f, 0f, 64f, 64f), sprites[index].rect);
        }

        AssertSpriteSheetImportSettings(BatSheetDirectory + sheetName, SpriteImportMode.Multiple);
    }

    [Test]
    public void BatMachineProjectileV2_RemainsA32By32LegacySpriteAt64PixelsPerUnit()
    {
        var importer = AssetImporter.GetAtPath(ProjectileSpritePath) as TextureImporter;
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ProjectileSpritePath);

        Assert.NotNull(importer, "The existing static projectile v2 asset must have a TextureImporter.");
        Assert.NotNull(sprite, "The existing static projectile v2 asset must remain a single sprite.");
        Assert.AreEqual(32, sprite.texture.width);
        Assert.AreEqual(32, sprite.texture.height);
        Assert.AreEqual(64f, sprite.pixelsPerUnit);
        Assert.AreEqual(SpriteImportMode.Single, importer.spriteImportMode);
        Assert.AreEqual(32, importer.maxTextureSize);
        Assert.AreEqual(FilterMode.Point, importer.filterMode);
        Assert.AreEqual(TextureImporterCompression.Uncompressed, importer.textureCompression);
        Assert.False(importer.mipmapEnabled);
    }

    [Test]
    public void BatMachineSetup_RestoresLegacyProjectileV2ImportSettings()
    {
        var importer = AssetImporter.GetAtPath(ProjectileSpritePath) as TextureImporter;
        Assert.NotNull(importer, "The existing static projectile v2 asset must have a TextureImporter.");
        var originalPixelsPerUnit = importer.spritePixelsPerUnit;
        var originalFilterMode = importer.filterMode;
        var originalCompression = importer.textureCompression;
        var originalMipmapEnabled = importer.mipmapEnabled;
        try
        {
            importer.spritePixelsPerUnit = 32f;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Compressed;
            importer.mipmapEnabled = true;
            importer.SaveAndReimport();

            RunBatMachineSetup();

            importer = AssetImporter.GetAtPath(ProjectileSpritePath) as TextureImporter;
            Assert.AreEqual(64f, importer.spritePixelsPerUnit);
            Assert.AreEqual(FilterMode.Point, importer.filterMode);
            Assert.AreEqual(TextureImporterCompression.Uncompressed, importer.textureCompression);
            Assert.False(importer.mipmapEnabled);
        }
        finally
        {
            importer = AssetImporter.GetAtPath(ProjectileSpritePath) as TextureImporter;
            importer.spritePixelsPerUnit = originalPixelsPerUnit;
            importer.filterMode = originalFilterMode;
            importer.textureCompression = originalCompression;
            importer.mipmapEnabled = originalMipmapEnabled;
            importer.SaveAndReimport();
        }
    }

    [Test]
    public void BatMachineSetup_WhenRunTwice_PreservesSpriteIdentitiesAndDoesNotDuplicateVisualComponents()
    {
        RunBatMachineSetup();
        var firstBatSpriteIdentifiers = GetSpriteIdentifiers(BatSheetDirectory + "BatMachine_Engage.png");
        var firstProjectileSpriteIdentifiers = GetSpriteIdentifiers(ProjectileSpinPath);
        RunBatMachineSetup();

        CollectionAssert.AreEqual(firstBatSpriteIdentifiers, GetSpriteIdentifiers(BatSheetDirectory + "BatMachine_Engage.png"));
        CollectionAssert.AreEqual(firstProjectileSpriteIdentifiers, GetSpriteIdentifiers(ProjectileSpinPath));
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/BatMachine.prefab");
        Assert.AreEqual(1, prefab.transform.Find("VisualRoot").GetComponents<BatMachineVisualController>().Length);
        Assert.AreEqual(1, prefab.transform.Find("ProjectileTemplate").GetComponents<ProjectileSpinVisual>().Length);
    }

    private static void AssertSpriteSheetImportSettings(string path, SpriteImportMode importMode)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;

        Assert.NotNull(importer, $"{path} must have a TextureImporter.");
        Assert.AreEqual(TextureImporterType.Sprite, importer.textureType);
        Assert.AreEqual(importMode, importer.spriteImportMode);
        Assert.AreEqual(64f, importer.spritePixelsPerUnit);
        Assert.AreEqual(FilterMode.Point, importer.filterMode);
        Assert.AreEqual(TextureImporterCompression.Uncompressed, importer.textureCompression);
        Assert.False(importer.mipmapEnabled);
    }

    private static string[] GetSpriteIdentifiers(string path)
    {
        return AssetDatabase.LoadAllAssetsAtPath(path)
            .OfType<Sprite>()
            .OrderBy(sprite => sprite.rect.x)
            .Select(sprite =>
            {
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(sprite, out var guid, out long localIdentifier);
                return $"{guid}:{localIdentifier}";
            })
            .ToArray();
    }

    private static void RunBatMachineSetup()
    {
        var setupType = System.Type.GetType(
            "TicGame.Architecture.EditorTools.BatMachinePrefabSetup, TicGame.Architecture.Editor");
        Assert.NotNull(setupType, "The Bat Machine setup assembly must be loaded for this integration test.");
        var setupMethod = setupType.GetMethod("CreateOrUpdateBatMachine");
        Assert.NotNull(setupMethod, "The Bat Machine setup command must be callable.");
        setupMethod.Invoke(null, null);
    }
}
