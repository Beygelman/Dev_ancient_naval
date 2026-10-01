using System.Text.Json.Nodes;
using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Units;
using DevAncientNaval.Core.World;

internal static class VoyageStatisticsChecks
{
    internal static int Run(BattleRules rules)
    {
        int checks = 0;
        void Check(bool condition, string name)
        {
            if (!condition)
                throw new InvalidOperationException("Voyage statistics: " + name);
            checks++;
        }

        BattleState Water(params (Side Owner, ShipClass Class, GridPosition Position)[] fleet)
            => new(new GameBoard(20, 20, _ => TerrainType.Water), rules,
                fleet, Array.Empty<GridPosition>(), villageSpots: Array.Empty<GridPosition>());

        var battle = Water(
            (Side.Player, ShipClass.Mothership, new(2, 2)),
            (Side.Enemy, ShipClass.Mothership, new(18, 18)));
        Check(battle.Statistics == VoyageStatistics.Empty, "starting reserve and starting fleets are excluded");
        var built = battle.Build(Side.Player, 1, ShipClass.Fishing, new(3, 2));
        Check(built.Success && battle.Statistics.ShipsBuilt == 1
            && battle.Statistics.CurrencyEarned == 0, "actual fishing construction counts without treating spending as income");
        Check(!battle.Build(Side.Player, 1, ShipClass.Garrison, new(2, 3)).Success
            && battle.Statistics.ShipsBuilt == 1, "rejected production never increments the ledger");
        int gross = battle.GrossIncome(Side.Player);
        Check(battle.EndTurn(Side.Player).Success && battle.EndTurn(Side.Enemy).Success
            && battle.Statistics.CurrencyEarned == gross, "only the player's gross positive receipts count");
        var oldStats = battle.Statistics;
        Check(!battle.EndTurn(Side.Enemy).Success && battle.Statistics == oldStats, "rejected duplicate turn cannot duplicate receipts");
        Check(BattleState.LoadJson(battle.SaveJson()).Statistics == oldStats, "earned income and production survive Continue");

        var oldSave = JsonNode.Parse(battle.SaveJson())!.AsObject();
        oldSave.Remove("Statistics");
        Check(BattleState.LoadJson(oldSave.ToJsonString()).Statistics == VoyageStatistics.Empty,
            "older voyages start a zero ledger without invented historical totals");
        var invalidSave = JsonNode.Parse(battle.SaveJson())!.AsObject();
        invalidSave["Statistics"]!["CurrencyEarned"] = -1;
        bool rejected = false;
        try { BattleState.LoadJson(invalidSave.ToJsonString()); }
        catch (ArgumentException) { rejected = true; }
        Check(rejected, "negative stored totals are rejected");

        battle = Water(
            (Side.Player, ShipClass.Mothership, new(2, 2)),
            (Side.Enemy, ShipClass.Mothership, new(18, 18)),
            (Side.Player, ShipClass.Kolonel, new(6, 6)),
            (Side.Pirates, ShipClass.PirateSchooner, new(7, 6)));
        battle.Find(4)!.Health = 1;
        var presentation = battle.Prepare(b => b.Attack(Side.Player, 3, 4));
        Check(presentation.Result.Success && battle.Statistics == VoyageStatistics.Empty,
            "calculated shot leaves live totals unchanged while the shell flies");
        presentation.Impact("attack");
        Check(battle.Statistics.EnemyShipsDestroyed == 1
            && battle.Statistics.CurrencyEarned == rules.PirateCurrencyReward,
            "pirate kill and bounty enter the ledger at the impact");
        oldStats = battle.Statistics;
        presentation.Impact("attack");
        presentation.Finish();
        presentation.Finish();
        Check(battle.Statistics == oldStats, "repeated impact and Finish do not double-count the bounty or kill");

        battle = Water(
            (Side.Player, ShipClass.Mothership, new(2, 2)),
            (Side.Enemy, ShipClass.Mothership, new(9, 6)),
            (Side.Player, ShipClass.AncientGun, new(6, 6)),
            (Side.Enemy, ShipClass.Garrison, new(15, 15)));
        battle.Find(2)!.Health = 1;
        presentation = battle.Prepare(b => b.Attack(Side.Player, 3, 2));
        Check(presentation.Result.Success && battle.Winner is null
            && battle.Statistics.NationsDefeated == 0, "nation survives in the live ledger until flagship impact");
        presentation.Impact("attack");
        Check(battle.Winner == Side.Player && battle.Statistics.NationsDefeated == 1
            && battle.Statistics.EnemyShipsDestroyed == 1 && battle.Find(4) is not null,
            "flagship kill counted once while its follower waits to sink");
        oldStats = battle.Statistics;
        battle.CompleteFleetCollapse(Side.Enemy);
        presentation.Finish();
        Check(battle.Find(4) is null && battle.Statistics == oldStats,
            "automatic fleet scuttling is not an additional combat kill");
        var resumed = BattleState.LoadJson(battle.SaveJson());
        Check(resumed.Winner == Side.Player && resumed.Statistics == oldStats,
            "a resumed victory preserves exact totals");

        battle = Water(
            (Side.Player, ShipClass.Mothership, new(2, 2)),
            (Side.Enemy, ShipClass.Mothership, new(18, 18)),
            (Side.Player, ShipClass.Balloon, new(6, 6)),
            (Side.Player, ShipClass.Garrison, new(7, 6)),
            (Side.Enemy, ShipClass.Garrison, new(7, 7)),
            (Side.Enemy, ShipClass.CannonTower, new(8, 6)));
        battle.Find(4)!.Health = battle.Find(5)!.Health = battle.Find(6)!.Health = 1;
        Check(battle.Move(Side.Player, 3, new(7, 6)).Success
            && battle.DropBomb(Side.Player, 3).Success, "real moved balloon bomb resolves its mixed targets");
        Check(battle.Statistics.EnemyShipsDestroyed == 1 && battle.Statistics.NationsDefeated == 0,
            "friendly casualties and destroyed buildings do not count as enemy vessels");

        battle = Water(
            (Side.Player, ShipClass.Mothership, new(2, 2)),
            (Side.Enemy, ShipClass.Mothership, new(18, 18)),
            (Side.Player, ShipClass.Kolonel, new(6, 6)),
            (Side.Enemy, ShipClass.Garrison, new(7, 6)));
        battle.Find(4)!.Health = 1;
        Check(battle.EndTurn(Side.Player).Success && battle.Attack(Side.Enemy, 4, 3).Success,
            "hostile gun triggers a real player counterattack");
        Check(battle.Statistics.EnemyShipsDestroyed == 1,
            "player counterfire kills count even during the opponent's turn");

        var villageBoard = new GameBoard(20, 20,
            p => p == new GridPosition(6, 6) ? TerrainType.Land : TerrainType.Water);
        battle = new BattleState(villageBoard, rules, new[]
        {
            (Side.Player, ShipClass.Mothership, new GridPosition(2, 2)),
            (Side.Enemy, ShipClass.Mothership, new GridPosition(18, 18)),
            (Side.Enemy, ShipClass.Garrison, new GridPosition(7, 6))
        }, Array.Empty<GridPosition>(), villageSpots: new[] { new GridPosition(6, 6) });
        var state = battle.CaptureSnapshot();
        state.Villages = state.Villages.Select(town => town with
            { Owner = Side.Player, Level = 2, Health = 10, Fortified = true }).ToArray();
        state.Ships.Single(ship => ship.Id == 3).Health = 1;
        battle = BattleState.LoadJson(BattleState.SerializeSnapshot(state));
        var townId = battle.Villages.Single().Id;
        Check(battle.BuildFromVillage(Side.Player, townId, ShipClass.Fishing, new(5, 6)).Success
            && battle.Statistics.ShipsBuilt == 1, "village shipyard construction enters the same ledger");
        var ended = battle.EndTurn(Side.Player);
        Check(ended.Success && ended.OutpostShots?.Single().TargetSunk == true
            && battle.Statistics.EnemyShipsDestroyed == 1,
            "automatic player outpost fire credits its real kill");

        battle = Water(
            (Side.Player, ShipClass.Mothership, new(2, 2)),
            (Side.Enemy, ShipClass.Mothership, new(18, 18)),
            (Side.Player, ShipClass.Kolonel, new(5, 6)));
        state = battle.CaptureSnapshot();
        int treasuryId = state.NextId++;
        state.Treasuries = new[] { new Treasury(treasuryId, new(6, 6)) };
        state.Outcomes = new[] { new SavedOutcome(treasuryId, TreasuryReward.Currency) };
        battle = BattleState.LoadJson(BattleState.SerializeSnapshot(state));
        Check(battle.Move(Side.Player, 3, new(6, 6)).Success
            && battle.EndTurn(Side.Player).Success && battle.EndTurn(Side.Enemy).Success,
            "a real crew sails to the treasury and waits its owner turn");
        long earnings = battle.Statistics.CurrencyEarned;
        Check(battle.LootTreasury(Side.Player, 3).Success
            && battle.Statistics.CurrencyEarned == earnings + rules.Treasury.CurrencyReward
            && battle.Statistics.ShipsBuilt == 0, "treasury receipt counts as income without a fictional construction");
        return checks;
    }
}
