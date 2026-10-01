using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Units;
using DevAncientNaval.Core.World;

internal static partial class BattleScenarios
{
    private static void SeaEventRules()
    {
        RepairRules(); MortarTurnOrder(); TreasuryRules(); PirateRules();
    }

    private static void RepairRules()
    {
        var b=Fixture(ShipClass.Kolonel); var ship=b.Find(3)!;
        ship.Health=4;
        Check(b.Repair(Side.Player,3).Amount==5 && ship.Health==9,"Manual repair heals five");
        Check(!b.Repair(Side.Player,3).Success,"Manual repair cannot repeat");
        b.EndTurn(Side.Player); Check(ship.Health==9,"Manual repair prevents a second automatic repair");
        b.EndTurn(Side.Enemy); b.EndTurn(Side.Player);
        Check(ship.Health==11,"Automatic repair restores two HP");
        b.EndTurn(Side.Enemy);
        b.Attack(Side.Player,3,4); double damaged=ship.Health;
        b.EndTurn(Side.Player); Check(ship.Health==damaged,"Own attack prevents automatic repair");

        b=Fixture(ShipClass.Kolonel); ship=b.Find(3)!;
        b.EndTurn(Side.Player); b.Attack(Side.Enemy,4,3);
        Check(ship.AttacksUsed==0,"Counterattack does not consume an attack action");
        b.EndTurn(Side.Enemy); damaged=ship.Health;
        b.EndTurn(Side.Player);
        Check(ship.Health==Math.Min(ship.MaxHealth,damaged+2),"Counterattack allows automatic repair on the next own turn");

        b=VillageArena(); var town=b.Villages.Single(); town.Health=0; Round(b);
        Check(b.CaptureVillage(Side.Player,town.Id).Success && b.Find(3)!.IsExhausted,"Capture spends the adjacent ship's actions");
        Check(!b.Move(Side.Player,3,new(7,8)).Success && !b.Repair(Side.Player,3).Success,"Captor cannot move or repair afterward");
        town.Level=5; town.Health=10;
        Check(b.RepairVillage(Side.Player,town.Id).Amount==5 && town.Health==15,"Active village repair heals five");
        Check(!b.RepairVillage(Side.Player,town.Id).Success && b.VillageBuildBlockReason(Side.Player,town.Id,ShipClass.Garrison) is not null,"Village repair is one action, blocking production");
        Round(b); Check(town.Health==15,"Village active repair prevents a duplicate automatic repair");
        Round(b); Check(town.Health==20,"Village automatic repair also heals five");
        var distant=new GridPosition(13,8);
        Check(!b.Vision.IsVisible(Side.Player,distant),"Ordinary village sight excludes four cells away");
        Check(b.FortifyVillage(Side.Player,town.Id).Success && town.VisualRange==5 && b.Vision.IsVisible(Side.Player,distant),"Fortification immediately extends village sight by two");

        b=VillageArena(); town=b.Villages.Single(); town.Health=0;
        b.Find(1)!.Position=new(9,7); b.Vision.Recompute(b.Ships,b.Round,b.Villages); Round(b);
        Check(b.Move(Side.Player,3,new(7,8)).Success && b.CanCaptureVillage(Side.Player,town.Id),"A second waiting crew preserves capture if one ship leaves");
        Check(b.CaptureVillage(Side.Player,town.Id).Success && b.Find(1)!.IsExhausted,"Waiting flagship can capture the town");
        Check(b.BuildBlockReason(Side.Player,1,ShipClass.Garrison) is not null && b.RadarBlockReason(Side.Player,1) is not null && b.CollectionCells(1).Count==0,"Capturing flagship loses production, equipment and collection actions");

    }

    private static void MortarTurnOrder()
    {
        var passage=Fixture(target:new(9,8));
        Check(passage.StepCost(3,new(8,8),new(9,9))==20,"Passing diagonally beside an enemy is slowed, not blocked");
        var b=Fixture(ShipClass.Togus,target:new(12,8));
        Check(b.Attack(Side.Player,3,4).Success && b.Move(Side.Player,3,new(8,9)).Success,"Granado shoots then moves");
        Round(b);
        Check(b.Move(Side.Player,3,new(8,8)).Success && !b.Attack(Side.Player,3,4).Success && b.Find(3)!.AttacksRemaining==0,"Granado cannot fire after moving");
        b=Fixture(ShipClass.Togus); var towerRules=Funded;
        b=new BattleState(new GameBoard(20,20,_=>TerrainType.Water),towerRules,new[]{
            (Side.Player,ShipClass.Mothership,new GridPosition(1,1)),(Side.Enemy,ShipClass.Mothership,new GridPosition(18,18)),
            (Side.Player,ShipClass.AncientGun,new GridPosition(8,8)),(Side.Enemy,ShipClass.Kolonel,new GridPosition(13,8))},Array.Empty<GridPosition>());
        var tower=b.Find(3)!; tower.Health=3;
        Check(!tower.CanMove && tower.RadarRange==5 && tower.VisualRange==3 && tower.CurrentMortarDamage==5,"Ancient tower is immobile with fixed damage and specified sight/radar");
        Check(b.Attack(Side.Player,3,4).Amount==5 && !b.Repair(Side.Player,3).Success,"Tower attacks radar contacts for five and cannot repair");
        Round(b); Round(b); Check(tower.Health==3,"Tower never repairs automatically");
    }

