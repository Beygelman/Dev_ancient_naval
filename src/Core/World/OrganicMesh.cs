using System.Numerics;
using DevAncientNaval.Core.Grid;

namespace DevAncientNaval.Core.World;
<<<<<<< Updated upstream
=======

>>>>>>> Stashed changes
/// <summary>A boundary-fitted, quadrilateral-dominant mesh. Cell addresses are IDs;
/// connectivity comes from shared vertices, never from the address coordinates.</summary>
public sealed partial class OrganicMesh
{
    public const float MinimumProjectedAngle = 32;
    public IReadOnlyList<Vector2> Boundary { get; private set; } = Array.Empty<Vector2>();
    public IReadOnlyList<int> SideTileCounts { get; private set; } = Array.Empty<int>();
    public IReadOnlyDictionary<int, Vector2> Vertices => _vertices;
    public IReadOnlyDictionary<GridPosition, List<int>> Faces => _faces;
    public IReadOnlyDictionary<GridPosition, Vector2> Centers => _centers;
    public int Width => 34;
    public int Height => (_faces.Count + 31) / 32 + 2;
    public int RowRedirects { get; private set; }
<<<<<<< Updated upstream

=======
>>>>>>> Stashed changes
    private readonly Dictionary<int, Vector2> _vertices = new();
    private readonly Dictionary<GridPosition, List<int>> _faces = new();
    private readonly Dictionary<GridPosition, Vector2> _centers = new();
    private readonly Dictionary<GridPosition, GridPosition[]> _neighbors = new(), _surrounding = new();
    private readonly Dictionary<GridPosition, Dictionary<GridPosition, int>> _distances = new();
<<<<<<< Updated upstream
    private readonly Dictionary<(GridPosition Origin, int Radius), IReadOnlyList<GridPosition>> _coverage = new();
=======
>>>>>>> Stashed changes
    private readonly HashSet<int> _fixed = new();
    private static (int, int) Edge(int a, int b) => a < b ? (a, b) : (b, a);
    private static float Cross(Vector2 a, Vector2 b) => a.X * b.Y - a.Y * b.X;
    private static Vector2 Project(Vector2 p) => new(p.X - p.Y, (p.X + p.Y) * .5f);
    private static float Angle(Vector2 a, Vector2 b) => MathF.Acos(Math.Clamp(Vector2.Dot(a, b) / (a.Length() * b.Length()), -1, 1)) * 180 / MathF.PI;
<<<<<<< Updated upstream
=======

>>>>>>> Stashed changes
    public static OrganicMesh Create(int seed, float scale = 1)
    {
        if (!float.IsFinite(scale) || scale is < .5f or > 1.5f)
            throw new ArgumentOutOfRangeException(nameof(scale));
        int minimum = (int)MathF.Round(24 * scale);
        // Six patch sums add to an even number. Seven candidate counts avoid
        // an impossible all-distinct choice when six consecutive counts sum odd.
        int maximum = Math.Max(minimum + 6, (int)MathF.Round(30 * scale));
        var random = new Random(seed);
        OrganicMesh mesh;
        int attempt = 0;
        do
        {
            mesh = new();
            int[] k, counts;
            do
            {
                int low = (int)MathF.Floor(minimum / 2f) - 1;
                int high = (int)MathF.Ceiling(maximum / 2f) + 2;
                k = Enumerable.Range(0, 6).Select(_ => random.Next(low, high)).ToArray();
                counts = Enumerable.Range(0, 6).Select(i => k[(i + 5) % 6] + k[(i + 1) % 6]).ToArray();
<<<<<<< Updated upstream
            }
            while (counts.Distinct().Count() != 6 || counts.Any(n => n < minimum || n > maximum));
            if (attempt++ > 2048)
                throw new InvalidOperationException("Cannot construct a valid six-sided mesh.");
            mesh.BuildPatches(k, counts, random.NextDouble() * Math.Tau);
        }
        while (mesh._faces.Values.Any(f => !mesh.Valid(f)));
=======
            } while (counts.Distinct().Count() != 6 || counts.Any(n => n < minimum || n > maximum));
            if (attempt++ > 2048) throw new InvalidOperationException("Cannot construct a valid six-sided mesh.");
            mesh.BuildPatches(k, counts, random.NextDouble() * Math.Tau);
        } while (mesh._faces.Values.Any(f => !mesh.Valid(f)));
