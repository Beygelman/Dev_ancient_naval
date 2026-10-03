using DevAncientNaval.Core.AI;
using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Units;
using DevAncientNaval.Core.World;

internal static partial class BattleScenarios
{
    private static void FactionEconomy()
    {
<<<<<<< Updated upstream
        var rules = BattleRules.FromJson(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "balance-v0202.json")));
        Check(rules.StartingCredits == 8 && rules.Get(ShipClass.Garrison).Price == 5 && rules.Get(ShipClass.Kolonel).Price == 12 && rules.Get(ShipClass.Invader).Price == 7, "Current hull prices follow the requested 0.21 economy");
        Check(rules.Get(ShipClass.Kolonel).AttackRange == 2 && rules.Get(ShipClass.Invader).AttackRange == 2, "Kolonel and the improved Galleon reach two tiles");
=======
        var rules = BattleRules.FromJson(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "balance.json")));
        Check(rules.StartingCredits == 8 && rules.Get(ShipClass.Garrison).Price == 6 && rules.Get(ShipClass.Kolonel).Price == 20, "Current economy requires saving for larger warships");
        Check(rules.Get(ShipClass.Kolonel).AttackRange == 2 && rules.Get(ShipClass.Invader).AttackRange == 2, "Kolonel and Galleon cannons reach two tiles");
>>>>>>> Stashed changes
        Check(Rules.Economy.MothershipIncomePerLevel == 2 && Rules.Economy.CombatShipsPerUpkeep == 0 && !Rules.Economy.AdjacentCollectionOnly, "Released balance snapshots preserve their economy and collection rules");
        var board = new GameBoard(32, 32, _ => TerrainType.Water);
        var setup = new List<(Side, ShipClass, GridPosition)>();
        for (int i = 0; i < 5; i++)
        {
            var side = BattleState.PlayableSides[i];
            setup.Add((side, ShipClass.Mothership, new(2 + i * 5, 2)));
            setup.Add((side, ShipClass.Garrison, new(2 + i * 5, 3)));
            setup.Add((side, ShipClass.Fishing, new(2 + i * 5, 4)));
        }
<<<<<<< Updated upstream

=======
>>>>>>> Stashed changes
        var battle = new BattleState(board, rules, setup, new[] { new GridPosition(3, 2), new GridPosition(4, 2) }, villageSpots: Array.Empty<GridPosition>());
        battle.SetPlayerColor(FleetColor.White);
        Check(battle.OpponentCount == 4 && battle.Factions.Count == 5 && battle.AliveFactions.Count() == 5, "One player and four independently owned rivals deploy");
        Check(battle.Factions.Select(battle.ColorFor).Distinct().Count() == 5 && battle.ColorFor(Side.Player) == FleetColor.White, "All fleets have distinct colors excluding the player choice");
        Check(battle.CollectionCells(1).Contains(new(3, 2)) && !battle.CollectionCells(1).Contains(new(4, 2)), "Only directly adjacent resources can be collected");
        foreach (var side in battle.Factions)
            Check(battle.Income(side) == 3 && battle.Upkeep(side) == 0, "Initial two warships and one fisher yield three net income");
        int funds = battle.Credits(Side.Player);
        foreach (var side in battle.Factions.ToArray())
            Check(battle.EndTurn(side).Success, "Every living faction advances its own turn");
        Check(battle.ActiveSide == Side.Player && battle.Round == 2 && battle.Credits(Side.Player) == funds + 3, "Full five-fleet round credits player exactly once");
        string saved = battle.SaveJson();
        var restored = BattleState.LoadJson(saved);
        Check(restored.SaveJson() == saved && restored.Factions.SequenceEqual(battle.Factions), "Roster, colors, fog and accounts survive save exactly");
        Check(restored.Factions.All(side => restored.ColorFor(side) == battle.ColorFor(side)), "Random rival colors remain fixed on resume");
<<<<<<< Updated upstream
=======

