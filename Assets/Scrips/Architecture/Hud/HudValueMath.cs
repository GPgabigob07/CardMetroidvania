using System;
using UnityEngine;

namespace TicGame.Architecture
{
    public static class HudValueMath
    {
        public static int GetHealthPipCount(float current, int capacity)
        {
            return Mathf.Clamp(
                value: Mathf.CeilToInt(Mathf.Max(0f, current)),
                min: 0,
                max: Mathf.Max(0, capacity));
        }

        public static int GetFilledSegmentCount(
            float current,
            float maximum,
            int segmentCount)
        {
            if (maximum <= 0f || segmentCount <= 0)
            {
                return 0;
            }

            return Mathf.Clamp(
                value: Mathf.CeilToInt(
                    Mathf.Clamp01(current / maximum) * segmentCount),
                min: 0,
                max: segmentCount);
        }

        public static string FormatWholeResource(float current)
        {
            return Math.Max(0, Mathf.FloorToInt(current)).ToString();
        }

        public static int GetCardCount(PlayerCardTimeState state)
        {
            return state switch
            {
                PlayerCardTimeState.Neutral => 1,
                PlayerCardTimeState.Chain => 2,
                PlayerCardTimeState.Finisher => 3,
                _ => 0
            };
        }
    }
}
