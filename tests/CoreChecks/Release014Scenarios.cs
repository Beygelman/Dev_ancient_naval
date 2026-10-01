using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Units;
using DevAncientNaval.Core.World;
using DevAncientNaval.Core.AI;

internal static partial class BattleScenarios
{
    private static void Release014Rules()
    {
        var counts=Enumerable.Range(0,100).GroupBy(BattleState.RewardForRoll).ToDictionary(g=>g.Key,g=>g.Count());
        Check(counts.Count==5 && counts.All(g=>g.Value==(g.Key==TreasuryReward.Whirlpool?12:22)),"Exact 12/22/22/22/22 reward table");
        var b=Fixture(ShipClass.Kolonel,ShipClass.Balloon,new(8,5));
        b.BuyRadar(Side.Player,1); b.Find(1)!.Level=5; b.Find(1)!.Health=b.Find(1)!.MaxHealth; b.BuyMortar(Side.Player,1);
        Check(b.CanAttack(1,4)&&!b.CanAttack(3,4)&&b.Damage(b.Find(1)!,b.Find(4)!)==3,"Only flagship cannons cover balloon within three");
        b.Find(4)!.Position=new(9,5);b.Vision.Recompute(b.Ships,b.TurnSerial);
        Check(!b.CanAttack(1,4),"Mortar cannot shoot down a balloon four tiles away");
        b.Find(4)!.Position=new(8,5);b.Vision.Recompute(b.Ships,b.TurnSerial);
        Check(b.AttackAt(Side.Player,1,new(8,5)).Success && b.Find(4) is null,"A one HP balloon is destroyed by one targeted shot");
        b=Fixture();var mother=b.Find(1)!;
        mother.Level=3;Check(mother.MovementAllowance==1,"Level three retains initial speed");
        mother.Level=4;Check(mother.MovementAllowance==2,"Level four adds a movement tile independently of upgrade choice");
        mother.MobilityUpgrade=true;Check(mother.MovementAllowance==3,"Level four movement stacks with mobility choice");

        b=new BattleState(new GameBoard(20,20,_=>TerrainType.Water),Funded,new[]{
            (Side.Player,ShipClass.Mothership,new GridPosition(1,1)),(Side.Enemy,ShipClass.Mothership,new GridPosition(18,18)),
            (Side.Player,ShipClass.Togus,new GridPosition(8,8)),(Side.Enemy,ShipClass.Kolonel,new GridPosition(12,8)),
            (Side.Enemy,ShipClass.Garrison,new GridPosition(12,9)),(Side.Player,ShipClass.Garrison,new GridPosition(11,9)),
            (Side.Enemy,ShipClass.FishingDock,new GridPosition(13,9))},Array.Empty<GridPosition>());
        b.Find(7)!.Health=2;var volley=b.Attack(Side.Player,3,4);
        Check(volley.Amount==8 && volley.Splash!.Count==2 && b.Find(5)!.Health==3 && b.Find(6)!.Health==5,"Mortar adds two splash damage only to enemy neighbors");
        Check(b.Find(7) is null&&b.Shoals.Contains(new(13,9)),"Splash-destroyed dock restores a reusable fishing site");

        var tinyCap=new BattleRules{StartingCredits=30,FleetLimit=1,Ships=Rules.Ships};
        b=new BattleState(new GameBoard(20,20,_=>TerrainType.Water),tinyCap,new[]{
            (Side.Player,ShipClass.Mothership,new GridPosition(5,5)),(Side.Enemy,ShipClass.Mothership,new GridPosition(18,18)),
            (Side.Player,ShipClass.Fishing,new GridPosition(5,6))},Array.Empty<GridPosition>());
        Check(b.BuildBlockReason(Side.Player,1,ShipClass.Garrison) is not null && b.Build(Side.Player,1,ShipClass.Fishing,new(6,5)).Success,"Fishing boats can be built at combat fleet capacity");

        // Preserve every action flag, cooldown, discovery, mesh coordinate and wait.
        for(int seed=20;seed<24;seed++)
        {
            b=SkirmishSetup.Create(ArchipelagoGenerator.Create(seed),Funded);b.SetPlayerColor(FleetColor.Purple);b.SetCreative(true);
            Check(b.Treasuries.Count==6 && b.Treasuries.Count(t=>b.Board.StartingTerritory(t.Position)==0)==3,"Three treasuries in each starting territory");
            Check(b.Ships.Count(s=>s.Owner==Side.Pirates)==2,"One pirate per player");
            var center=b.Board.Mesh!.Boundary.Aggregate(System.Numerics.Vector2.Zero,(a,p)=>a+p)/6;
            var axisA=System.Numerics.Vector2.Normalize(b.Board.Center(b.Board.FleetAnchor(false))-center);
            var axisB=System.Numerics.Vector2.Normalize(b.Board.Center(b.Board.FleetAnchor(true))-center);
            Check(System.Numerics.Vector2.Dot(axisA,axisB)<-.99f,"Fleet anchors occupy opposite ends of a common center axis");
            var first=b.Find(2)!; var treasure=b.Treasuries.First();first.Position=treasure.Position;
            b.EndTurn(Side.Player);b.EndTurn(Side.Enemy);b.EndTurn(Side.Pirates);
            var saved=b.SaveJson();var restored=BattleState.LoadJson(saved);
            Check(restored.PlayerColor==FleetColor.Purple&&restored.Creative,"Color and mode survive save");
            Check(restored.Board.Mesh!.Vertices.SequenceEqual(b.Board.Mesh!.Vertices),"Save restores exact tile geometry");
            Check(restored.SaveJson()==saved,"Entire state round trip preserves action flags, discovery, waits and economy");
            Check(b.CanLootTreasury(Side.Player,first.Id)==restored.CanLootTreasury(Side.Player,first.Id),"Treasury wait remains ready after loading");
            var originalReward=b.LootTreasury(Side.Player,first.Id);var loadedReward=restored.LootTreasury(Side.Player,first.Id);
            Check(originalReward.Success&&loadedReward.Success&&b.LastTreasuryReward==restored.LastTreasuryReward,"Saving cannot reroll a treasury reward");
            Check(b.SaveJson()==restored.SaveJson(),"Reward resolves identically after loading");
            // Reload again after an outcome; includes generated reward entities or whirlpool.
            Check(BattleState.LoadJson(b.SaveJson()).SaveJson()==b.SaveJson(),"Reward state survives a second round trip");
        }
        b=VillageArena();var town=b.Villages.Single();town.Health=0;Round(b);b.CaptureVillage(Side.Player,town.Id);b.FortifyVillage(Side.Player,town.Id);
        b.Find(1)!.PendingUpgradeLevel=3;b.Find(1)!.Level=3;
        var loaded=BattleState.LoadJson(b.SaveJson());
        Check(loaded.Villages.Single().IsFortified && loaded.PendingUpgrade(Side.Player)?.Id==1 && loaded.Find(3)!.IsExhausted,"Village, pending choice and exhausted captor survive loading");
    }
}