>>>>>>> Stashed changes
        // Put rival warships in sight: an AI fleet attacks a different AI fleet.
        var one = battle.OwnShips(Side.Enemy).First(ship => ship.Definition.Class == ShipClass.Garrison);
        var two = battle.OwnShips(Side.Enemy2).First(ship => ship.Definition.Class == ShipClass.Garrison);
        one.Position = new(12, 12);
        two.Position = new(13, 12);
        battle.EndTurn(Side.Player);
        battle.Vision.Recompute(battle.Ships, battle.TurnSerial);
        var shot = SimpleOpponent.Step(battle);
        Check(shot.Kind == CommandKind.Attack && shot.ActorId == one.Id && shot.TargetId == two.Id, "Rival factions fight one another using ordinary AI commands");
<<<<<<< Updated upstream
=======

>>>>>>> Stashed changes
        // Defeat only one flagship: the other living fleets must still receive turns.
        var enemyMother = battle.Mothership(Side.Enemy2)!;
        one.Position = new(17, 17);
        enemyMother.Position = new(18, 17);
        enemyMother.Health = 1;
        one.AttacksUsed = 0;
        battle.Vision.Recompute(battle.Ships, battle.TurnSerial);
<<<<<<< Updated upstream
        Check(battle.Attack(Side.Enemy, one.Id, enemyMother.Id).Success && battle.Mothership(Side.Enemy2)is null && !battle.IsOver, "One defeated rival does not end a five-fleet battle");
=======
        Check(battle.Attack(Side.Enemy, one.Id, enemyMother.Id).Success && battle.Mothership(Side.Enemy2) is null && !battle.IsOver, "One defeated rival does not end a five-fleet battle");
>>>>>>> Stashed changes
        Check(!battle.OwnShips(Side.Enemy2).Any(), "Defeated flagship withdraws its fleet");
        battle.EndTurn(Side.Enemy);
        Check(battle.ActiveSide == Side.Enemy3, "Turn order skips the eliminated faction");
        Check(BattleState.LoadJson(battle.SaveJson()).SaveJson() == battle.SaveJson(), "An eliminated faction remains compatible with saved turn order");
<<<<<<< Updated upstream
        // Production / maintenance are exercised using a funded current-rules aggregate.
        var fundedDocument = System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "balance-v0202.json")))!;
        fundedDocument["startingCredits"] = 100;
        var fundedRules = BattleRules.FromJson(fundedDocument.ToJsonString());
        battle = new BattleState(new GameBoard(20, 20, _ => TerrainType.Water), fundedRules, new[] { (Side.Player, ShipClass.Mothership, new GridPosition(5, 5)), (Side.Player, ShipClass.Garrison, new GridPosition(6, 5)), (Side.Player, ShipClass.Fishing, new GridPosition(5, 6)), (Side.Enemy, ShipClass.Mothership, new GridPosition(18, 18)) }, Array.Empty<GridPosition>(), villageSpots: Array.Empty<GridPosition>());
=======

        // Production / maintenance are exercised using a funded current-rules aggregate.
        var fundedDocument = System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "balance.json")))!;
        fundedDocument["startingCredits"] = 100;
        var fundedRules = BattleRules.FromJson(fundedDocument.ToJsonString());
        battle = new BattleState(new GameBoard(20, 20, _ => TerrainType.Water), fundedRules, new[]
        {
            (Side.Player, ShipClass.Mothership, new GridPosition(5, 5)),
            (Side.Player, ShipClass.Garrison, new GridPosition(6, 5)),
            (Side.Player, ShipClass.Fishing, new GridPosition(5, 6)),
            (Side.Enemy, ShipClass.Mothership, new GridPosition(18, 18))
        }, Array.Empty<GridPosition>(), villageSpots: Array.Empty<GridPosition>());
>>>>>>> Stashed changes
        var mother = battle.Mothership(Side.Player)!;
        mother.Level = 5;
        mother.Health = mother.MaxHealth;
        var built = battle.Build(Side.Player, mother.Id, ShipClass.CannonTower, new(4, 5));
        var tower = battle.Find(built.TargetId)!;
        Check(built.Success && tower.IsStructure && !tower.HasMortar && !tower.CanMove && !tower.CountsTowardFleet && tower.AttacksRemaining == 1, "A buildable cannon tower is stationary, distinct from an ancient mortar and outside fleet capacity");
