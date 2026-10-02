using System;
using System.Linq;
using Godot;

namespace DevAncientNaval.Presentation.Map;

internal readonly record struct TownHouse(Vector2 Position, float Height, float Width, int Style)
{
    internal Rect2 GroundBounds => new(Position + new Vector2(-Width, 0), new Vector2(Width * 2 + 3, 4));
}

/// <summary>A small staggered isometric street lattice. Jitter changes the lane
/// shape, never the spacing guarantee or the centre reserved for the monument.</summary>
internal static class TownLayout
{
    internal static TownHouse[] Build(int seed)
    {
        var random = new Random(seed);
        // Twelve-pixel streets keep every roof and door distinct. Staggered
        // rows gather the town behind a small, genuinely open shrine plaza.
        var sites = new[]
        {
            new Vector2(0, -28),
            new Vector2(-12, -22), new Vector2(0, -22), new Vector2(12, -22),
            new Vector2(-24, -16), new Vector2(-12, -16), new Vector2(0, -16),
            new Vector2(12, -16), new Vector2(24, -16),
            new Vector2(-24, -10), new Vector2(-12, -10),
            new Vector2(12, -10), new Vector2(24, -10)
        };
        return sites
            .OrderBy(p => p.Y).ThenBy(p => p.X)
            .Select((p, i) => new TownHouse(p + new Vector2((float)(random.NextDouble() - .5) * .7f,
                (float)(random.NextDouble() - .5) * .45f), 8 + random.Next(5), 3 + (float)random.NextDouble(), i % 5))
            .OrderBy(h => h.Position.Y).ThenBy(h => h.Position.X)
            .ToArray();
    }
}
