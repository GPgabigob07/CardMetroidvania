using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TicGame.Architecture.Tests
{
    public sealed class ProjectileSpinVisualTests
    {
        private readonly List<Object> objectsToDestroy = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (var instance in objectsToDestroy)
            {
                Object.DestroyImmediate(instance);
            }

            objectsToDestroy.Clear();
        }

        [Test]
        public void Tick_NegativeProjectileDirection_FlipsRendererWithoutChangingDirection()
        {
            var rig = CreateLaunchedProjectileRig(Vector2.left);

            rig.Visual.Tick(0.2f);

            Assert.True(rig.Renderer.flipX);
            Assert.AreEqual(Vector2.left, rig.Projectile.Direction);
        }

        [Test]
        public void Tick_PositiveProjectileDirection_UnflipsRendererWithoutChangingDirection()
        {
            var rig = CreateLaunchedProjectileRig(Vector2.right);
            rig.Renderer.flipX = true;

            rig.Visual.Tick(0.2f);

            Assert.False(rig.Renderer.flipX);
            Assert.AreEqual(Vector2.right, rig.Projectile.Direction);
        }

        [Test]
        public void Tick_LoopsAcrossFiveFrames()
        {
            var rig = CreateLaunchedProjectileRig(Vector2.right);

            rig.Visual.Tick(1.1f);

            Assert.AreSame(rig.Frames[0], rig.Renderer.sprite);
        }

        [Test]
        public void Tick_DoesNotChangeProjectileBodyVelocity()
        {
            var rig = CreateLaunchedProjectileRig(Vector2.left);
            var expectedVelocity = new Vector2(2f, -3f);
            rig.Body.linearVelocity = expectedVelocity;

            rig.Visual.Tick(0.2f);

            Assert.AreEqual(expectedVelocity, rig.Body.linearVelocity);
        }

        [Test]
        public void Tick_InvalidFiveFrameBinding_PreservesSpriteAndLogsOneError()
        {
            var rig = CreateLaunchedProjectileRig(Vector2.right);
            var existingSprite = rig.Frames[0];
            rig.Renderer.sprite = existingSprite;
            rig.Frames[3] = null;
            LogAssert.Expect(LogType.Error, new Regex("Projectile spin visual requires a projectile, renderer, and exactly 5 non-null frames\\."));

            rig.Visual.Tick(0.2f);
            rig.Visual.Tick(0.2f);

            Assert.AreSame(existingSprite, rig.Renderer.sprite);
        }

        [Test]
        public void Tick_MissingProjectileDependency_PreservesSpriteAndLogsOneError()
        {
            var rig = CreateLaunchedProjectileRig(Vector2.right);
            var existingSprite = rig.Frames[0];
            rig.Renderer.sprite = existingSprite;
            rig.Visual.Configure(null, rig.Renderer, rig.Frames);
            LogAssert.Expect(LogType.Error, new Regex("Projectile spin visual requires a projectile, renderer, and exactly 5 non-null frames\\."));

            rig.Visual.Tick(0.2f);
            rig.Visual.Tick(0.2f);

            Assert.AreSame(existingSprite, rig.Renderer.sprite);
        }

        private ProjectileSpinRig CreateLaunchedProjectileRig(Vector2 direction)
        {
            var projectileObject = CreateObject("Projectile");
            var body = projectileObject.AddComponent<Rigidbody2D>();
            var projectile = projectileObject.AddComponent<EnemyProjectile2D>();
            var renderer = projectileObject.AddComponent<SpriteRenderer>();
            var visual = projectileObject.AddComponent<ProjectileSpinVisual>();
            var frames = CreateFrames();
            visual.Configure(projectile, renderer, frames);
            projectile.Launch(direction, null, 4f);

            return new ProjectileSpinRig(body, projectile, renderer, visual, frames);
        }

        private Sprite[] CreateFrames()
        {
            var frames = new Sprite[5];
            for (var index = 0; index < frames.Length; index++)
            {
                var texture = new Texture2D(1, 1);
                var sprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f));
                frames[index] = sprite;
                objectsToDestroy.Add(sprite);
                objectsToDestroy.Add(texture);
            }

            return frames;
        }

        private GameObject CreateObject(string name)
        {
            var instance = new GameObject(name);
            objectsToDestroy.Add(instance);
            return instance;
        }

        private sealed class ProjectileSpinRig
        {
            public ProjectileSpinRig(
                Rigidbody2D body,
                EnemyProjectile2D projectile,
                SpriteRenderer renderer,
                ProjectileSpinVisual visual,
                Sprite[] frames)
            {
                Body = body;
                Projectile = projectile;
                Renderer = renderer;
                Visual = visual;
                Frames = frames;
            }

            public Rigidbody2D Body { get; }
            public EnemyProjectile2D Projectile { get; }
            public SpriteRenderer Renderer { get; }
            public ProjectileSpinVisual Visual { get; }
            public Sprite[] Frames { get; }
        }
    }
}
