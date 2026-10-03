using UnityEngine;

namespace TicGame.Architecture
{
    public static class BeamSegmentGeometry
    {
        public static bool TryIntersectVerticalGuard(Vector2 start, Vector2 end, Vector2 center, float height,
            float radius, out float fraction)
        {
            var low = center - Vector2.up * (height / 2);
            var high = center + Vector2.up * (height / 2);
            fraction = float.PositiveInfinity;
            IncludeBox(start, end, new Vector2(center.x - radius, low.y), new Vector2(center.x + radius, high.y), ref fraction);
            IncludeCircle(start, end, low, radius, ref fraction);
            IncludeCircle(start, end, high, radius, ref fraction);
            return float.IsFinite(fraction);
        }

        public static bool TryIntersectBounds(Vector2 start, Vector2 end, Bounds bounds, float radius, out float fraction)
        {
            var min = (Vector2)bounds.min; var max = (Vector2)bounds.max;
            fraction = float.PositiveInfinity;
            IncludeBox(start, end, min - new Vector2(radius, 0), max + new Vector2(radius, 0), ref fraction);
            IncludeBox(start, end, min - new Vector2(0, radius), max + new Vector2(0, radius), ref fraction);
            foreach (var corner in new[] { min, max, new Vector2(min.x, max.y), new Vector2(max.x, min.y) })
                IncludeCircle(start, end, corner, radius, ref fraction);
            return float.IsFinite(fraction);
        }

        private static void IncludeBox(Vector2 start, Vector2 end, Vector2 min, Vector2 max, ref float earliest)
        {
            var direction = end - start;
            var entry = 0f; var exit = 1f;
            for (var axis = 0; axis < 2; axis++)
            {
                if (direction[axis] == 0)
                { if (start[axis] < min[axis] || start[axis] > max[axis]) return; continue; }
                var first = (min[axis] - start[axis]) / direction[axis];
                var second = (max[axis] - start[axis]) / direction[axis];
                entry = Mathf.Max(entry, Mathf.Min(first, second));
                exit = Mathf.Min(exit, Mathf.Max(first, second));
                if (entry > exit) return;
            }
            earliest = Mathf.Min(earliest, entry);
        }

        private static void IncludeCircle(Vector2 start, Vector2 end, Vector2 center, float radius, ref float earliest)
        {
            var offset = start - center;
            var c = offset.sqrMagnitude - radius * radius;
            if (c <= 0) { earliest = 0; return; }
            var delta = end - start;
            var a = delta.sqrMagnitude;
            if (a == 0) return;
            var b = Vector2.Dot(offset, delta);
            var discriminant = (double)b * b - (double)a * c;
            if (discriminant < 0) return;
            var entry = (float)((-b - System.Math.Sqrt(discriminant)) / a);
            if (entry >= 0 && entry <= 1) earliest = Mathf.Min(earliest, entry);
        }
    }
}
