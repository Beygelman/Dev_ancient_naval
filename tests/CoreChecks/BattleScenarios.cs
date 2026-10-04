using DevAncientNaval.Core.AI;
using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Units;
using DevAncientNaval.Core.World;
using DevAncientNaval.Core.Vision;

internal static partial class BattleScenarios
{
    private static int _checks;
    private static readonly BattleRules Rules = BattleRules.FromJson(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "balance-0.14.1.json")));
    private static readonly BattleRules Funded = new()
    {
        StartingCredits = 30,
        IncomePerMothership = Rules.IncomePerMothership,
        RepairAmount = Rules.RepairAmount,
        FleetLimit = Rules.FleetLimit,
        Ships = Rules.Ships
    };
    private static void Check(bool ok, string name)
    {
        if (!ok)
            throw new Exception("FAIL: " + name);
        _checks++;
    }

    private static readonly GridPosition[] Resources =
    {
        new(6, 5),
        new(5, 6),
        new(4, 5),
        new(5, 4),
        new(6, 6),
        new(4, 4),
        new(4, 6),
        new(6, 4),
        new(7, 5)
    };
    private static BattleState Fixture(ShipClass kind = ShipClass.Garrison, ShipClass enemy = ShipClass.Kolonel, GridPosition? target = null, IEnumerable<GridPosition>? fish = null, Func<GridPosition, TerrainType>? terrain = null) => new(new GameBoard(20, 20, terrain ?? (_ => TerrainType.Water)), Funded, new (Side, ShipClass, GridPosition)[] { (Side.Player, ShipClass.Mothership, new(5, 5)), (Side.Enemy, ShipClass.Mothership, new(18, 18)), (Side.Player, kind, new(8, 8)), (Side.Enemy, enemy, target ?? new(10, 8)) }, fish ?? Array.Empty<GridPosition>());
    private static void Round(BattleState b)
    {
        Check(b.EndTurn(Side.Player).Success, "End player");
        Check(b.EndTurn(Side.Enemy).Success, "End enemy");
    }

    public static void Run()
    {
        Balance();
        Combat();
        Radar();
        Progression();
        Navigation();
        NavigationRefactor();
        MovementPreviews();
        DataDrivenRules();
        OrganicRules();
        MortarsDocksMaps();
        RequestedRules();
        SeaEventRules();
        Release014Rules();
        FactionEconomy();
        Refinement016();
        PortsAndDifficulty();
        PresentedCombat();
        Opponent();
        Console.WriteLine($"PASS: {_checks} Thor/progression/combat checks; 8 complete AI matches.");
    }

    private static void Balance()
    {
        Check(Rules.StartingCredits == 5 && Rules.IncomePerMothership == 2, "Five starting Thors and base income");
        foreach (var d in Rules.Ships)
            Check((d.Class == ShipClass.Balloon ? d.MaxHealth == 1 : d.Class == ShipClass.PirateSchooner ? d.MaxHealth == 7 : d.MaxHealth % 5 == 0) && d.Armor == 0, "Hull tiers and no armor penalty");
        Check(Rules.Get(ShipClass.Garrison).Price == 2 && Rules.Get(ShipClass.Invader).Price == 4 && Rules.Get(ShipClass.Kolonel).Price == 8 && Rules.Get(ShipClass.Fishing).Price == 3, "Explicit ship prices");
        Check(Rules.Get(ShipClass.Fishing).IncomePerTurn == 2 && Rules.Get(ShipClass.Fishing).VisualRange == 2, "Fishing income and minimum sight");
        Check(Rules.Get(ShipClass.Mothership).VisualRange == 2 && Rules.Get(ShipClass.Mothership).RadarRange == 5, "Reduced Mothership sight and radar");
        var b = Fixture(ShipClass.Fishing);
        Check(b.Income(Side.Player) == 4, "Starting mother plus fisher income");
        var made = b.Build(Side.Player, 1, ShipClass.Fishing, new(5, 6));
        Check(made.Success && b.Credits(Side.Player) == 27 && b.Income(Side.Player) == 6, "Fisher purchase adds income");
        Check(!b.Find(1)!.CanMove && b.Find(made.TargetId)!.CanMove, "Production locks mother, first ship ready");
        Round(b);
        Check(b.Credits(Side.Player) == 33 && b.Find(1)!.CanMove, "Income once and movement reset");
        int money = b.Credits(Side.Player);
        Check(!b.EndTurn(Side.Enemy).Success && b.Credits(Side.Player) == money, "No duplicate income");
        made = b.Build(Side.Player, 1, ShipClass.Garrison, new(6, 5));
        Check(made.Success && !b.Find(made.TargetId)!.CanMove, "Later production exhausted");
        Check(!b.Build(Side.Player, 1, ShipClass.Balloon, new(4, 5)).Success, "Balloon only through level choice");
        b = Fixture(ShipClass.Kolonel, ShipClass.Fishing);
        b.Find(4)!.Health = 1;
        b.Attack(Side.Player, 3, 4);
        Check(b.Income(Side.Enemy) == 2, "Sinking stops fish income");
    }

    private static void Combat()
    {
        var b = Fixture(ShipClass.Invader);
        var attacker = b.Find(3)!;
        var defender = b.Find(4)!;
        attacker.Health = attacker.MaxHealth / 2;
        Check(attacker.CurrentDamage == 4, "Half HP damage is rounded to integer");
        Check(b.Damage(attacker, defender) == 4, "Rounded injury damage");
        attacker.Health = attacker.MaxHealth / 4;
        Check(attacker.MovementAllowance == 4, "Exactly quarter HP unchanged");
        attacker.Health = 1;
        attacker.MovementSpentUnits = 20;
        Check(attacker.MovementAllowance == 3 && attacker.MovementRemainingUnits == 10, "Below quarter reduces shared movement budget");
        b = Fixture(ShipClass.Kolonel);
        attacker = b.Find(3)!;
        defender = b.Find(4)!;
        double preview = b.PreviewCounterDamage(attacker, defender);
        var shot = b.Attack(Side.Player, 3, 4);
        Check(shot.Shots!.Count == 2 && shot.Shots[1].IsCounterattack && shot.Shots[1].Damage == preview, "One reply, prediction matches rounded damage");
        Check(b.Attack(Side.Player, 3, 4).Shots!.Count == 2, "Reply to every incoming attack");
        Check(defender.AttacksUsed == 0 && !b.Attack(Side.Player, 3, 4).Success, "Replies do not spend own attacks; heavy max two");
        Check(b.Ships.All(s => s.Health == Math.Floor(s.Health)), "Integer health after combat");
        b = Fixture(ShipClass.Invader, ShipClass.Fishing);
        Check(b.Attack(Side.Player, 3, 4).Shots!.Count == 1, "No fish reply");
        b = Fixture(ShipClass.Fishing);
        Check(!b.Attack(Side.Player, 3, 4).Success, "No fish attack");
        b = Fixture(ShipClass.Kolonel, ShipClass.Garrison, new(11, 8));
        // Allied mother does not spot this cell; move defender one cell closer to the heavy's sight for range test.
        b.Vision.RevealCombat(Side.Player, new(11, 8));
        b.Vision.Recompute(b.Ships, 1);
        Check(b.Attack(Side.Player, 3, 4).Shots!.Count == 1, "Out-of-range reply forbidden");
        b = Fixture();
        b.Find(4)!.Health = 1;
        Check(b.Attack(Side.Player, 3, 4).Shots!.Count == 1 && b.Find(4)is null, "Dead defender never replies");
        foreach (var type in new[]
        {
            ShipClass.Garrison,
            ShipClass.Invader,
            ShipClass.Kolonel
        }

        )
        {
            b = Fixture(type, ShipClass.Fishing);
            b.Find(4)!.Health = 100;
            Check(b.Move(Side.Player, 3, new(9, 8)).Success && b.Attack(Side.Player, 3, 4).Success, "Move and attack profile");
            Check(b.Move(Side.Player, 3, new(9, 9)).Success == (type == ShipClass.Garrison), "Post-attack movement profile");
        }

        b = Fixture(ShipClass.Kolonel, ShipClass.Fishing);
        attacker = b.Find(3)!;
        attacker.Kills = 2;
        attacker.Health = 10;
        b.Find(4)!.Health = 1;
        Check(b.Attack(Side.Player, 3, 4).Shots![0].Promoted, "Third kill promotes");
        Check(attacker.MaxHealth == 19 && attacker.Health == 19 && attacker.FullDamage == 5, "Rounded veteran bonus and full heal");
        attacker.Health = 10;
        Round(b);
        Check(b.Repair(Side.Player, 3).Amount == 5 && attacker.Health == 15, "Integer healing");
        b = Fixture(ShipClass.Garrison);
        attacker = b.Find(3)!;
        defender = b.Find(4)!;
        attacker.Health = 1;
        defender.Kills = 2;
        defender.Health = 10;
        Check(b.Attack(Side.Player, 3, 4).Shots![1].Promoted && defender.Health == defender.MaxHealth, "Counter kill grants veteran");
        b = new BattleState(new GameBoard(20, 20, _ => TerrainType.Water), Rules, new (Side, ShipClass, GridPosition)[] { (Side.Player, ShipClass.Mothership, new(5, 5)), (Side.Enemy, ShipClass.Mothership, new(18, 18)), (Side.Enemy, ShipClass.Fishing, new(6, 5)) }, Array.Empty<GridPosition>());
        b.Find(1)!.Kills = 2;
        b.Find(3)!.Health = 1;
        b.Attack(Side.Player, 1, 3);
        Check(!b.Find(1)!.IsVeteran && b.Find(1)!.Level == 1 && b.Find(1)!.MaxHealth == 20, "Mother never becomes veteran");
    }

    private static void Radar()
    {
        var b = Fixture(ShipClass.Garrison, target: new(10, 5));
        Check(b.Vision.Contacts(Side.Player).Count == 0 && b.FindObserved(Side.Player, 4)is null, "Radar absent until purchase");
        Check(b.BuyRadar(Side.Player, 1).Success && b.Credits(Side.Player) == 28, "Radar costs two");
        Check(b.Vision.Contacts(Side.Player).Contains(new(10, 5)) && b.FindObserved(Side.Player, 4)is null, "Radar contact only, no identity");
        Check(b.Vision.KnownTerrain(Side.Player, new(10, 5))is null, "Radar does not reveal terrain");
        int funds = b.Credits(Side.Player);
        Check(!b.BuyRadar(Side.Player, 1).Success && b.Credits(Side.Player) == funds, "Cannot purchase twice");
        foreach (var type in new[]
        {
            ShipClass.Garrison,
            ShipClass.Invader,
            ShipClass.Fishing
        }

        )
        {
            b = Fixture(type);
            Check(!b.BuyRadar(Side.Player, 3).Success && !b.Find(3)!.HasRadar, "Other classes cannot purchase radar");
        }

        b = Fixture(ShipClass.Kolonel);
        Check(b.BuyRadar(Side.Player, 3).Success && b.Find(3)!.RadarRange == 5, "Heavy radar four");
        Check(b.Find(1)!.RadarRange == 0, "Radar purchase independent per ship");
        var second = Fixture(ShipClass.Fishing, target: new(11, 8));
        Check(!second.Vision.IsVisible(Side.Player, new(11, 8)) && second.Find(3)!.VisualRange == 2, "Fisher shortest sight");
        b = Fixture();
        var old = new GridPosition(5, 3);
        Check(b.Vision.IsVisible(Side.Player, old), "Starting sight");
        b.Find(1)!.Position = new(1, 10);
        b.Vision.Recompute(b.Ships, 5);
        Check(b.Vision.IsExplored(Side.Player, old) && !b.Vision.IsVisible(Side.Player, old), "Terrain memory");
    }

    private static void Progression()
    {
        var b = Fixture(fish: Resources);
        var mother = b.Find(1)!;
        Check(b.Collect(Side.Player, 1, Resources[0]).Success && mother.Resources == 1 && mother.Level == 1 && b.Credits(Side.Player) == 28, "First resource costs 2");
        Check(!b.Collect(Side.Player, 1, Resources[0]).Success && mother.Resources == 1, "Fish consumed once");
        mother.Health = 10;
        Check(b.Collect(Side.Player, 1, Resources[1]).Success && mother.Level == 2 && mother.Resources == 0 && mother.ResourcesRequired == 3, "Two points advance to level two");
        Check(mother.MaxHealth == 25 && mother.Health == 15 && mother.FullDamage == 3 && b.Income(Side.Player) == 4, "Level adds five HP and preserves damage taken");
        Check(b.PendingUpgrade(Side.Player) == mother && b.UpgradeOptions(1).SequenceEqual(new[] { UpgradeChoice.Mobility, UpgradeChoice.FishingBoat }), "Level two choices");
        Check(!b.Collect(Side.Player, 1, Resources[2]).Success && !b.EndTurn(Side.Player).Success && !b.Build(Side.Player, 1, ShipClass.Garrison, new(5, 6)).Success, "Pending choice blocks other commands");
        Check(!b.ChooseUpgrade(Side.Enemy, 1, UpgradeChoice.Mobility).Success && !b.ChooseUpgrade(Side.Player, 1, UpgradeChoice.Balloon).Success, "Choice ownership/level enforced");
        Check(b.ChooseUpgrade(Side.Player, 1, UpgradeChoice.Mobility).Success && mother.MovementAllowance == 2, "Mobility adds one move");
        Check(!b.ChooseUpgrade(Side.Player, 1, UpgradeChoice.FishingBoat).Success, "Cannot take both choices");
        foreach (var cell in Resources.Skip(2).Take(3))
            Check(b.Collect(Side.Player, 1, cell).Success, "Collect level three progress");
        Check(mother.Level == 3 && mother.ResourcesRequired == 4 && mother.MaxHealth == 30 && b.Income(Side.Player) == 6, "Level three stats and threshold");
        Check(b.ChooseUpgrade(Side.Player, 1, UpgradeChoice.Vision).Success && mother.VisualRange == 4, "Choose improved sight");
        foreach (var cell in Resources.Skip(5))
            Check(b.Collect(Side.Player, 1, cell).Success, "Collect level four progress");
        Check(mother.Level == 4 && mother.Resources == 0 && mother.MaxHealth == 35 && mother.FullDamage == 3 && b.Income(Side.Player) == 8, "Level four hull and income");
        Check(b.PendingUpgrade(Side.Player) == mother && b.ChooseUpgrade(Side.Player, 1, UpgradeChoice.Balloon).Success, "Level four balloon choice");
        var air = b.OwnShips(Side.Player).Single(s => s.IsAirborne);
        Check(air.VisualRange == 8 && b.At(mother.Position) == mother, "Air and water layers separate");
        Check(b.Credits(Side.Player) == 12, "Nine resources cost exactly eighteen");
        Check(!b.Move(Side.Player, air.Id, new(19, 0)).Success, "Balloon cannot cross entire map");
        Check(b.Move(Side.Player, air.Id, new(9, 5)).Success && air.MovementRemainingUnits == 0, "Flight consumes four-point budget");
        Check(!b.Move(Side.Player, air.Id, new(0, 19)).Success, "One free flight per own turn");
        Check(!b.Attack(Side.Player, air.Id, 2).Success && !b.Repair(Side.Player, air.Id).Success, "Balloon has no cannon or repair");
        Round(b);
        b.Find(2)!.Position = new(8, 6);
        Check(b.Move(Side.Player, air.Id, new(8, 6)).Success && b.At(new(8, 6))!.Id == 2, "Balloon can overlap enemy ship");
        b.EndTurn(Side.Player);
        Check(b.CanAttack(2, air.Id) && b.Attack(Side.Enemy, 2, air.Id).Success && b.Find(air.Id)is null, "Mothership shoots down balloon at close range");
        b = Fixture(ShipClass.Fishing, fish: new[] { new GridPosition(9, 8), new GridPosition(18, 0) });
        Check(b.Collect(Side.Player, 3, new(9, 8)).Success && b.Find(1)!.Resources == 1, "Fisher sends resource to mother remotely");
        Check(!b.Collect(Side.Player, 3, new(18, 0)).Success, "Cannot collect hidden remote fish");
        b = Fixture(fish: Resources);
        b.Collect(Side.Player, 1, Resources[0]);
        b.Collect(Side.Player, 1, Resources[1]);
        b.ChooseUpgrade(Side.Player, 1, UpgradeChoice.Mobility);
        Check(b.Find(1)!.MovementAllowance == 2 && b.Income(Side.Player) == 4, "Mobility alternative");
        foreach (var cell in Resources.Skip(2).Take(3))
            b.Collect(Side.Player, 1, cell);
        b.ChooseUpgrade(Side.Player, 1, UpgradeChoice.Restoration);
        foreach (var cell in Resources.Skip(5))
            b.Collect(Side.Player, 1, cell);
        b.ChooseUpgrade(Side.Player, 1, UpgradeChoice.SecondAttack);
        Check(b.Find(1)!.AttacksRemaining == 2 && !b.Ships.Any(s => s.IsAirborne), "Second attack alternative");
        var limited = new BattleRules
        {
            StartingCredits = 1,
            IncomePerMothership = 2,
            RepairAmount = 5,
            FleetLimit = 12,
            Ships = Rules.Ships
        };
        b = new BattleState(new GameBoard(20, 20, _ => TerrainType.Water), limited, new (Side, ShipClass, GridPosition)[] { (Side.Player, ShipClass.Mothership, new(5, 5)), (Side.Enemy, ShipClass.Mothership, new(18, 18)) }, Resources);
        Check(!b.Collect(Side.Player, 1, Resources[0]).Success && b.FishSpots.Contains(Resources[0]) && b.Find(1)!.Resources == 0, "Failed collection is atomic");
        Check(!b.BuyRadar(Side.Player, 1).Success && b.Credits(Side.Player) == 1, "Failed radar is atomic");
        var map = new GameBoard(20, 20, p => p == new GridPosition(10, 10) ? TerrainType.Land : TerrainType.Water);
        b = new BattleState(map, Rules, new (Side, ShipClass, GridPosition)[] { (Side.Player, ShipClass.Mothership, new(1, 1)), (Side.Enemy, ShipClass.Mothership, new(18, 18)), (Side.Player, ShipClass.Balloon, new(8, 8)) });
        Check(b.Move(Side.Player, 3, new(10, 10)).Success, "Balloon ignores land/coastal penalties");
        Check(b.FishSpots.All(p => map.GetTile(p).Terrain != TerrainType.Land), "Fish only on sea");
    }

    private static void Navigation()
    {
        var b = Fixture(target: new(18, 16));
        Check(b.StepCost(3, new(8, 8), new(9, 9)) == 10 && b.PathTo(3, new(11, 11)).Count == 4, "Diagonal steps cost exactly one tile");
        Check(b.Move(Side.Player, 3, new(11, 11)).Success && b.Find(3)!.MovementRemainingUnits == 10, "Three diagonals consume three movement points");
        foreach (var(kind, cost)in new[]
        {
            (ShipClass.Garrison, 10),
            (ShipClass.Invader, 13),
            (ShipClass.Kolonel, 15),
            (ShipClass.Fishing, 10)
        }

        )
        {
            b = Fixture(kind, target: new(18, 16), terrain: p => p == new GridPosition(9, 7) ? TerrainType.Land : TerrainType.Water);
            Check(b.StepCost(3, new(8, 8), new(9, 8)) == cost, "Coastal class penalty");
        }

        b = Fixture(target: new(18, 16), terrain: p => p == new GridPosition(9, 8) ? TerrainType.Land : TerrainType.Water);
        Check(b.StepCost(3, new(8, 8), new(9, 9))is null, "No island corner cutting");
        b = new BattleState(new GameBoard(20, 20, p => p == new GridPosition(4, 6) || p == new GridPosition(6, 6) ? TerrainType.Land : TerrainType.Water), Rules, new (Side, ShipClass, GridPosition)[] { (Side.Player, ShipClass.Mothership, new(5, 5)), (Side.Enemy, ShipClass.Mothership, new(18, 18)) }, Array.Empty<GridPosition>());
        Check(b.StepCost(1, new(5, 5), new(5, 6))is null, "Mother cannot pass one-cell strait");
    }

    private static void OrganicRules()
    {
        var center = new GridPosition(5, 5);
        foreach (int range in new[]
        {
            1,
            2
        }

        )
        {
            var navigationRules = new BattleRules
            {
                StartingCredits = 5,
                IncomePerMothership = 2,
                RepairAmount = 5,
                FleetLimit = 12,
                Ships = Rules.Ships.Select(s => s.Class == ShipClass.Garrison ? s with { Movement = range } : s).ToArray()
            };
            var navigation = new BattleState(new GameBoard(20, 20, _ => TerrainType.Water), navigationRules, new (Side, ShipClass, GridPosition)[] { (Side.Player, ShipClass.Mothership, new(0, 0)), (Side.Enemy, ShipClass.Mothership, new(19, 19)), (Side.Player, ShipClass.Garrison, center) });
            Check(navigation.Reachable(3).Count == (range == 1 ? 9 : 25), "Eight-way one-cost movement covers square rings");
            Check(navigation.Move(Side.Player, 3, range == 1 ? new(6, 6) : new(7, 6)).Success, "Diagonal ring destinations are executable");
        }

        Check(Enumerable.Range(3, 5).Sum(x => Enumerable.Range(3, 5).Count(y => BattleVision.InRadius(center, new(x, y), 1))) == 9, "Radius one includes eight neighbors");
        Check(Enumerable.Range(3, 5).Sum(x => Enumerable.Range(3, 5).Count(y => BattleVision.InRadius(center, new(x, y), 2))) == 21, "Radius two excludes exactly four corners");
        var b = Fixture(ShipClass.Fishing, target: new(10, 5), fish: Resources);
        Check(!b.CanAttack(1, 4), "Unseen target outside sight cannot be attacked");
        b.BuyRadar(Side.Player, 1);
        b.Find(1)!.Level = 5;
        b.Find(1)!.Health = b.Find(1)!.MaxHealth;
        b.BuyMortar(Side.Player, 1);
        Check(b.FindObserved(Side.Player, 4)is null && b.TargetCells(1).Contains(new(10, 5)), "Radar targeting exposes location only");
        Check(b.AttackAt(Side.Player, 1, new(10, 5)).Success, "Radar contact can be fired on within installed radar range");
        Check(!b.CanAttack(3, 4), "Unarmed collector never gains an attack");
        b = new BattleState(new GameBoard(20, 20, _ => TerrainType.Water), Rules, new (Side, ShipClass, GridPosition)[] { (Side.Player, ShipClass.Mothership, new(5, 5)), (Side.Enemy, ShipClass.Mothership, new(18, 18)) }, Resources);
        Check(b.Credits(Side.Player) == 5 && !b.Build(Side.Player, 1, ShipClass.Kolonel, new(6, 5)).Success, "Five starting Thors cannot buy heavy");
        b.SetCreative(true);
        Check(b.Build(Side.Player, 1, ShipClass.Garrison, new(6, 5)).Success && b.Credits(Side.Player) == 5, "Creative build free");
        Check(b.Collect(Side.Player, 1, Resources[0]).Success && b.Credits(Side.Player) == 5 && b.Find(1)!.Resources == 1, "Creative collection free but progresses");
        Check(b.BuildPrice(Side.Enemy, ShipClass.Kolonel) == 8 && b.CollectionCost(Side.Enemy) == 2, "Creative affects player only");
        Check(b.BuyRadar(Side.Player, 1).Success && b.Credits(Side.Player) == 3, "Creative preserves radar cost");
        b.SetCreative(false);
        Check(b.CollectionCost(Side.Player) == 2 && b.BuildPrice(Side.Player, ShipClass.Kolonel) == 8, "Turning creative off restores prices");
    }

    private static void Opponent()
    {
        for (int scenario = 0; scenario < 8; scenario++)
        {
            int seed = scenario;
            var map = scenario >= 6 ? ArchipelagoGenerator.Create(seed) : new GameBoard(20, 20, p => p.X >= 8 && p.X <= 10 && p.Y >= 4 + seed && p.Y <= 6 + seed ? TerrainType.Land : TerrainType.Water);
            var a = map.FleetAnchor(false);
            var z = map.FleetAnchor(true);
            var left = map.HarborCells(false);
            var right = map.HarborCells(true);
            var b = new BattleState(map, Rules, new (Side, ShipClass, GridPosition)[] { (Side.Player, ShipClass.Mothership, a), (Side.Player, ShipClass.Garrison, map.Mesh is null ? new(a.X + 2, a.Y) : left[1]), (Side.Player, ShipClass.Fishing, map.Mesh is null ? new(a.X + 1, a.Y + 2) : left[2]), (Side.Enemy, ShipClass.Mothership, z), (Side.Enemy, ShipClass.Garrison, map.Mesh is null ? new(z.X - 2, z.Y) : right[1]), (Side.Enemy, ShipClass.Fishing, map.Mesh is null ? new(z.X - 1, z.Y - 2) : right[2]) }, resourceSeed: seed);
            int actions = 0, perTurn = 0;
            while (!b.IsOver && b.Round < 180 && actions++ < 15000)
            {
                var side = b.ActiveSide;
                var result = SimpleOpponent.Step(b);
                Check(result.Success, "AI legal command " + result.Message);
                Check(b.Ships.All(s => s.Health > 0 && s.Health <= s.MaxHealth && s.Health == Math.Floor(s.Health)), "Integer valid HP");
                Check(b.Ships.Where(s => !s.IsAirborne).Select(s => s.Position).Distinct().Count() == b.Ships.Count(s => !s.IsAirborne), "Unique sea occupancy");
                Check(b.Ships.All(s => s.IsAirborne || map.GetTile(s.Position).Terrain != TerrainType.Land), "Sea movement legal");
                if (side != b.ActiveSide)
                    perTurn = 0;
                else
                    Check(++perTurn < 256, "AI finishes turn");
            }

            if (!b.IsOver)
                foreach (var ship in b.Ships)
                    Console.WriteLine($"STALL {ship.Owner} {ship.Definition.Class} at {ship.Position} HP {ship.Health} level {ship.Level} radar {ship.HasRadar} resources {ship.Resources}");
            Check(b.IsOver, $"AI scenario {scenario} completes");
            Console.WriteLine($"AI {scenario}: {b.Winner}, round {b.Round}, actions {actions}");
        }
    }
}
