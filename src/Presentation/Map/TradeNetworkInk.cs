using System;
using System.Collections.Generic;
using System.Linq;
using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Navigation;
using DevAncientNaval.Core.Units;
using Godot;
using Side = DevAncientNaval.Core.Units.Side;

namespace DevAncientNaval.Presentation.Map;
public partial class BoardView
{
    private readonly List<(GridPosition From, GridPosition To)> _renderedTradeEdges = new();
    internal IReadOnlyList<(GridPosition From, GridPosition To)> RenderedTradeEdges => _renderedTradeEdges;
    internal int TradeDashSegmentCount { get; private set; }
    private TradeNetwork? _tradeNetwork;
    private IsometricProjection? _tradeProjection;
    private long _tradeVision = -1;

    private void BuildTradeInk(TradeNetwork network)
    {
        _tradeInk.Clear();
        _renderedTradeEdges.Clear();
        TradeDashSegmentCount = 0;
        if (network.IsEmpty) return;
        var color = new Color(1, .98f, .92f, .52f);
        void Stroke(IReadOnlyList<Vector2> points, bool shore)
        {
            Vector2 P(int i) => points[Math.Clamp(i, 0, points.Count - 1)];
            float distance = 0;
            for (int segment = 0; segment < points.Count - 1; segment++)
            {
                var previous = P(segment);
                for (int sample = 1; sample <= 18; sample++)
                {
                    float t = sample / 18f;
                    var point = P(segment).CubicInterpolate(P(segment + 1), P(segment - 1), P(segment + 2), t);
                    var cell = Projection.WorldToGrid(point);
                    if (!shore && (!Board.Contains(cell) || Battle.Vision.KnownTerrain(Side.Player, cell) == Core.World.TerrainType.Land))
                        point = P(segment).Lerp(P(segment + 1), t);
                    if (!shore)
                    {
                        var ray = (P(segment + 1) - P(segment)).Normalized();
                        var bend = point + ray.Orthogonal() * MathF.Sin(t * Mathf.Tau) * 2.2f;
                        var at = Projection.WorldToGrid(bend);
                        if (Board.Contains(at) && Battle.Vision.KnownTerrain(Side.Player, at) is Core.World.TerrainType.Water or Core.World.TerrainType.Coast)
                            point = bend;
                    }
                    float length = previous.DistanceTo(point);
                    var current = Projection.WorldToGrid((previous + point) * .5f);
                    bool known = Board.Contains(current) && Battle.Vision.IsExplored(Side.Player, current);
                    // Emit each dash interval once along the merged topology.
                    for (float at = 0; known && at < length - .0001f;)
                    {
                        float phase = (distance + at) % 12;
                        float end = Math.Min(length, at + (phase < 7 ? 7 - phase : 12 - phase));
                        if (phase < 7 && end - at > .001f)
                        {
                            _tradeInk.Line(previous.Lerp(point, at / length), previous.Lerp(point, end / length), color);
                            TradeDashSegmentCount++;
                        }
                        at = Math.Max(end, at + .001f);
                    }
                    distance += length;
                    previous = point;
                }
            }
        }
        foreach (var route in network.RenderRoutes)
        {
            if (route.Count < 2) continue;
            for (int i = 1; i < route.Count; i++) _renderedTradeEdges.Add((route[i - 1], route[i]));
            Stroke(route.Select(Projection.GridToWorld).ToArray(), false);
        }
        // Chain branches can share an endpoint; draw its connector once.
        var linkedCells = network.Edges.SelectMany(e => new[] { e.From, e.To }).ToHashSet();
        foreach (var port in Battle.Villages.Where(v => v.Owner == Side.Player && v.HasPort && v.Health > 0 && linkedCells.Contains(Battle.PortBerth(v))))
            Stroke(new[] { Projection.GridToWorld(Battle.PortBerth(port)), PortAnchor(port) }, true);
        foreach (var light in Battle.Ships.Where(s => s.Owner == Side.Player && s.Health > 0 && s.Definition.Class == ShipClass.Lighthouse && linkedCells.Contains(s.Position)))
            Stroke(new[] { Projection.GridToWorld(light.Position), Projection.GridToWorld(light.Position) + FleetView.LighthouseOffset }, true);
    }
}
