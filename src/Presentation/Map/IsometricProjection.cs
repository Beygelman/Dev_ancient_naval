using System;
using System.Collections.Generic;
using System.Linq;
using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.World;
using Godot;

namespace DevAncientNaval.Presentation.Map;

/// <summary>A shared planar mesh whose opposite edges continue the same row tangent.
/// Curved seams are sampled once, then reused by both neighbours and picking.</summary>
public sealed class IsometricProjection
{
    public float TileWidth { get; }
    public float TileHeight { get; }
    public int TriangleCount => _faces.Values.Count(f => f.Count == 3);
    public int PentagonCount => _faces.Values.Count(f => f.Count == 5);
    public int HexagonCount => _faces.Values.Count(f => f.Count == 6);
    public const float MinimumCornerAngle = 32f;
    private const int SeamSteps = 6;
    private readonly Dictionary<int, Vector2> _vertices = new();
    private readonly Dictionary<GridPosition, List<int>> _faces = new();
    private readonly Dictionary<GridPosition, Vector2[]> _outlines = new();
    private readonly Dictionary<GridPosition, Vector2[]> _closedOutlines = new();
    private readonly Dictionary<GridPosition, Vector2> _centers = new();
    private readonly Dictionary<(int, int), Vector2[]> _edges = new();
    private readonly Dictionary<(int, int), Vector2[]> _orientedEdges = new();
    private readonly Dictionary<(int, int), List<GridPosition>> _pickBins = new();

