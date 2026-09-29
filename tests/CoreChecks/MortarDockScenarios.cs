using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Units;
using DevAncientNaval.Core.Vision;
using DevAncientNaval.Core.World;

internal static partial class BattleScenarios
{
    private static void MortarsDocksMaps()
    {
        var b=Fixture(ShipClass.Togus,target:new(12,8)); var togus=b.Find(3)!; var target=b.Find(4)!;
        Check(togus.HasMortar&&togus.HasRadar&&togus.MaxHealth==15&&togus.RadarRange==6,"Togus has builtin mortar/radar and fragile hull");
        foreach(var p in new[] {new GridPosition(9,8),new(11,8),new(11,9)})
        { target.Position=p; b.Vision.Recompute(b.Ships,1); Check(!b.CanAttack(3,4),"Mortar excludes entire radius three"); }
        target.Position=new(14,8); b.Vision.Recompute(b.Ships,1);
        Check(b.CanAttack(3,4)&&b.FindObserved(Side.Player,4) is null,"Mortar can target radar boundary");
        var result=b.Attack(Side.Player,3,4);
        Check(result.Success&&result.Shots![0].IsMortar&&result.Shots.Count==1,"Mortar uses ballistic shot without distant reply");
        b=Fixture(ShipClass.Garrison,ShipClass.Togus); result=b.Attack(Side.Player,3,4);
        Check(result.Success&&result.Shots!.Count==1&&!b.CanCounterattack(b.Find(4)!,b.Find(3)!),"Togus never counterattacks");
        b=Fixture(target:new(10,5));
        Check(!b.BuyMortar(Side.Player,1).Success,"Mother mortar requires installed radar");
        b.BuyRadar(Side.Player,1); int money=b.Credits(Side.Player);
        Check(!b.CanAttack(1,4)&&b.BuyMortar(Side.Player,1).Success&&b.Credits(Side.Player)==money-10,"Only mortar extends mother's gun to radar");
        Check(!b.BuyMortar(Side.Player,1).Success&&b.CanAttack(1,4),"Mortar purchase once");
        b.Find(4)!.Position=new(6,5); b.Vision.Recompute(b.Ships,1);
        Check(b.CanAttack(1,4)&&!BattleState.UsesMortar(b.Find(1)!,new(6,5)),"Mother retains normal close-range gun");
        b=Fixture(ShipClass.Kolonel,target:new(13,8)); b.BuyRadar(Side.Player,3);
        Check(!b.CanAttack(3,4)&&b.Vision.Contacts(Side.Player).Contains(new(13,8)),"Heavy radar does not extend normal cannon");
        b=Fixture(ShipClass.Fishing,target:new(10,10));
        Check(b.Vision.IsVisible(Side.Player,new(10,10))&&!b.Vision.IsVisible(Side.Player,new(11,10)),"Fishing sees a full five by five square");

        b=Fixture(fish:Resources); b.Collect(Side.Player,1,Resources[0]);
        var site=b.DockCells(1).First(); money=b.Credits(Side.Player);
        result=b.BuildDock(Side.Player,1,site); var dock=b.Find(result.TargetId)!;
        Check(result.Success&&dock.IsStructure&&dock.Health==15&&!dock.CanMove&&!dock.IsArmed,"Dock is stationary destructible structure");
        Check(b.Credits(Side.Player)==money-4&&b.Find(1)!.Level==2&&b.Find(1)!.Resources==1,"Dock grants exactly two points with carried progress");
        Check(b.Income(Side.Player)==5&&!b.Shoals.Contains(site)&&!b.BuildDock(Side.Player,1,site).Success,"Dock income once; site consumed once");
        b.ChooseUpgrade(Side.Player,1,UpgradeChoice.Mobility);
        b.Find(4)!.Position=new(site.X+1,site.Y); b.EndTurn(Side.Player);
        int kills=b.Find(4)!.Kills; b.Attack(Side.Enemy,4,dock.Id); b.Attack(Side.Enemy,4,dock.Id);
        Check(b.Find(dock.Id) is null&&b.Income(Side.Player)==4&&b.Find(4)!.Kills==kills,"Destroying dock removes income without ship veteran credit");
        b=Fixture(); var mother=b.Find(1)!; mother.Level=4; mother.PendingUpgradeLevel=4; mother.Health=40;
        Check(b.ChooseUpgrade(Side.Player,1,UpgradeChoice.Fortification).Success&&mother.MaxHealth==85&&mother.Health==45,"Level four fortification grants five HP");
        b.Find(4)!.Position=new(6,5);
        Check(b.Damage(mother,b.Find(4)!,true)==b.Damage(mother,b.Find(4)!)+3,"Fortification adds three only to counter damage");
        b=Fixture(); mother=b.Find(1)!; mother.Level=4; mother.PendingUpgradeLevel=4;
        Check(b.ChooseUpgrade(Side.Player,1,UpgradeChoice.Shipwright).Success&&b.BuildPrice(Side.Player,ShipClass.Kolonel)==6&&b.BuildPrice(Side.Player,ShipClass.Togus)==5&&b.BuildPrice(Side.Player,ShipClass.Garrison)==1,"Shipwright rounds discounted prices down");
        Check(b.DockPrice(Side.Player)==4&&b.BuildPrice(Side.Enemy,ShipClass.Kolonel)==8,"Discount only own ships");

        var signatures=new HashSet<string>();
        for(int seed=0;seed<100;seed++)
        {
            var board=ArchipelagoGenerator.Create(seed); var again=ArchipelagoGenerator.Create(seed);
            var land=board.Tiles.Where(t=>t.Terrain==TerrainType.Land).Select(t=>t.Position).ToHashSet();
            Check(land.Count>=14&&land.Count<130,"Generated island sizes bounded");
            Check(board.Tiles.Select(t=>t.Terrain).SequenceEqual(again.Tiles.Select(t=>t.Terrain)),"Seed is reproducible");
            Check(!board.Tiles.Any(t=>t.Terrain!=TerrainType.Land&&board.IsNarrowPassage(t.Position)),"No one-cell island straits");
            var q=new Queue<GridPosition>(); var visited=new HashSet<GridPosition>(); q.Enqueue(new(2,9));
            while(q.TryDequeue(out var p)) { if(!visited.Add(p)) continue; foreach(var n in board.GetNeighbors(p)) if(board.GetTile(n).Terrain!=TerrainType.Land&&!visited.Contains(n)) q.Enqueue(n); }
            Check(visited.Contains(new(17,9))&&visited.Contains(new(3,11))&&visited.Contains(new(16,7)),"Starting fleets share navigable open sea");
            signatures.Add(string.Join(',',land.OrderBy(p=>p.X).ThenBy(p=>p.Y)));
        }
        Check(signatures.Count==100,"Different seeds produce different maps");
    }
}
