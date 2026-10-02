using System;
using System.Collections.Generic;
using Godot;

namespace DevAncientNaval.Presentation.Map;

/// <summary>Subtract a convex beach quad by partitioning half planes. The result
/// consists of disjoint convex pieces, never a polygon-with-holes flattened into fills.</summary>
internal static class ConvexSoilClip
{
    internal static IEnumerable<Vector2[]> Subtract(Vector2[] subject, Vector2[] beach, float clearance = 0)
    {
        var remaining = subject;
        for (int edge = 0; edge < beach.Length && remaining.Length >= 3; edge++)
        {
            var a = beach[edge];
            var b = beach[(edge + 1) % beach.Length];
            var outside = HalfPlane(remaining, a, b, false, clearance);
            if (outside.Length >= 3) yield return outside;
            remaining = HalfPlane(remaining, a, b, true, clearance);
        }
        // The final remainder is inside every beach half plane and is removed.
    }

    private static Vector2[] HalfPlane(Vector2[] subject, Vector2 a, Vector2 b, bool inside, float clearance)
    {
        var output = new List<Vector2>(subject.Length + 1);
        var previous = subject[^1];
        float margin = clearance * a.DistanceTo(b);
        float previousDistance = (b - a).Cross(previous - a) + margin;
        bool previousKept = inside ? previousDistance >= 0 : previousDistance <= 0;
        foreach (var current in subject)
        {
            float distance = (b - a).Cross(current - a) + margin;
            bool kept = inside ? distance >= 0 : distance <= 0;
            if (kept != previousKept)
            {
                float divisor = previousDistance - distance;
                if (MathF.Abs(divisor) > .000001f)
                    Add(previous.Lerp(current, Math.Clamp(previousDistance / divisor, 0, 1)));
            }
            if (kept) Add(current);
            previous = current;
            previousDistance = distance;
            previousKept = kept;
        }
        if (output.Count > 1 && output[0].DistanceSquaredTo(output[^1]) < .000001f) output.RemoveAt(output.Count - 1);
        return output.Count < 3 ? Array.Empty<Vector2>() : output.ToArray();
        void Add(Vector2 p)
        {
            if (output.Count == 0 || output[^1].DistanceSquaredTo(p) >= .000001f) output.Add(p);
        }
    }
}