>>>>>>> Stashed changes
        mesh.FindBoundaryVertices();
        mesh.RedirectRows(random);
        mesh.Deform(random);
        mesh.ChangeTopology(random);
        mesh.IndexTopology();
        return mesh;
    }

    private void BuildPatches(int[] divisions, int[] counts, double rotation)
    {
        SideTileCounts = Array.AsReadOnly(counts);
        double low = counts.Max() / 2d, high = counts.Sum();
        for (int i = 0; i < 64; i++)
        {
            double radius = (low + high) / 2;
<<<<<<< Updated upstream
            if (counts.Sum(n => 2 * Math.Asin(n / (2 * radius))) > Math.Tau)
                low = radius;
            else
                high = radius;
        }

=======
            if (counts.Sum(n => 2 * Math.Asin(n / (2 * radius))) > Math.Tau) low = radius;
            else high = radius;
        }
>>>>>>> Stashed changes
        double r = (low + high) / 2, angle = rotation;
        var boundary = new Vector2[6];
        for (int i = 0; i < 6; i++)
        {
            boundary[i] = new((float)(Math.Cos(angle) * r), (float)(Math.Sin(angle) * r));
            angle += 2 * Math.Asin(counts[i] / (2 * r));
        }
<<<<<<< Updated upstream

        var offset = new Vector2(1 - boundary.Min(v => v.X), 1 - boundary.Min(v => v.Y));
        for (int i = 0; i < 6; i++)
            boundary[i] += offset;
=======
        var offset = new Vector2(1 - boundary.Min(v => v.X), 1 - boundary.Min(v => v.Y));
        for (int i = 0; i < 6; i++) boundary[i] += offset;
>>>>>>> Stashed changes
        Boundary = Array.AsReadOnly(boundary);
        var center = boundary.Aggregate(Vector2.Zero, (a, b) => a + b) / 6;
        var midpoints = Enumerable.Range(0, 6).Select(i => Vector2.Lerp(boundary[i], boundary[(i + 1) % 6], divisions[(i + 5) % 6] / (float)counts[i])).ToArray();
        var ids = new Dictionary<(int, int, int), int>();
        int Vertex(Vector2 p, int patch, int x, int y)
        {
            var key = x == 0 && y == 0 ? (0, 0, 0) : x == 0 ? (1, (patch + 1) % 6, y) : y == 0 ? (1, patch, x) : (patch + 2, x, y);
<<<<<<< Updated upstream
            if (!ids.TryGetValue(key, out int id))
            {
                id = _vertices.Count;
                ids[key] = id;
                _vertices[id] = p;
            }

            return id;
        }

=======
            if (!ids.TryGetValue(key, out int id)) { id = _vertices.Count; ids[key] = id; _vertices[id] = p; }
            return id;
        }
