using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Units;
using DevAncientNaval.Core.World;

internal static partial class BattleScenarios
{
    private static void PresentedCombat()
    {
        var rules = BattleRules.FromJson(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "balance.json")));
        BattleState Fixture() => new(new GameBoard(24, 24, _ => TerrainType.Water), rules, new[] { (Side.Player, ShipClass.Mothership, new GridPosition(2, 2)), (Side.Enemy, ShipClass.Mothership, new GridPosition(20, 20)), (Side.Player, ShipClass.Kolonel, new GridPosition(8, 8)), (Side.Enemy, ShipClass.Garrison, new GridPosition(9, 8)) }, Array.Empty<GridPosition>(), villageSpots: Array.Empty<GridPosition>());
        var battle = Fixture();
        double original = battle.Find(4)!.Health;
        var liveTarget = battle.Find(4);
        var immediate = BattleState.LoadJson(battle.SaveJson());
        var expected = immediate.Attack(Side.Player, 3, 4);
        var prepared = battle.Prepare(b => b.Attack(Side.Player, 3, 4));
        Check(prepared.Result.Success && battle.Find(4)!.Health == original && battle.Find(3)!.AttacksUsed == 0, "Preparing a salvo leaves live health/actions unchanged");
        Check(!battle.Move(Side.Player, 3, new(7, 8)).Success && !battle.EndTurn(Side.Player).Success, "An unresolved salvo rejects competing orders");
        bool refused = false;
        try
        {
            battle.SaveJson();
        }
        catch (InvalidOperationException)
        {
            refused = true;
        }

        Check(refused, "A save cannot persist a partly resolved projectile");
        prepared.Impact("attack");
        Check(battle.Find(4)!.Health == original - expected.Shots![0].Damage && battle.Find(3)!.Health == rules.Get(ShipClass.Kolonel).MaxHealth, "Direct impact applies health before a separate reply");
        Check(ReferenceEquals(liveTarget, battle.Find(4)), "Impact commits preserve entity identity");
        prepared.Impact("attack");
        Check(battle.Find(4)!.Health == original - expected.Shots![0].Damage, "An impact is idempotent");
        prepared.Impact("counter");
        prepared.Finish();
        Check(battle.SaveJson() == immediate.SaveJson(), "Staged and immediate combat produce the same final aggregate");
        battle = Fixture();
        battle.Find(3)!.Kills = 2;
        battle.Find(4)!.Health = 1;
        prepared = battle.Prepare(b => b.Attack(Side.Player, 3, 4));
        Check(!battle.Find(3)!.IsVeteran && battle.Find(4)is not null, "Neither kill nor veterancy occurs in flight");
        prepared.Impact("attack");
        Check(battle.Find(3)!.IsVeteran && battle.Find(4)is null, "Impact awards one actual kill and promotion");
        prepared.Finish();
        battle = new BattleState(new GameBoard(24, 24, _ => TerrainType.Water), rules, new[] { (Side.Player, ShipClass.Mothership, new GridPosition(8, 8)), (Side.Enemy, ShipClass.Mothership, new GridPosition(9, 8)), (Side.Enemy, ShipClass.Garrison, new GridPosition(10, 9)) }, Array.Empty<GridPosition>(), villageSpots: Array.Empty<GridPosition>());
        battle.Find(2)!.Health = 1;
        prepared = battle.Prepare(b => b.Attack(Side.Player, 1, 2));
        Check(battle.Find(2)is not null && battle.Find(3)is not null, "A doomed fleet remains during the flagship salvo");
        prepared.Impact("attack");
        Check(battle.Find(2)is null && battle.Find(3)is not null, "Flagship impact preserves followers until its wreck finishes");
        battle.CompleteFleetCollapse(Side.Enemy);
        Check(battle.Find(3)is null, "Only flagship sinking completion releases the remaining fleet");
        prepared.Finish();
        Check(battle.Winner == Side.Player, "The surviving flagship wins after a staged collapse");
        Check(WorldNames.Captains.Count > 20 && WorldNames.Towns.Count > 40 &&
            WorldNames.Captains.Distinct().Count() == WorldNames.Captains.Count &&
            WorldNames.Towns.Distinct().Count() == WorldNames.Towns.Count,
            "Expanded captain and town catalogs retain unique identities");
        var mapped = SkirmishSetup.Create(DevAncientNaval.Presentation.PrototypeBoard.Create(opponentCount: 1), rules, 1);
        var scout = mapped.OwnShips(Side.Player).First(s => s.Definition.Class == ShipClass.Fishing);
        var beforeShip = scout;
        var berth = mapped.Reachable(scout.Id).Keys.First(p => p != scout.Position);
        mapped.Prepare(b => b.Move(Side.Player, scout.Id, berth)).Finish();
        Check(ReferenceEquals(beforeShip, mapped.Find(scout.Id)) && scout.Position == berth, "A mapped voyage moves its actual surviving ship instance");
        Check(mapped.SaveJson() == BattleState.LoadJson(mapped.SaveJson()).SaveJson(), "Prepared map commands preserve exact save identity and geometry");
        var resumed = BattleState.LoadJson(battle.SaveJson());
        Check(resumed.FactionName(Side.Enemy) == battle.FactionName(Side.Enemy), "Captain identities survive save/resume");
        battle = Fixture();
        Check(battle.StepCost(3, new(8, 8), new(9, 9), false) == 20, "A diagonally reached hostile neighborhood retains its threat penalty");
        Check(battle.StepCost(3, new(8, 8), new(7, 7), false) == 10, "A clear diagonal costs exactly one tile");
        battle = Fixture();
        battle.Find(4)!.Health = 1;
        var doubleKill = battle.Attack(Side.Player, 3, 4, true);
        Check(doubleKill.Success && doubleKill.SalvoCharges == 2 && doubleKill.Shots!.Count == 1
            && battle.Find(3)!.AttacksRemaining == 0,
            "A double salvo still launches both charges when the first impact sinks its target");
        battle = Fixture();
        Check(battle.Attack(Side.Player, 3, 4).SalvoCharges == 1,
            "A normal shot presents exactly one charge");
    }
}