<<<<<<< Updated upstream
        Check(tower.Definition.AttackRange == 4 && tower.Definition.Damage == 3 && tower.MaxHealth == 10, "Cannon tower has short-range guns and ten HP");
=======
        Check(tower.Definition.AttackRange == 2 && tower.Definition.Damage == 3 && tower.MaxHealth == 10, "Cannon tower has short-range guns and ten HP");
>>>>>>> Stashed changes
        battle.EndTurn(Side.Player);
        var credited = battle.EndTurn(Side.Enemy);
        Check(credited.IncomeReceipts!.Sum(receipt => receipt.Amount) == 3 && credited.IncomeReceipts!.Any(receipt => receipt.SourceId == $"ship:{mother.Id}" && receipt.Position == mother.Position), "Per-source income events reconcile with actual currency");
        Check(battle.Income(Side.Player) == 3, "A level-five Mothership no longer multiplies its base income");
        Check(Enumerable.Range(1, 5).Select(level => battle.VillageIncome(new Village(999, new(3, 3)) { Level = level })).SequenceEqual(new[] { 1, 1, 2, 2, 3 }), "Village levels improve shipyards and hulls faster than income");
        Check(battle.Build(Side.Player, mother.Id, ShipClass.Garrison, new(5, 4)).Success, "Additional warship launches from an adjacent shipyard berth");
        Check(battle.Upkeep(Side.Player) == 1 && battle.Income(Side.Player) == 2, "Additional warships create visible maintenance instead of unlimited economic snowballing");
        battle.EndTurn(Side.Player);
        credited = battle.EndTurn(Side.Enemy);
        Check(credited.IncomeReceipts!.Sum(receipt => receipt.Amount) == 2 && credited.IncomeReceipts!.Single(receipt => receipt.IsUpkeep).Amount == -1, "Upkeep receipt matches the net credit at turn start");
<<<<<<< Updated upstream
        battle = new BattleState(new GameBoard(20, 20, _ => TerrainType.Water), rules, new[] { (Side.Player, ShipClass.Mothership, new GridPosition(2, 2)), (Side.Enemy, ShipClass.Mothership, new GridPosition(18, 18)), (Side.Player, ShipClass.CannonTower, new GridPosition(8, 8)), (Side.Enemy, ShipClass.Garrison, new GridPosition(9, 8)) }, Array.Empty<GridPosition>(), villageSpots: Array.Empty<GridPosition>());
        var towerShot = battle.Attack(Side.Player, 3, 4);
        Check(towerShot.Success && towerShot.Amount == 3 && towerShot.Shots![0].IsMortar == false && towerShot.Splash!.Count == 0, "Defensive tower fires a short-range cannon without mortar splash");
        Check(!battle.CanAttack(3, 4) && !battle.Move(Side.Player, 3, new(8, 9)).Success, "A cannon tower has one shot and cannot move");
        battle = new BattleState(new GameBoard(20, 20, _ => TerrainType.Water), rules, new[] { (Side.Player, ShipClass.Mothership, new GridPosition(5, 5)), (Side.Enemy, ShipClass.Mothership, new GridPosition(6, 5)), (Side.Enemy2, ShipClass.Mothership, new GridPosition(6, 6)) }, Array.Empty<GridPosition>(), villageSpots: Array.Empty<GridPosition>());
