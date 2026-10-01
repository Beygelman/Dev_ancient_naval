using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.World;
using System.Numerics;

internal static partial class BattleScenarios
{
    private static void CheckPentagonalTopology(GameBoard board)
    {
        var mesh=board.Mesh!;
        var edges=new Dictionary<(int,int),List<GridPosition>>();
        foreach(var (p,f) in mesh.Faces)
        {
            Check(f.Count is >=3 and <=5,"Only triangles, quadrilaterals and pentagons");
            foreach(var q in board.GetSurrounding(p)) Check(board.GetSurrounding(q).Contains(p)&&mesh.Steps(p,q)==1,"Shared-corner neighbors are symmetric, one step apart");
            for(int i=0;i<f.Count;i++)
            {
                int a=f[i],b=f[(i+1)%f.Count];var k=a<b?(a,b):(b,a);
                if(!edges.TryGetValue(k,out var cells)) edges[k]=cells=new();
                cells.Add(p);
            }
        }
        Check(mesh.Faces.Values.Count(f=>f.Count==4)>mesh.Faces.Count*.74,"At least 74 percent quadrilateral cells");
        Check(mesh.RowRedirects>=10,"Local three/five-way junction pairs redirect rows");
        Check(edges.Values.All(c=>c.Count is 1 or 2),"Manifold: every seam has one or two cells");
        Check(mesh.Faces.Values.SelectMany(f=>f).Distinct().Count()-edges.Count+mesh.Faces.Count==1,"One continuous filled disk without holes");
        var sideCells=Enumerable.Range(0,6).Select(_=>new HashSet<GridPosition>()).ToArray();
        foreach(var (edge,cells) in edges.Where(e=>e.Value.Count==1))
        {
            var start=mesh.Vertices[edge.Item1];var end=mesh.Vertices[edge.Item2];int found=-1;
            for(int side=0;side<6;side++)
            {
                var a=mesh.Boundary[side];var d=mesh.Boundary[(side+1)%6]-a;
                float Cross(Vector2 v)=>d.X*v.Y-d.Y*v.X;
                if(Math.Abs(Cross(start-a))<.003&&Math.Abs(Cross(end-a))<.003) {found=side;break;}
            }
            Check(found>=0,"Every external edge lies on one of six straight sides");
            if(found>=0) sideCells[found].Add(cells[0]);
        }
        for(int side=0;side<6;side++) Check(sideCells[side].Count==mesh.SideTileCounts[side],"Actual boundary tiles match each declared side length");
        var origin=board.CentralCell;
        Check(board.BlastCells(origin).Count==9,"Balloon expiry covers exactly nine nearby cells on the organic mesh");
        Check(board.BlastCells(origin).All(p=>mesh.Steps(origin,p)<=2),"Blast stays local across patch junctions");
    }
}