>>>>>>> Stashed changes
        // Six patches meet at one shared central node. Each boundary interval
        // belongs to exactly one tile, including either side of a corner tile.
        for (int patch = 0; patch < 6; patch++)
        {
            int nx = divisions[patch], ny = divisions[(patch + 1) % 6];
            var nodes = new int[nx + 1, ny + 1];
<<<<<<< Updated upstream
            for (int y = 0; y <= ny; y++)
                for (int x = 0; x <= nx; x++)
                {
                    float Spacing(float t, int axis) => t + .035f * MathF.Sin(MathF.PI * t) * MathF.Sin(MathF.Tau * t + axis * 1.71f + (float)rotation);
                    float u = Spacing(x / (float)nx, patch), v = Spacing(y / (float)ny, (patch + 1) % 6);
                    nodes[x, y] = Vertex((1 - u) * (1 - v) * center + u * (1 - v) * midpoints[patch] + u * v * boundary[(patch + 1) % 6] + (1 - u) * v * midpoints[(patch + 1) % 6], patch, x, y);
                }

            for (int y = 0; y < ny; y++)
                for (int x = 0; x < nx; x++)
                {
                    int index = _faces.Count;
                    _faces[new(index % 32 + 1, index / 32 + 1)] = new()
                    {
                        nodes[x, y],
                        nodes[x + 1, y],
                        nodes[x + 1, y + 1],
                        nodes[x, y + 1]
                    };
                }
=======
            for (int y = 0; y <= ny; y++) for (int x = 0; x <= nx; x++)
            {
                float Spacing(float t,int axis) => t + .035f * MathF.Sin(MathF.PI*t) * MathF.Sin(MathF.Tau*t+axis*1.71f+(float)rotation);
                float u = Spacing(x / (float)nx,patch), v = Spacing(y / (float)ny,(patch+1)%6);
                nodes[x, y] = Vertex((1 - u) * (1 - v) * center + u * (1 - v) * midpoints[patch] +
                    u * v * boundary[(patch + 1) % 6] + (1 - u) * v * midpoints[(patch + 1) % 6], patch, x, y);
            }
            for (int y = 0; y < ny; y++) for (int x = 0; x < nx; x++)
            {
                int index = _faces.Count;
                _faces[new(index % 32 + 1, index / 32 + 1)] = new() { nodes[x, y], nodes[x + 1, y], nodes[x + 1, y + 1], nodes[x, y + 1] };
            }
>>>>>>> Stashed changes
        }
    }

    private Dictionary<(int, int), List<GridPosition>> EdgeCells()
    {
        var result = new Dictionary<(int, int), List<GridPosition>>();
<<<<<<< Updated upstream
        foreach (var(cell, f)in _faces)
            for (int i = 0; i < f.Count; i++)
            {
                var key = Edge(f[i], f[(i + 1) % f.Count]);
                if (!result.TryGetValue(key, out var cells))
                    result[key] = cells = new();
                cells.Add(cell);
            }

        return result;
    }

    private void FindBoundaryVertices()
    {
        foreach (var(edge, cells)in EdgeCells())
            if (cells.Count == 1)
            {
                _fixed.Add(edge.Item1);
                _fixed.Add(edge.Item2);
            }
    }

    private Dictionary<int, List<GridPosition>> Incident()
    {
        var result = _vertices.Keys.ToDictionary(id => id, _ => new List<GridPosition>());
        foreach (var(p, f)in _faces)
            foreach (int id in f)
                result[id].Add(p);
        return result;
    }

    private bool Valid(List<int> face)
    {
        if (face.Count is < 3 or > 5)
            return false;
=======
        foreach (var (cell, f) in _faces) for (int i = 0; i < f.Count; i++)
        {
            var key = Edge(f[i], f[(i + 1) % f.Count]);
            if (!result.TryGetValue(key, out var cells)) result[key] = cells = new();
            cells.Add(cell);
        }
        return result;
    }
    private void FindBoundaryVertices()
    {
        foreach (var (edge, cells) in EdgeCells()) if (cells.Count == 1) { _fixed.Add(edge.Item1); _fixed.Add(edge.Item2); }
    }
    private Dictionary<int, List<GridPosition>> Incident()
    {
        var result = _vertices.Keys.ToDictionary(id => id, _ => new List<GridPosition>());
        foreach (var (p, f) in _faces) foreach (int id in f) result[id].Add(p);
        return result;
    }
    private bool Valid(List<int> face)
    {
        if (face.Count is < 3 or > 5) return false;
>>>>>>> Stashed changes
        float area = 0;
        for (int i = 0; i < face.Count; i++)
        {
            var p = _vertices[face[i]];
            var a = _vertices[face[(i + face.Count - 1) % face.Count]] - p;
            var b = _vertices[face[(i + 1) % face.Count]] - p;
<<<<<<< Updated upstream
            if (a.LengthSquared() < .1f || Cross(b, a) < .05f || Angle(a, b) < 38 || Angle(Project(a), Project(b)) < MinimumProjectedAngle)
                return false;
            area += Cross(p, _vertices[face[(i + 1) % face.Count]]);
        }

        return area > .8f;
    }

    private void Deform(Random random)
    {
        var incident = Incident();
        float minX = Boundary.Min(p => p.X), minY = Boundary.Min(p => p.Y), width = Boundary.Max(p => p.X) - minX, height = Boundary.Max(p => p.Y) - minY;
=======
            if (a.LengthSquared() < .1f || Cross(b, a) < .05f || Angle(a, b) < 38 || Angle(Project(a), Project(b)) < MinimumProjectedAngle) return false;
            area += Cross(p, _vertices[face[(i + 1) % face.Count]]);
        }
        return area > .8f;
    }
    private void Deform(Random random)
    {
        var incident = Incident();
        float minX=Boundary.Min(p=>p.X), minY=Boundary.Min(p=>p.Y), width=Boundary.Max(p=>p.X)-minX,height=Boundary.Max(p=>p.Y)-minY;
>>>>>>> Stashed changes
        var vortices = Enumerable.Range(0, 7).Select(_ => (Center: new Vector2(minX + (float)random.NextDouble() * width, minY + (float)random.NextDouble() * height), Radius: 7 + (float)random.NextDouble() * 8, Turn: ((float)random.NextDouble() - .5f) * .32f)).ToArray();
        var targets = _vertices.ToDictionary(v => v.Key, v =>
        {
            var p = v.Value;
            foreach (var swirl in vortices)
            {
                var delta = p - swirl.Center;
                float a = swirl.Turn * MathF.Exp(-delta.LengthSquared() / (swirl.Radius * swirl.Radius));
                p = swirl.Center + new Vector2(delta.X * MathF.Cos(a) - delta.Y * MathF.Sin(a), delta.X * MathF.Sin(a) + delta.Y * MathF.Cos(a));
            }
<<<<<<< Updated upstream

            return p + new Vector2(MathF.Sin(p.Y * .18f + p.X * .08f) * .28f, MathF.Cos(p.X * .16f - p.Y * .10f) * .28f);
        });
        // Incremental moves preserve convexity and usable screen angles, with
        // the actual six straight sides pinned throughout the deformation.
        for (int pass = 0; pass < 10; pass++)
            foreach (int id in _vertices.Keys.OrderBy(_ => random.Next()).ToArray())
            {
                if (_fixed.Contains(id))
                    continue;
                var old = _vertices[id];
                _vertices[id] = Vector2.Lerp(old, targets[id], .18f);
                if (incident[id].Any(p => !Valid(_faces[p])))
                    _vertices[id] = old;
            }
    // There is no independent per-vertex jitter: every displacement belongs
    // to a broad field, so neighbouring rows inherit the same bend.
    }

