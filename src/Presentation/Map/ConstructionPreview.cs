using System;
using System.Linq;
using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Units;
using Godot;
using Side = DevAncientNaval.Core.Units.Side;

namespace DevAncientNaval.Presentation.Map;

/// <summary>Retained, noninteractive construction drawing. Hover never changes simulation or terrain.</summary>
internal partial class ConstructionPreview : Node2D
{
    private BoardView _board = null!;
    private ShipClass? _kind;
    private GridPosition? _cell;
    private Vector2[][] _coverage = Array.Empty<Vector2[]>();
    internal GridPosition? Cell => _cell;
    internal ShipClass? Kind => _kind;
    internal int CoverageEdges => _coverage.Length;
    internal int Rebuilds { get; private set; }
    private readonly Color _ink = new(.65f, .91f, 1, .83f);

    internal void ShowAt(BoardView board, ShipClass? kind, GridPosition? cell)
    {
        if (ReferenceEquals(_board, board) && _kind == kind && _cell == cell) return;
        _board = board; _kind = kind; _cell = cell;
        Visible = kind is not null && cell is not null;
        _coverage = Array.Empty<Vector2[]>();
        if (Visible && kind is ShipClass.CannonTower or ShipClass.Lighthouse)
        {
            var definition = board.Battle.Rules.Get(kind.Value);
            int range = kind == ShipClass.Lighthouse ? definition.VisualRange : definition.AttackRange;
            var known = board.Board.Tiles.Where(t => board.Battle.Vision.IsExplored(Side.Player, t.Position)
                && board.Board.InRadius(cell!.Value, t.Position, range)).Select(t => t.Position);
            _coverage = board.Projection.BoundaryEdges(known).ToArray();
        }
        Rebuilds++;
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (_cell is not { } cell || _kind is not { } kind) return;
        foreach (var edge in _coverage)
            DrawPolyline(edge, kind == ShipClass.Lighthouse ? new Color(.81f, .94f, .63f, .65f)
                : new Color(.97f, .59f, .56f, .65f), 1.4f, true);
        DrawPolyline(_board.Projection.ClosedOutline(cell), new Color(_ink, .4f), 1.3f, true);
        var center = _board.Projection.GridToWorld(cell);
        var profile = ShipVisualProfile.For(kind);
        Vector2 P(float x, float y, float z) => center + DeckProjection.Point(x,y,z,-.35f,profile.Size,profile.DeckWidth);
        void Line(Vector2 a, Vector2 b, float width = 1) => DrawLine(a,b,_ink,width,true);
        void Ring(float rx,float ry,float height)
        {
            var points = Enumerable.Range(0,25).Select(i => P(MathF.Cos(i*Mathf.Tau/24)*rx,MathF.Sin(i*Mathf.Tau/24)*ry,height)).ToArray();
            DrawPolyline(points,_ink,1.2f,true);
        }
        void Box(float x,float y,float z,float width,float depth,float height)
        {
            var floor = new[] { P(x-width/2,y-depth/2,z),P(x+width/2,y-depth/2,z),P(x+width/2,y+depth/2,z),P(x-width/2,y+depth/2,z) };
            var roof = new[] { P(x-width/2,y-depth/2,z+height),P(x+width/2,y-depth/2,z+height),P(x+width/2,y+depth/2,z+height),P(x-width/2,y+depth/2,z+height) };
            DrawColoredPolygon(roof, new Color(.36f,.73f,.88f,.10f));
            for(int i=0;i<4;i++) { Line(floor[i],floor[(i+1)%4]);Line(roof[i],roof[(i+1)%4]);Line(floor[i],roof[i]); }
            Line(roof[0],roof[2],.6f);
        }
        if (kind == ShipClass.Lighthouse)
        {
            center += FleetView.LighthouseOffset;
            Box(0,0,0,16,16,30);Box(0,0,30,18,18,8);
            Line(P(-9,-9,38),P(0,0,48));Line(P(9,9,38),P(0,0,48));
            return;
        }
        if (kind is ShipClass.CannonTower or ShipClass.AncientGun)
        {
            Box(0,0,0,24,20,23);Box(0,0,23,28,24,4);
            for(int tooth=0;tooth<3;tooth++)Box(-9+tooth*9,-10,27,5,5,5);
            Line(P(0,0,26),P(15,-6,31),3);Line(P(0,2,24),P(15,-4,29));
            return;
        }
        if (kind == ShipClass.FishingDock)
        {
            Box(0,0,0,30,24,3);Box(0,-3,3,12,10,12);return;
        }
        Ring(29,10,1);Ring(25,8,-7);
        for(int i=0;i<8;i++)
        {
            float a=i*Mathf.Tau/8;Line(P(MathF.Cos(a)*29,MathF.Sin(a)*10,1),P(MathF.Cos(a)*25,MathF.Sin(a)*8,-7),.7f);
        }
        if(kind==ShipClass.Fishing)
        {
            Box(-15,-3,2,10,8,12);Box(-2,4,2,9,7,10);Box(12,-3,2,8,7,8);
            Line(P(5,-5,2),P(5,-5,22));Line(P(5,-5,22),P(18,-5,18));Line(P(18,-5,18),P(18,-5,5));
        }
        else
        {
            Box(-18,0,2,13,10,8);
            int masts=kind==ShipClass.Kolonel?3:kind==ShipClass.Invader?2:1;
            for(int i=0;i<masts;i++)
            {
                float x=-7+i*13;Line(P(x,0,1),P(x,0,36),1.4f);
                Line(P(x,-9,29),P(x,9,29));Line(P(x,-9,29),P(x+3,-9,12));
                Line(P(x+3,-9,12),P(x+3,9,12));Line(P(x+3,9,12),P(x,9,29));
            }
        }
    }
}
