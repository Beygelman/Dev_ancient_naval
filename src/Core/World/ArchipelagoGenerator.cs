using DevAncientNaval.Core.Grid;
using System.Numerics;

namespace DevAncientNaval.Core.World;

/// <summary>Seeded island clusters separated by open channels; every layout keeps a broad central route.</summary>
/// <summary>A seeded convex pentagon, five 18–24-cell sides and separated island clusters.</summary>
public static class ArchipelagoGenerator
{
    public static GameBoard Create(int seed)
    {
        var random=new Random(seed); int pattern=random.Next(3);
        var random = new Random(seed);
        Vector2[] boundary;
        do
        {
            double rotation = random.NextDouble() * .35 - .175;
            boundary = Enumerable.Range(0,5).Select(i => {
                double a = -Math.PI/2 + i*Math.Tau/5 + rotation + (random.NextDouble()-.5)*.09;
                double r = 16.7 + random.NextDouble()*3;
                return new Vector2((float)(Math.Cos(a)*r),(float)(Math.Sin(a)*r));
            }).ToArray();
        } while (Enumerable.Range(0,5).Any(i => Vector2.Distance(boundary[i],boundary[(i+1)%5]) is <18 or >24));
        var offset = new Vector2(1-boundary.Min(v=>v.X),1-boundary.Min(v=>v.Y));
        boundary = boundary.Select(v=>v+offset).ToArray();
        int width=(int)Math.Ceiling(boundary.Max(v=>v.X))+2, height=(int)Math.Ceiling(boundary.Max(v=>v.Y))+2;
        bool Inside(GridPosition p)
        {
            for(int i=0;i<5;i++) { var a=boundary[i]; var d=boundary[(i+1)%5]-a; var q=new Vector2(p.X,p.Y)-a; if(d.X*q.Y-d.Y*q.X<0) return false; }
            return true;
        }
        int mid=height/2, pattern=random.Next(3);
        var row=Enumerable.Range(0,width).Where(x=>Inside(new(x,mid))).ToArray();
        var left=new GridPosition(row.Min()+3,mid); var right=new GridPosition(row.Max()-3,mid);
        var land=new HashSet<GridPosition>();
        bool Reserved(GridPosition p)
        {
            if(p.X<=4&&p.Y>=7&&p.Y<=12 || p.X>=15&&p.Y>=6&&p.Y<=11) return true;
            if(Math.Abs(p.X-left.X)<=3&&Math.Abs(p.Y-mid)<=3 || Math.Abs(p.X-right.X)<=3&&Math.Abs(p.Y-mid)<=3) return true;
            if(Math.Abs(p.Y-mid)<2.5) return true;
            return pattern switch {
                0 => p.Y>=8&&p.Y<=11,
                1 => Math.Abs(p.Y-(7+p.X*.2))<2.0,
                _ => p.Y>=8&&p.Y<=10 || (p.X-10)*(p.X-10)+(p.Y-9)*(p.Y-9)<20
                0 => Math.Abs(p.X-width*.5)<2,
                1 => Math.Abs(p.Y-(mid+(p.X-width*.5)*.28))<2,
                _ => (p.X-width*.5)*(p.X-width*.5)+(p.Y-mid)*(p.Y-mid)<36
            };
        }
        int accepted=0;
        for(int attempt=0;attempt<180&&accepted<7;attempt++)
        int accepted=0, desired=12+random.Next(4);
        for(int attempt=0;attempt<600&&accepted<desired;attempt++)
        {
            double cx=2+random.NextDouble()*15,cy=2+random.NextDouble()*15;
            double size=accepted<2?2.4+random.NextDouble():accepted<4?1.2+random.NextDouble():.55+random.NextDouble()*.6;
            double rx=size,ry=size*(.6+random.NextDouble()*.7),angle=random.NextDouble()*Math.PI,phase=random.NextDouble()*6;
            double cx=2+random.NextDouble()*(width-4), cy=2+random.NextDouble()*(height-4);
            double size=accepted<3?3+random.NextDouble()*1.5:accepted<7?1.5+random.NextDouble()*1.5:.6+random.NextDouble()*.8;
            double ry=size*(.6+random.NextDouble()*.7), angle=random.NextDouble()*Math.PI, phase=random.NextDouble()*6;
            var candidate=new HashSet<GridPosition>();
            for(int y=1;y<19;y++) for(int x=1;x<19;x++)
            for(int y=1;y<height-1;y++) for(int x=1;x<width-1;x++)
            {
                var p=new GridPosition(x,y); if(Reserved(p)) continue;
                double dx=x-cx,dy=y-cy,u=(dx*Math.Cos(angle)+dy*Math.Sin(angle))/rx,v=(-dx*Math.Sin(angle)+dy*Math.Cos(angle))/ry;
                double a=Math.Atan2(v,u),coast=1+.2*Math.Sin(3*a+phase)+.12*Math.Cos(5*a-phase);
                var p=new GridPosition(x,y); if(!Inside(p)||Reserved(p)||p.OrthogonalNeighbors().Any(n=>!Inside(n))) continue;
                double dx=x-cx,dy=y-cy,u=(dx*Math.Cos(angle)+dy*Math.Sin(angle))/size,v=(-dx*Math.Sin(angle)+dy*Math.Cos(angle))/ry;
                double a=Math.Atan2(v,u),coast=1+.22*Math.Sin(3*a+phase)+.13*Math.Cos(5*a-phase);
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
            if(largest.Count==0 || accepted<3&&largest.Count<10) continue;
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
            for(int y=1;y<height-1;y++) for(int x=1;x<width-1;x++)
            {
                var p=new GridPosition(x,y);
                if(land.Contains(p)||!GameBoard.IsNarrowPassage(p,land.Contains)) continue;
                if(!Inside(p)||land.Contains(p)||!GameBoard.IsNarrowPassage(p,land.Contains)) continue;
                var bank=p.OrthogonalNeighbors().Where(land.Contains).OrderBy(q=>q.OrthogonalNeighbors().Count(land.Contains)).First();
                land.Remove(bank); widened=true;
            }
        } while(widened);
        return new GameBoard(20,20,p=>land.Contains(p)?TerrainType.Land:TerrainType.Water,seed);
        return new GameBoard(width,height,p=>land.Contains(p)?TerrainType.Land:TerrainType.Water,seed,Inside,boundary);
    }
}
