using DevAncientNaval.Core.Grid;

namespace DevAncientNaval.Core.World;

/// <summary>Seeded island clusters separated by open channels; every layout keeps a broad central route.</summary>
public static class ArchipelagoGenerator
{
    public static GameBoard Create(int seed)
    {
        var random=new Random(seed); int pattern=random.Next(3);
        var land=new HashSet<GridPosition>();
        bool Reserved(GridPosition p)
        {
            if(p.X<=4&&p.Y>=7&&p.Y<=12 || p.X>=15&&p.Y>=6&&p.Y<=11) return true;
            return pattern switch {
                0 => p.Y>=8&&p.Y<=11,
                1 => Math.Abs(p.Y-(7+p.X*.2))<2.0,
                _ => p.Y>=8&&p.Y<=10 || (p.X-10)*(p.X-10)+(p.Y-9)*(p.Y-9)<20
            };
        }
        int accepted=0;
        for(int attempt=0;attempt<180&&accepted<7;attempt++)
        {
            double cx=2+random.NextDouble()*15,cy=2+random.NextDouble()*15;
            double size=accepted<2?2.4+random.NextDouble():accepted<4?1.2+random.NextDouble():.55+random.NextDouble()*.6;
            double rx=size,ry=size*(.6+random.NextDouble()*.7),angle=random.NextDouble()*Math.PI,phase=random.NextDouble()*6;
            var candidate=new HashSet<GridPosition>();
            for(int y=1;y<19;y++) for(int x=1;x<19;x++)
            {
                var p=new GridPosition(x,y); if(Reserved(p)) continue;
                double dx=x-cx,dy=y-cy,u=(dx*Math.Cos(angle)+dy*Math.Sin(angle))/rx,v=(-dx*Math.Sin(angle)+dy*Math.Cos(angle))/ry;
                double a=Math.Atan2(v,u),coast=1+.2*Math.Sin(3*a+phase)+.12*Math.Cos(5*a-phase);
                if(u*u+v*v<coast*coast) candidate.Add(p);
            }
            if(candidate.Count==0) continue;
            // Keep the largest connected lobe when clipping an island against a sea lane.
            var remaining=new HashSet<GridPosition>(candidate); var largest=new HashSet<GridPosition>();
            while(remaining.Count>0)
            {
                var component=new HashSet<GridPosition>(); var q=new Queue<GridPosition>(); q.Enqueue(remaining.First());
                while(q.TryDequeue(out var p)) { if(!remaining.Remove(p)) continue; component.Add(p); foreach(var n in p.OrthogonalNeighbors()) if(remaining.Contains(n)) q.Enqueue(n); }
                if(component.Count>largest.Count) largest=component;
            }
            if(largest.Any(p=>land.Any(q=>Math.Max(Math.Abs(p.X-q.X),Math.Abs(p.Y-q.Y))<3))) continue;
            if(accepted<2&&largest.Count<7) continue;
            land.UnionWith(largest); accepted++;
        }
        // Erode the thinner bank of pinched bays rather than leaving a one-cell trap.
        bool widened;
        do
        {
            widened=false;
            for(int y=1;y<19;y++) for(int x=1;x<19;x++)
            {
                var p=new GridPosition(x,y);
                if(land.Contains(p)||!GameBoard.IsNarrowPassage(p,land.Contains)) continue;
                var bank=p.OrthogonalNeighbors().Where(land.Contains).OrderBy(q=>q.OrthogonalNeighbors().Count(land.Contains)).First();
                land.Remove(bank); widened=true;
            }
        } while(widened);
        return new GameBoard(20,20,p=>land.Contains(p)?TerrainType.Land:TerrainType.Water,seed);
    }
}
