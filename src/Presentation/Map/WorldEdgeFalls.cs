using System;
using System.Collections.Generic;
using System.Linq;
using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Units;
using DevAncientNaval.Core.World;
using Godot;
using Side = DevAncientNaval.Core.Units.Side;

namespace DevAncientNaval.Presentation.Map;

/// <summary>The chart's exposed water lip falls into the void. The retained mesh
/// changes only with chart geometry/exploration; shader time supplies motion.
/// It contains no terrain, unit or gameplay information from unknown cells.</summary>
internal partial class WorldEdgeFalls : MeshInstance2D
{
    private readonly ShaderMaterial _falls = new()
    {
        Shader = new Shader { Code = """
            shader_type canvas_item;
            render_mode unshaded;
            varying vec2 fall_uv;
            void vertex() { fall_uv = UV; }
            void fragment() {
                float depth = fall_uv.y;
                float ribbon = sin(fall_uv.x * 0.42 + sin(fall_uv.x * 0.11) * 2.0);
                float stream = pow(0.5 + 0.5 * ribbon, 7.0);
                float droplets = 0.5 + 0.5 * sin(depth * 44.0 - TIME * 3.6 + fall_uv.x * 0.21);
                float edge = 1.0 - smoothstep(0.015, 0.095, depth);
                float fade = pow(1.0 - smoothstep(0.12, 1.0, depth), 1.8);
                vec3 water = mix(vec3(0.055, 0.15, 0.19), vec3(0.32, 0.62, 0.65), stream * 0.65);
                water = mix(water, vec3(0.70, 0.83, 0.79), edge * (0.64 + droplets * 0.20));
                float alpha = (0.35 + stream * 0.42 + droplets * stream * 0.12 + edge * 0.2) * fade;
                COLOR = vec4(water, alpha);
            }
            """ }
    };
    internal int BuildCount { get; private set; }
    internal int ExposedEdgeCount { get; private set; }
    private IsometricProjection? _projection;
    private GameBoard? _board;
    private (GridPosition Cell, Vector2[] Points)[] _boundary = Array.Empty<(GridPosition, Vector2[])>();
    private bool[] _explored = Array.Empty<bool>();

    internal void Rebuild(BoardView view)
    {
        bool changed = !ReferenceEquals(_projection, view.Projection) || !ReferenceEquals(_board, view.Board);
        if (changed)
        {
            _projection = view.Projection; _board = view.Board;
            var edges = new Dictionary<(Vector2, Vector2), (int Count, GridPosition Cell, Vector2[] Points)>();
            foreach (var tile in view.Board.Tiles)
                foreach (var points in view.Projection.CellEdges(tile.Position))
                {
                    var a = points[0]; var b = points[^1];
                    bool forward = a.X < b.X || a.X == b.X && a.Y < b.Y;
                    var key = forward ? (a, b) : (b, a);
                    edges[key] = (edges.GetValueOrDefault(key).Count + 1, tile.Position, points);
                }
            _boundary = edges.Values.Where(e => e.Count == 1).Select(e => (e.Cell, e.Points)).ToArray();
            _explored = new bool[_boundary.Length];
        }
        // Interior discoveries never upload the perimeter again. Only a real
        // boundary knowledge change replaces this retained shader geometry.
        for (int i = 0; i < _boundary.Length; i++)
        {
            bool explored = view.Battle.Vision.IsExplored(Side.Player, _boundary[i].Cell);
            if (_explored[i] != explored) changed = true;
            _explored[i] = explored;
        }
        if (!changed) return;
        var vertices = new List<Vector2>();
        var uv = new List<Vector2>();
        var indices = new List<int>();
        ExposedEdgeCount = 0;
        for (int boundary = 0; boundary < _boundary.Length; boundary++)
        {
            if (!_explored[boundary]) continue;
            var entry = _boundary[boundary];
            ExposedEdgeCount++;
            float distance = 0;
            for (int i = 1; i < entry.Points.Length; i++)
            {
                var a = entry.Points[i - 1]; var b = entry.Points[i];
                float next = distance + a.DistanceTo(b);
                // Downward is upright height, independent of the chart's planar XY.
                // Behind the retained map, rear curtains are naturally occluded.
                int start = vertices.Count;
                const float drop = 330;
                vertices.Add(a); vertices.Add(b);
                vertices.Add(a + new Vector2(0, drop)); vertices.Add(b + new Vector2(0, drop));
                float salt = entry.Cell.X * 13.7f + entry.Cell.Y * 7.9f;
                uv.Add(new(distance + salt, 0)); uv.Add(new(next + salt, 0));
                uv.Add(new(distance + salt, 1)); uv.Add(new(next + salt, 1));
                indices.Add(start); indices.Add(start + 1); indices.Add(start + 2);
                indices.Add(start + 1); indices.Add(start + 3); indices.Add(start + 2);
                distance = next;
            }
        }
        var old = Mesh;
        if (vertices.Count == 0) Mesh = null;
        else
        {
            var arrays = new Godot.Collections.Array();
            arrays.Resize((int)Godot.Mesh.ArrayType.Max);
            arrays[(int)Godot.Mesh.ArrayType.Vertex] = vertices.ToArray();
            arrays[(int)Godot.Mesh.ArrayType.TexUV] = uv.ToArray();
            arrays[(int)Godot.Mesh.ArrayType.Index] = indices.ToArray();
            var mesh = new ArrayMesh();
            mesh.AddSurfaceFromArrays(Godot.Mesh.PrimitiveType.Triangles, arrays);
            Mesh = mesh;
        }
        old?.Dispose();
        Material = _falls;
        Visible = Mesh is not null;
        BuildCount++;
        SetProcess(false);
    }
}
