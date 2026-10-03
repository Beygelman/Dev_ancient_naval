using System.Text.Json.Nodes;
using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Units;
using DevAncientNaval.Core.World;

internal static class Refinement020Checks
{
    internal static int Run(BattleRules rules)
    {
        int checks = 0;
        void Check(bool condition, string message)
        {
            if (!condition) throw new Exception("0.20: " + message);
            checks++;
        }
        Check(rules.FreeCoastalNavigation && rules.Balloon.KolonelAntiAir, "new voyages enable free coast and Kolonel AA");
        foreach (var kind in new[] {ShipClass.Mothership, ShipClass.Garrison, ShipClass.Fishing, ShipClass.Invader, ShipClass.Kolonel, ShipClass.Togus})
        {
            var battle = new BattleState(new GameBoard(20,20,p => p.X is 7 or 9 && p.Y == 8 ? TerrainType.Land : TerrainType.Water), rules,
                new[] {(Side.Player,kind == ShipClass.Mothership ? ShipClass.Garrison : ShipClass.Mothership,new GridPosition(2,2)),(Side.Enemy,ShipClass.Mothership,new GridPosition(18,18)),(Side.Player,kind,new GridPosition(8,7))}, Array.Empty<GridPosition>());
            battle.SetGodEye(true);
            Check(battle.Board.IsNarrowPassage(new(8,8)), "fixture is a one-cell river");
            Check(battle.StepCost(3,new(8,7),new(8,8),false)==10, kind+" has no narrow/coastal penalty");
            Check(battle.PreviewMovement(3).Costs.ContainsKey(new(8,8)), kind+" preview reaches the river");
            Check(battle.Move(Side.Player,3,new(8,8)).Success && battle.Find(3)!.Position==new GridPosition(8,8), kind+" actually enters the river");
            Check(battle.StepCost(3,new(8,8),new(7,8),false) is null, "land still blocks movement");
            Check(BattleState.LoadJson(battle.SaveJson()).Rules.FreeCoastalNavigation, "new navigation policy survives Continue");
        }
        var air = new BattleState(new GameBoard(20,20,_=>TerrainType.Water), rules,
            new[] {(Side.Player,ShipClass.Mothership,new GridPosition(2,2)),(Side.Enemy,ShipClass.Mothership,new GridPosition(18,18)),(Side.Player,ShipClass.Kolonel,new GridPosition(8,8)),(Side.Enemy,ShipClass.Balloon,new GridPosition(10,8)),(Side.Player,ShipClass.Invader,new GridPosition(10,9))},Array.Empty<GridPosition>());
        Check(air.CanAttack(3,4) && air.TargetCells(3).Contains(new(10,8)), "Kolonel acquires an observed balloon");
        Check(!air.CanAttack(5,4) && air.Damage(air.Find(5)!,air.Find(4)!)==0, "Galleon has no anti-air gun");
        Check(air.PathToAttackPosition(3,4).Count>0, "AI gun-position search supports Kolonel AA");
        air.SetPlayerColor(FleetColor.Red);
        var restored = BattleState.LoadJson(air.SaveJson());
        Check(restored.PlayerColor==FleetColor.Red && restored.CanAttack(3,4), "red fleet and AA survive Continue");
        var legacy = JsonNode.Parse(air.SaveJson())!;
        legacy["Rules"]!.AsObject().Remove("FreeCoastalNavigation");
        legacy["Rules"]!["Balloon"]!.AsObject().Remove("KolonelAntiAir");
        var old = BattleState.LoadJson(legacy.ToJsonString());
        Check(!old.Rules.FreeCoastalNavigation && !old.Rules.Balloon.KolonelAntiAir && !old.CanAttack(3,4), "omitted rules retain legacy semantics");
        Check(air.Attack(Side.Player,3,4).Success && air.Find(4) is null, "Kolonel shot destroys a balloon once");
        foreach (int seed in Enumerable.Range(801,12))
        {
            var board = ArchipelagoGenerator.Create(seed,3,WorldKind.Pangaea);
            var battle = SkirmishSetup.Create(board,rules,3);
            Check(battle.Villages.Count(v=>PangaeaWaters.IsInterior(board,v.Position))>battle.Villages.Count/2, "most villages occupy the inland river/lake network with new three-tile clearance, seed "+seed);
            foreach (var first in battle.Villages)
                foreach (var second in battle.Villages.Where(v=>v.Id>first.Id))
                    Check(!board.InRadius(first.Position,second.Position,3),"village sites stay more than three cells apart, seed "+seed);
            Check(battle.FishSpots.Count(p=>PangaeaWaters.IsInterior(board,p))>battle.FishSpots.Count/2, "more fish inside Pangaea, seed "+seed);
            var loaded = BattleState.LoadJson(battle.SaveJson());
            Check(loaded.FishSpots.SequenceEqual(battle.FishSpots) && loaded.Board.Mesh!.Vertices.SequenceEqual(board.Mesh!.Vertices), "Continue preserves exact fish and topology");
        }
        return checks;
    }
}
