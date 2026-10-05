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
    private readonly HashSet<GridPosition> _hazards = new();
    private Vector2[] _beacons = Array.Empty<Vector2>();
    private static readonly Vector2 BeaconClearance = new(28, 19);
    private Village[] _ports = Array.Empty<Village>();
    private float _time;
    private int _skinOffset;
    internal int BoatCount => _boats.Count;
    internal int Departures { get; private set; }
    internal int Arrivals { get; private set; }
    internal int? LastArrivalCity { get; private set; }
    internal int RouteRepairs { get; private set; }
    internal float ElapsedSeconds => _time;
    internal IReadOnlyList<int> Skins => _boats.Select(b => b.Canvas.Skin).ToArray();
    internal IReadOnlyList<(int Source, int Destination)> Voyages => _boats.Select(b => (b.Source,b.Destination)).ToArray();
    internal IReadOnlyList<Vector2> Positions => _boats.Select(b => b.Canvas.Position).ToArray();
    internal IReadOnlyList<string> BoatIds => _boats.Select(b => b.Canvas.Name.ToString()).ToArray();
    internal IReadOnlyList<float> SailedDistances => _boats.Select(b => b.Sailed).ToArray();
    internal IReadOnlyList<GridPosition[]> Routes => _boats.Select(b => b.Cells.ToArray()).ToArray();
    internal IReadOnlyDictionary<int, float> DepartureTimes => _departures;

    private sealed class MerchantVoyage
    {
        internal MerchantCanvas Canvas = null!;
        internal Vector2[] Points = Array.Empty<Vector2>();
        internal float[] Lengths = Array.Empty<float>();
        internal float Distance;
        internal float Sailed, Remaining;
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
            _time=0; Departures=0; Arrivals=0; LastArrivalCity=null; RouteRepairs=0;
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
            var hazards = battle.Whirlpools.SelectMany(hazard => hazard.Cells).ToHashSet();
            var beacons = battle.Ships.Where(ship => ship.Owner == Side.Player && ship.Health > 0
                    && ship.Definition.Class == ShipClass.Lighthouse).OrderBy(ship => ship.Id)
                .Select(ship => Board.Projection.GridToWorld(ship.Position) + FleetView.LighthouseOffset).ToArray();
            bool footprintsChanged = !_hazards.SetEquals(hazards) || !_beacons.SequenceEqual(beacons);
            _hazards.Clear();
            _hazards.UnionWith(hazards);
            _beacons = beacons;
            foreach (var edge in network.Edges)
            {
                if (!_adjacency.TryGetValue(edge.Item1,out var first)) _adjacency[edge.Item1]=first=new();
                if (!_adjacency.TryGetValue(edge.Item2,out var second)) _adjacency[edge.Item2]=second=new();
                first.Add(edge.Item2); second.Add(edge.Item1);
            }
            // Core restoration can replace an identical network. Keep the existing
            // canvas, elapsed voyage and departure schedule; repair a remaining
            // lane only if it disappeared or now crosses a new beacon footprint.
            for (int index = _boats.Count - 1; index >= 0; index--)
            {
                var boat = _boats[index];
                bool portsOpen = ids.Contains(boat.Source) && ids.Contains(boat.Destination);
                bool laneExists = portsOpen && RouteStillExists(boat.Cells);
                if (laneExists && (!footprintsChanged || RemainingLaneSafe(boat))) continue;
                if (portsOpen && RepairRemainingVoyage(boat)) continue;
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
            float movement = Math.Max(0, seconds) * Speed;
            float actual = Math.Min(movement, boat.Remaining);
            boat.Sailed += actual;
            boat.Remaining -= actual;
            boat.Distance+=movement;
            while (boat.Segment<boat.Lengths.Length && boat.Distance>=boat.Lengths[boat.Segment])
                boat.Distance-=boat.Lengths[boat.Segment++];
            if (boat.Segment>=boat.Lengths.Length)
            {
                Arrivals++;
                LastArrivalCity=boat.Destination;
                boat.Canvas.QueueFree(); _boats.RemoveAt(index); continue;
            }
            var first=boat.Points[boat.Segment]; var second=boat.Points[boat.Segment+1];
            var at=first.Lerp(second,boat.Distance/boat.Lengths[boat.Segment]);
            boat.Canvas.Position=at;
            boat.Canvas.SetHeading((second-first).Angle());
            boat.Canvas.SetDistantDetail(Board.FarSceneryActive);
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
            var points=Lane(cells,HarborAnchor(source),HarborAnchor(destination));
            if (points is null) continue;
            var lengths=points.Zip(points.Skip(1),(a,b)=>a.DistanceTo(b)).ToArray();
            if (lengths.Length==0 || lengths.Any(length=>length<.001f)) continue;
            var canvas=new MerchantCanvas { Name="Merchant"+Departures, Skin=(_skinOffset+Departures)%3, Position=points[0] };
            AddChild(canvas);
            _boats.Add(new MerchantVoyage { Canvas=canvas,Points=points,Lengths=lengths,Source=source.Id,
                Destination=destination.Id,Cells=cells,Remaining=lengths.Sum() });
            Departures++;
            return;
        }
    }

    private GridPosition[]? Route(GridPosition start,GridPosition finish)
    {
        // MaximumRouteLength limits each Core link, not a voyage through the
        // connected graph. Lighthouses are intermediate nodes, never destinations.
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

    private bool RouteStillExists(GridPosition[] cells)
    {
        for (int cell = 1; cell < cells.Length; cell++)
            if (!_adjacency.TryGetValue(cells[cell - 1], out var adjacent) || !adjacent.Contains(cells[cell])) return false;
        return true;
    }

    private bool Water(Vector2 point)
    {
        var cell = Board.Projection.WorldToGrid(point);
        return Board.Board.Contains(cell) && Board.Board.GetTile(cell).Terrain != Core.World.TerrainType.Land
            && !_hazards.Contains(cell);
    }

    private bool WaterSegment(Vector2 from, Vector2 to)
    {
        int samples = Math.Max(1, (int)MathF.Ceiling(from.DistanceTo(to) / 4));
        for (int sample = 0; sample <= samples; sample++)
            if (!Water(from.Lerp(to, sample / (float)samples))) return false;
        return true;
    }

    private static bool CrossesBeacon(Vector2 from, Vector2 to, Vector2 beacon)
    {
        var first = (from - beacon) / BeaconClearance;
        var second = (to - beacon) / BeaconClearance;
        var ray = second - first;
        float t = ray.LengthSquared() < .00001f ? 0 : Math.Clamp(-first.Dot(ray) / ray.LengthSquared(), 0, 1);
        return (first + ray * t).LengthSquared() < 1;
    }

    private bool SafeSegment(Vector2 from, Vector2 to, bool escapeOrigin = false)
    {
        if (!WaterSegment(from, to)) return false;
        foreach (var beacon in _beacons)
        {
            if (!CrossesBeacon(from, to, beacon)) continue;
            var first = (from - beacon) / BeaconClearance;
            var ray = (to - from) / BeaconClearance;
            // A newly built beacon may appear under an already moving cosmetic
            // hull. Let that immutable origin leave the footprint monotonically;
            // this is never permission for a later segment to cross the rock.
            if (escapeOrigin && first.LengthSquared() < 1 && first.Dot(ray) >= -.0001f
                && (first + ray).LengthSquared() >= first.LengthSquared()) continue;
            return false;
        }
        return true;
    }

    private bool RemainingLaneSafe(MerchantVoyage boat)
    {
        var at = boat.Canvas.Position;
        for (int next = boat.Segment + 1; next < boat.Points.Length; next++)
        {
            if (!SafeSegment(at, boat.Points[next], next == boat.Segment + 1)) return false;
            at = boat.Points[next];
        }
        return true;
    }

    private Vector2 HarborAnchor(Village port)
    {
        var shore = Board.TradePortAnchor(port);
        var sea = Board.Projection.GridToWorld(_battle!.PortBerth(port));
        // Shore art may lie just inside the land polygon. Depart immediately
        // offshore rather than making the decorative hull sail across that soil.
        for (int sample = 1; sample <= 16; sample++)
        {
            var point = shore.Lerp(sea, sample / 16f);
            if (Water(point) && OutsideBeacons(point)) return point;
        }
        // A saved city berth may also contain a lighthouse. Its sea cell remains
        // a valid connection; launch beside the rock rather than inside it.
        if (Water(sea) && OutsideBeacons(sea)) return sea;
        foreach (float radius in new[] { 8f, 16f, 24f, 32f, 40f })
            foreach (var point in Enumerable.Range(0, 24)
                         .Select(sample => sea + Vector2.FromAngle(sample * Mathf.Tau / 24) * radius)
                         .OrderBy(point => point.DistanceSquaredTo(shore)))
                if (Water(point) && OutsideBeacons(point)) return point;
        return sea;
    }

    private bool OutsideBeacons(Vector2 point) => !_beacons.Any(beacon =>
        ((point - beacon) / BeaconClearance).LengthSquared() < 1);

    private bool RepairRemainingVoyage(MerchantVoyage boat)
    {
        var destination = _ports.First(port => port.Id == boat.Destination);
        var finish = _battle!.PortBerth(destination);
        var at = boat.Canvas.Position;
        // Searches happen only on a real topology/footprint change, never per
        // frame. First connect to the nearest water-safe surviving graph node.
        foreach (var start in _adjacency.Keys.OrderBy(cell => Board.Projection.GridToWorld(cell).DistanceSquaredTo(at))
                     .ThenBy(cell => cell.Y).ThenBy(cell => cell.X))
        {
            var cells = Route(start, finish);
            if (cells is null || !WaterSegment(at, Board.Projection.GridToWorld(start))) continue;
            var points = Lane(cells, at, HarborAnchor(destination));
            if (points is null || points.Length < 2 || points[0] != at) continue;
            boat.Points = points;
            boat.Lengths = points.Zip(points.Skip(1), (a, b) => a.DistanceTo(b)).ToArray();
            boat.Remaining = boat.Lengths.Sum();
            boat.Cells = cells;
            boat.Segment = 0;
            boat.Distance = 0;
            RouteRepairs++;
            return true;
        }
        return false;
    }

    private Vector2[]? Lane(GridPosition[] cells,Vector2 fromPort,Vector2 toPort)
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
        return DetourBeacons(points);
    }

    private Vector2[]? DetourBeacons(List<Vector2> points)
    {
        // Shift interior control points out of the actual drawn rock footprint.
        // Existing voyage origin is deliberately immutable during replanning.
        for (int index = 1; index < points.Count; index++)
            foreach (var beacon in _beacons)
                if (((points[index] - beacon) / BeaconClearance).LengthSquared() < 1)
                {
                    var radial = ((points[index] - beacon) / BeaconClearance).Normalized();
                    if (radial.LengthSquared() < .001f) radial = Vector2.Down;
                    var candidates = Enumerable.Range(0, 24).Select(sample => beacon
                        + radial.Rotated(sample * Mathf.Tau / 24) * BeaconClearance * 1.08f)
                        .Where(Water).OrderBy(point => point.DistanceSquaredTo(points[index]));
                    var safe = candidates.Where(point => !_beacons.Any(other =>
                        ((point - other) / BeaconClearance).LengthSquared() < 1)).Take(1).ToArray();
                    if (safe.Length == 0) return null;
                    points[index] = safe[0];
                }
        var result = new List<Vector2> { points[0] };
        for (int index = 1; index < points.Count; index++)
        {
            var to = points[index];
            if (result[^1].DistanceSquaredTo(to) < .002f) continue;
            if (SafeSegment(result[^1], to, result.Count == 1)) { result.Add(to); continue; }
            var beacon = _beacons.FirstOrDefault(light => CrossesBeacon(result[^1], to, light));
            if (!_beacons.Contains(beacon)) return null;
            var from = result[^1];
            float startAngle = ((from - beacon) / BeaconClearance).Angle();
            float endAngle = ((to - beacon) / BeaconClearance).Angle();
            Vector2[]? best = null;
            float bestLength = float.MaxValue;
            foreach (int direction in new[] { -1, 1 })
            {
                float span = Mathf.PosMod((endAngle - startAngle) * direction, Mathf.Tau);
                int samples = Math.Max(1, (int)MathF.Ceiling(span / (Mathf.Pi / 12)));
                var bypass = new List<Vector2> { from };
                for (int sample = 0; sample <= samples; sample++)
                    bypass.Add(beacon + Vector2.FromAngle(startAngle + span * direction * sample / samples)
                        * BeaconClearance * 1.08f);
                bypass.Add(to);
                if (Enumerable.Range(1, bypass.Count - 1).Any(step =>
                    !SafeSegment(bypass[step - 1], bypass[step], result.Count == 1 && step == 1))) continue;
                float length = bypass.Zip(bypass.Skip(1), (a, b) => a.DistanceTo(b)).Sum();
                if (length >= bestLength) continue;
                best = bypass.ToArray();
                bestLength = length;
            }
            if (best is null) return null;
            foreach (var point in best.Skip(1))
                if (result[^1].DistanceSquaredTo(point) > .002f) result.Add(point);
        }
        return result.Count > 1 ? result.ToArray() : null;
    }
}

