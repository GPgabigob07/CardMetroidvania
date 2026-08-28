using System.Collections.Generic;
using NUnit.Framework;

namespace TicGame.Architecture.Tests
{
    public sealed class GameplayTimeModifierResolverTests
    {
        [Test]
        public void Resolve_NoModifiers_ReturnsBaseline()
        {
            var result = GameplayTimeModifierResolver.Resolve(
                baselineScale: 1f,
                modifiers: new List<GameplayTimeModifier>());

            Assert.AreEqual(1f, result);
        }

        [Test]
        public void Resolve_CardTimeAndHitStop_ReturnsHitStop()
        {
            var result = GameplayTimeModifierResolver.Resolve(
                baselineScale: 1f,
                modifiers: new[]
                {
                    new GameplayTimeModifier(
                        GameplayTimeModifierKind.CardTime,
                        requestedScale: 0.1f),
                    new GameplayTimeModifier(
                        GameplayTimeModifierKind.HitStop,
                        requestedScale: 0f)
                });

            Assert.AreEqual(0f, result);
        }

        [Test]
        public void Resolve_RemainingCardTime_ReturnsCardTimeScale()
        {
            var result = GameplayTimeModifierResolver.Resolve(
                baselineScale: 1f,
                modifiers: new[]
                {
                    new GameplayTimeModifier(
                        GameplayTimeModifierKind.CardTime,
                        requestedScale: 0.1f)
                });

            Assert.AreEqual(0.1f, result);
        }
    }
}