    private static void TreasuryRules()
    {
        var seen=new HashSet<TreasuryReward>();
        for(int seed=0;seed<45;seed++)
        {
            var b=new BattleState(new GameBoard(20,20,_=>TerrainType.Water),Funded,new[]{
                (Side.Player,ShipClass.Mothership,new GridPosition(1,1)),(Side.Enemy,ShipClass.Mothership,new GridPosition(18,18)),
                (Side.Player,ShipClass.Garrison,new GridPosition(2,2))},Array.Empty<GridPosition>(),resourceSeed:seed,seaEvents:true);
            var treasure=b.Treasuries.First(); var ship=b.Find(3)!; ship.Position=treasure.Position;
            Check(!b.CanLootTreasury(Side.Player,3),"Treasury cannot be looted on arrival");
            b.EndTurn(Side.Player); b.EndTurn(Side.Enemy); b.EndTurn(Side.Pirates);
            Check(b.CanLootTreasury(Side.Player,3),"Holding a treasury until next turn enables loot");
            int credits=b.Credits(Side.Player);
            var result=b.LootTreasury(Side.Player,3); var reward=b.LastTreasuryReward!.Value; seen.Add(reward);
            Check(result.Success && b.TreasuryAt(treasure.Position) is null && !b.LootTreasury(Side.Player,3).Success,"Treasury resolves once and disappears");
            switch(reward)
            {
                case TreasuryReward.Currency: Check(b.Credits(Side.Player)==credits+5,"Coin reward gives five Thors"); break;
                case TreasuryReward.Resources: Check(b.Find(1)!.Level==2 && b.PendingUpgrade(Side.Player) is not null,"Two resources trigger ordinary flagship progression"); break;
                case TreasuryReward.AncientBalloon: Check(b.OwnShips(Side.Player).Any(s=>s.IsAirborne && s.IsAncient && s.IsExhausted),"Ancient balloon reward belongs to looter and waits until next turn"); break;
                case TreasuryReward.AncientGun: Check(b.OwnShips(Side.Player).Any(s=>s.Definition.Class==ShipClass.AncientGun && s.IsExhausted),"Tower reward belongs to looter and waits until next turn"); break;
                case TreasuryReward.Whirlpool:
                    Check(b.Find(3) is null && b.Whirlpools.Single().Cells.Count==9 && b.Whirlpools.Single().Cells.All(b.IsForbidden),"Whirlpool sinks looter and blocks exactly nine cells");
                    Check(b.Whirlpools.Single().Cells.All(p=>!b.IsFreeWater(p)),"Whirlpool cannot accept ship construction"); break;
            }
            if(b.Find(3) is { } survivor) Check(survivor.IsExhausted,"Loot spends the ship's actions");
        }
        Check(seen.Count==5,"Seeded treasury scenarios exercise all five weighted outcomes");
    }

    private static void PirateRules()
    {
        var b=new BattleState(new GameBoard(20,20,_=>TerrainType.Water),Funded,new[]{
            (Side.Player,ShipClass.Mothership,new GridPosition(1,1)),(Side.Enemy,ShipClass.Mothership,new GridPosition(18,18)),
            (Side.Player,ShipClass.Kolonel,new GridPosition(8,8)),(Side.Pirates,ShipClass.PirateSchooner,new GridPosition(10,8))},Array.Empty<GridPosition>());
        var pirate=b.Find(4)!; int credits=b.Credits(Side.Player);
        Check(pirate.MaxHealth==7 && pirate.FullDamage==4 && pirate.VisualRange>Rules.Get(ShipClass.Garrison).VisualRange,"Pirate is slightly stronger and sees farther than a Brig");
        b.Attack(Side.Player,3,4); b.Attack(Side.Player,3,4);
        Check(b.Find(4) is null && b.Credits(Side.Player)==credits+2 && b.Find(1)!.Resources==1,"Destroying pirate grants exactly two Thors and one resource");
        b.EndTurn(Side.Player); b.EndTurn(Side.Enemy);
        Check(b.ActiveSide==Side.Player,"Destroyed pirate no longer receives a turn");

        b=new BattleState(new GameBoard(20,20,_=>TerrainType.Water),Funded,new[]{
            (Side.Player,ShipClass.Mothership,new GridPosition(1,1)),(Side.Enemy,ShipClass.Mothership,new GridPosition(18,18)),
            (Side.Player,ShipClass.Kolonel,new GridPosition(8,8)),(Side.Pirates,ShipClass.PirateSchooner,new GridPosition(10,8))},Array.Empty<GridPosition>());
        b.EndTurn(Side.Player);b.EndTurn(Side.Enemy);
        Check(b.PirateStep().Kind==CommandKind.Attack && b.Find(3)!.Health<15,"Pirate attacks an enemy that enters its sight");

        b=new BattleState(new GameBoard(20,20,_=>TerrainType.Water),Funded,new[]{
            (Side.Player,ShipClass.Mothership,new GridPosition(1,1)),(Side.Enemy,ShipClass.Mothership,new GridPosition(18,18)),
            (Side.Pirates,ShipClass.PirateSchooner,new GridPosition(10,8))},Array.Empty<GridPosition>());
        var home=b.Find(3)!.Position; var visited=new HashSet<GridPosition>();
        for(int round=0;round<10;round++)
        {
            b.EndTurn(Side.Player); b.EndTurn(Side.Enemy); int limit=8;
            while(b.ActiveSide==Side.Pirates && limit-->0)
            {
                Check(b.PirateStep().Success,"Pirate takes legal patrol actions"); visited.Add(b.Find(3)!.Position);
                Check(b.Board.InRadius(home,b.Find(3)!.Position,5),"Pirate remains inside its patrol territory");
            }
            Check(b.ActiveSide==Side.Player,"Pirate finishes its turn");
        }
        Check(visited.Count>1,"Pirate patrol changes position");
    }
}