=======

        battle = new BattleState(new GameBoard(20, 20, _ => TerrainType.Water), rules, new[]
        {
            (Side.Player, ShipClass.Mothership, new GridPosition(2, 2)),
            (Side.Enemy, ShipClass.Mothership, new GridPosition(18, 18)),
            (Side.Player, ShipClass.CannonTower, new GridPosition(8, 8)),
            (Side.Enemy, ShipClass.Garrison, new GridPosition(10, 8))
        }, Array.Empty<GridPosition>(), villageSpots: Array.Empty<GridPosition>());
        var towerShot = battle.Attack(Side.Player, 3, 4);
        Check(towerShot.Success && towerShot.Amount == 3 && towerShot.Shots![0].IsMortar == false && towerShot.Splash!.Count == 0, "Defensive tower fires a short-range cannon without mortar splash");
        Check(!battle.CanAttack(3, 4) && !battle.Move(Side.Player, 3, new(8, 9)).Success, "A cannon tower has one shot and cannot move");

        battle = new BattleState(new GameBoard(20, 20, _ => TerrainType.Water), rules, new[]
        {
            (Side.Player, ShipClass.Mothership, new GridPosition(5, 5)),
            (Side.Enemy, ShipClass.Mothership, new GridPosition(7, 5)),
            (Side.Enemy2, ShipClass.Mothership, new GridPosition(7, 6))
        }, Array.Empty<GridPosition>(), villageSpots: Array.Empty<GridPosition>());
>>>>>>> Stashed changes
        battle.Find(2)!.Health = 1;
        battle.Find(3)!.Health = 1;
        Check(battle.Attack(Side.Player, 1, 2).Success && !battle.IsOver, "Destroying the first rival flagship leaves the other rival in the battle");
        battle.EndTurn(Side.Player);
        battle.EndTurn(Side.Enemy2);
        Check(battle.Attack(Side.Player, 1, 3).Success && battle.Winner == Side.Player && battle.IsOver, "Victory requires the last rival flagship to fall");
<<<<<<< Updated upstream
=======

>>>>>>> Stashed changes
        for (int opponents = 1; opponents <= 4; opponents++)
        {
            for (int seed = 25; seed < 28; seed++)
            {
                battle = SkirmishSetup.Create(ArchipelagoGenerator.Create(seed, opponents), rules, opponents);
                Check(battle.Factions.Count == opponents + 1 && battle.Ships.Count(ship => ship.Owner == Side.Pirates) == opponents + 1, "Pirate population follows the number of player fleets");
                var villageCounts = Enumerable.Range(0, opponents + 1).Select(index => battle.Villages.Count(village => battle.Board.StartingTerritory(village.Position, opponents + 1) == index)).ToArray();
                var treasureCounts = Enumerable.Range(0, opponents + 1).Select(index => battle.Treasuries.Count(treasure => battle.Board.StartingTerritory(treasure.Position, opponents + 1) == index)).ToArray();
                Check(villageCounts.Distinct().Count() == 1 && villageCounts[0] > 0 && treasureCounts.Distinct().Count() == 1 && treasureCounts[0] > 0, "Every generated territory receives equal nonempty villages and treasuries");
                Check(BattleState.LoadJson(battle.SaveJson()).SaveJson() == battle.SaveJson(), "Every supported opponent count saves and resumes");
            }
        }

        // A complete game continues through eliminated fleets until one AI/player
        // remains. The compact clear sea isolates faction/turn/economy rules.
        for (int opponents = 2; opponents <= 4; opponents += 2)
        {
            battle = SkirmishSetup.Create(new GameBoard(24, 24, _ => TerrainType.Water, seed: 19), rules, opponents);
            int commands = 0;
            int perTurn = 0;
            while (!battle.IsOver && battle.Round < 180 && commands++ < 20_000)
            {
                var previous = battle.ActiveSide;
                var result = SimpleOpponent.Step(battle);
                Check(result.Success, "Multi-fleet AI produces legal commands: " + result.Message);
                Check(battle.Ships.All(ship => ship.Health > 0 && ship.Health <= ship.MaxHealth), "Multi-fleet match retains valid hull health");
                if (previous != battle.ActiveSide)
                    perTurn = 0;
                else
                    Check(++perTurn < 256, "A multi-fleet AI turn terminates");
            }
<<<<<<< Updated upstream

=======
>>>>>>> Stashed changes
            Check(battle.IsOver && battle.AliveFactions.Count() <= 1, "A complete current-balance multi-fleet match finishes");
            Console.WriteLine($"MULTI_AI opponents={opponents} winner={battle.Winner} round={battle.Round} commands={commands}");
        }
    }
}
