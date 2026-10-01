using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Units;
using DevAncientNaval.Core.Vision;
using DevAncientNaval.Core.World;

internal static partial class BattleScenarios
{
    private static void MortarsDocksMaps()
    {
        foreach(var (kind,damage,range) in new[] { (ShipClass.Garrison,3,2),(ShipClass.Invader,5,2),(ShipClass.Kolonel,4,3),(ShipClass.Mothership,3,3),(ShipClass.Togus,8,5) })
            Check(Rules.Get(kind).Damage==damage&&Rules.Get(kind).AttackRange==range,"Exact requested gun balance");
        var b=Fixture(ShipClass.Kolonel,ShipClass.Fishing);
        b.Move(Side.Player,3,new(9,8));
        Check(b.Find(3)!.AttacksRemaining==2&&b.Attack(Side.Player,3,4).Success&&b.Attack(Side.Player,3,4).Success,"Kolonel retains both attacks after movement");
        b=Fixture(ShipClass.Togus,target:new(12,8)); var togus=b.Find(3)!; var target=b.Find(4)!;
        Check(togus.HasMortar&&togus.HasRadar&&togus.MaxHealth==5&&togus.MortarRange==5&&togus.CurrentMortarDamage==8,"Togus mortar balance");
        foreach(var p in new[] {new GridPosition(9,8),new(11,8),new(11,9),new(14,8)})
        { target.Position=p; b.Vision.Recompute(b.Ships,1); Check(!b.CanAttack(3,4),"Mortar excludes dead zone and beyond five"); }
        target.Position=new(13,8); b.Vision.Recompute(b.Ships,1);
        Check(b.CanAttack(3,4)&&b.FindObserved(Side.Player,4) is null,"Mortar targets unseen contact at five");
        var result=b.Attack(Side.Player,3,4);
        Check(result.Success&&result.Shots![0].IsMortar&&result.Shots.Count==1&&result.Shots[0].Damage==8,"Mortar deals eight without distant reply");
        Check(!result.Shots![0].TargetVisibleToPlayer&&b.FindObserved(Side.Player,4) is null&&!b.Vision.IsVisible(Side.Player,target.Position),"Hit never reveals radar target");
        Check(!result.Message.Contains('8')&&!result.Message.Contains("Kolonel")&&!result.Message.Contains("sunk",StringComparison.OrdinalIgnoreCase),"No hidden damage or identity in message");
        Round(b); target.Health=1; result=b.Attack(Side.Player,3,4);
        Check(b.Find(4) is null&&!result.Message.Contains("sunk",StringComparison.OrdinalIgnoreCase)&&!result.Shots![0].TargetVisibleToPlayer,"Hidden kill has no optical confirmation");
        b=Fixture(ShipClass.Garrison,ShipClass.Togus); result=b.Attack(Side.Player,3,4);
        Check(result.Success&&result.Shots!.Count==1&&!b.CanCounterattack(b.Find(4)!,b.Find(3)!),"Togus never counterattacks");
        b=Fixture(target:new(10,5)); b.BuyRadar(Side.Player,1);
        Check(!b.BuyMortar(Side.Player,1).Success,"Mother mortar locked below five");
        b.Find(1)!.Level=5; b.Find(1)!.Health=b.Find(1)!.MaxHealth; int money=b.Credits(Side.Player);
        Check(b.BuyMortar(Side.Player,1).Success&&b.Credits(Side.Player)==money-10&&b.Find(1)!.CurrentMortarDamage==8,"Mother mortar costs ten at five");
        Check(!b.BuyMortar(Side.Player,1).Success&&b.CanAttack(1,4),"Mortar purchase once");
        b.Find(4)!.Position=new(6,5); b.Vision.Recompute(b.Ships,1);
        Check(b.CanAttack(1,4)&&!b.UsesMortar(b.Find(1)!,new(6,5)),"Mother retains close-range cannon");
        b=Fixture(ShipClass.Kolonel,target:new(13,8)); b.BuyRadar(Side.Player,3);
        Check(!b.CanAttack(3,4)&&b.Vision.Contacts(Side.Player).Contains(new(13,8)),"Radar never extends cannon");
        b=Fixture(ShipClass.Fishing,target:new(10,10));
        Check(b.Vision.IsVisible(Side.Player,new(10,10))&&!b.Vision.IsVisible(Side.Player,new(11,10)),"Fishing square sight");

        b=Fixture(fish:Resources);
        Check(b.DockCells(1).Count==0&&b.DockCells(3).Count==0,"Docks locked at one");
        b.Collect(Side.Player,1,Resources[0]); b.Collect(Side.Player,1,Resources[1]); b.ChooseUpgrade(Side.Player,1,UpgradeChoice.Mobility);
        b.Collect(Side.Player,1,Resources[2]);
        var site=b.DockCells(1).First(); money=b.Credits(Side.Player);
        result=b.BuildDock(Side.Player,1,site); var dock=b.Find(result.TargetId)!;
        Check(result.Success&&dock.IsStructure&&dock.Health==10&&!dock.CanMove&&!dock.IsArmed,"Dock is destructible noncombat structure");
        Check(b.Credits(Side.Player)==money-4&&b.Find(1)!.Level==3&&b.Find(1)!.Resources==0,"Dock grants two resources");
        Check(b.Income(Side.Player)==7&&!b.Shoals.Contains(site)&&!b.BuildDock(Side.Player,1,site).Success,"Dock income and consumption once");
        b.ChooseUpgrade(Side.Player,1,UpgradeChoice.Vision);
        b.Find(4)!.Position=site.OrthogonalNeighbors().First(p=>b.IsFreeWater(p)); b.EndTurn(Side.Player); dock.Health=6;
        int kills=b.Find(4)!.Kills; b.Attack(Side.Enemy,4,dock.Id); b.Attack(Side.Enemy,4,dock.Id);
        Check(b.Find(dock.Id) is null&&b.Income(Side.Player)==6&&b.Find(4)!.Kills==kills,"Destroyed dock removes income and gives no veteran kill");
        b.EndTurn(Side.Enemy);
        Check(b.Shoals.Contains(site)&&b.BuildDock(Side.Player,1,site).Success,"A destroyed fishing dock can actually be rebuilt on its restored shoal");

        b=Fixture(); var mother=b.Find(1)!; mother.Level=3; mother.PendingUpgradeLevel=3; mother.Health=20;
        Check(b.ChooseUpgrade(Side.Player,1,UpgradeChoice.Restoration).Success&&mother.MaxHealth==35&&mother.Health==25,"Restoration grants five HP");
        mother.Level=5; mother.PendingUpgradeLevel=5;
        Check(b.ChooseUpgrade(Side.Player,1,UpgradeChoice.Firepower).Success,"Final firepower choice");
        b.Find(4)!.Position=new(6,5);
        Check(b.Damage(mother,b.Find(4)!,true)==mother.CurrentDamage+2,"Firepower adds two counter damage");
        b=Fixture(); mother=b.Find(1)!; mother.Level=5; mother.PendingUpgradeLevel=5;
        Check(b.ChooseUpgrade(Side.Player,1,UpgradeChoice.Shipwright).Success&&b.BuildPrice(Side.Player,ShipClass.Kolonel)==6&&b.BuildPrice(Side.Player,ShipClass.Togus)==5,"Shipwright discount");
        for(int level=1;level<=5;level++)
        {
            b=Fixture(); b.Find(1)!.Level=level; b.SetCreative(true);
            foreach(var type in new[] {ShipClass.Garrison,ShipClass.Fishing,ShipClass.Invader,ShipClass.Kolonel,ShipClass.Togus})
                Check((b.BuildBlockReason(Side.Player,1,type) is null)==(level>=BattleState.RequiredLevel(type)),"Unlocks enforced even in creative");
        }
        var resources=(from y in Enumerable.Range(3,5) from x in Enumerable.Range(3,5) let p=new GridPosition(x,y) where p!=new GridPosition(5,5)&&BattleVision.InRadius(p,new(5,5),2) select p).Take(14).ToArray();
        b=Fixture(fish:resources); mother=b.Find(1)!;
        foreach(var p in resources) { Check(b.Collect(Side.Player,1,p).Success,"Earn all fourteen resources"); if(mother.PendingUpgradeLevel>0) b.ChooseUpgrade(Side.Player,1,b.UpgradeOptions(1)[0]); }
        Check(mother.Level==5&&mother.Resources==0&&mother.ResourcesRequired==0&&b.PendingUpgrade(Side.Player) is null,"Five is final level after its upgrade choice");
        Check(mother.MaxHealth==40&&mother.FullDamage==3&&b.CollectionCells(1).Count==0,"Five adds four hull increments; shipwright keeps base gun");

        var signatures=new HashSet<string>();
        for(int seed=0;seed<100;seed++)
        {
            var board=ArchipelagoGenerator.Create(seed); var again=ArchipelagoGenerator.Create(seed);
            var land=board.Tiles.Where(t=>t.Terrain==TerrainType.Land).Select(t=>t.Position).ToHashSet();
            Check(board.Boundary.Count==6 && board.Mesh!.SideTileCounts.Distinct().Count()==6 && board.Mesh.SideTileCounts.All(n=>n is >=24 and <=30),"Hexagon has six different side counts of 24–30 tiles");
            CheckPentagonalTopology(board);
            Check(board.Tiles.Count<board.Width*board.Height&&!board.Contains(new(0,0)),"Outside pentagon is not playable");
            Check(land.Count>=30&&land.Count<500,"Island variety and open sea");
            Check(board.Tiles.SequenceEqual(again.Tiles),"Seed reproducible");
            Check(!board.Tiles.Any(t=>t.Terrain!=TerrainType.Land&&board.IsNarrowPassage(t.Position)),"No one-cell island straits");
            var a=board.FleetAnchor(false); var z=board.FleetAnchor(true);
            var q=new Queue<GridPosition>(); var visited=new HashSet<GridPosition>(); q.Enqueue(a);
            while(q.TryDequeue(out var p)) { if(!visited.Add(p)) continue; foreach(var n in board.GetNeighbors(p)) if(board.GetTile(n).Terrain!=TerrainType.Land&&!visited.Contains(n)) q.Enqueue(n); }
            Check(visited.Contains(z)&&board.HarborCells(false).All(visited.Contains)&&board.HarborCells(true).All(visited.Contains),"Both harbors connected");
            var game=new BattleState(board,Rules,new[] {(Side.Player,ShipClass.Mothership,a),(Side.Enemy,ShipClass.Mothership,z)},resourceSeed:seed);
            Check(game.Villages.Count>0 && game.Villages.Count(v=>board.StartingTerritory(v.Position)==0)==game.Villages.Count(v=>board.StartingTerritory(v.Position)==1),"Equal villages on both sides");
            Check(game.FishSpots.Count<=20&&game.Shoals.Count<=8,"Sparse fish supply");
            Check(game.CollectionCells(1).Count>=2&&game.CollectionCells(2).Count==0,"Player start has two resources; enemy waits turn");
            signatures.Add(string.Join(',',land.OrderBy(p=>p.X).ThenBy(p=>p.Y)));
        }
        Check(signatures.Count==100,"Different seeds change islands");
    }
}
