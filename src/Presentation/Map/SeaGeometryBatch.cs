using System;
using System.Collections.Generic;
using Godot;

namespace DevAncientNaval.Presentation.Map;
/// <summary>One native submission per sea layer instead of a call per fish,
/// coral branch or shore segment. Buffers retain their capacity between frames.</summary>
internal sealed class SeaGeometryBatch
{
    private readonly List<Vector2> _vertices, _lines;
    private readonly List<Color> _colors, _lineColors;
    private readonly List<int> _indices;
    internal SeaGeometryBatch(int initialCapacity = 8192)
    {
        _vertices = new(initialCapacity); _lines = new(initialCapacity);
        _colors = new(initialCapacity); _lineColors = new(initialCapacity);
        _indices = new(initialCapacity * 2);
    }
    private Vector2[] _nativeVertices = Array.Empty<Vector2>();
    private Color[] _nativeColors = Array.Empty<Color>();
    private int[] _nativeIndices = Array.Empty<int>();
    private bool _prepared;
    private float _preparedWidth;
    internal void Clear()
    {
        _prepared = false;
        _vertices.Clear();
        _colors.Clear();
        _indices.Clear();
        _lines.Clear();
        _lineColors.Clear();
    }

    internal void Polygon(ReadOnlySpan<Vector2> points, Color color, Transform2D transform)
    {
        _prepared = false;
        int start = _vertices.Count;
        foreach (var point in points)
        {
            _vertices.Add(transform * point);
            _colors.Add(color);
        }

        for (int i = 1; i < points.Length - 1; i++)
        {
            _indices.Add(start);
            _indices.Add(start + i);
            _indices.Add(start + i + 1);
        }
    }

    internal void Line(Vector2 a, Vector2 b, Color color)
    {
        _prepared = false;
        _lines.Add(a);
        _lines.Add(b);
        _lineColors.Add(color);
    }

    internal void Triangle(ReadOnlySpan<Vector2> points, Color first, Color second, Color third)
    {
        if (points.Length != 3) return;
        _prepared = false;
        int start = _vertices.Count;
        _vertices.Add(points[0]); _vertices.Add(points[1]); _vertices.Add(points[2]);
        _colors.Add(first); _colors.Add(second); _colors.Add(third);
        _indices.Add(start); _indices.Add(start + 1); _indices.Add(start + 2);
    }

    internal void Polyline(ReadOnlySpan<Vector2> points, Color color)
    {
        for (int i = 1; i < points.Length; i++)
            Line(points[i - 1], points[i], color);
    }

    internal void Submit(CanvasItem canvas, float width = 1)
    {
        if (_prepared && width == _preparedWidth)
        {
            if (_nativeVertices.Length > 0)
                RenderingServer.CanvasItemAddTriangleArray(canvas.GetCanvasItem(), _nativeIndices, _nativeVertices, _nativeColors);
            return;
        }
        int verticesBeforeLines = _vertices.Count;
        int indicesBeforeLines = _indices.Count;
        // Godot's antialiased multiline command expands into a draw operation
        // for each segment. Bake soft-edged strips into the same triangle batch.
        for (int line = 0; line < _lines.Count; line += 2)
        {
            var a = _lines[line];
            var b = _lines[line + 1];
            var direction = b - a;
            if (direction.LengthSquared() < .000001f) continue;
            var normal = direction.Normalized().Orthogonal();
            float inner = Math.Max(.1f, width * .5f);
            float outer = inner + .65f;
            int start = _vertices.Count;
            var color = _lineColors[line / 2];
            var clear = new Color(color, 0);
            StripVertex(a - normal * outer, clear);
            StripVertex(b - normal * outer, clear);
            StripVertex(a - normal * inner, color);
            StripVertex(b - normal * inner, color);
            StripVertex(a + normal * inner, color);
            StripVertex(b + normal * inner, color);
            StripVertex(a + normal * outer, clear);
            StripVertex(b + normal * outer, clear);
            for (int band = 0; band < 3; band++)
            {
                int offset = start + band * 2;
                _indices.Add(offset); _indices.Add(offset + 1); _indices.Add(offset + 2);
                _indices.Add(offset + 1); _indices.Add(offset + 3); _indices.Add(offset + 2);
            }
        }
        if (_vertices.Count > 0)
        {
            if (_nativeVertices.Length != _vertices.Count) Array.Resize(ref _nativeVertices, _vertices.Count);
            if (_nativeColors.Length != _colors.Count) Array.Resize(ref _nativeColors, _colors.Count);
            if (_nativeIndices.Length != _indices.Count) Array.Resize(ref _nativeIndices, _indices.Count);
            _vertices.CopyTo(_nativeVertices);
            _colors.CopyTo(_nativeColors);
            _indices.CopyTo(_nativeIndices);
            RenderingServer.CanvasItemAddTriangleArray(canvas.GetCanvasItem(), _nativeIndices, _nativeVertices, _nativeColors);
        }
        else
        {
            _nativeVertices = Array.Empty<Vector2>();
            _nativeColors = Array.Empty<Color>();
            _nativeIndices = Array.Empty<int>();
        }
        _vertices.RemoveRange(verticesBeforeLines, _vertices.Count - verticesBeforeLines);
        _colors.RemoveRange(verticesBeforeLines, _colors.Count - verticesBeforeLines);
        _indices.RemoveRange(indicesBeforeLines, _indices.Count - indicesBeforeLines);
        _preparedWidth = width;
        _prepared = true;
    }

    private void StripVertex(Vector2 point, Color color)
    {
        _vertices.Add(point);
        _colors.Add(color);
    }
}
