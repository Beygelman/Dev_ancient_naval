using System.Diagnostics;
using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Navigation;
using DevAncientNaval.Core.Units;
using DevAncientNaval.Core.World;

internal static partial class BattleScenarios
{
    private static void NavigationRefactor()
    {
        foreach (var kind in new[]
        {
            ShipClass.Mothership,
            ShipClass.Garrison,
            ShipClass.Kolonel
        }

        )
        {
            var battle = Fixture(ShipClass.Garrison, terrain: p => p.Y == 10 && p.X is > 4 and < 16 && p.X != 9 ? TerrainType.Land : TerrainType.Water);
            var ship = kind == ShipClass.Mothership ? battle.Find(1)! : battle.Find(3)!;
            if (kind == ShipClass.Kolonel)
                battle = Fixture(kind);
            ship = battle.Find(ship.Id)!;
            CompareNavigation(battle, ship, null);
            battle.Find(1)!.HasRadar = true;
            battle.Vision.Recompute(battle.Ships, battle.TurnSerial);
            CompareNavigation(battle, ship, null);
            RevealBoard(battle);
            CompareNavigation(battle, ship, null);
        }

        var organic = SkirmishSetup.Create(ArchipelagoGenerator.Create(731), Rules);
        foreach (var ship in organic.Ships.Where(s => !s.IsAirborne).ToArray())
            CompareNavigation(organic, ship, ship.Owner == Side.Pirates ? ship.Position : null);
        RevealBoard(organic);
        CompareNavigation(organic, organic.Find(1)!, null);
        // A new query must observe mutations instead of retaining stale occupancy.
        var changing = Fixture(target: new(9, 8));
        var mover = changing.Find(3)!;
        Check(changing.StepCost(mover.Id, mover.Position, new(9, 8)) is null, "Enemy blocks destination");
        changing.Find(4)!.Position = new(15, 15);
        changing.Vision.Recompute(changing.Ships, changing.TurnSerial);
        Check(changing.StepCost(mover.Id, mover.Position, new(9, 8)) == 10, "Fresh query observes enemy departure");
        foreach (int size in new[]
        {
            16,
            128,
            256
        }

        )
            BenchmarkNavigation(size);
    }

    private static void RevealBoard(BattleState battle)
    {
        foreach (var side in Enum.GetValues<Side>())
            foreach (var tile in battle.Board.Tiles)
                battle.Vision.RevealCombat(side, tile.Position);
        battle.Vision.Recompute(battle.Ships, battle.TurnSerial);
    }

    private static NavalNavigationQuery Query(BattleState battle, Ship ship, GridPosition? home, bool knowledge = true) => new(battle.Board, ship, battle.Vision, battle.Ships, battle.Whirlpools.SelectMany(w => w.Cells).ToHashSet(), home, knowledge);
    private static void CompareNavigation(BattleState battle, Ship ship, GridPosition? home)
    {
        var original = new NavigationReference(battle, home);
        foreach (bool knowledge in new[]
        {
            true,
            false
        }

        )
        {
            var query = Query(battle, ship, home, knowledge);
            foreach (var tile in battle.Board.Tiles)
                foreach (var next in battle.Board.GetSurrounding(tile.Position))
                    Check(original.StepCost(ship, tile.Position, next, knowledge) == query.StepCost(tile.Position, next), "Navigation preserves terrain, fog, threats and diagonal corner costs");
        }

        var before = original.Flood(ship, 10000);
        var afterQuery = Query(battle, ship, home);
        var after = PathSearch.Find(ship.Position, 10000, battle.Board.GetSurrounding, afterQuery.StepCost);
        Check(before.Costs.Count == after.Costs.Count && before.Costs.All(p => after.Costs.GetValueOrDefault(p.Key, -1) == p.Value), "Dijkstra preserves every reachable cost");
        Check(before.Previous.Count == after.Previous.Count && before.Previous.All(p => after.Previous.TryGetValue(p.Key, out var previous) && previous == p.Value), "Dijkstra preserves equal-cost route tie order");
    }

    private static void BenchmarkNavigation(int fleetSize)
    {
        var board = new GameBoard(28, 28, _ => TerrainType.Water);
        var rules = new BattleRules
        {
            StartingCredits = Rules.StartingCredits,
            IncomePerMothership = Rules.IncomePerMothership,
            RepairAmount = Rules.RepairAmount,
            FleetLimit = fleetSize,
            Ships = Rules.Ships
        };
        var setup = new List<(Side, ShipClass, GridPosition)>
        {
            (Side.Player, ShipClass.Mothership, new(0, 0)),
            (Side.Enemy, ShipClass.Mothership, new(27, 27))
        };
        for (int i = 0; i < fleetSize - 2; i++)
            setup.Add((i % 2 == 0 ? Side.Player : Side.Enemy, ShipClass.Garrison, new(i % 26 + 1, i / 26 + 1)));
        var battle = new BattleState(board, rules, setup, Array.Empty<GridPosition>(), villageSpots: Array.Empty<GridPosition>());
        RevealBoard(battle);
        var ship = battle.Find(1)!;
        var reference = new NavigationReference(battle);
        void Before() => reference.Flood(ship, 10000);
        void After()
        {
            var query = Query(battle, ship, null);
            PathSearch.Find(ship.Position, 10000, board.GetSurrounding, query.StepCost);
        }

        Before();
        After();
        static (long Bytes, double Ms) Measure(Action action)
        {
            long bytes = GC.GetAllocatedBytesForCurrentThread();
            long start = Stopwatch.GetTimestamp();
            for (int i = 0; i < 3; i++)
                action();
            return (GC.GetAllocatedBytesForCurrentThread() - bytes, Stopwatch.GetElapsedTime(start).TotalMilliseconds);
        }

        var before = Measure(Before);
        var after = Measure(After);
        Check(after.Bytes < before.Bytes, "Navigation reduces managed allocation in synthetic fleets");
        Console.WriteLine($"NAVIGATION ships={fleetSize} iterations=3 before_bytes={before.Bytes} after_bytes={after.Bytes} before_ms={before.Ms:0.###} after_ms={after.Ms:0.###}");
    }
}
