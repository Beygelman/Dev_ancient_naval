using System;
using System.Collections.Generic;
using System.Linq;
using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Units;
using Godot;
using Side = DevAncientNaval.Core.Units.Side;

namespace DevAncientNaval.Presentation.Map;
public partial class BoardView
{
    private (IsometricProjection Projection, int Ship, long Vision, bool Mortar)? _coverageContext;
    private readonly Dictionary<GridPosition, Coverage> _coverage = new();
    private sealed record Coverage(Vector2[][] Guns, Vector2[][] Mortar, Vector2[][] Collection, Vector2[][] MortarTiles);
    private void DrawProjectedCoverage()
    {
        if (Building || SelectedShipId is not { } id || Battle.FindObserved(Side.Player, id)is not { Owner: Side.Player } ship)
            return;
        var context = (Projection, id, Battle.Vision.Revision, ship.HasMortar);
        if (_coverageContext != context)
        {
            _coverageContext = context;
            _coverage.Clear();
        }

        bool forecast = PreviewPath.Count > 1;
        if (!forecast && !ship.HasMortar)
            return;
        var origin = forecast ? PreviewPath[^1] : ship.Position;
        if (!_coverage.TryGetValue(origin, out var coverage))
        {
            if (_coverage.Count >= 96)
                _coverage.Clear();
            var known = Board.Tiles.Where(t => Battle.Vision.IsExplored(Side.Player, t.Position)).Select(t => t.Position).ToArray();
            var guns = ship.Definition.Class != ShipClass.Togus && ship.IsArmed ? known.Where(p => Board.InRadius(origin, p, ship.Definition.AttackRange)).ToArray() : Array.Empty<GridPosition>();
            var mortar = ship.HasMortar ? known.Where(p => Board.InRadius(origin, p, ship.MortarRange) && (ship.Definition.Class == ShipClass.AncientGun || !Board.InRadius(origin, p, 3))).ToArray() : Array.Empty<GridPosition>();
            var collection = ship.Definition.CollectionRange > 0 ? known.Where(p => Board.InRadius(origin, p, Battle.Rules.Economy.AdjacentCollectionOnly ? 1 : ship.Definition.CollectionRange)).ToArray() : Array.Empty<GridPosition>();
            coverage = new(Projection.BoundaryEdges(guns).ToArray(), Projection.BoundaryEdges(mortar).ToArray(), Projection.BoundaryEdges(collection).ToArray(), mortar.Select(Projection.Diamond).ToArray());
            _coverage[origin] = coverage;
        }

        foreach (var tile in coverage.MortarTiles)
            DrawColoredPolygon(tile, new Color(.85f, .42f, .29f, .055f));
        foreach (var edge in coverage.Mortar)
            DrawPolyline(edge, new Color(.96f, .61f, .35f, .65f), 1.6f, true);
        if (!forecast)
            return;
        foreach (var edge in coverage.Guns)
            DrawPolyline(edge, new Color(.83f, .42f, .48f, .60f), 1.5f, true);
        foreach (var edge in coverage.Collection)
            DrawPolyline(edge, new Color(.92f, .84f, .51f, .70f), 1.2f, true);
    }

    private void DrawTreasureRoute()
    {
        if (PreviewPath.Count < 2)
            return;
        Vector2 P(int i) => Projection.GridToWorld(PreviewPath[Math.Clamp(i, 0, PreviewPath.Count - 1)]);
        var previous = P(0);
        float distance = 0;
        var color = new Color("d19ba7");
        for (int segment = 0; segment < PreviewPath.Count - 1; segment++)
            for (int sample = 1; sample <= 18; sample++)
            {
                float t = sample / 18f;
                var point = P(segment).CubicInterpolate(P(segment + 1), P(segment - 1), P(segment + 2), t);
                var cell = Projection.WorldToGrid(point);
                if (!Board.Contains(cell) || Battle.Vision.KnownTerrain(Side.Player, cell) == Core.World.TerrainType.Land)
                    point = P(segment).Lerp(P(segment + 1), t);
                var direction = (P(segment + 1) - P(segment)).Normalized();
                point += direction.Orthogonal() * MathF.Sin(t * Mathf.Tau) * 2.2f;
                float length = previous.DistanceTo(point);
                float used = 0;
                while (used < length)
                {
                    float phase = Mathf.PosMod(distance + used, 12);
                    float span = Math.Min(length - used, (phase < 7 ? 7 : 12) - phase);
                    span = Math.Max(.001f, span);
                    if (phase < 7 && length > .001f)
                        DrawLine(previous.Lerp(point, used / length), previous.Lerp(point, Math.Min(1, (used + span) / length)), color, 2.1f, true);
                    used += span;
                }

                distance += length;
                previous = point;
            }

        var destination = P(PreviewPath.Count - 1);
        DrawLine(destination + new Vector2(-8, -5), destination + new Vector2(8, 5), color, 2.8f, true);
        DrawLine(destination + new Vector2(-8, 5), destination + new Vector2(8, -5), color, 2.8f, true);
    }
}
