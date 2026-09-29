using System;
using System.Collections.Generic;
using System.Linq;
using DevAncientNaval.Core.Grid;
using Godot;

namespace DevAncientNaval.Presentation.Map;

/// <summary>Seeded rotating patches, shared edge collapses and curved seams.
/// Logical addresses stay stable while actual tiles include triangles and curved quads.</summary>
public sealed class IsometricProjection
{
    public float TileWidth { get; }
    public float TileHeight { get; }
    public int TriangleCount { get; private set; }
    private readonly Dictionary<GridPosition,Vector2> _vertices=new();
    private readonly Dictionary<GridPosition,Vector2[]> _outlines=new();
    private readonly Dictionary<(GridPosition,GridPosition),Vector2[]> _edges=new();
    private readonly int _seed;
    private readonly (Vector2 Center,float Angle,float Radius)[] _twists;
    public IsometricProjection(float tileWidth=96,float tileHeight=48,int seed=0)
    {
        if(!float.IsFinite(tileWidth)||tileWidth<=0) throw new ArgumentOutOfRangeException(nameof(tileWidth));
        if(!float.IsFinite(tileHeight)||tileHeight<=0) throw new ArgumentOutOfRangeException(nameof(tileHeight));
        TileWidth=tileWidth; TileHeight=tileHeight; _seed=seed;
        var random=new Random(seed); _twists=new (Vector2,float,float)[5];
        for(int i=0;i<_twists.Length;i++) _twists[i]=(new(2+random.Next(16),2+random.Next(16)),(float)(random.NextDouble()*1.6-.8),3.8f+random.Next(3));
        for(int y=-3;y<=23;y++) for(int x=-3;x<=23;x++) _vertices[new(x,y)]=Warp(x,y);
        // Collapse isolated shared edges only if all incident polygons retain usable positive area.
        var candidates=(from y in Enumerable.Range(2,16) from x in Enumerable.Range(2,16) select new GridPosition(x,y)).OrderBy(_=>random.Next()).ToArray();
        var reserved=new HashSet<GridPosition>();
        foreach(var a in candidates)
        {
            if(TriangleCount>=26) break;
            var b=random.Next(2)==0?new GridPosition(a.X+1,a.Y):new GridPosition(a.X,a.Y+1);
            if(reserved.Any(p=>Math.Max(Math.Abs(p.X-a.X),Math.Abs(p.Y-a.Y))<3)) continue;
            var oldA=_vertices[a]; var oldB=_vertices[b]; _vertices[a]=_vertices[b]=(oldA+oldB)/2;
            bool valid=true;
            for(int y=a.Y-1;y<=b.Y;y++) for(int x=a.X-1;x<=b.X;x++)
            {
                var quad=Corners(new(x,y)).Distinct().ToArray();
                if(quad.Length<3 || Area(quad)<.24f || Enumerable.Range(0,quad.Length).Any(i=>(quad[(i+1)%quad.Length]-quad[i]).Cross(quad[(i+2)%quad.Length]-quad[(i+1)%quad.Length])<.015f)) valid=false;
            }
            if(!valid) { _vertices[a]=oldA; _vertices[b]=oldB; continue; }
            reserved.Add(a); reserved.Add(b); TriangleCount+=2;
        }
    }
    private Vector2 Warp(int x,int y)
    {
        var p=new Vector2(x-.5f,y-.5f);
        foreach(var twist in _twists) { var d=p-twist.Center; float weight=MathF.Exp(-d.LengthSquared()/(twist.Radius*twist.Radius)); p=twist.Center+d.Rotated(twist.Angle*weight); }
        uint h=unchecked((uint)(x*374761393+y*668265263+_seed)); h=(h^(h>>13))*1274126177u; h^=h>>16;
        return p+new Vector2(((h&255)/255f-.5f)*.12f,(((h>>8)&255)/255f-.5f)*.12f);
    }
    private Vector2 Vertex(GridPosition p)=>_vertices.TryGetValue(p,out var v)?v:Warp(p.X,p.Y);
    private static GridPosition[] Keys(GridPosition p)=>new[] { p,new GridPosition(p.X+1,p.Y),new GridPosition(p.X+1,p.Y+1),new GridPosition(p.X,p.Y+1) };
    private Vector2[] Corners(GridPosition p)=>Keys(p).Select(Vertex).ToArray();
    private Vector2 Project(Vector2 p)=>new((p.X-p.Y)*TileWidth/2,(p.X+p.Y)*TileHeight/2);
    private static float Area(Vector2[] p) { float area=0; for(int i=0;i<p.Length;i++) area+=p[i].Cross(p[(i+1)%p.Length]); return area/2; }
    public Vector2[] Edge(GridPosition cell,int side)
    {
        var keys=Keys(cell); var a=keys[side]; var b=keys[(side+1)%4]; bool reverse=a.X>b.X||a.X==b.X&&a.Y>b.Y;
        var key=reverse?(b,a):(a,b);
        if(!_edges.TryGetValue(key,out var edge))
        {
            var start=Vertex(key.Item1); var end=Vertex(key.Item2); var d=end-start;
            // A shallow shared quadratic arc rounds the region without breaking its neighbours.
            float bend=MathF.Sin(key.Item1.X*1.7f+key.Item1.Y*2.3f+_seed%97)*.028f;
            var control=(start+end)/2+new Vector2(-d.Y,d.X)*bend;
            edge=Enumerable.Range(0,5).Select(i=>{float t=i/4f;return Project((1-t)*(1-t)*start+2*(1-t)*t*control+t*t*end);}).ToArray();
            _edges[key]=edge;
        }
        return reverse?edge.Reverse().ToArray():edge;
    }
    public Vector2[] Diamond(GridPosition cell)
    {
        if(!_outlines.TryGetValue(cell,out var outline))
        {
            var points=new List<Vector2>();
            for(int side=0;side<4;side++) foreach(var p in Edge(cell,side).SkipLast(1)) if(points.Count==0||points[^1].DistanceTo(p)>.01f) points.Add(p);
            if(points.Count>1&&points[0].DistanceTo(points[^1])<.01f) points.RemoveAt(points.Count-1);
            _outlines[cell]=outline=points.ToArray();
        }
        return outline;
    }
    public int CornerCount(GridPosition p)=>Corners(p).Distinct().Count();
    public Vector2 GridToWorld(GridPosition p)
    {
        var corners=Corners(p).Distinct().ToArray(); return Project(corners.Aggregate(Vector2.Zero,(sum,v)=>sum+v)/corners.Length);
    }
    public GridPosition WorldToGrid(Vector2 point)
    {
        int x=(int)MathF.Floor(point.X/TileWidth+point.Y/TileHeight+.5f),y=(int)MathF.Floor(point.Y/TileHeight-point.X/TileWidth+.5f);
        for(int dy=-6;dy<=6;dy++) for(int dx=-6;dx<=6;dx++) { var p=new GridPosition(x+dx,y+dy); if(Geometry2D.IsPointInPolygon(point,Diamond(p))) return p; }
        return new(x,y);
    }
    public Rect2 BoardBounds(int width,int height)
    {
        var bounds=new Rect2(GridToWorld(new(0,0)),Vector2.Zero);
        for(int y=0;y<height;y++) for(int x=0;x<width;x++) foreach(var p in Diamond(new(x,y))) bounds=bounds.Expand(p);
        return bounds.Grow(4);
    }
}
