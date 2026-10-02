using System;
using System.Linq;
using DevAncientNaval.Core.Units;
using DevAncientNaval.Core.Grid;
using Godot;
using Side = DevAncientNaval.Core.Units.Side;

namespace DevAncientNaval.Presentation.Map;
public partial class WorldAmbience
{
    private sealed record FishSchool(GridPosition Cell, Vector2 Center, Vector2[] Water, float Radius, bool Reef);
    private readonly SeaGeometryBatch _fishBatch = new();
    private FishSchool[] _schools = Array.Empty<FishSchool>();
    private static readonly Vector2[] FishBody =
    {
        new(-4, 0),
        new(-1, -2),
        new(3, -1.5f),
        new(5, 0),
        new(3, 1.5f),
        new(-1, 2)
    };
    private static readonly Vector2[] FishTail =
    {
        new(-4, 0),
        new(-7, -2),
        new(-7, 2)
    };
    private static readonly Vector2[] ReefStone =
    {
        new(-7, 1),
        new(-4, -4),
        new(2, -6),
        new(7, -1),
        new(5, 4),
        new(-3, 4)
    };
    internal int AnimatedFishCount => _schools.Sum(s => s.Reef ? 14 : 3);

    private void RefreshFishSchools()
    {
        var battle = BoardView.Battle;
        var reefs = battle.KnownShoals(Side.Player).ToHashSet();
        reefs.UnionWith(battle.ObservedShips(Side.Player).Where(s => s.Definition.Class == ShipClass.FishingDock).Select(s => s.Position));
        _schools = battle.KnownFish(Side.Player).Concat(reefs).Distinct().Select(cell =>
        {
            var center = BoardView.Projection.GridToWorld(cell);
            var water = BoardView.Projection.Diamond(cell);
            float radius = Math.Clamp(water.Min(p => p.DistanceTo(center)) * .62f, 10, 24);
            return new FishSchool(cell, center, water, radius, reefs.Contains(cell));
        }).ToArray();
    }

    private void DrawFishSchools()
    {
        _fishBatch.Clear();
        foreach (var school in _schools)
        {
            if (!_drawBounds.HasPoint(school.Center))
                continue;
            if (school.Reef)
            {
                for (int stone = 0; stone < 6; stone++)
                {
                    float angle = stone * 2.4f + school.Cell.X;
                    var at = school.Center + new Vector2(MathF.Cos(angle) * 10, MathF.Sin(angle) * 5);
                    var stoneTransform = new Transform2D(angle, new Vector2(.8f + stone % 3 * .3f, .52f), 0, at);
                    _fishBatch.Polygon(ReefStone, new Color(.35f, .65f, .56f, .32f), stoneTransform);
                    // Branching submerged coral, deliberately geometric and readable.
                    var stem = at + new Vector2(0, -4);
                    _fishBatch.Line(at, stem + new Vector2(0, -5), new Color(.84f, .53f, .37f, .48f));
                    _fishBatch.Line(stem, stem + new Vector2(-4, -3), new Color(.84f, .53f, .37f, .48f));
                    _fishBatch.Line(stem, stem + new Vector2(4, -3), new Color(.84f, .53f, .37f, .48f));
                }
            }

            int count = school.Reef ? 14 : 3;
            for (int fish = 0; fish < count; fish++)
            {
                float phase = _time * (.35f + fish % 3 * .06f) + fish * 2.399f + school.Cell.X * .71f + school.Cell.Y;
                float radius = school.Radius * (.35f + (fish % 5) * .11f);
                var position = school.Center + new Vector2(MathF.Cos(phase) * radius, MathF.Sin(phase) * radius * .58f);
                var tangent = new Vector2(-MathF.Sin(phase), MathF.Cos(phase) * .58f);
                float size = school.Reef ? .58f + fish % 3 * .12f : .9f;
                var fishTransform = new Transform2D(tangent.Angle(), new Vector2(size, size * .75f), 0, position);
                _fishBatch.Polygon(FishBody, school.Reef ? new Color(.92f, .77f, .42f, .67f) : new Color(.72f, .85f, .73f, .75f), fishTransform);
                _fishBatch.Polygon(FishTail, new Color(.67f, .80f, .68f, .64f), fishTransform);
                _fishBatch.Line(fishTransform * new Vector2(2.7f, -.3f), fishTransform * new Vector2(3.3f, -.3f), new Color(.1f, .3f, .36f, .65f));
            }

            // One thin water wash above the silhouettes puts the school below the surface.
            _fishBatch.Polygon(school.Water, new Color(.17f, .40f, .49f, .13f), Transform2D.Identity);
            float shimmer = MathF.Sin(_time * .7f + school.Cell.X) * 3;
            _fishBatch.Line(school.Center + new Vector2(-school.Radius, -5 + shimmer), school.Center + new Vector2(-3, -7 + shimmer), new Color(.69f, .88f, .87f, .18f));
        }
        _fishBatch.Submit(this, 1.2f);
    }
}
