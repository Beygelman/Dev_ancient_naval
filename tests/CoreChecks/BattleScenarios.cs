using DevAncientNaval.Core.AI;
using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Units;
using DevAncientNaval.Core.World;
using DevAncientNaval.Core.Economy;

internal static class BattleScenarios
{
    private static int _checks;
    private static readonly BattleRules Rules = BattleRules.FromJson(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "balance.json")));
    private static void Check(bool ok, string name)
    {
        if (!ok) throw new Exception($"FAIL: {name}");
        _checks++;
    }
    private static BattleState Duel(ShipClass kind, GridPosition? target = null, Func<GridPosition, TerrainType>? terrain = null) => new(
        new GameBoard(12, 12, terrain ?? (_ => TerrainType.Water)), Rules,
        new (Side, ShipClass, GridPosition)[] {
            (Side.Player, ShipClass.Mothership, new(0, 0)), (Side.Enemy, ShipClass.Mothership, new(11, 11)),
            (Side.Player, kind, new(4, 4)), (Side.Enemy, ShipClass.Kolonel, target ?? new(6, 4)) });
    private static void NextRound(BattleState battle)
    {
        Check(battle.EndTurn(Side.Player).Success, "End player turn");
        Check(battle.EndTurn(Side.Enemy).Success, "End enemy turn");
    }

    public static void Run()
    {
        var b = Duel(ShipClass.Garrison);
        Check(b.Move(Side.Player, 3, new(5, 4)).Success, "Scout move");
        Check(b.Attack(Side.Player, 3, 4).Success, "Scout attack");
        Check(b.Move(Side.Player, 3, new(5, 7)).Success, "Scout move after attack");
        Check(b.Find(3)!.MovementRemaining == 1, "Scout shared budget");
        Check(!b.Move(Side.Player, 3, new(5, 9)).Success, "Scout cannot overspend");
        Check(!b.Attack(Side.Player, 3, 4).Success, "Scout cannot attack twice");

        b = Duel(ShipClass.Invader);
        Check(b.Move(Side.Player, 3, new(5, 4)).Success && b.Attack(Side.Player, 3, 4).Success, "Invader move attack");
        Check(!b.Move(Side.Player, 3, new(5, 5)).Success, "Invader post-attack move forbidden");
        b = Duel(ShipClass.Invader);
        Check(b.Attack(Side.Player, 3, 4).Success && b.Move(Side.Player, 3, new(4, 5)).Success, "Invader attack move");
        Check(b.Move(Side.Player, 3, new(4, 6)).Success, "Invader can split its post-attack movement phase");

        b = Duel(ShipClass.Kolonel);
        Check(b.Attack(Side.Player, 3, 4).Success, "Heavy first attack");
        Check(!b.Move(Side.Player, 3, new(4, 5)).Success, "Heavy cannot move after attack");
        Check(b.Attack(Side.Player, 3, 4).Success, "Heavy second stationary attack");
        Check(!b.Attack(Side.Player, 3, 4).Success, "Heavy third attack forbidden");
        b = Duel(ShipClass.Kolonel);
        Check(b.Move(Side.Player, 3, new(5, 4)).Success && b.Attack(Side.Player, 3, 4).Success, "Heavy move attack");
        Check(!b.Attack(Side.Player, 3, 4).Success, "Heavy moved only one attack");

        b = Duel(ShipClass.Invader, terrain: p => p == new GridPosition(5, 4) ? TerrainType.Land : TerrainType.Water);
        var path = b.PathTo(3, new(6, 5));
        Check(path.Count == 4 && path.All(p => b.Board.GetTile(p).Terrain == TerrainType.Water), "BFS detours around land and occupied cells");
        var before = b.Find(3)!.Position;
        Check(!b.Move(Side.Enemy, 3, new(4, 5)).Success && b.Find(3)!.Position == before, "Wrong side cannot move");
        Check(!b.Move(Side.Player, 4, new(6, 5)).Success, "Cannot move enemy");
        Check(!b.Move(Side.Player, 3, new(6, 4)).Success, "Cannot overlap ships");
        Check(!b.Move(Side.Player, 3, new(-1, 0)).Success, "Cannot leave board");
        Check(!b.Attack(Side.Player, 3, 1).Success, "Cannot attack ally");
        Check(!b.Repair(Side.Player, 3).Success, "No repair at full health");
        b.EndTurn(Side.Player);
        Check(b.Attack(Side.Enemy, 4, 3).Success, "Enemy attack for repair setup");
        int hp = b.Find(3)!.Health;
        b.EndTurn(Side.Enemy);
        Check(b.Repair(Side.Player, 3).Success && b.Find(3)!.Health == Math.Min(hp + Rules.RepairAmount, Rules.Get(ShipClass.Invader).MaxHealth), "Repair amount");
        Check(!b.Move(Side.Player, 3, new(4, 5)).Success && !b.Attack(Side.Player, 3, 4).Success, "Repair consumes actions");
        NextRound(b);
        Check(b.Find(3)!.CanMove && b.Find(3)!.AttacksRemaining == 1, "New turn resets action profile");

        b = Duel(ShipClass.Garrison);
        int money = b.Credits(Side.Player);
        Check(!b.Build(Side.Player, 1, ShipClass.Garrison, new(3, 3)).Success && b.Credits(Side.Player) == money, "Bad spawn has no cost");
        var first = b.Build(Side.Player, 1, ShipClass.Garrison, new(1, 0));
        Check(first.Success && b.Find(first.TargetId)!.CanMove, "First produced ship acts immediately");
        Check(b.Credits(Side.Player) == money - Rules.Get(ShipClass.Garrison).Price, "Build charges once");
        Check(!b.Build(Side.Player, 1, ShipClass.Garrison, new(0, 1)).Success, "One build per turn");
        NextRound(b);
        var second = b.Build(Side.Player, 1, ShipClass.Garrison, new(0, 1));
        Check(second.Success && !b.Find(second.TargetId)!.CanMove && b.Find(second.TargetId)!.AttacksRemaining == 0, "Later production sleeps");
        Check(!b.Move(Side.Player, second.TargetId, new(0, 2)).Success, "New sleeping ship cannot act");
        NextRound(b);
        Check(b.Find(second.TargetId)!.CanMove, "Produced ship wakes next turn");
        money = b.Credits(Side.Player);
        Check(!b.Build(Side.Player, 1, ShipClass.Garrison, new(0, 1)).Success && b.Credits(Side.Player) == money, "Blocked spawn has no charge");
        Check(!b.Build(Side.Player, 1, ShipClass.Mothership, new(0, 2)).Success, "Cannot build mothership");
        Check(!b.EndTurn(Side.Enemy).Success && b.Credits(Side.Player) == money, "Duplicate/wrong end-turn cannot give income");

        b = Duel(ShipClass.Garrison);
        Check(b.Build(Side.Player, 1, ShipClass.Kolonel, new(1, 0)).Success, "Expensive production setup");
        NextRound(b);
        money = b.Credits(Side.Player);
        Check(!b.Build(Side.Player, 1, ShipClass.Invader, new(0, 1)).Success && b.Credits(Side.Player) == money && !b.Find(1)!.HasProduced,
            "Insufficient funds preserve money and build slot");
        b.EndTurn(Side.Player);
        var enemyFirst = b.Build(Side.Enemy, 2, ShipClass.Garrison, new(10, 11));
        Check(enemyFirst.Success && b.Find(enemyFirst.TargetId)!.CanMove, "First production exception is per side");

        var blocked = new BattleState(new GameBoard(5, 5, p => p == new GridPosition(1, 0) || p == new GridPosition(0, 1) ? TerrainType.Land : TerrainType.Water), Rules,
            new (Side, ShipClass, GridPosition)[] { (Side.Player, ShipClass.Mothership, new(0, 0)), (Side.Enemy, ShipClass.Mothership, new(4, 4)) });
        Check(blocked.SpawnCells(1).Count == 0 && !blocked.Build(Side.Player, 1, ShipClass.Garrison, new(1, 0)).Success, "Surrounded mother cannot build");
        Check(blocked.Credits(Side.Player) == Rules.StartingCredits && !blocked.Find(1)!.HasProduced, "Failed build preserves credits and slot");

        b = Duel(ShipClass.Kolonel);
        int expectedDamage = Rules.Get(ShipClass.Kolonel).Damage - Rules.Get(ShipClass.Kolonel).Armor;
        var attack = b.Attack(Side.Player, 3, 4);
        Check(attack.Amount == expectedDamage && b.Find(4)!.Health == Rules.Get(ShipClass.Kolonel).MaxHealth - expectedDamage, "Deterministic armor");
        for (int rounds = 0; rounds < 5 && b.Find(4) is not null; rounds++)
        {
            while (b.CanAttack(3, 4)) b.Attack(Side.Player, 3, 4);
            if (b.Find(4) is not null) NextRound(b);
        }
        Check(b.Find(4) is null && b.At(new(6, 4)) is null, "Sinking frees occupancy");

        foreach (var winningSide in Enum.GetValues<Side>())
        {
            var setup = new (Side, ShipClass, GridPosition)[] { (Side.Player, ShipClass.Mothership, new(0, 0)),
                (Side.Enemy, ShipClass.Mothership, new(1, 0)), (winningSide, ShipClass.Kolonel, new(0, 1)) };
            var match = new BattleState(new GameBoard(4, 4, _ => TerrainType.Water), Rules, setup);
            if (winningSide == Side.Enemy) match.EndTurn(Side.Player);
            int target = winningSide == Side.Player ? 2 : 1;
            for (int rounds = 0; rounds < 10 && !match.IsOver; rounds++)
            {
                while (match.CanAttack(3, target)) match.Attack(winningSide, 3, target);
                if (!match.IsOver) { match.EndTurn(winningSide); match.EndTurn(match.ActiveSide); }
            }
            Check(match.Winner == winningSide, "Mothership loss determines winner");
            Check(!match.EndTurn(winningSide).Success && !match.Move(winningSide, 3, new(1, 1)).Success, "No commands after result");
        }

        b = Duel(ShipClass.Garrison);
        b.SetIncomeSource(new IncomeSource("test-station", Side.Player, 7));
        Check(b.Income(Side.Player) == Rules.IncomePerMothership + 7, "Income supports sources other than mothership");
        NextRound(b);
        Check(b.Credits(Side.Player) == Rules.StartingCredits + Rules.IncomePerMothership + 7, "All sources accrue once");
        b.RemoveIncomeSource("test-station");
        Check(b.Income(Side.Player) == Rules.IncomePerMothership, "Removing source changes income");

        int completed = 0;
        for (int scenario = 0; scenario < 8; scenario++)
        {
            int seed = scenario;
            var map = new GameBoard(20, 20, p => (p.X >= 7 && p.X <= 9 && p.Y >= 3 + seed && p.Y <= 7 + seed) ? TerrainType.Land : TerrainType.Water);
            var match = new BattleState(map, Rules, new (Side, ShipClass, GridPosition)[] {
                (Side.Player, ShipClass.Mothership, new(2, 9)), (Side.Player, ShipClass.Garrison, new(4, 9)),
                (Side.Player, ShipClass.Invader, new(3, 11)), (Side.Player, ShipClass.Kolonel, new(2, 7)),
                (Side.Enemy, ShipClass.Mothership, new(17, 9)), (Side.Enemy, ShipClass.Garrison, new(15, 9)),
                (Side.Enemy, ShipClass.Invader, new(16, 7)), (Side.Enemy, ShipClass.Kolonel, new(17, 11)) });
            int actions = 0, actionsThisTurn = 0;
            while (!match.IsOver && match.Round < 100 && actions++ < 10000)
            {
                var side = match.ActiveSide;
                var result = SimpleOpponent.Step(match);
                Check(result.Success, "AI emits legal command");
                Check(match.Ships.All(s => s.Health > 0 && s.Health <= s.Definition.MaxHealth && s.MovementRemaining >= 0), "Ship invariants");
                Check(match.Ships.Select(s => s.Position).Distinct().Count() == match.Ships.Count, "Unique occupancy during match");
                Check(match.Ships.All(s => map.GetTile(s.Position).Terrain == TerrainType.Water), "All ships remain at sea");
                if (side != match.ActiveSide) actionsThisTurn = 0;
                else Check(++actionsThisTurn < 256, "AI finishes turn within budget");
            }
            Check(match.IsOver, $"AI match {scenario} finishes within 100 rounds");
            completed++;
        }
        Console.WriteLine($"PASS: {_checks} battle checks; {completed} complete AI matches.");
    }
}
