using System;
using System.Collections.Generic;
using System.Linq;
using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Navigation;
using DevAncientNaval.Core.Units;
using Godot;
using Side = DevAncientNaval.Core.Units.Side;

namespace DevAncientNaval.Presentation.Map;

public partial class BoardView
{
    internal Vector2 TradePortAnchor(Village town) => PortAnchor(town);
}

/// <summary>Decorative traffic uses the existing owned sea-lane graph, never simulation RNG or ship commands.</summary>
internal partial class TradeTraffic : Node2D
{
    internal const int MaximumBoats = 64;
    internal const float Speed = 12;
    internal BoardView Board { get; set; } = null!;
    private BattleState? _battle;
    private TradeNetwork? _network;
    private IsometricProjection? _projection;
    private Random _random = new(9149);
    private readonly Dictionary<int, float> _departures = new();
    private readonly Dictionary<GridPosition, List<GridPosition>> _adjacency = new();
    private readonly List<MerchantVoyage> _boats = new();
    private Village[] _ports = Array.Empty<Village>();
    private float _time;
    private int _skinOffset;
    internal int BoatCount => _boats.Count;
    internal int Departures { get; private set; }
    internal IReadOnlyList<int> Skins => _boats.Select(b => b.Canvas.Skin).ToArray();
    internal IReadOnlyList<(int Source, int Destination)> Voyages => _boats.Select(b => (b.Source,b.Destination)).ToArray();
    internal IReadOnlyList<Vector2> Positions => _boats.Select(b => b.Canvas.Position).ToArray();

    private sealed class MerchantVoyage
    {
        internal MerchantCanvas Canvas = null!;
        internal Vector2[] Points = Array.Empty<Vector2>();
        internal float[] Lengths = Array.Empty<float>();
        internal float Distance;
        internal int Segment, Source, Destination;
        internal GridPosition[] Cells = Array.Empty<GridPosition>();
    }

    public override void _Ready()
    {
        VisibilityChanged += () => SetProcess(IsVisibleInTree() && (_ports.Length>=2 || _boats.Count>0));
        SetProcess(false);
    }

    internal void Refresh()
    {
        var battle = Board.Battle;
        if (!ReferenceEquals(_battle,battle) || !ReferenceEquals(_projection,Board.Projection))
        {
            foreach (var boat in _boats) boat.Canvas.QueueFree();
            _boats.Clear(); _departures.Clear();
            _time=0; Departures=0;
            _random=new Random(battle.Board.Seed ^ 9149);
            _skinOffset=_random.Next(3);
            _battle=battle; _projection=Board.Projection; _network=null;
        }
        _ports=battle.Villages.Where(v => v.Owner==Side.Player && v.HasPort && v.Health>0).OrderBy(v=>v.Id).ToArray();
        var ids=_ports.Select(v=>v.Id).ToHashSet();
        foreach (int id in _departures.Keys.Where(id=>!ids.Contains(id)).ToArray()) _departures.Remove(id);
        foreach (var port in _ports)
            if (!_departures.ContainsKey(port.Id)) _departures[port.Id]=_time+Interval();
        var network=battle.TradeRoutes(Side.Player);
        if (!ReferenceEquals(_network,network))
        {
            _network=network; _adjacency.Clear();
            foreach (var edge in network.Edges)
            {
                if (!_adjacency.TryGetValue(edge.Item1,out var first)) _adjacency[edge.Item1]=first=new();
                if (!_adjacency.TryGetValue(edge.Item2,out var second)) _adjacency[edge.Item2]=second=new();
                first.Add(edge.Item2); second.Add(edge.Item1);
            }
            // Staged command restoration clears the Core cache even when every route is unchanged.
            // Preserve those voyages; remove only boats whose port or actual lane disappeared.
            for (int index = _boats.Count - 1; index >= 0; index--)
            {
                var boat = _boats[index];
                bool valid = ids.Contains(boat.Source) && ids.Contains(boat.Destination);
                for (int cell = 1; valid && cell < boat.Cells.Length; cell++)
                    valid = _adjacency.TryGetValue(boat.Cells[cell - 1], out var adjacent)
                        && adjacent.Contains(boat.Cells[cell]);
                if (valid) continue;
                boat.Canvas.QueueFree();
                _boats.RemoveAt(index);
            }
        }
        SetProcess(IsVisibleInTree() && (_ports.Length>=2 || _boats.Count>0));
    }

    private float Interval() => 12+(float)_random.NextDouble()*8;

    public override void _Process(double delta)
    {
        if (!IsVisibleInTree() || _battle is null) return;
        Advance((float)delta);
    }

    internal void Advance(float seconds)
    {
        _time+=seconds;
        foreach (var port in _ports)
            if (_departures[port.Id]<=_time)
            {
                _departures[port.Id]=_time+Interval();
                if (_boats.Count<MaximumBoats) Depart(port);
            }
        for (int index=_boats.Count-1;index>=0;index--)
        {
            var boat=_boats[index];
            boat.Distance+=seconds*Speed;
            while (boat.Segment<boat.Lengths.Length && boat.Distance>=boat.Lengths[boat.Segment])
                boat.Distance-=boat.Lengths[boat.Segment++];
            if (boat.Segment>=boat.Lengths.Length)
            {
                boat.Canvas.QueueFree(); _boats.RemoveAt(index); continue;
            }
            var first=boat.Points[boat.Segment]; var second=boat.Points[boat.Segment+1];
            var at=first.Lerp(second,boat.Distance/boat.Lengths[boat.Segment]);
            boat.Canvas.Position=at;
            boat.Canvas.SetHeading((second-first).Angle());
            var cell=Board.Projection.WorldToGrid(at);
            boat.Canvas.Visible=Board.Board.Contains(cell) && Board.Battle.Vision.IsVisible(Side.Player,cell);
        }
    }

