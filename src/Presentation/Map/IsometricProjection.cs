using System;
using System.Collections.Generic;
using System.Linq;
using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.World;
using Godot;

namespace DevAncientNaval.Presentation.Map;

/// <summary>Seeded rotating patches, shared edge collapses and curved seams.
/// Logical addresses stay stable while actual tiles include triangles and curved quads.</summary>
/// <summary>Shared planar mesh: local vortices, constrained vertex jitter, vertex splits
/// (pentagons) and edge collapses (triangles). Curved seams and picking use the same mesh.</summary>
public sealed class IsometricProjection
{
    public float TileWidth { get; }
    public float TileHeight { get; }
    public int TriangleCount { get; private set; }
    private readonly Dictionary<GridPosition,Vector2> _vertices=new();
    public int TriangleCount => _faces.Values.Count(f=>f.Count==3);
    public int PentagonCount => _faces.Values.Count(f=>f.Count==5);
    private readonly Dictionary<int,Vector2> _vertices=new();
    private readonly Dictionary<GridPosition,List<int>> _faces=new();
    private readonly Dictionary<GridPosition,Vector2[]> _outlines=new();
    private readonly Dictionary<(GridPosition,GridPosition),Vector2[]> _edges=new();
    private readonly Dictionary<(int,int),Vector2[]> _edges=new();
    private readonly Dictionary<(int,int),List<GridPosition>> _pickBins=new();
    private readonly int _seed;
    private readonly (Vector2 Center,float Angle,float Radius)[] _twists;
    public IsometricProjection(float tileWidth=96,float tileHeight=48,int seed=0)
    public IsometricProjection(float tileWidth=96,float tileHeight=48,int seed=0,int width=20,int height=20)
    {
        if(!float.IsFinite(tileWidth)||tileWidth<=0) throw new ArgumentOutOfRangeException(nameof(tileWidth));
        if(!float.IsFinite(tileHeight)||tileHeight<=0) throw new ArgumentOutOfRangeException(nameof(tileHeight));
        TileWidth=tileWidth; TileHeight=tileHeight; _seed=seed;
        var random=new Random(seed); _twists=new (Vector2,float,float)[5];
        for(int i=0;i<_twists.Length;i++) _twists[i]=(new(2+random.Next(16),2+random.Next(16)),(float)(random.NextDouble()*1.6-.8),3.8f+random.Next(3));
        for(int y=-3;y<=23;y++) for(int x=-3;x<=23;x++) _vertices[new(x,y)]=Warp(x,y);
        // Collapse isolated shared edges only if all incident polygons retain usable positive area.
        var candidates=(from y in Enumerable.Range(2,16) from x in Enumerable.Range(2,16) select new GridPosition(x,y)).OrderBy(_=>random.Next()).ToArray();
        var random=new Random(seed); var keys=new Dictionary<GridPosition,int>();
        var twists=Enumerable.Range(0,Math.Max(6,width*height/90)).Select(_=>(
            Center:new Vector2(random.Next(width),random.Next(height)), Angle:(float)(random.NextDouble()*2.5-1.25),Radius:4f+random.Next(4))).ToArray();
        Vector2 Warp(Vector2 p) { foreach(var twist in twists) { var d=p-twist.Center; p=twist.Center+d.Rotated(twist.Angle*MathF.Exp(-d.LengthSquared()/(twist.Radius*twist.Radius))); } return p; }
        for(int y=-2;y<=height+2;y++) for(int x=-2;x<=width+2;x++)
        { int id=_vertices.Count; keys[new(x,y)]=id; _vertices[id]=Warp(new(x-.5f,y-.5f)); }
        for(int y=-2;y<height+2;y++) for(int x=-2;x<width+2;x++)
            _faces[new(x,y)]=new() { keys[new(x,y)],keys[new(x+1,y)],keys[new(x+1,y+1)],keys[new(x,y+1)] };
        // Back off extreme vortex combinations until every cell is convex and usable.
        for(int pass=0;pass<12 && _faces.Values.Any(f=>!Valid(f));pass++)
            foreach(var (p,id) in keys) _vertices[id]=_vertices[id].Lerp(new(p.X-.5f,p.Y-.5f),.2f);
        var incident=_vertices.Keys.ToDictionary(id=>id,id=>_faces.Where(f=>f.Value.Contains(id)).Select(f=>f.Key).ToArray());
        for(int pass=0;pass<3;pass++) foreach(var id in _vertices.Keys.OrderBy(_=>random.Next()).ToArray())
        {
            var old=_vertices[id]; _vertices[id]+=new Vector2((float)(random.NextDouble()-.5)*.75f,(float)(random.NextDouble()-.5)*.75f);
            if(incident[id].Any(p=>!Valid(_faces[p]))) _vertices[id]=old;
        }
        var reserved=new HashSet<GridPosition>();
        foreach(var p in keys.Keys.Where(p=>p.X>0&&p.Y>0&&p.X<width&&p.Y<height).OrderBy(_=>random.Next()))
        {
            if(random.NextDouble()>.48 || reserved.Any(q=>Math.Max(Math.Abs(p.X-q.X),Math.Abs(p.Y-q.Y))<2)) continue;
            int id=keys[p]; var old=_vertices[id]; int extra=_vertices.Count;
            var cells=new[] {new GridPosition(p.X-1,p.Y-1),new(p.X,p.Y-1),new(p.X-1,p.Y),p};
            var saved=cells.ToDictionary(c=>c,c=>new List<int>(_faces[c]));
            bool other=random.Next(2)==0;
            var delta=(_vertices[keys[new(p.X+1,p.Y)]]-_vertices[keys[new(p.X-1,p.Y)]])*.105f
                +(_vertices[keys[new(p.X,p.Y+1)]]-_vertices[keys[new(p.X,p.Y-1)]])*(other?.105f:-.105f);
            _vertices[id]=old+delta; _vertices[extra]=old-delta;
            void Replace(GridPosition c,params int[] replacement) { var f=_faces[c]; int i=f.IndexOf(id); f.RemoveAt(i); f.InsertRange(i,replacement); }
            if(!other) { Replace(cells[0],id,extra); Replace(cells[3],extra,id); Replace(cells[2],extra); }
            else { Replace(cells[1],extra,id); Replace(cells[2],id,extra); Replace(cells[0],extra); }
            if(cells.Any(c=>!Valid(_faces[c]))) { foreach(var c in cells) _faces[c]=saved[c]; _vertices[id]=old; _vertices.Remove(extra); }
            else reserved.Add(p);
        }
        var edges=_faces.Values.SelectMany(f=>Enumerable.Range(0,f.Count).Select(i=>Key(f[i],f[(i+1)%f.Count]))).Distinct().OrderBy(_=>random.Next()).ToArray();
        var touched=new HashSet<int>(); int collapses=0;
        foreach(var (a,b) in edges)
        {
            if(collapses>=width*height/24 || touched.Contains(a)||touched.Contains(b)) continue;
            var cells=_faces.Where(f=>f.Value.Contains(a)||f.Value.Contains(b)).Select(f=>f.Key).ToArray();
            if(cells.Any(p=>p.X<0||p.Y<0||p.X>=width||p.Y>=height)) continue;
            var saved=cells.ToDictionary(p=>p,p=>new List<int>(_faces[p])); var old=_vertices[a];
            _vertices[a]=(_vertices[a]+_vertices[b])/2;
            foreach(var c in cells) _faces[c]=_faces[c].Select(id=>id==b?a:id).Distinct().ToList();
            if(cells.Any(c=>!Valid(_faces[c]))) { foreach(var c in cells) _faces[c]=saved[c]; _vertices[a]=old; }
            else { collapses++; foreach(var f in saved.Values) touched.UnionWith(f); }
        }
        // Curvature near an acute triangle tip must not fold back across a center ray.
        // Straighten the shared seams there; both neighbours use the same replacement.
        foreach(var (p,f) in _faces)
        {
            var outline=CellEdges(p).SelectMany(edge=>edge.SkipLast(1)).ToArray(); var center=GridToWorld(p);
            if(Enumerable.Range(0,outline.Length).Any(i=>(outline[(i+1)%outline.Length]-outline[i]).Cross(center-outline[i])<0))
                for(int i=0;i<f.Count;i++)
                {
                    var k=Key(f[i],f[(i+1)%f.Count]); var a=Project(_vertices[k.Item1]); var b=Project(_vertices[k.Item2]);
                    _edges[k]=Enumerable.Range(0,5).Select(j=>a.Lerp(b,j/4f)).ToArray();
                }
        }
        foreach(var p in _faces.Keys)
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
            var outline=Diamond(p).Select(Unproject).ToArray();
            for(int y=(int)Math.Floor(outline.Min(v=>v.Y));y<=(int)Math.Floor(outline.Max(v=>v.Y));y++)
            for(int x=(int)Math.Floor(outline.Min(v=>v.X));x<=(int)Math.Floor(outline.Max(v=>v.X));x++)
            { if(!_pickBins.TryGetValue((x,y),out var list)) _pickBins[(x,y)]=list=new(); list.Add(p); }
        }
    }
    private Vector2 Warp(int x,int y)
    private bool Valid(List<int> face)
    {
        var p=new Vector2(x-.5f,y-.5f);
        foreach(var twist in _twists) { var d=p-twist.Center; float weight=MathF.Exp(-d.LengthSquared()/(twist.Radius*twist.Radius)); p=twist.Center+d.Rotated(twist.Angle*weight); }
        uint h=unchecked((uint)(x*374761393+y*668265263+_seed)); h=(h^(h>>13))*1274126177u; h^=h>>16;
        return p+new Vector2(((h&255)/255f-.5f)*.12f,(((h>>8)&255)/255f-.5f)*.12f);
        if(face.Count<3) return false;
        var p=face.Select(id=>_vertices[id]).ToArray();
        float area=0;
        for(int i=0;i<p.Length;i++) {
            area+=p[i].Cross(p[(i+1)%p.Length]);
            if((p[(i+1)%p.Length]-p[i]).Cross(p[(i+2)%p.Length]-p[(i+1)%p.Length])<.025f) return false;
        }
        return area>.44f;
    }
    private Vector2 Vertex(GridPosition p)=>_vertices.TryGetValue(p,out var v)?v:Warp(p.X,p.Y);
    private static GridPosition[] Keys(GridPosition p)=>new[] { p,new GridPosition(p.X+1,p.Y),new GridPosition(p.X+1,p.Y+1),new GridPosition(p.X,p.Y+1) };
    private Vector2[] Corners(GridPosition p)=>Keys(p).Select(Vertex).ToArray();
    private static (int,int) Key(int a,int b)=>a<b?(a,b):(b,a);
    private Vector2 Project(Vector2 p)=>new((p.X-p.Y)*TileWidth/2,(p.X+p.Y)*TileHeight/2);
    private static float Area(Vector2[] p) { float area=0; for(int i=0;i<p.Length;i++) area+=p[i].Cross(p[(i+1)%p.Length]); return area/2; }
    public Vector2[] Edge(GridPosition cell,int side)
    private Vector2 Unproject(Vector2 p)=>new(p.X/TileWidth+p.Y/TileHeight,p.Y/TileHeight-p.X/TileWidth);
    private Vector2[] Seam(int a,int b)
    {
        var keys=Keys(cell); var a=keys[side]; var b=keys[(side+1)%4]; bool reverse=a.X>b.X||a.X==b.X&&a.Y>b.Y;
        var key=reverse?(b,a):(a,b);
        var key=Key(a,b);
        if(!_edges.TryGetValue(key,out var edge))
        {
            var start=Vertex(key.Item1); var end=Vertex(key.Item2); var d=end-start;
            // A shallow shared quadratic arc rounds the region without breaking its neighbours.
            float bend=MathF.Sin(key.Item1.X*1.7f+key.Item1.Y*2.3f+_seed%97)*.028f;
            var start=_vertices[key.Item1]; var end=_vertices[key.Item2]; var d=end-start;
            // Amplitude scales with length and stays shallow near triangular tips.
            float bend=MathF.Sin(key.Item1*1.7f+key.Item2*2.3f+_seed%97)*.045f;
            var control=(start+end)/2+new Vector2(-d.Y,d.X)*bend;
            edge=Enumerable.Range(0,5).Select(i=>{float t=i/4f;return Project((1-t)*(1-t)*start+2*(1-t)*t*control+t*t*end);}).ToArray();
            edge=Enumerable.Range(0,5).Select(i=> { float t=i/4f; return Project((1-t)*(1-t)*start+2*(1-t)*t*control+t*t*end); }).ToArray();
            _edges[key]=edge;
        }
        return reverse?edge.Reverse().ToArray():edge;
        return a==key.Item1?edge:edge.Reverse().ToArray();
    }
    public IEnumerable<Vector2[]> BoundaryEdges(IEnumerable<GridPosition> cells)
    {
        var counts=new Dictionary<(int,int),int>();
        foreach(var p in cells.Distinct()) if(_faces.TryGetValue(p,out var f))
            for(int i=0;i<f.Count;i++) { var k=Key(f[i],f[(i+1)%f.Count]); counts[k]=counts.GetValueOrDefault(k)+1; }
        foreach(var (k,count) in counts) if(count==1) yield return Seam(k.Item1,k.Item2);
    }
    public Vector2[] Diamond(GridPosition cell)
    public IEnumerable<Vector2[]> CellEdges(GridPosition p)
    {
        if(!_outlines.TryGetValue(cell,out var outline))
        var f=_faces[p]; for(int i=0;i<f.Count;i++) yield return Seam(f[i],f[(i+1)%f.Count]);
    }
    public Vector2[] Diamond(GridPosition p)
    {
        if(!_outlines.TryGetValue(p,out var outline))
        {
            var points=new List<Vector2>();
            for(int side=0;side<4;side++) foreach(var p in Edge(cell,side).SkipLast(1)) if(points.Count==0||points[^1].DistanceTo(p)>.01f) points.Add(p);
            if(points.Count>1&&points[0].DistanceTo(points[^1])<.01f) points.RemoveAt(points.Count-1);
            _outlines[cell]=outline=points.ToArray();
            if(!_faces.ContainsKey(p)) return new[] { Project(new(p.X-.5f,p.Y-.5f)),Project(new(p.X+.5f,p.Y-.5f)),Project(new(p.X+.5f,p.Y+.5f)),Project(new(p.X-.5f,p.Y+.5f)) };
            _outlines[p]=outline=CellEdges(p).SelectMany(edge=>edge.SkipLast(1)).ToArray();
        }
        return outline;
    }
    public int CornerCount(GridPosition p)=>Corners(p).Distinct().Count();
    public Vector2 GridToWorld(GridPosition p)
    {
        var corners=Corners(p).Distinct().ToArray(); return Project(corners.Aggregate(Vector2.Zero,(sum,v)=>sum+v)/corners.Length);
    }
    public int CornerCount(GridPosition p)=>_faces.TryGetValue(p,out var f)?f.Count:4;
    public Vector2 GridToWorld(GridPosition p)=>_faces.TryGetValue(p,out var f)?Project(f.Select(id=>_vertices[id]).Aggregate(Vector2.Zero,(sum,v)=>sum+v)/f.Count):Project(new(p.X,p.Y));
    public GridPosition WorldToGrid(Vector2 point)
    {
        int x=(int)MathF.Floor(point.X/TileWidth+point.Y/TileHeight+.5f),y=(int)MathF.Floor(point.Y/TileHeight-point.X/TileWidth+.5f);
        for(int dy=-6;dy<=6;dy++) for(int dx=-6;dx<=6;dx++) { var p=new GridPosition(x+dx,y+dy); if(Geometry2D.IsPointInPolygon(point,Diamond(p))) return p; }
        return new(x,y);
        var q=Unproject(point); var key=((int)Math.Floor(q.X),(int)Math.Floor(q.Y));
        if(_pickBins.TryGetValue(key,out var list)) foreach(var p in list) if(Geometry2D.IsPointInPolygon(point,Diamond(p))) return p;
        return new(int.MinValue,int.MinValue);
    }
    public Rect2 BoardBounds(int width,int height)
    public Rect2 BoardBounds(GameBoard board)=>Bounds(board.Tiles.Select(t=>t.Position));
    public Rect2 BoardBounds(int width,int height)=>Bounds(from y in Enumerable.Range(0,height) from x in Enumerable.Range(0,width) select new GridPosition(x,y));
    private Rect2 Bounds(IEnumerable<GridPosition> cells)
    {
        var bounds=new Rect2(GridToWorld(new(0,0)),Vector2.Zero);
        for(int y=0;y<height;y++) for(int x=0;x<width;x++) foreach(var p in Diamond(new(x,y))) bounds=bounds.Expand(p);
        return bounds.Grow(4);
        var points=cells.SelectMany(Diamond).ToArray(); var bounds=new Rect2(points[0],Vector2.Zero);
        foreach(var p in points) bounds=bounds.Expand(p); return bounds.Grow(4);
    }
}