    private readonly HashSet<(int, int)> _boundaryEdges = new();
    private readonly Dictionary<(int Vertex, int Neighbor), Vector2> _rowTangents = new();
    public IsometricProjection(GameBoard board) : this(seed: board.Seed, width: board.Width, height: board.Height, mesh: board.Mesh) { }
    public IsometricProjection(float tileWidth = 96, float tileHeight = 48, int seed = 0, int width = 20, int height = 20, OrganicMesh? mesh = null)
    {
        if (!float.IsFinite(tileWidth) || tileWidth <= 0) throw new ArgumentOutOfRangeException(nameof(tileWidth));
        if (!float.IsFinite(tileHeight) || tileHeight <= 0) throw new ArgumentOutOfRangeException(nameof(tileHeight));
        TileWidth = tileWidth; TileHeight = tileHeight;
        if (mesh is not null)
        {
            foreach (var (id, p) in mesh.Vertices) _vertices[id] = new(p.X, p.Y);
            foreach (var (p, f) in mesh.Faces) _faces[p] = new(f);
        }
        else
        {
            var random = new Random(seed); var keys = new Dictionary<GridPosition, int>();
            float phase = (float)random.NextDouble() * Mathf.Tau;
            float phase2 = (float)random.NextDouble() * Mathf.Tau;
            Vector2 Warp(Vector2 p) => p + new Vector2(
                MathF.Sin(p.Y * .23f + phase) * 1.45f + MathF.Sin(p.Y * .11f + p.X * .1f + phase2) * .45f,
                MathF.Sin(p.X * .20f + phase2) * 1.45f + MathF.Cos(p.Y * .12f + p.X * .08f + phase) * .4f);
            for (int y = -2; y <= height + 2; y++) for (int x = -2; x <= width + 2; x++)
                { int id = _vertices.Count; keys[new(x, y)] = id; _vertices[id] = Warp(new(x - .5f, y - .5f)); }
            for (int y = -2; y < height + 2; y++) for (int x = -2; x < width + 2; x++)
                    _faces[new(x, y)] = new() { keys[new(x, y)], keys[new(x + 1, y)], keys[new(x + 1, y + 1)], keys[new(x, y + 1)] };
            // Bound the smooth deformation without introducing independent corners.
            for (int pass = 0; pass < 24 && _faces.Values.Any(f => !Valid(f)); pass++)
                foreach (var (p, id) in keys) _vertices[id] = _vertices[id].Lerp(new(p.X - .5f, p.Y - .5f), .2f);
            var incident = _vertices.Keys.ToDictionary(id => id, _ => new List<GridPosition>(4));
            foreach (var (cell, face) in _faces) foreach (int id in face) incident[id].Add(cell);
            // The deformation is a smooth field shared by whole rows, not independent
            // shards. A very small jitter only keeps the result from feeling stamped.
            for (int pass = 0; pass < 1; pass++) foreach (var id in _vertices.Keys.OrderBy(_ => random.Next()).ToArray())
                {
                    var old = _vertices[id]; _vertices[id] += new Vector2((float)(random.NextDouble() - .5) * .09f, (float)(random.NextDouble() - .5) * .09f);
                    if (incident[id].Any(p => !Valid(_faces[p]))) _vertices[id] = old;
                }
            var reserved = new HashSet<GridPosition>();
            var splitCandidates = keys.Keys.Where(p => p.X > 0 && p.Y > 0 && p.X < width && p.Y < height).OrderBy(_ => random.Next()).ToArray();
            bool SplitVertex(GridPosition p)
            {
                int id = keys[p]; var old = _vertices[id]; int extra = _vertices.Count;
                var cells = new[] { new GridPosition(p.X - 1, p.Y - 1), new(p.X, p.Y - 1), new(p.X - 1, p.Y), p };
                var saved = cells.ToDictionary(c => c, c => new List<int>(_faces[c]));
                bool other = random.Next(2) == 0;
                var delta = (_vertices[keys[new(p.X + 1, p.Y)]] - _vertices[keys[new(p.X - 1, p.Y)]]) * .105f
                    + (_vertices[keys[new(p.X, p.Y + 1)]] - _vertices[keys[new(p.X, p.Y - 1)]]) * (other ? .105f : -.105f);
                _vertices[id] = old + delta; _vertices[extra] = old - delta;
                void Replace(GridPosition c, params int[] replacement) { var f = _faces[c]; int i = f.IndexOf(id); f.RemoveAt(i); f.InsertRange(i, replacement); }
                if (!other) { Replace(cells[0], id, extra); Replace(cells[3], extra, id); Replace(cells[2], extra); }
                else { Replace(cells[1], extra, id); Replace(cells[2], id, extra); Replace(cells[0], extra); }
                if (cells.Any(c => !Valid(_faces[c])))
                { foreach (var c in cells) _faces[c] = saved[c]; _vertices[id] = old; _vertices.Remove(extra); return false; }
                reserved.Add(p); return true;
            }
            // Sparse splits leave quadrilaterals as the clear majority. A few adjacent
            // splits create genuine six-sided faces without introducing tiny slivers.
            foreach (var p in splitCandidates)
            {
                if (reserved.Count >= Math.Max(2, width * height / 55)) break;
                if (reserved.Any(q => Math.Max(Math.Abs(p.X - q.X), Math.Abs(p.Y - q.Y)) < 2)) continue;
                SplitVertex(p);
            }
            int hexagonGoal = Math.Max(1, width * height / 180);
            foreach (var p in splitCandidates)
            {
                if (HexagonCount >= hexagonGoal) break;
                if (reserved.Contains(p)) continue;
                var neighbours = new[] { new GridPosition(p.X - 1, p.Y - 1), new(p.X, p.Y - 1), new(p.X - 1, p.Y), p };
                if (neighbours.Any(c => _faces[c].Count == 5)) SplitVertex(p);
            }
            var edges = _faces.Values.SelectMany(f => Enumerable.Range(0, f.Count).Select(i => Key(f[i], f[(i + 1) % f.Count]))).Distinct().OrderBy(_ => random.Next()).ToArray();
            var touched = new HashSet<int>(); int collapses = 0;
            foreach (var (a, b) in edges)
            {
                if (collapses >= Math.Max(1, width * height / 180) || touched.Contains(a) || touched.Contains(b)) continue;
                var cells = _faces.Where(f => f.Value.Contains(a) || f.Value.Contains(b)).Select(f => f.Key).ToArray();
                if (cells.Any(p => p.X < 0 || p.Y < 0 || p.X >= width || p.Y >= height || _faces[p].Count == 6)) continue;
                var saved = cells.ToDictionary(p => p, p => new List<int>(_faces[p])); var old = _vertices[a];
                _vertices[a] = (_vertices[a] + _vertices[b]) / 2;
                foreach (var c in cells) _faces[c] = _faces[c].Select(id => id == b ? a : id).Distinct().ToList();
                if (cells.Any(c => !Valid(_faces[c]))) { foreach (var c in cells) _faces[c] = saved[c]; _vertices[a] = old; }
                else { collapses++; foreach (var f in saved.Values) touched.UnionWith(f); }
            }
        }
        var counts = new Dictionary<(int, int), int>();
        foreach (var f in _faces.Values) for (int i = 0; i < f.Count; i++) { var k = Key(f[i], f[(i + 1) % f.Count]); counts[k] = counts.GetValueOrDefault(k) + 1; }
        foreach (var (k, n) in counts) if (n == 1) _boundaryEdges.Add(k);
        BuildRowTangents(counts.Keys);
        // Keep curved corner tangents usable too. Straightening a shared seam can
        // affect its neighbour, so repeat until the entire mesh is stable.
        bool adjusted;
        do
        {
            adjusted = false;
            foreach (var (p, f) in _faces)
            {
                var outline = CellEdges(p).SelectMany(edge => edge.SkipLast(1)).ToArray(); var center = GridToWorld(p);
                bool invalid = Enumerable.Range(0, outline.Length).Any(i =>
                    (outline[(i + 1) % outline.Length] - outline[i]).Cross(center - outline[i]) < 0 ||
                    (i % SeamSteps == 0 && CornerAngle(outline[(i + outline.Length - 1) % outline.Length] - outline[i], outline[(i + 1) % outline.Length] - outline[i]) < MinimumCornerAngle));
                if (!invalid) continue;
                for (int i = 0; i < f.Count; i++)
                {
                    var k = Key(f[i], f[(i + 1) % f.Count]); var a = Project(_vertices[k.Item1]); var b = Project(_vertices[k.Item2]);
                    if (_edges[k][SeamSteps / 2].DistanceSquaredTo(a.Lerp(b, .5f)) < .0001f) continue;
                    _edges[k] = Enumerable.Range(0, SeamSteps + 1).Select(j => a.Lerp(b, j / (float)SeamSteps)).ToArray();
                    _orientedEdges.Remove((k.Item1, k.Item2)); _orientedEdges.Remove((k.Item2, k.Item1));
                    adjusted = true;
                }
            }
        }
        while (adjusted);
        foreach (var p in _faces.Keys)
        {
            var outline = Diamond(p).Select(Unproject).ToArray();
            for (int y = (int)Math.Floor(outline.Min(v => v.Y)); y <= (int)Math.Floor(outline.Max(v => v.Y)); y++)
                for (int x = (int)Math.Floor(outline.Min(v => v.X)); x <= (int)Math.Floor(outline.Max(v => v.X)); x++)
                { if (!_pickBins.TryGetValue((x, y), out var list)) _pickBins[(x, y)] = list = new(); list.Add(p); }
        }
    }
    private bool Valid(List<int> face)
    {
        if (face.Count < 3 || face.Count > 6) return false;
        var p = face.Select(id => _vertices[id]).ToArray();
        float area = 0;
        for (int i = 0; i < p.Length; i++)
        {
            area += p[i].Cross(p[(i + 1) % p.Length]);
            var incoming = p[(i + p.Length - 1) % p.Length] - p[i];
            var outgoing = p[(i + 1) % p.Length] - p[i];
            if (incoming.LengthSquared() < .09f || outgoing.Cross(incoming) < .04f) return false;
            if (CornerAngle(incoming, outgoing) < 46f || CornerAngle(Project(incoming), Project(outgoing)) < MinimumCornerAngle) return false;
        }
        return area > .9f;
    }
    private static float CornerAngle(Vector2 a, Vector2 b) => Mathf.RadToDeg(MathF.Acos(Math.Clamp(a.Dot(b) / (a.Length() * b.Length()), -1, 1)));
    private static (int, int) Key(int a, int b) => a < b ? (a, b) : (b, a);
    private Vector2 Project(Vector2 p) => new((p.X - p.Y) * TileWidth / 2, (p.X + p.Y) * TileHeight / 2);
    private Vector2 Unproject(Vector2 p) => new(p.X / TileWidth + p.Y / TileHeight, p.Y / TileHeight - p.X / TileWidth);
    private void BuildRowTangents(IEnumerable<(int, int)> edges)
    {
        var adjacent = new Dictionary<int, List<int>>();
        foreach (var (a, b) in edges)
        {
            if (!adjacent.TryGetValue(a, out var first)) adjacent[a] = first = new();
            if (!adjacent.TryGetValue(b, out var second)) adjacent[b] = second = new();
            first.Add(b);
            second.Add(a);
        }
        foreach (var (vertex, neighbors) in adjacent)
        {
            var remaining = new List<int>(neighbors);
            while (remaining.Count > 1)
            {
                int first = -1, second = -1;
                float score = -.25f;
                for (int i = 0; i < remaining.Count; i++)
                for (int j = i + 1; j < remaining.Count; j++)
                {
                    float dot = (_vertices[remaining[i]] - _vertices[vertex]).Normalized()
                        .Dot((_vertices[remaining[j]] - _vertices[vertex]).Normalized());
                    if (dot >= score) continue;
                    score = dot;
                    first = remaining[i];
                    second = remaining[j];
                }
                if (first < 0) break;
                var direction = ((_vertices[first] - _vertices[vertex]).Normalized() -
                    (_vertices[second] - _vertices[vertex]).Normalized()).Normalized();
                _rowTangents[(vertex, first)] = direction;
                _rowTangents[(vertex, second)] = -direction;
                remaining.Remove(first);
                remaining.Remove(second);
            }
            foreach (int neighbor in remaining)
                _rowTangents[(vertex, neighbor)] = (_vertices[neighbor] - _vertices[vertex]).Normalized();
        }
    }
    private Vector2[] Seam(int a, int b)
    {
        if (_orientedEdges.TryGetValue((a, b), out var oriented)) return oriented;
        var key = Key(a, b);
        if (!_edges.TryGetValue(key, out var edge))
        {
            var start = _vertices[key.Item1]; var end = _vertices[key.Item2]; var d = end - start;
            float length = d.Length();
            var first = start + _rowTangents[(key.Item1, key.Item2)] * length / 3;
            var second = end + _rowTangents[(key.Item2, key.Item1)] * length / 3;
            edge = new Vector2[SeamSteps + 1];
            for (int i = 0; i <= SeamSteps; i++)
            {
                float t = i / (float)SeamSteps, u = 1 - t;
                edge[i] = Project(_boundaryEdges.Contains(key) ? start.Lerp(end, t) :
                    u * u * u * start + 3 * u * u * t * first + 3 * u * t * t * second + t * t * t * end);
            }
            _edges[key] = edge;
        }
        return _orientedEdges[(a, b)] = a == key.Item1 ? edge : edge.Reverse().ToArray();
    }
    /// <summary>Boundary seams paired with an interior point for inward-only glow.</summary>
    public IEnumerable<(Vector2[] Edge, Vector2 Inside)> BoundarySegments(IEnumerable<GridPosition> cells)
    {
        var boundary = new Dictionary<(int, int), (int Count, GridPosition Cell)>();
        foreach (var p in cells.Distinct()) if (_faces.TryGetValue(p, out var f))
                for (int i = 0; i < f.Count; i++)
                { var k = Key(f[i], f[(i + 1) % f.Count]); boundary[k] = (boundary.GetValueOrDefault(k).Count + 1, p); }
        foreach (var (edge, entry) in boundary) if (entry.Count == 1)
                yield return (Seam(edge.Item1, edge.Item2), GridToWorld(entry.Cell));
    }
    public IEnumerable<Vector2[]> BoundaryEdges(IEnumerable<GridPosition> cells)
    {
        var counts = new Dictionary<(int, int), int>();
        foreach (var p in cells.Distinct()) if (_faces.TryGetValue(p, out var f))
                for (int i = 0; i < f.Count; i++) { var k = Key(f[i], f[(i + 1) % f.Count]); counts[k] = counts.GetValueOrDefault(k) + 1; }
        foreach (var (k, count) in counts) if (count == 1) yield return Seam(k.Item1, k.Item2);
    }

