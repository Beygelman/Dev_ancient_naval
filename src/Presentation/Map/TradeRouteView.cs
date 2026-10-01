using System;
using System.Linq;
using Godot;
using Side = DevAncientNaval.Core.Units.Side;

namespace DevAncientNaval.Presentation.Map;
public partial class BoardView
{
    private readonly SeaGeometryBatch _tradeInk = new();
    private void DrawTradeRoutes(Node2D canvas, Rect2 bounds)
    {
        _tradeInk.Clear();
        var color = new Color(1, .98f, .92f, .52f);
        var ports = Battle.Villages.Where(v => v.Owner == Side.Player && v.HasPort)
            .GroupBy(v => Battle.PortBerth(v)).ToDictionary(g => g.Key, g => PortAnchor(g.First()));
        foreach (var route in Battle.TradeRoutes(Side.Player).Routes)
        {
            if (route.Count < 2) continue;
            var points = route.Select(Projection.GridToWorld).ToList();
            if (ports.TryGetValue(route[0], out var start)) points.Insert(0, start);
            if (ports.TryGetValue(route[^1], out var end)) points.Add(end);
            Vector2 P(int i) => points[Math.Clamp(i, 0, points.Count-1)];
            float distance = 0;
            for (int segment = 0; segment < points.Count-1; segment++)
            {
                var previous = P(segment);
                // Only owned port links may extend halfway onto their known shore.
                bool shore = segment == 0 && ports.ContainsKey(route[0]) || segment == points.Count-2 && ports.ContainsKey(route[^1]);
                for (int sample = 1; sample <= 18; sample++)
                {
                    float t = sample / 18f;
                    var point = P(segment).CubicInterpolate(P(segment+1),P(segment-1),P(segment+2),t);
                    var cell = Projection.WorldToGrid(point);
                    if (!shore && (!Board.Contains(cell) || Battle.Vision.KnownTerrain(Side.Player,cell) == Core.World.TerrainType.Land))
                        point = P(segment).Lerp(P(segment+1),t);
                    if (!shore)
                    {
                        var ray = (P(segment+1)-P(segment)).Normalized();
                        var bend = point + ray.Orthogonal() * MathF.Sin(t*Mathf.Tau)*2.2f;
                        var at = Projection.WorldToGrid(bend);
                        if (Board.Contains(at) && Battle.Vision.KnownTerrain(Side.Player,at) is Core.World.TerrainType.Water or Core.World.TerrainType.Coast)
                            point = bend;
                    }
                    float length = previous.DistanceTo(point);
                    var current = Projection.WorldToGrid(point);
                    bool known = Board.Contains(current) && Battle.Vision.IsExplored(Side.Player,current);
                    for (float at = 0; known && at < length; at += 1.5f)
                        if ((distance+at)%12 < 7)
                            _tradeInk.Line(previous.Lerp(point,at/length),previous.Lerp(point,Math.Min(length,at+1.5f)/length),color);
                    distance += length;
                    previous = point;
                }
            }
        }
        _tradeInk.Submit(canvas, 1.7f);
    }
}
