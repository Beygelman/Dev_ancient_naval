using System;
using System.Collections.Generic;
using Godot;

namespace DevAncientNaval.Presentation.Map;

/// <summary>Visible water only. Geometry changes with discoveries; the GPU moves
/// retained soft-edged strips without rebuilding/uploading them each frame.</summary>
internal partial class RetainedSeaWaves : MeshInstance2D
{
    private readonly List<Vector2> _vertices = new(), _phases = new();
    private readonly List<Color> _motion = new();
    private readonly List<int> _indices = new();
    private readonly ShaderMaterial _waterMaterial = new()
    {
        Shader = new Shader { Code = """
            shader_type canvas_item;
            render_mode unshaded;
            uniform float wave_clock = 0.0;
            varying float wave_alpha;
            varying vec3 wave_color;
            void vertex() {
                bool shore = UV.y > 0.5;
                float phase = fract(wave_clock * (shore ? 0.27 : 0.17) + UV.x);
                vec2 direction = COLOR.rg * 2.0 - 1.0;
                VERTEX += direction * (shore ? (1.0 - phase) * 11.0 : (phase - 0.5) * 7.0);
                wave_alpha = COLOR.b * sin(phase * 3.14159265) * (shore ? 0.23 : 0.085);
                wave_color = shore ? vec3(0.86, 0.94, 0.88) : vec3(0.75, 0.90, 0.91);
            }
            void fragment() { COLOR = vec4(wave_color, wave_alpha); }
            """ }
    };
    internal int BuildCount { get; private set; }
    internal int VertexCount => _vertices.Count;
    internal void Begin()
    {
        _vertices.Clear(); _phases.Clear(); _motion.Clear(); _indices.Clear();
    }
    internal void Segment(Vector2 a, Vector2 b, Vector2 driftA, Vector2 driftB, float phase, bool shore)
    {
        var delta = b - a;
        if (delta.LengthSquared() < .000001f) return;
        var normal = delta.Normalized().Orthogonal();
        int first = _vertices.Count;
        for (int band = 0; band < 4; band++)
        {
            float width = band switch { 0 => -1.25f, 1 => -.6f, 2 => .6f, _ => 1.25f };
            float fringe = band is 0 or 3 ? 0 : 1;
            Vertex(a + normal * width, driftA, fringe);
            Vertex(b + normal * width, driftB, fringe);
        }
        for (int band = 0; band < 3; band++)
        {
            int offset = first + band * 2;
            _indices.Add(offset); _indices.Add(offset + 1); _indices.Add(offset + 2);
            _indices.Add(offset + 1); _indices.Add(offset + 3); _indices.Add(offset + 2);
        }
        void Vertex(Vector2 p, Vector2 drift, float fringe)
        {
            _vertices.Add(p);
            _phases.Add(new Vector2(phase, shore ? 1 : 0));
            _motion.Add(new Color(drift.X * .5f + .5f, drift.Y * .5f + .5f, fringe, 1));
        }
    }
    internal void Finish()
    {
        var previous = Mesh;
        if (_vertices.Count == 0) Mesh = null;
        else
        {
            var arrays = new Godot.Collections.Array();
            arrays.Resize((int)Godot.Mesh.ArrayType.Max);
            arrays[(int)Godot.Mesh.ArrayType.Vertex] = _vertices.ToArray();
            arrays[(int)Godot.Mesh.ArrayType.TexUV] = _phases.ToArray();
            arrays[(int)Godot.Mesh.ArrayType.Color] = _motion.ToArray();
            arrays[(int)Godot.Mesh.ArrayType.Index] = _indices.ToArray();
            var mesh = new ArrayMesh();
            mesh.AddSurfaceFromArrays(Godot.Mesh.PrimitiveType.Triangles, arrays);
            Mesh = mesh;
        }
        previous?.Dispose();
        Material = _waterMaterial;
        BuildCount++;
    }
    internal void SetClock(float clock) => _waterMaterial.SetShaderParameter("wave_clock", clock);
}
