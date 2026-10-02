using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Godot;

namespace DevAncientNaval.Presentation.Map;
/// <summary>One native submission per sea layer instead of a call per fish,
/// coral branch or shore segment. Buffers retain their capacity between frames.</summary>
internal sealed class SeaGeometryBatch
{
    private readonly List<Vector2> _vertices = new(8192), _lines = new(8192);
    private readonly List<Color> _colors = new(8192), _lineColors = new(8192);
    private readonly List<int> _indices = new(16384);
    private Vector2[] _nativeVertices = Array.Empty<Vector2>();
    private Color[] _nativeColors = Array.Empty<Color>();
    private int[] _nativeIndices = Array.Empty<int>();
    internal void Clear()
    {
        _vertices.Clear();
        _colors.Clear();
        _indices.Clear();
        _lines.Clear();
        _lineColors.Clear();
    }

    internal void Polygon(ReadOnlySpan<Vector2> points, Color color, Transform2D transform)
    {
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
        _lines.Add(a);
        _lines.Add(b);
        _lineColors.Add(color);
    }

    internal void Polyline(ReadOnlySpan<Vector2> points, Color color)
    {
        for (int i = 1; i < points.Length; i++)
            Line(points[i - 1], points[i], color);
    }

    internal void Submit(Node2D canvas, float width = 1)
    {
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
        if (_lines.Count > 0)
            canvas.DrawMultilineColors(CollectionsMarshal.AsSpan(_lines), CollectionsMarshal.AsSpan(_lineColors), width, true);
    }
}
