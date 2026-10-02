using System;
using System.Collections.Generic;
using System.Linq;
using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Units;
using DevAncientNaval.Core.World;
using Godot;
using DevAncientNaval.Presentation.Diagnostics;
using Side = DevAncientNaval.Core.Units.Side;

namespace DevAncientNaval.Presentation.Map;
/// <summary>Quiet, bounded environmental animation. The static tile layer is never
/// redrawn for these effects, and wildlife only uses already visible water.</summary>
public partial class WorldAmbience : Node2D
{
    public BoardView BoardView { get; set; } = null !;

    private BattleState? _battle;
    private GridPosition[] _water = Array.Empty<GridPosition>();
    private GridPosition[] _fish = Array.Empty<GridPosition>();
    private Village[] _towns = Array.Empty<Village>();
    private Rect2 _seaBounds;
    private readonly List<ShoreWave> _shores = new();
    private readonly HashSet<GridPosition> _visibleWater = new();
    private GridPosition[] _harborWater = Array.Empty<GridPosition>();
    private (int Id, Vector2 Center)[] _docks = Array.Empty<(int, Vector2)>();
    private readonly SeaGeometryBatch _wavesBatch = new();
    private readonly Vector2[] _waterWave = new Vector2[4];
    private readonly Vector2[] _gullWings = new Vector2[5];
    private Rect2 _drawBounds;
    private sealed class ShoreWave
    {
        public Vector2[] Edge { get; }
        public Vector2 Land { get; }
        public Vector2[] Directions { get; }
        public Vector2[] Points { get; }

        public ShoreWave(Vector2[] edge, Vector2 land)
        {
            Edge = edge;
            Land = land;
            Directions = new Vector2[edge.Length];
            Points = new Vector2[edge.Length];
            for (int i = 0; i < edge.Length; i++)
            {
                Directions[i] = (edge[i] - land).Normalized();
            }
        }
    }

    private readonly List<Gull> _gulls = new();
    private readonly List<Dolphin> _dolphins = new();
    private Random _random = new(1701);
    private float _time, _frame, _nextGull = 2, _nextDolphin = 48;
    private sealed record Gull(Vector2 Start, Vector2 Velocity, float Born, float Phase, float Lifetime, bool Circling);
    private sealed record Dolphin(Vector2 Center, float Born, float Facing);
    internal int WildlifeCount => _gulls.Count + _dolphins.Count;

    public void RefreshVisibility()
    {
        using var trace = PerformanceTrace.Measure("Ambience.Refresh");
        var battle = BoardView.Battle;
        if (!ReferenceEquals(_battle, battle))
        {
            _battle = battle;
            _gulls.Clear();
            _dolphins.Clear();
            _time = 0;
            _seaBounds = BoardView.Projection.BoardBounds(battle.Board);
            _random = new Random(battle.Board.Seed ^ 31771);
            _nextGull = 2;
            _nextDolphin = 48;
        }

        _water = battle.Board.Tiles.Where(t => t.Terrain != TerrainType.Land && battle.Vision.IsVisible(Side.Player, t.Position)).Select(t => t.Position).ToArray();
        RefreshFishSchools();
        _fish = battle.KnownFish(Side.Player).Concat(battle.KnownShoals(Side.Player)).ToArray();
        _towns = battle.ObservedVillages(Side.Player).ToArray();
        _visibleWater.Clear();
        _visibleWater.UnionWith(_water);
        _harborWater = _towns.SelectMany(town => battle.Board.GetSurrounding(town.Position)).Where(_visibleWater.Contains).Distinct().ToArray();
        _docks = battle.ObservedShips(Side.Player).Where(ship => ship.Definition.Class == ShipClass.FishingDock).Select(ship => (ship.Id, BoardView.Projection.GridToWorld(ship.Position))).ToArray();
        _shores.Clear();
        foreach (var(edge, inside)in BoardView.VisibleShoreSegments())
        {
            var mid = edge[edge.Length / 2];
            var outside = mid + (mid - inside).Normalized() * 4;
            var p = BoardView.Projection.WorldToGrid(outside);
            if (battle.Board.Contains(p) && battle.Board.GetTile(p).Terrain != TerrainType.Land && battle.Vision.IsVisible(Side.Player, p))
                _shores.Add(new ShoreWave(edge, inside));
        }

        QueueRedraw();
    }

