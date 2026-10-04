using System;
using System.Collections.Generic;
using System.Linq;
using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Grid;
using Godot;
using Side = DevAncientNaval.Core.Units.Side;

namespace DevAncientNaval.Presentation.Map;

public partial class WorldAmbience
{
    private readonly RetainedTreasuryGlow _treasuryGlow = new() { Name = "TreasuryLight", ZIndex = 1 };
    private readonly Dictionary<int, bool> _seenTreasuryCollections = new();
    private readonly Dictionary<int, float> _treasuryFades = new();
    private BattleState? _treasuryGlowBattle;
    internal int TreasureGlowBuildCount => _treasuryGlow.BuildCount;
    internal int TreasureGlowVertexCount => _treasuryGlow.VertexCount;
    internal int ActiveTreasurePulses => _treasuryFades.Count(p => _time - p.Value < .75f);

    private void RefreshTreasuryGlow()
    {
        if (!ReferenceEquals(_treasuryGlowBattle, _battle))
        {
            _treasuryGlowBattle = _battle;
            _seenTreasuryCollections.Clear();
            _treasuryFades.Clear();
        }
        foreach (var old in _treasuryFades.Where(p => _time - p.Value >= .75f).Select(p => p.Key).ToArray())
            _treasuryFades.Remove(old);
        var glows = new List<(Vector2 Center, float Fade)>();
        foreach (var treasury in _battle!.ObservedTreasuryRuins(Side.Player))
        {
            if (treasury.IsCollected && _seenTreasuryCollections.TryGetValue(treasury.Id, out bool collected) && !collected)
                _treasuryFades.TryAdd(treasury.Id, _time);
            _seenTreasuryCollections[treasury.Id] = treasury.IsCollected;
            bool ready = !treasury.IsCollected;
            if (_treasuryFades.TryGetValue(treasury.Id, out float fade))
                glows.Add((BoardView.Projection.GridToWorld(treasury.Position), fade));
            else if (ready)
                glows.Add((BoardView.Projection.GridToWorld(treasury.Position), -1));
        }
        _treasuryGlow.Rebuild(glows);
        _treasuryGlow.SetClock(_time);
    }

    // Optional ceremony adapter. Automatic observed collection transitions
    // already call the same effect once, so retries cannot restart the pulse.
    internal void TreasuryCollected(GridPosition cell)
    {
        var treasury = _battle?.ObservedTreasuryRuins(Side.Player).FirstOrDefault(t => t.Position == cell && t.IsCollected);
        if (treasury is null || _treasuryFades.ContainsKey(treasury.Id)) return;
        _treasuryFades[treasury.Id] = _time;
        RefreshTreasuryGlow();
    }
}

internal partial class RetainedTreasuryGlow : MeshInstance2D
{
    private readonly ShaderMaterial _light = new()
    {
        Shader = new Shader { Code = """
            shader_type canvas_item;
            render_mode unshaded;
            uniform float light_clock = 0.0;
            varying vec4 ray_color;
            void vertex() {
                float age = max(0.0, light_clock - UV.x);
                float fade = UV.y > 0.5 ? max(0.0, 1.0 - age / 0.75) * (0.9 + 0.22 * sin(age * 23.0))
                    : 0.8 + 0.16 * sin(light_clock * 1.7 + VERTEX.x * 0.003);
                ray_color = vec4(COLOR.rgb, COLOR.a * fade);
            }
            void fragment() { COLOR = ray_color; }
            """ }
    };
    private string _key = "";
    internal int BuildCount { get; private set; }
    internal int VertexCount { get; private set; }

    internal void Rebuild(IReadOnlyList<(Vector2 Center, float Fade)> glows)
    {
        // Inputs change on a visibility/ready/collection transition; panning
        // and the clock never touch the retained mesh or its GPU buffers.
        string key = string.Join(';', glows.Select(g => $"{g.Center.X:R},{g.Center.Y:R},{g.Fade:R}"));
        if (_key == key) return;
        _key = key;
        var vertices = new List<Vector2>();
        var phases = new List<Vector2>();
        var colors = new List<Color>();
        var indices = new List<int>();
        foreach (var glow in glows)
            for (int ray = -3; ray <= 3; ray++)
            {
                float x = ray * 4;
                float height = 28 + (3 - Math.Abs(ray)) * 9;
                var bottom = glow.Center + new Vector2(x, -2 + Math.Abs(ray) * .45f);
                var top = bottom + new Vector2(ray * 1.5f, -height);
                int at = vertices.Count;
                vertices.Add(bottom + new Vector2(-2.3f, 0)); vertices.Add(bottom + new Vector2(2.3f, 0));
                vertices.Add(top + new Vector2(-.8f, 0)); vertices.Add(top + new Vector2(.8f, 0));
                for (int i = 0; i < 4; i++)
                {
                    phases.Add(new Vector2(Math.Max(0, glow.Fade), glow.Fade >= 0 ? 1 : 0));
                    colors.Add(new Color(.94f, .88f, .60f, i < 2 ? .25f : 0));
                }
                indices.Add(at); indices.Add(at + 1); indices.Add(at + 2);
                indices.Add(at + 1); indices.Add(at + 3); indices.Add(at + 2);
            }
        var old = Mesh;
        VertexCount = vertices.Count;
        if (vertices.Count == 0) Mesh = null;
        else
        {
            var arrays = new Godot.Collections.Array(); arrays.Resize((int)Godot.Mesh.ArrayType.Max);
            arrays[(int)Godot.Mesh.ArrayType.Vertex] = vertices.ToArray();
            arrays[(int)Godot.Mesh.ArrayType.TexUV] = phases.ToArray();
            arrays[(int)Godot.Mesh.ArrayType.Color] = colors.ToArray();
            arrays[(int)Godot.Mesh.ArrayType.Index] = indices.ToArray();
            var mesh = new ArrayMesh(); mesh.AddSurfaceFromArrays(Godot.Mesh.PrimitiveType.Triangles, arrays);
            Mesh = mesh;
        }
        old?.Dispose();
        Material = _light;
        BuildCount++;
    }
    internal void SetClock(float clock) => _light.SetShaderParameter("light_clock", clock);
}
