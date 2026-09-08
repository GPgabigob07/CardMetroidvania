using NUnit.Framework;
using TicGame.Architecture;
using UnityEditor;
using UnityEngine;

public sealed class BatMachinePrefabTests
{
    private const string ProjectileSpritePath = "Assets/Art/Enemies/BatMachine/BatMachineProjectile-v2.png";

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
    public void BatMachinePrefab_ProjectileTemplateUsesAuthoredProjectileSprite()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/BatMachine.prefab");
        var expectedSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ProjectileSpritePath);

        Assert.NotNull(prefab, "The authored Bat Machine prefab must exist.");
        Assert.NotNull(expectedSprite, "The authored projectile sprite must be imported as a Sprite.");
        var template = prefab.transform.Find("ProjectileTemplate");
        Assert.NotNull(template, "The projectile template child must exist.");
        var renderer = template.GetComponent<SpriteRenderer>();
        Assert.NotNull(renderer, "The projectile template must render its launched projectile.");
        Assert.AreEqual(expectedSprite, renderer.sprite);
        Assert.LessOrEqual(expectedSprite.texture.width, 32);
        Assert.LessOrEqual(expectedSprite.texture.height, 32);
        Assert.AreEqual(64f, expectedSprite.pixelsPerUnit);
        Assert.LessOrEqual(expectedSprite.bounds.size.x, 0.5f);
        Assert.LessOrEqual(expectedSprite.bounds.size.y, 0.5f);
    }

    [Test]
    public void BatMachineProjectileV2_HasNativeSourceDimensionsOf32By32()
    {
        var importer = AssetImporter.GetAtPath(ProjectileSpritePath) as TextureImporter;

        Assert.NotNull(importer, "The authored projectile image must have a TextureImporter.");
        importer.GetSourceTextureWidthAndHeight(out var sourceWidth, out var sourceHeight);
        Assert.AreEqual(32, sourceWidth);
        Assert.AreEqual(32, sourceHeight);
    }
}