    private void Depart(Village source)
    {
        var possibilities=_ports.Where(p=>p.Id!=source.Id).OrderBy(_=>_random.Next()).ToArray();
        foreach (var destination in possibilities)
        {
            var cells=Route(_battle!.PortBerth(source),_battle.PortBerth(destination));
            if (cells is null) continue;
            var points=Lane(cells,Board.TradePortAnchor(source),Board.TradePortAnchor(destination));
            var lengths=points.Zip(points.Skip(1),(a,b)=>a.DistanceTo(b)).ToArray();
            if (lengths.Length==0 || lengths.Any(length=>length<.001f)) continue;
            var canvas=new MerchantCanvas { Name="Merchant"+Departures, Skin=(_skinOffset+Departures)%3, Position=points[0] };
            AddChild(canvas);
            _boats.Add(new MerchantVoyage { Canvas=canvas,Points=points,Lengths=lengths,Source=source.Id,
                Destination=destination.Id,Cells=cells });
            Departures++;
            return;
        }
    }

    private GridPosition[]? Route(GridPosition start,GridPosition finish)
    {
        // A chain of nearby ports does not create a direct long-distance voyage.
        if (_battle!.Rules.Ports.MaximumRouteLength > 0)
        {
            var direct = _network!.Routes.FirstOrDefault(route =>
                route[0] == start && route[^1] == finish || route[0] == finish && route[^1] == start);
            return direct is null ? null : (direct[0] == start ? direct.ToArray() : direct.Reverse().ToArray());
        }
        if (!_adjacency.ContainsKey(start) || !_adjacency.ContainsKey(finish)) return null;
        var parents=new Dictionary<GridPosition,GridPosition>();
        var visited=new HashSet<GridPosition> { start };
        var queue=new Queue<GridPosition>(); queue.Enqueue(start);
        while (queue.Count>0)
        {
            var current=queue.Dequeue();
            if (current==finish)
            {
                var path=new List<GridPosition> { finish };
                while (path[^1]!=start) path.Add(parents[path[^1]]);
                path.Reverse(); return path.ToArray();
            }
            foreach (var next in _adjacency[current])
                if (visited.Add(next)) { parents[next]=current; queue.Enqueue(next); }
        }
        return null;
    }

    private Vector2[] Lane(GridPosition[] cells,Vector2 fromPort,Vector2 toPort)
    {
        var basePoints=cells.Select(Board.Projection.GridToWorld).ToArray();
        var points=new List<Vector2> { fromPort };
        void Add(Vector2 p) { if (points[^1].DistanceSquaredTo(p)>.002f) points.Add(p); }
        Add(basePoints[0]);
        Vector2 P(int i)=>basePoints[Math.Clamp(i,0,basePoints.Length-1)];
        for (int segment=0;segment<basePoints.Length-1;segment++)
            for (int sample=1;sample<=18;sample++)
            {
                float t=sample/18f;
                var point=P(segment).CubicInterpolate(P(segment+1),P(segment-1),P(segment+2),t);
                var at=Board.Projection.WorldToGrid(point);
                if (!Board.Board.Contains(at) || Board.Board.GetTile(at).Terrain==Core.World.TerrainType.Land)
                    point=P(segment).Lerp(P(segment+1),t);
                var ray=(P(segment+1)-P(segment)).Normalized();
                var bend=point+ray.Orthogonal()*MathF.Sin(t*Mathf.Tau)*2.2f;
                at=Board.Projection.WorldToGrid(bend);
                if (Board.Board.Contains(at) && Board.Board.GetTile(at).Terrain!=Core.World.TerrainType.Land) point=bend;
                Add(point);
            }
        Add(toPort);
        return points.ToArray();
    }
}

internal partial class MerchantCanvas : Node2D
{
    internal int Skin { get; init; }
    private readonly Node2D _hull=new();
    public override void _Ready()
    {
        var hull=new BoardTerrainLayer { DrawWorld=canvas=>DrawHull(canvas) };
        _hull.AddChild(hull); AddChild(_hull);
    }
    internal void SetHeading(float radians) => _hull.Rotation=radians;
    private void DrawHull(CanvasItem canvas)
    {
        canvas.DrawColoredPolygon(new[] { new Vector2(-8,0),new(-5,-3),new(5,-2.6f),new(9,0),new(5,3),new(-5,3) },new Color(Skin==2?"76503b":"ab8c58"));
        canvas.DrawLine(new(-6,-.5f),new(6,-.5f),new Color("ecdcac"),1,true);
        if (Skin==2)
            for(int box=0;box<3;box++) canvas.DrawRect(new Rect2(-5+box*3,0,2,2),new Color("cdb582"));
        canvas.DrawPolyline(new[] { new Vector2(-9,-2),new(-13,-1),new(-16,-1) },new Color(.83f,.93f,.91f,.23f),.7f,true);
    }
    public override void _Draw()
    {
        var sail=new Color(Skin==0?"f4e6bd":Skin==1?"dec9a1":"d6b3a0");
        DrawLine(new(0,1),new(0,-11),new Color("765e3d"),.85f,true);
        if (Skin==1)
        {
            DrawColoredPolygon(new[] { new Vector2(-1,-10),new(-6,-1),new(-1,-2) },sail);
            DrawColoredPolygon(new[] { new Vector2(1,-9),new(5,-3),new(1,-2) },sail.Lightened(.12f));
        }
        else
        {
            DrawColoredPolygon(new[] { new Vector2(1,-10),new(1,-2),new(6,-3) },sail);
            if (Skin==2) DrawLine(new(1,-6),new(4,-6),new Color("783541"),1,true);
        }
    }
}
