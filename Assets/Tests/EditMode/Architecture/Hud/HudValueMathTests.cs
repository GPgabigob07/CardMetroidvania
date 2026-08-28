using NUnit.Framework;

namespace TicGame.Architecture.Tests
{
    public sealed class HudValueMathTests
    {
        [TestCase(5f, 5, 5)]
        [TestCase(4f, 5, 4)]
        [TestCase(0f, 5, 0)]
        [TestCase(10f, 5, 5)]
        public void HealthPips_ClampToAuthoredCapacity(
            float current,
            int capacity,
            int expected)
        {
            Assert.AreEqual(
                expected,
                HudValueMath.GetHealthPipCount(current, capacity));
        }

        [TestCase(30f, 100f, 30, 9)]
        [TestCase(100f, 100f, 30, 30)]
        [TestCase(0f, 100f, 30, 0)]
        [TestCase(1f, 100f, 30, 1)]
        public void EnergySegments_MapNormalizedValueToFixedBar(
            float current,
            float maximum,
            int segmentCount,
            int expected)
        {
            Assert.AreEqual(
                expected,
                HudValueMath.GetFilledSegmentCount(
                    current,
                    maximum,
                    segmentCount));
        }

        [TestCase(PlayerCardTimeState.None, 0)]
        [TestCase(PlayerCardTimeState.Neutral, 1)]
        [TestCase(PlayerCardTimeState.Chain, 2)]
        [TestCase(PlayerCardTimeState.Finisher, 3)]
        public void CardCount_MatchesComboTier(
            PlayerCardTimeState state,
            int expected)
        {
            Assert.AreEqual(expected, HudValueMath.GetCardCount(state));
        }
    }
}
