using System;
using Godot;

namespace DevAncientNaval.Presentation.Map;

/// <summary>Projects moving solid fragments, clipping their 3D faces at the sea surface.</summary>
internal sealed class WreckRenderer : System.Collections.Generic.IComparer<int>
{
    internal WreckPart[] Parts { get; }
    internal float Seconds { get; set; }
    internal bool Opaque { get; }
    internal int ImmersedFaces { get; private set; }
    private readonly float _yaw, _size, _width;
    private readonly bool _mother;
    private readonly Vector3[] _clipped = new Vector3[40];
    private readonly Vector2[] _triangle = new Vector2[3];
    private readonly Color[] _ink = new Color[1];
    private readonly VisibleFace[] _visible;
    private readonly int[] _order;
    private int _count;
    private sealed class VisibleFace
    {
        internal readonly Vector3[] Points = new Vector3[40];
        internal int Count;
        internal float Depth;
        internal Color Color;
    }

    internal WreckRenderer(WreckPart[] parts, float yaw, ShipVisualProfile profile, bool mother)
    {
        Parts = parts;
        _yaw = yaw;
        _size = profile.Size;
        _width = profile.DeckWidth;
        _mother = mother;
        int count = 0;
        Opaque = true;
        foreach (var part in parts)
            foreach (var face in part.Faces)
            {
                count++;
                Opaque &= face.Color.A == 1;
            }
        _visible = new VisibleFace[count];
        _order = new int[count];
        for (int i = 0; i < count; i++) _visible[i] = new();
    }

    private static Vector3 Rotate(Vector3 p, Vector3 angles)
    {
        float sx = MathF.Sin(angles.X), cx = MathF.Cos(angles.X);
        p = new(p.X, p.Y * cx - p.Z * sx, p.Y * sx + p.Z * cx);
        float sy = MathF.Sin(angles.Y), cy = MathF.Cos(angles.Y);
        p = new(p.X * cy + p.Z * sy, p.Y, -p.X * sy + p.Z * cy);
        float sz = MathF.Sin(angles.Z), cz = MathF.Cos(angles.Z);
        return new(p.X * cz - p.Y * sz, p.X * sz + p.Y * cz, p.Z);
    }
    private Vector2 Project(Vector3 p) => DeckProjection.Point(p.X, p.Y, p.Z, _yaw, _size, _width) + new Vector2(0, -5);
    private float Depth(Vector3 p) => DeckProjection.Point(p.X, p.Y, 0, _yaw, _size, _width).Y + p.Z * .15f;
    internal Transform3D Motion(WreckPart part)
    {
        float released = Math.Max(0, Seconds - part.Release);
        float keelProgress = Math.Clamp(Seconds * .7f, 0, 1);
        var bodyTilt = new Vector3((_mother ? .17f : .25f) * keelProgress, (_mother ? .07f : -.1f) * keelProgress, 0);
        var pivot = Rotate(part.Pivot, bodyTilt);
        var angles = bodyTilt + part.Spin * Math.Min(released, 1.4f);
        var basis = new Basis(Rotate(Vector3.Right, angles), Rotate(Vector3.Up, angles), Rotate(Vector3.Back, angles));
        float splash = part.Drift.Z * released - part.Sink * released * released;
        return new(basis, pivot - basis * part.Pivot + new Vector3(part.Drift.X * released, part.Drift.Y * released, splash));
    }
    internal Vector3 Position(WreckPart part, Vector3 vertex) => Motion(part) * vertex;
    public int Compare(int a, int b) => _visible[a].Depth.CompareTo(_visible[b].Depth);
    internal void Draw(CanvasItem canvas)
    {
        _count = 0;
        ImmersedFaces = 0;
        foreach (var part in Parts)
        {
            var motion = Motion(part);
            foreach (var face in part.Faces)
            {
                // Sutherland-Hodgman against physical sea height, before projection.
                // A falling mast can enter the water tip-first, with opaque exposed wood.
                int clipped = 0;
                for (int i = 0; i < face.Vertices.Length; i++)
                {
                    var a = motion * face.Vertices[i];
                    var b = motion * face.Vertices[(i + 1) % face.Vertices.Length];
                    bool inside = a.Z >= -4, next = b.Z >= -4;
                    if (inside) _clipped[clipped++] = a;
                    else ImmersedFaces++;
                    if (inside != next) _clipped[clipped++] = a.Lerp(b, (-4 - a.Z) / (b.Z - a.Z));
                }
                if (clipped < 3) continue;
                var normal = (_clipped[1] - _clipped[0]).Cross(_clipped[2] - _clipped[0]).Normalized();
                var a2 = Project(_clipped[0]);
                var b2 = Project(_clipped[1]);
                var c2 = Project(_clipped[2]);
                if (!face.TwoSided && (b2 - a2).Cross(c2 - a2) <= .025f) continue;
                var visible = _visible[_count];
                visible.Count = clipped;
                visible.Depth = 0;
                for (int i = 0; i < clipped; i++)
                {
                    visible.Points[i] = _clipped[i];
                    visible.Depth += Depth(_clipped[i]);
                }
                visible.Depth /= clipped;
                float light = Math.Clamp(normal.Dot(new Vector3(-.4f, -.5f, .85f).Normalized()), -.6f, 1);
                visible.Color = light >= 0 ? face.Color.Lightened(light * .12f) : face.Color.Darkened(-light * .26f);
                _order[_count] = _count;
                _count++;
            }
        }
        Array.Sort(_order, 0, _count, this);
        for (int n = 0; n < _count; n++)
        {
            var face = _visible[_order[n]];
            _ink[0] = face.Color;
            for (int i = 1; i < face.Count - 1; i++)
            {
                _triangle[0] = Project(face.Points[0]);
                _triangle[1] = Project(face.Points[i]);
                _triangle[2] = Project(face.Points[i + 1]);
                canvas.DrawPrimitive(_triangle, _ink, Array.Empty<Vector2>());
            }
        }
    }
}