=======
            return p + new Vector2(MathF.Sin(p.Y*.18f+p.X*.08f)*.28f,MathF.Cos(p.X*.16f-p.Y*.10f)*.28f);
        });
        // Incremental moves preserve convexity and usable screen angles, with
        // the actual six straight sides pinned throughout the deformation.
        for (int pass = 0; pass < 10; pass++) foreach (int id in _vertices.Keys.OrderBy(_ => random.Next()).ToArray())
        {
            if (_fixed.Contains(id)) continue;
            var old = _vertices[id]; _vertices[id] = Vector2.Lerp(old, targets[id], .18f);
            if (incident[id].Any(p => !Valid(_faces[p]))) _vertices[id] = old;
        }
        // There is no independent per-vertex jitter: every displacement belongs
        // to a broad field, so neighbouring rows inherit the same bend.
    }
>>>>>>> Stashed changes
    private void RedirectRows(Random random)
    {
        // A different diagonal across a pair of quads creates a three/five-way
        // junction pair. Relaxing its neighbourhood bends complete rows around
        // that pair instead of adding unrelated noise to individual tiles.
        int changed = 0;
<<<<<<< Updated upstream
        foreach (var(edge, pair)in EdgeCells().OrderBy(_ => random.Next()).ToArray())
        {
            if (changed >= Math.Max(12, _faces.Count / 34))
                break;
            if (pair.Count != 2 || _fixed.Contains(edge.Item1) || _fixed.Contains(edge.Item2))
                continue;
            var a = _faces[pair[0]];
            var b = _faces[pair[1]];
            if (a.Count != 4 || b.Count != 4 || !a.Contains(edge.Item1) || !a.Contains(edge.Item2) || !b.Contains(edge.Item1) || !b.Contains(edge.Item2))
                continue;
            var links = new Dictionary<int, int>();
            foreach (var f in new[]
            {
                a,
                b
            }

            )
                for (int i = 0; i < 4; i++)
                    if (Edge(f[i], f[(i + 1) % 4]) != edge)
                        links[f[i]] = f[(i + 1) % 4];
            if (links.Count != 6)
                continue;
            var ring = new List<int>
            {
                edge.Item1
            };
            for (int i = 1; i < 6; i++)
                ring.Add(links[ring[^1]]);
            int offset = random.Next(2) + 1;
            var cycle = Enumerable.Range(0, 6).Select(i => ring[(i + offset) % 6]).ToArray();
            _faces[pair[0]] = new()
            {
                cycle[0],
                cycle[1],
                cycle[2],
                cycle[3]
            };
            _faces[pair[1]] = new()
            {
                cycle[3],
                cycle[4],
                cycle[5],
                cycle[0]
            };
            var incident = Incident();
            var movable = ring.ToHashSet();
            for (int pass = 0; pass < 3; pass++)
                movable.UnionWith(movable.SelectMany(id => incident[id]).SelectMany(p => _faces[p]).ToArray());
=======
        foreach (var (edge, pair) in EdgeCells().OrderBy(_ => random.Next()).ToArray())
        {
            if (changed >= Math.Max(12, _faces.Count / 34)) break;
            if (pair.Count != 2 || _fixed.Contains(edge.Item1) || _fixed.Contains(edge.Item2)) continue;
            var a = _faces[pair[0]]; var b = _faces[pair[1]];
            if (a.Count != 4 || b.Count != 4 || !a.Contains(edge.Item1) || !a.Contains(edge.Item2) || !b.Contains(edge.Item1) || !b.Contains(edge.Item2)) continue;
            var links = new Dictionary<int, int>();
            foreach (var f in new[] { a, b }) for (int i = 0; i < 4; i++)
                if (Edge(f[i], f[(i + 1) % 4]) != edge) links[f[i]] = f[(i + 1) % 4];
            if (links.Count != 6) continue;
            var ring = new List<int> { edge.Item1 };
            for (int i = 1; i < 6; i++) ring.Add(links[ring[^1]]);
            int offset = random.Next(2) + 1;
            var cycle = Enumerable.Range(0, 6).Select(i => ring[(i + offset) % 6]).ToArray();
            _faces[pair[0]] = new() { cycle[0], cycle[1], cycle[2], cycle[3] };
            _faces[pair[1]] = new() { cycle[3], cycle[4], cycle[5], cycle[0] };
            var incident = Incident();
            var movable = ring.ToHashSet();
            for (int pass = 0; pass < 3; pass++) movable.UnionWith(movable.SelectMany(id => incident[id]).SelectMany(p => _faces[p]).ToArray());
>>>>>>> Stashed changes
            movable.ExceptWith(_fixed);
            var affected = movable.SelectMany(id => incident[id]).Distinct().ToArray();
            var saved = movable.ToDictionary(id => id, id => _vertices[id]);
            var neighbors = movable.ToDictionary(id => id, id => incident[id].SelectMany(p =>
            {
<<<<<<< Updated upstream
                var f = _faces[p];
                int i = f.IndexOf(id);
                return new[]
                {
                    f[(i + f.Count - 1) % f.Count],
                    f[(i + 1) % f.Count]
                };
=======
                var f = _faces[p]; int i = f.IndexOf(id); return new[] { f[(i + f.Count - 1) % f.Count], f[(i + 1) % f.Count] };
>>>>>>> Stashed changes
            }).Distinct().ToArray());
            for (int pass = 0; pass < 45; pass++)
            {
                var next = movable.ToDictionary(id => id, id => Vector2.Lerp(_vertices[id], neighbors[id].Aggregate(Vector2.Zero, (sum, n) => sum + _vertices[n]) / neighbors[id].Length, .55f));
<<<<<<< Updated upstream
                foreach (var(id, p)in next)
                    _vertices[id] = p;
            }

            if (affected.Any(p => !Valid(_faces[p])))
            {
                _faces[pair[0]] = a;
                _faces[pair[1]] = b;
                foreach (var(id, p)in saved)
                    _vertices[id] = p;
            }
            else
                changed++;
        }

        RowRedirects = changed;
    }

=======
                foreach (var (id, p) in next) _vertices[id] = p;
            }
            if (affected.Any(p => !Valid(_faces[p])))
            { _faces[pair[0]] = a; _faces[pair[1]] = b; foreach (var (id, p) in saved) _vertices[id] = p; }
            else changed++;
        }
        RowRedirects = changed;
    }
