using System.Numerics;
using DevAncientNaval.Core.Grid;

namespace DevAncientNaval.Core.World;
public sealed record SavedVertex(int Id, float X, float Y);
public sealed record SavedFace(GridPosition Cell, int[] Vertices);
public sealed record SavedPoint(float X, float Y);
public sealed record SavedMesh(SavedVertex[] Vertices, SavedFace[] Faces, SavedPoint[] Boundary, int[] SideCounts, int Redirects);
public sealed partial class OrganicMesh
{
    internal SavedMesh Save() => new(_vertices.Select(v => new SavedVertex(v.Key, v.Value.X, v.Value.Y)).ToArray(), _faces.Select(f => new SavedFace(f.Key, f.Value.ToArray())).ToArray(), Boundary.Select(p => new SavedPoint(p.X, p.Y)).ToArray(), SideTileCounts.ToArray(), RowRedirects);
    internal static OrganicMesh Restore(SavedMesh data)
    {
        if (data.Boundary is null || data.SideCounts is null || data.Faces is null || data.Vertices is null || data.Boundary.Length != 6 || data.SideCounts.Length != 6 || data.Faces.Length is < 1 or > 10_000 || data.Vertices.Length is < 3 or > 20_000 || data.SideCounts.Any(n => n <= 0) || data.Redirects < 0)
            throw new ArgumentException("Unsupported saved mesh.");
        if (data.Boundary.Any(p => p is null || !float.IsFinite(p.X) || !float.IsFinite(p.Y)))
            throw new ArgumentException("Invalid mesh boundary.");
        var mesh = new OrganicMesh
        {
            Boundary = data.Boundary.Select(p => new Vector2(p.X, p.Y)).ToArray(),
            SideTileCounts = Array.AsReadOnly(data.SideCounts.ToArray()),
            RowRedirects = data.Redirects
        };
        foreach (var vertex in data.Vertices)
        {
            if (vertex is null || !float.IsFinite(vertex.X) || !float.IsFinite(vertex.Y) || !mesh._vertices.TryAdd(vertex.Id, new(vertex.X, vertex.Y)))
                throw new ArgumentException("Invalid mesh coordinates or duplicate vertex.");
        }

        foreach (var face in data.Faces)
        {
            if (face is null || face.Vertices is null || face.Vertices.Length is < 3 or > 5 || face.Vertices.Distinct().Count() != face.Vertices.Length || face.Vertices.Any(id => !mesh._vertices.ContainsKey(id)) || !mesh._faces.TryAdd(face.Cell, face.Vertices.ToList()))
                throw new ArgumentException("Invalid saved cell references.");
        }

        if (mesh._faces.Values.Any(f => !mesh.Valid(f)))
            throw new ArgumentException("Invalid saved cell geometry.");
        mesh.FindBoundaryVertices();
        mesh.IndexTopology();
        return mesh;
    }
}
