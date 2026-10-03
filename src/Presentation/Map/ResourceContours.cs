using System;
using System.Linq;
using DevAncientNaval.Core.Grid;
using Godot;

namespace DevAncientNaval.Presentation.Map;

public partial class BoardView
{
    internal Vector2[] ResourceContour(GridPosition cell, bool reef)
    {
        var center = Projection.GridToWorld(cell);
        float radius = reef ? 26 : 19;
        float phase = (Board.Seed ^ cell.X * 733 ^ cell.Y * 1217) * .003f;
        return Enumerable.Range(0, 18).Select(i =>
        {
            float angle = i * Mathf.Tau / 18;
            float uneven = 1 + .10f * MathF.Sin(angle * 3 + phase) + .055f * MathF.Cos(angle * 5 - phase);
            return center + new Vector2(MathF.Cos(angle), MathF.Sin(angle) * .48f) * radius * uneven;
        }).ToArray();
    }
    internal void DrawResourceContours(Node2D canvas)
    {
        foreach (var cell in Collection.Concat(DockSites).Distinct())
        {
            bool reef = DockSites.Contains(cell);
            var contour = ResourceContour(cell, reef);
            var closed = contour.Append(contour[0]).ToArray();
            var color = reef ? new Color("9ee6ce") : new Color("f3dd95");
            canvas.DrawPolyline(closed, new Color(color, .18f), 4.2f, true);
            canvas.DrawPolyline(closed, new Color(color, .8f), 1.1f, true);
        }
    }
}