>>>>>>> Stashed changes
    private void ChangeTopology(Random random)
    {
        // Split four-way junctions into two three-way junctions. The cyclic face
        // order makes the new edge shared, with two true pentagons around it.
<<<<<<< Updated upstream
        var incident = Incident();
        int splits = 0;
        foreach (int id in _vertices.Keys.OrderBy(_ => random.Next()).ToArray())
        {
            if (_fixed.Contains(id) || incident[id].Count != 4 || splits >= _faces.Count / 32)
                continue;
            var cells = incident[id].OrderBy(p =>
            {
                var f = _faces[p];
                var c = f.Aggregate(Vector2.Zero, (a, v) => a + _vertices[v]) / f.Count - _vertices[id];
                return MathF.Atan2(c.Y, c.X);
            }).ToArray();
            if (cells.Any(p => _faces[p].Count != 4))
                continue;
            var saved = cells.ToDictionary(p => p, p => new List<int>(_faces[p]));
            var old = _vertices[id];
            int extra = _vertices.Count;
            var direction = _faces[cells[1]].Aggregate(Vector2.Zero, (a, v) => a + _vertices[v]) / 4 - old;
            var delta = Vector2.Normalize(direction) * .30f;
            _vertices[id] = old - delta;
            _vertices[extra] = old + delta;
            foreach (var p in cells)
            {
                var f = _faces[p];
                int at = f.IndexOf(id);
                if (p == cells[1])
                    f[at] = extra;
=======
        var incident = Incident(); int splits = 0;
        foreach (int id in _vertices.Keys.OrderBy(_ => random.Next()).ToArray())
        {
            if (_fixed.Contains(id) || incident[id].Count != 4 || splits >= _faces.Count / 32) continue;
            var cells = incident[id].OrderBy(p => { var f = _faces[p]; var c = f.Aggregate(Vector2.Zero, (a, v) => a + _vertices[v]) / f.Count - _vertices[id]; return MathF.Atan2(c.Y, c.X); }).ToArray();
            if (cells.Any(p => _faces[p].Count != 4)) continue;
            var saved = cells.ToDictionary(p => p, p => new List<int>(_faces[p]));
            var old = _vertices[id]; int extra = _vertices.Count;
            var direction = _faces[cells[1]].Aggregate(Vector2.Zero, (a, v) => a + _vertices[v]) / 4 - old;
            var delta = Vector2.Normalize(direction) * .30f;
            _vertices[id] = old - delta; _vertices[extra] = old + delta;
            foreach (var p in cells)
            {
                var f = _faces[p]; int at = f.IndexOf(id);
                if (p == cells[1]) f[at] = extra;
>>>>>>> Stashed changes
                else if (p == cells[0] || p == cells[2])
                {
                    // Choose the orientation whose edge follows this face CCW.
                    var prev = _vertices[f[(at + f.Count - 1) % f.Count]];
                    bool idFirst = Cross(_vertices[id] - prev, _vertices[extra] - _vertices[id]) > 0;
<<<<<<< Updated upstream
                    f.RemoveAt(at);
                    f.InsertRange(at, idFirst ? new[] { id, extra } : new[] { extra, id });
                }
            }

            if (cells.Any(p => !Valid(_faces[p])))
            {
                foreach (var p in cells)
                    _faces[p] = saved[p];
                _vertices[id] = old;
                _vertices.Remove(extra);
            }
            else
                splits++;
        }

        int collapses = 0;
        foreach (var edge in EdgeCells().Keys.OrderBy(_ => random.Next()).ToArray())
        {
            var(a, b) = edge;
            if (_fixed.Contains(a) || _fixed.Contains(b) || collapses >= _faces.Count / 64)
                continue;
            var cells = _faces.Where(f => f.Value.Contains(a) || f.Value.Contains(b)).Select(f => f.Key).ToArray();
            if (cells.Any(p => _faces[p].Count != 4))
                continue;
            var saved = cells.ToDictionary(p => p, p => new List<int>(_faces[p]));
            var old = _vertices[a];
            _vertices[a] = (_vertices[a] + _vertices[b]) / 2;
            foreach (var p in cells)
                _faces[p] = _faces[p].Select(id => id == b ? a : id).Distinct().ToList();
            if (cells.Any(p => !Valid(_faces[p])))
            {
                foreach (var p in cells)
                    _faces[p] = saved[p];
                _vertices[a] = old;
            }
            else
                collapses++;
        }
    }

    private void IndexTopology()
    {
        var incident = Incident();
        var edges = EdgeCells();
        foreach (var(p, f)in _faces)
=======
                    f.RemoveAt(at); f.InsertRange(at, idFirst ? new[] { id, extra } : new[] { extra, id });
                }
            }
            if (cells.Any(p => !Valid(_faces[p])))
            { foreach (var p in cells) _faces[p] = saved[p]; _vertices[id] = old; _vertices.Remove(extra); }
            else splits++;
        }
        int collapses = 0;
        foreach (var edge in EdgeCells().Keys.OrderBy(_ => random.Next()).ToArray())
        {
            var (a, b) = edge;
            if (_fixed.Contains(a) || _fixed.Contains(b) || collapses >= _faces.Count / 64) continue;
            var cells = _faces.Where(f => f.Value.Contains(a) || f.Value.Contains(b)).Select(f => f.Key).ToArray();
            if (cells.Any(p => _faces[p].Count != 4)) continue;
            var saved = cells.ToDictionary(p => p, p => new List<int>(_faces[p])); var old = _vertices[a];
            _vertices[a] = (_vertices[a] + _vertices[b]) / 2;
            foreach (var p in cells) _faces[p] = _faces[p].Select(id => id == b ? a : id).Distinct().ToList();
            if (cells.Any(p => !Valid(_faces[p]))) { foreach (var p in cells) _faces[p] = saved[p]; _vertices[a] = old; }
            else collapses++;
        }
    }
    private void IndexTopology()
    {
        var incident = Incident(); var edges = EdgeCells();
        foreach (var (p, f) in _faces)
>>>>>>> Stashed changes
        {
            _centers[p] = f.Aggregate(Vector2.Zero, (sum, id) => sum + _vertices[id]) / f.Count;
            _surrounding[p] = f.SelectMany(id => incident[id]).Where(q => q != p).Distinct().ToArray();
            _neighbors[p] = Enumerable.Range(0, f.Count).SelectMany(i => edges[Edge(f[i], f[(i + 1) % f.Count])]).Where(q => q != p).Distinct().ToArray();
        }
    }