internal partial class MerchantCanvas : Node2D
{
    internal int Skin { get; init; }
    private readonly Node2D _hull=new();
    private BoardTerrainLayer? _hullArt;
    private bool _distant;
    public override void _Ready()
    {
        _hullArt=new BoardTerrainLayer { DrawWorld=canvas=>DrawHull(canvas) };
        _hull.AddChild(_hullArt); AddChild(_hull);
    }
    internal void SetHeading(float radians) => _hull.Rotation=radians;
    internal void SetDistantDetail(bool far)
    {
        if (_distant == far) return;
        _distant = far; _hullArt?.QueueRedraw(); QueueRedraw();
    }
    private void DrawHull(CanvasItem canvas)
    {
        if (_distant)
        {
            canvas.DrawColoredPolygon(new[] { new Vector2(-8,0),new(-5,-3),new(5,-2.6f),new(9,0),new(5,3),new(-5,3) },new Color(Skin==2?"76503b":"ab8c58"));
            canvas.DrawLine(new(-6,-.5f),new(6,-.5f),new Color("ecdcac"),1,true);
            return;
        }
        canvas.DrawColoredPolygon(new[] { new Vector2(-9,2),new(-6,-1),new(6,-1),new(10,2),new(6,5),new(-6,5) },new Color(.02f,.08f,.1f,.27f));
        canvas.DrawColoredPolygon(new[] { new Vector2(-8,0),new(-5,-3),new(5,-2.6f),new(9,0),new(6,3.4f),new(-5,4) },new Color(Skin==2?"5e4539":"806843"));
        canvas.DrawColoredPolygon(new[] { new Vector2(-7,-1),new(-5,-3),new(5,-2.6f),new(8,-.5f),new(5,2),new(-5,2.4f) },new Color(Skin==2?"bda176":"d3bd88"));
        canvas.DrawPolyline(new[] { new Vector2(-7,-1),new(-5,-3),new(5,-2.6f),new(8,-.5f) },new Color("f1dfab"),.65f,true);
        canvas.DrawLine(new(-6,2.3f),new(5,2),new Color(Skin==2?"8e4750":"596f76"),.85f,true);
        for(int plank=0;plank<5;plank++)
            canvas.DrawLine(new(-4+plank*2,-2.2f),new(-4+plank*2,1.8f),new Color("af996b"),.45f);
        if (Skin==2)
            for(int box=0;box<3;box++)
            {
                canvas.DrawRect(new Rect2(-5+box*3,-.2f,2.5f,2.3f),new Color("a58858"));
                canvas.DrawLine(new(-5+box*3,0),new(-3+box*3,1.6f),new Color("e9d0a0"),.55f,true);
            }
        else canvas.DrawRect(new Rect2(-6,-1.5f,3,2.5f),new Color("9b835c"));
        canvas.DrawPolyline(new[] { new Vector2(-9,-2),new(-13,-1),new(-16,-1) },new Color(.83f,.93f,.91f,.23f),.7f,true);
        canvas.DrawPolyline(new[] { new Vector2(-9,3),new(-13,4),new(-17,3.2f) },new Color(.83f,.93f,.91f,.18f),.7f,true);
    }
    public override void _Draw()
    {
        var sail=new Color(Skin==0?"f4e6bd":Skin==1?"dec9a1":"d6b3a0");
        DrawLine(new(0,1),new(0,-11),new Color("765e3d"),.85f,true);
        if (_distant)
        {
            DrawColoredPolygon(new[] { new Vector2(1,-10),new(1,-2),new(6,-3) },sail);
            return;
        }
        if (Skin==1)
        {
            DrawColoredPolygon(new[] { new Vector2(-1,-10),new(-6,-1),new(-1,-2) },sail);
            DrawColoredPolygon(new[] { new Vector2(1,-9),new(5,-3),new(1,-2) },sail.Lightened(.12f));
            DrawLine(new(-1,-9),new(-4,-3),new Color("a99973"),.45f,true);
            DrawLine(new(1,-8),new(4,-3),new Color("f4e8c8"),.6f,true);
        }
        else
        {
            DrawColoredPolygon(new[] { new Vector2(1,-10),new(1,-2),new(6,-3) },sail);
            DrawColoredPolygon(new[] { new Vector2(1,-10),new(3,-7),new(6,-3),new(3,-3) },sail.Lightened(.1f));
            if (Skin==2) DrawLine(new(1,-6),new(4,-6),new Color("783541"),1,true);
        }
        DrawLine(new(0,-10),new(-6,1),new Color("c2ac7c"),.45f,true);
        DrawLine(new(0,-10),new(6,1),new Color("c2ac7c"),.45f,true);
    }
}