    public override void _Process(double delta)
    {
        if (_battle is null)
            return;
        _time += (float)delta;
        _gulls.RemoveAll(g => _time - g.Born > g.Lifetime);
        _dolphins.RemoveAll(d => _time - d.Born > 3.2f);
        if (_time >= _nextGull)
        {
            _nextGull = _time + 4 + (float)_random.NextDouble() * 3;
            if (_water.Length > 0 && _gulls.Count < 18)
            {
                var candidates = _towns.Length > 0 && _harborWater.Length > 0 && _random.Next(3) == 0 ? _harborWater : _fish.Length > 0 && _random.Next(10) != 0 ? _fish : _water;
                SpawnGulls(candidates[_random.Next(candidates.Length)], _random.Next(3) == 0 ? 3 : 1);
            }
        }

        if (_time >= _nextDolphin)
        {
            _nextDolphin = _time + 65 + _random.Next(50);
            if (_water.Length > 0 && _dolphins.Count == 0)
                SpawnDolphin(_water[_random.Next(_water.Length)]);
        }

        _frame += (float)delta;
        if (_frame < 1f / 30)
            return;
        _frame = 0;
        BoardView.AnimateTowns();
        QueueRedraw();
    }

    internal void SpawnGulls(GridPosition cell, int count)
    {
        if (!_visibleWater.Contains(cell))
            return;
        float angle = (float)_random.NextDouble() * Mathf.Tau;
        var velocity = Vector2.FromAngle(angle) * new Vector2(10, 5);
        for (int i = 0; i < Math.Min(count, 18 - _gulls.Count); i++)
            _gulls.Add(new(BoardView.Projection.GridToWorld(cell) + new Vector2(-i * 13, i * 6), velocity, _time, i * 1.8f, 24, _fish.Contains(cell)));
    }

    internal void SpawnDolphin(GridPosition cell)
    {
        if (_visibleWater.Contains(cell) && _dolphins.Count == 0)
            _dolphins.Add(new(BoardView.Projection.GridToWorld(cell), _time, _random.Next(2) == 0 ? -1 : 1));
    }

    private bool VisibleWater(Vector2 point)
    {
        var p = BoardView.Projection.WorldToGrid(point);
        return _battle!.Board.Contains(p) && _battle.Board.GetTile(p).Terrain != TerrainType.Land && _battle.Vision.IsVisible(Side.Player, p);
    }

    public override void _Draw()
    {
        using var trace = PerformanceTrace.Measure("Ambience.Draw");
        if (_battle is null)
            return;
        var inverse = GetGlobalTransformWithCanvas().AffineInverse();
        var viewport = GetViewportRect();
        _drawBounds = new Rect2(inverse * viewport.Position, Vector2.Zero);
        _drawBounds = _drawBounds.Expand(inverse * new Vector2(viewport.End.X, viewport.Position.Y));
        _drawBounds = _drawBounds.Expand(inverse * viewport.End);
        _drawBounds = _drawBounds.Expand(inverse * new Vector2(viewport.Position.X, viewport.End.Y)).Grow(150);
        using (PerformanceTrace.Measure("Ambience.Waves"))
        {
            _wavesBatch.Clear();
            foreach (var cell in _water)
            {
                if ((cell.X * 7 + cell.Y * 13) % 4 != 0)
                    continue;
                float phase = (_time * .17f + cell.X * .31f + cell.Y * .19f) % 1;
                var center = BoardView.Projection.GridToWorld(cell) + new Vector2(0, (phase - .5f) * 7);
                if (!_drawBounds.HasPoint(center))
                    continue;
                float alpha = MathF.Sin(phase * Mathf.Pi) * .085f;
                _waterWave[0] = center + new Vector2(-11, 1);
                _waterWave[1] = center + new Vector2(-4, -1);
                _waterWave[2] = center + new Vector2(4, -1);
                _waterWave[3] = center + new Vector2(11, 1);
                _wavesBatch.Polyline(_waterWave, new Color(.75f, .9f, .91f, alpha));
            }

            foreach (var shore in _shores)
            {
                if (!_drawBounds.HasPoint(shore.Edge[shore.Edge.Length / 2]))
                    continue;
                var land = shore.Land;
                for (int wave = 0; wave < 2; wave++)
                {
                    float phase = (_time * .27f + land.X * .001f + wave * .5f) % 1;
                    for (int i = 0; i < shore.Edge.Length; i++)
                    {
                        shore.Points[i] = shore.Edge[i] + shore.Directions[i] * (1 - phase) * 11;
                    }

                    _wavesBatch.Polyline(shore.Points, new Color(.86f, .94f, .88f, MathF.Sin(phase * Mathf.Pi) * .23f));
                }
            }
            _wavesBatch.Submit(this, 1.2f);
        }

        using (PerformanceTrace.Measure("Ambience.Fish"))
            DrawFishSchools();
        _sky?.QueueRedraw();
        DrawWhirlpools();
        DrawClouds(this, shadows: true);
        DrawGulls(this, shadows: true);
        foreach (var dolphin in _dolphins)
        {
            float phase = (_time - dolphin.Born) / 3.2f;
            var water = dolphin.Center + new Vector2((phase - .5f) * 25 * dolphin.Facing, 0);
            if (!_drawBounds.HasPoint(water) || !VisibleWater(water))
                continue;
            float jump = MathF.Sin(phase * Mathf.Pi);
            var body = water + new Vector2(0, -jump * 12);
            DrawSetTransform(water, 0, new Vector2(1, .4f));
            DrawArc(Vector2.Zero, 4 + phase * 21, 0, Mathf.Tau, 24, new Color(.8f, .94f, .94f, (1 - phase) * .24f), 1, true);
            DrawSetTransform(Vector2.Zero);
            // At emergence/submergence the silhouette collapses to a point.
            // Keep its water ripple, but do not submit a degenerate polygon.
            if (jump < .04f)
                continue;
            Vector2 P(float x, float y) => body + new Vector2(x * dolphin.Facing, y) * jump;
            DrawColoredPolygon(new[] { P(-9, 2), P(-5, -3), P(1, -4), P(8, -1), P(12, 1), P(6, 2), P(-4, 2), P(-11, 5) }, new Color("82a8af"));
            DrawColoredPolygon(new[] { P(-2, -3), P(0, -8), P(4, -3) }, new Color("aac3c5"));
        }
    }