    /// <summary>Connected oriented contours of a tile union, including separate islands.</summary>
    public IEnumerable<Vector2[]> BoundaryLoops(IEnumerable<GridPosition> cells)
    {
        var edges = new Dictionary<(int, int), (int From, int To, int Count)>();
        foreach (var cell in cells.Distinct())
        {
            if (!_faces.TryGetValue(cell, out var face)) continue;
            for (int i = 0; i < face.Count; i++)
            {
                int a = face[i], b = face[(i + 1) % face.Count];
                var key = Key(a, b);
                edges[key] = (a, b, edges.GetValueOrDefault(key).Count + 1);
            }
        }
        var next = edges.Values.Where(edge => edge.Count == 1).GroupBy(edge => edge.From)
            .ToDictionary(group => group.Key, group => group.Select(edge => edge.To).ToList());
        while (next.Count > 0)
        {
            int start = next.First().Key, current = start;
            var points = new List<Vector2>();
            do
            {
                if (!next.TryGetValue(current, out var options) || options.Count == 0) break;
                int target = options[^1];
                options.RemoveAt(options.Count - 1);
                if (options.Count == 0) next.Remove(current);
                var edge = Seam(current, target);
                for (int i = 0; i < edge.Length - 1; i++) points.Add(edge[i]);
                current = target;
            } while (current != start);
            if (points.Count >= 3) yield return points.ToArray();
        }
    }
    public IEnumerable<Vector2[]> CellEdges(GridPosition p)
    {
        var f = _faces[p]; for (int i = 0; i < f.Count; i++) yield return Seam(f[i], f[(i + 1) % f.Count]);
    }
    public Vector2[] Diamond(GridPosition p)
    {
        if (!_outlines.TryGetValue(p, out var outline))
        {
            if (!_faces.ContainsKey(p)) return new[] { Project(new(p.X - .5f, p.Y - .5f)), Project(new(p.X + .5f, p.Y - .5f)), Project(new(p.X + .5f, p.Y + .5f)), Project(new(p.X - .5f, p.Y + .5f)) };
            _outlines[p] = outline = CellEdges(p).SelectMany(edge => edge.SkipLast(1)).ToArray();
        }
        return outline;
    }
    public int CornerCount(GridPosition p) => _faces.TryGetValue(p, out var f) ? f.Count : 4;
    public float SmallestCornerAngle(GridPosition p)
    {
        if (!_faces.TryGetValue(p, out var f)) return CornerAngle(Project(new(1, 0)), Project(new(0, 1)));
        float angle = 180;
        for (int i = 0; i < f.Count; i++) angle = Math.Min(angle, CornerAngle(Project(_vertices[f[(i + f.Count - 1) % f.Count]] - _vertices[f[i]]), Project(_vertices[f[(i + 1) % f.Count]] - _vertices[f[i]])));
        return angle;
    }
    public Vector2[] ClosedOutline(GridPosition p)
    {
        if (!_closedOutlines.TryGetValue(p, out var result))
        { var points = Diamond(p); result = new Vector2[points.Length + 1]; points.CopyTo(result, 0); result[^1] = points[0]; _closedOutlines[p] = result; }
        return result;
    }
    public Vector2 GridToWorld(GridPosition p)
    {
        if (_centers.TryGetValue(p, out var center)) return center;
        if (!_faces.TryGetValue(p, out var f)) return Project(new(p.X, p.Y));
        var sum = Vector2.Zero; foreach (var id in f) sum += _vertices[id];
        return _centers[p] = Project(sum / f.Count);
    }
    public GridPosition WorldToGrid(Vector2 point)
    {
        var q = Unproject(point); var key = ((int)Math.Floor(q.X), (int)Math.Floor(q.Y));
        if (_pickBins.TryGetValue(key, out var list)) foreach (var p in list) if (ContainsPoint(point, Diamond(p))) return p;
        return new(int.MinValue, int.MinValue);
    }
    private static bool ContainsPoint(Vector2 point, Vector2[] outline)
    {
        // Double-precision crossing avoids false hits near a distant corner in
        // the native float-based polygon test. The same rendered seams are used.
        bool inside = false;
        for (int i = 0, j = outline.Length - 1; i < outline.Length; j = i++)
        {
            var a = outline[j]; var b = outline[i];
            double dx = (double)b.X - a.X, dy = (double)b.Y - a.Y;
            double px = (double)point.X - a.X, py = (double)point.Y - a.Y;
            double cross = px * dy - py * dx;
            if (Math.Abs(cross) < .00001 && px * dx + py * dy >= 0 && px * dx + py * dy <= dx * dx + dy * dy) return true;
            if ((a.Y > point.Y) != (b.Y > point.Y) && point.X < a.X + dx * ((double)point.Y - a.Y) / dy) inside = !inside;
        }
        return inside;
    }
    public Rect2 BoardBounds(GameBoard board) => Bounds(board.Tiles.Select(t => t.Position));
    public Rect2 BoardBounds(int width, int height) => Bounds(from y in Enumerable.Range(0, height) from x in Enumerable.Range(0, width) select new GridPosition(x, y));
    private Rect2 Bounds(IEnumerable<GridPosition> cells)
    {
        var points = cells.SelectMany(Diamond).ToArray(); var bounds = new Rect2(points[0], Vector2.Zero);
        foreach (var p in points) bounds = bounds.Expand(p); return bounds.Grow(4);
    }
}