<<<<<<< Updated upstream

    public IReadOnlyList<GridPosition> Neighbors(GridPosition p, bool diagonals = false) => (diagonals ? _surrounding : _neighbors).GetValueOrDefault(p) ?? Array.Empty<GridPosition>();
    public int Steps(GridPosition from, GridPosition to)
    {
        if (!_faces.ContainsKey(from) || !_faces.ContainsKey(to))
            return int.MaxValue / 100;
        if (from == to)
            return 0;
        if (_surrounding[from].Contains(to))
            return 1;
=======
    public IReadOnlyList<GridPosition> Neighbors(GridPosition p, bool diagonals = false) =>
        (diagonals ? _surrounding : _neighbors).GetValueOrDefault(p) ?? Array.Empty<GridPosition>();
    public int Steps(GridPosition from, GridPosition to)
    {
        if (!_faces.ContainsKey(from) || !_faces.ContainsKey(to)) return int.MaxValue / 100;
        if (from == to) return 0;
        if (_surrounding[from].Contains(to)) return 1;
>>>>>>> Stashed changes
        // The immutable mesh is undirected. Coverage checks from many candidate
        // cells towards the same ship can reuse the ship's existing search.
        if (!_distances.ContainsKey(from) && _distances.TryGetValue(to, out var reverse))
            return reverse.GetValueOrDefault(from, int.MaxValue / 100);
        if (!_distances.TryGetValue(from, out var distances))
        {
<<<<<<< Updated upstream
            distances = new()
            {
                [from] = 0
            };
            var pending = new Queue<GridPosition>();
            pending.Enqueue(from);
            while (pending.TryDequeue(out var p))
                foreach (var q in _surrounding[p])
                    if (distances.TryAdd(q, distances[p] + 1))
                        pending.Enqueue(q);
            _distances[from] = distances;
        }

        return distances.GetValueOrDefault(to, int.MaxValue / 100);
    }

    /// <summary>Exact unweighted corner-adjacency coverage. Optical/radar queries
    /// need a small neighborhood, not a full-board distance table for each origin.</summary>
    public IReadOnlyList<GridPosition> Within(GridPosition origin, int radius)
    {
        if (radius < 0 || !_faces.ContainsKey(origin))
            return Array.Empty<GridPosition>();
        var key = (origin, radius);
        if (_coverage.TryGetValue(key, out var cached))
            return cached;
        var distances = new Dictionary<GridPosition, int>
        {
            [origin] = 0
        };
        var pending = new Queue<GridPosition>();
        pending.Enqueue(origin);
        while (pending.TryDequeue(out var position))
        {
            int next = distances[position] + 1;
            if (next > radius)
                continue;
            foreach (var neighbor in _surrounding[position])
                if (distances.TryAdd(neighbor, next))
                    pending.Enqueue(neighbor);
        }

        var result = Array.AsReadOnly(distances.Keys.ToArray());
        _coverage.Add(key, result);
        return result;
    }
=======
            distances = new() { [from] = 0 }; var pending = new Queue<GridPosition>(); pending.Enqueue(from);
            while (pending.TryDequeue(out var p)) foreach (var q in _surrounding[p])
                if (distances.TryAdd(q, distances[p] + 1)) pending.Enqueue(q);
            _distances[from] = distances;
        }
        return distances.GetValueOrDefault(to, int.MaxValue / 100);
    }
>>>>>>> Stashed changes
}
