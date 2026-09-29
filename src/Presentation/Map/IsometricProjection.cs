using System;
using System.Collections.Generic;
using DevAncientNaval.Core.Grid;
using Godot;

namespace DevAncientNaval.Presentation.Map;

/// <summary>A shared, warped vertex lattice. Drawing and picking use the same convex quads.</summary>
public sealed class IsometricProjection
{
    public float TileWidth { get; }
    public float TileHeight { get; }
    private readonly Dictionary<GridPosition,Vector2[]> _quads=new();
    public IsometricProjection(float tileWidth=96,float tileHeight=48)
    {
        if(!float.IsFinite(tileWidth)||tileWidth<=0) throw new ArgumentOutOfRangeException(nameof(tileWidth));
        if(!float.IsFinite(tileHeight)||tileHeight<=0) throw new ArgumentOutOfRangeException(nameof(tileHeight));
        TileWidth=tileWidth; TileHeight=tileHeight;
    }
    private Vector2 Vertex(int x,int y)
    {
        // Low-amplitude local jitter plus broad waves keep cells convex and edge-sharing exact.
        uint h=unchecked((uint)(x*374761393+y*668265263)); h=(h^(h>>13))*1274126177u; h^=h>>16;
        float u=x-.5f+.26f*MathF.Sin(y*.51f)+.18f*MathF.Sin((x+y)*.3f)+((h&255)/255f-.5f)*.34f;
        float v=y-.5f+.25f*MathF.Sin(x*.43f)+.16f*MathF.Cos((x-y)*.34f)+(((h>>8)&255)/255f-.5f)*.34f;
        return new((u-v)*TileWidth/2,(u+v)*TileHeight/2);
    }
    public Vector2[] Diamond(GridPosition cell)
    {
        if(!_quads.TryGetValue(cell,out var quad))
            _quads[cell]=quad=new[] { Vertex(cell.X,cell.Y),Vertex(cell.X+1,cell.Y),Vertex(cell.X+1,cell.Y+1),Vertex(cell.X,cell.Y+1) };
        return quad;
    }
    public Vector2 GridToWorld(GridPosition cell)
    {
        var v=Diamond(cell); return (v[0]+v[1]+v[2]+v[3])/4;
    }
    public GridPosition WorldToGrid(Vector2 point)
    {
        int x=(int)MathF.Floor(point.X/TileWidth+point.Y/TileHeight+.5f);
        int y=(int)MathF.Floor(point.Y/TileHeight-point.X/TileWidth+.5f);
        for(int dy=-2;dy<=2;dy++) for(int dx=-2;dx<=2;dx++)
        {
            var cell=new GridPosition(x+dx,y+dy);
            if(Geometry2D.IsPointInPolygon(point,Diamond(cell))) return cell;
        }
        return new(x,y);
    }
    public Rect2 BoardBounds(int width,int height)
    {
        var bounds=new Rect2(Vertex(0,0),Vector2.Zero);
        for(int y=0;y<=height;y++) for(int x=0;x<=width;x++) bounds=bounds.Expand(Vertex(x,y));
        return bounds.Grow(4);
    }
}