    private void SetGullWings(Vector2 bird, float wing, float span, float bend)
    {
        _gullWings[0] = bird + new Vector2(-span, wing);
        _gullWings[1] = bird + new Vector2(-bend, -1);
        _gullWings[2] = bird;
        _gullWings[3] = bird + new Vector2(bend, -1);
        _gullWings[4] = bird + new Vector2(span, wing);
    }

    internal void DrawTownLife(Node2D canvas, Village town, Vector2 center)
    {
        foreach (var mill in BoardView.TownMills(town))
        {
            var hub = center + mill;
            float rotation = _time * .48f + town.Id;
            for (int i = 0; i < 4; i++)
            {
                var axis = Vector2.FromAngle(rotation + i * Mathf.Pi / 2);
                var side = axis.Orthogonal() * 2;
                canvas.DrawColoredPolygon(new[] { hub + axis * 2, hub + axis * 11, hub + axis * 10 + side, hub + axis * 4 + side }, new Color("ebe1bd"));
            }
            canvas.DrawCircle(hub, 2.2f, new Color("807858"));
        }
        if (town.Owner is null)
            return;
        var flag = center + BoardView.VillageFlagOffset;
        var color = FleetPalette.For(BoardView.Battle, town.Owner);
        var cloth = VillageFlagCloth(flag, _time, town.Id);
        canvas.DrawColoredPolygon(cloth, color);
        if (town.Owner == Side.Pirates)
        {
            var emblem = flag + new Vector2(6, 4);
            canvas.DrawCircle(emblem, 1.7f, new Color("e7dfca"));
            canvas.DrawLine(emblem + new Vector2(-2, 3), emblem + new Vector2(3, 6), new Color("e7dfca"), .8f, true);
            canvas.DrawLine(emblem + new Vector2(3, 3), emblem + new Vector2(-2, 6), new Color("e7dfca"), .8f, true);
        }
    }

    internal static Vector2[] VillageFlagCloth(Vector2 flag, float time, int townId)
    {
        var cloth = new Vector2[10];
        for (int i = 0; i < 5; i++)
        {
            float x = i * 4;
            float sway = MathF.Sin(time * 3 - i * .65f + townId) * i * .7f;
            // The first column is fixed to the exact pole; only the free edge waves.
            cloth[i] = flag + new Vector2(x, sway);
            cloth[9 - i] = flag + new Vector2(x, 10 + sway - i * .6f);
        }

        return cloth;
    }
}
