using DevAncientNaval.Core.AI;
using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Units;
using DevAncientNaval.Core.World;
using DevAncientNaval.Core.Vision;

internal static partial class BattleScenarios
{
    private static void Refinement016()
    {
        var rules = BattleRules.FromJson(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "balance.json")));
        Check(rules.Get(ShipClass.Kolonel).AttackRange == 2 && rules.Get(ShipClass.Invader).AttackRange == 1 && rules.Get(ShipClass.Garrison).AttackRange == 2, "Current cannon ranges include the extended Brig range");
        Check(rules.Get(ShipClass.CannonTower).AttackRange == 4 && rules.Get(ShipClass.CannonTower).VisualRange == 2 && rules.Get(ShipClass.CannonTower).RadarRange == 4, "Tower distinguishes optical sight and radar");
        Check(rules.Get(ShipClass.Balloon).VisualRange == 6 && rules.Balloon.AntiAirRange == 2 && Rules.Balloon.AntiAirRange == 3, "Reduced balloon sight/AA coexist with legacy range snapshots");
        var b = new BattleState(new GameBoard(24, 24, _ => TerrainType.Water), rules, new[] { (Side.Player, ShipClass.Mothership, new GridPosition(8, 8)), (Side.Enemy, ShipClass.Mothership, new GridPosition(22, 22)), (Side.Enemy, ShipClass.Balloon, new GridPosition(10, 8)), (Side.Enemy, ShipClass.Balloon, new GridPosition(11, 8)), (Side.Player, ShipClass.CannonTower, new GridPosition(4, 4)) }, Array.Empty<GridPosition>(), villageSpots: Array.Empty<GridPosition>());
        foreach (var tile in b.Board.Tiles)
            b.Vision.RevealCombat(Side.Player, tile.Position);
        b.Vision.Recompute(b.Ships, 1);
        Check(b.CanAttack(1, 3) && !b.CanAttack(1, 4) && b.Damage(b.Find(1)!, b.Find(4)!) == 0, "Mothership AA cannot reach a third tile even when visible");
        var airRoute = b.PathToAttackPosition(1, 4);
        Check(airRoute.Count > 1 && b.Board.InRadius(airRoute[^1], b.Find(4)!.Position, 2), "AI anti-air routes approach the actual two-tile limit");
        Check(b.BuyRadar(Side.Player, 5).Success && b.Find(5)!.RadarRange == 4 && b.Find(5)!.VisualRange == 2, "Tower buys radar without extending optical sight");
        Check(b.Find(5)!.CanEarnVeterancy, "Cannon tower can earn ship-kill veterancy");
        b = new BattleState(new GameBoard(24, 24, _ => TerrainType.Water), rules, new[] { (Side.Player, ShipClass.Mothership, new GridPosition(2, 2)), (Side.Enemy, ShipClass.Mothership, new GridPosition(22, 22)), (Side.Player, ShipClass.CannonTower, new GridPosition(8, 8)), (Side.Enemy, ShipClass.Fishing, new GridPosition(9, 8)), (Side.Enemy, ShipClass.Fishing, new GridPosition(10, 8)), (Side.Enemy, ShipClass.Fishing, new GridPosition(8, 9)) }, Array.Empty<GridPosition>(), villageSpots: Array.Empty<GridPosition>());
        for (int victim = 4; victim <= 6; victim++)
        {
            b.Find(victim)!.Health = 1;
            Check(b.Attack(Side.Player, 3, victim).Success, "Tower earns a real kill");
            if (victim < 6)
            {
                b.EndTurn(Side.Player);
                b.EndTurn(Side.Enemy);
            }
        }

        Check(b.Find(3)!.IsVeteran && b.Find(3)!.Kills == 3 && b.Find(3)!.Health == b.Find(3)!.MaxHealth && b.Find(3)!.FullDamage == 4, "Tower promotion heals and increases damage/health");
        Check(BattleState.LoadJson(b.SaveJson()).Find(3)!.IsVeteran, "Tower veterancy and radar-capable definition survive save");
        for (int level = 1; level <= 5; level++)
        {
            var cell = new GridPosition(8, 8);
            b = new BattleState(new GameBoard(24, 24, p => p == cell ? TerrainType.Land : TerrainType.Water), rules, new[] { (Side.Player, ShipClass.Mothership, new GridPosition(2, 2)), (Side.Enemy, ShipClass.Mothership, new GridPosition(22, 22)), (Side.Enemy, ShipClass.Garrison, new GridPosition(9, 8)), (Side.Enemy, ShipClass.Kolonel, new GridPosition(8, 9)), (Side.Enemy, ShipClass.Balloon, new GridPosition(9, 9)) }, Array.Empty<GridPosition>(), villageSpots: new[] { cell });
            var town = b.Villages.Single();
            town.Owner = Side.Player;
            town.IsFortified = true;
            town.Level = level;
            town.Health = town.MaxHealth - 1;
            b.Find(3)!.Health = 4;
            b.Find(4)!.Health = 6;
            b.Vision.Recompute(b.Ships, 1, b.Villages);
            Check(b.VillageBuildBlockReason(Side.Player, town.Id, ShipClass.CannonTower)is not null, "No village level can build a tower");
            var turn = b.EndTurn(Side.Player);
            if (level == 1)
                Check(turn.OutpostShots!.Count == 0 && !town.HasAttacked, "Level-one outpost cannot fire");
            else
            {
                var shot = turn.OutpostShots!.Single();
                Check(shot.Target.Id == 3 && shot.Damage == Math.Min(level, 4) && b.Find(4)!.Health == 6, "Outpost chooses lowest HP and applies one level-scaled hit");
                Check(town.HasAttacked && town.Health == town.MaxHealth - 1, "Active automatic fire prevents passive town repair");
                Check(b.VillageCounterDamage(town) == level + 1, "Town counter damage exceeds active damage by one");
            }

            Check(BattleState.LoadJson(b.SaveJson()).SaveJson() == b.SaveJson(), "Automatic town shot state saves exactly");
            b.EndTurn(Side.Enemy);
            if (level >= 2)
            {
                town.Health = town.MaxHealth - 1;
                Check(b.RepairVillage(Side.Player, town.Id).Success && b.EndTurn(Side.Player).OutpostShots!.Count == 0, "Manual town repair suppresses automatic fire");
            }
        }

        for (int level = 1; level <= 5; level++)
        {
            var cell = new GridPosition(8, 8);
            b = new BattleState(new GameBoard(24, 24, p => p == cell ? TerrainType.Land : TerrainType.Water), rules, new[] { (Side.Player, ShipClass.Mothership, new GridPosition(2, 2)), (Side.Enemy, ShipClass.Mothership, new GridPosition(22, 22)), (Side.Player, ShipClass.Kolonel, new GridPosition(9, 8)) }, Array.Empty<GridPosition>(), villageSpots: new[] { cell });
            var town = b.Villages.Single();
            town.Owner = Side.Enemy;
            town.Level = level;
            town.Health = town.MaxHealth;
            town.IsFortified = true;
            b.Vision.Recompute(b.Ships, 1, b.Villages);
            var hit = b.AttackVillage(Side.Player, 3, town.Id);
            Check(hit.Success && hit.StructureHit!.CounterDamage == (level >= 2 ? level + 1 : 0), "Surviving outpost replies with level plus one from level two");
            Check(!town.HasAttacked, "Town replies do not count as active attacks");
        }

        b = new BattleState(new GameBoard(24, 24, _ => TerrainType.Water), rules, new[] { (Side.Player, ShipClass.Mothership, new GridPosition(10, 10)), (Side.Enemy, ShipClass.Mothership, new GridPosition(22, 22)), (Side.Enemy, ShipClass.Kolonel, new GridPosition(12, 10)) }, Array.Empty<GridPosition>(), villageSpots: Array.Empty<GridPosition>());
        var mother = b.Find(1)!;
        mother.Health = mother.MaxHealth * .4;
        b.Vision.Recompute(b.Ships, 1);
        var enemy = b.Find(3)!;
        Check(b.WeaponCovers(enemy, mother.Position), "Retreat fixture starts inside a known threat");
        var result = SimpleOpponent.Step(b);
        Check(result.Success && result.Kind == CommandKind.Move && result.ActorId == mother.Id && !b.WeaponCovers(enemy, mother.Position), "Wounded flagship escapes before attacking/building");
        b = new BattleState(new GameBoard(24, 24, _ => TerrainType.Water), rules, new[] { (Side.Player, ShipClass.Mothership, new GridPosition(10, 10)), (Side.Enemy, ShipClass.Mothership, new GridPosition(22, 22)), (Side.Enemy, ShipClass.Kolonel, new GridPosition(13, 10)) }, Array.Empty<GridPosition>(), villageSpots: Array.Empty<GridPosition>());
        b.Find(1)!.Health = b.Find(1)!.MaxHealth * .4;
        b.Vision.Recompute(b.Ships, 1);
        Check(!b.ObservedShips(Side.Player).Any(s => s.Id == 3) && FlagshipSafety.Retreat(b, b.Find(1)!, b.ObservedShips(Side.Player).Where(s => s.Owner != Side.Player).ToArray())is null, "Cautious AI does not react to hidden enemies");
        var coast = new GridPosition(8, 8);
        b = new BattleState(new GameBoard(24, 24, p => p == coast ? TerrainType.Land : TerrainType.Water), rules, new[] { (Side.Player, ShipClass.Mothership, new GridPosition(2, 2)), (Side.Enemy, ShipClass.Mothership, new GridPosition(22, 22)), (Side.Pirates, ShipClass.PirateSchooner, new GridPosition(9, 8)) }, Array.Empty<GridPosition>(), villageSpots: new[] { coast });
        var outpost = b.Villages.Single();
        outpost.Owner = Side.Player;
        outpost.Level = 2;
        outpost.Health = 10;
        outpost.IsFortified = true;
        b.Find(3)!.Health = 2;
        b.Vision.Recompute(b.Ships, 1, b.Villages);
        int money = b.Credits(Side.Player);
        var pirateShot = b.EndTurn(Side.Player);
        Check(pirateShot.OutpostShots is { Count: 1 } && b.Find(3)is null && b.Credits(Side.Player) == money + 2 && b.Mothership(Side.Player)!.Resources == 1, "Outpost pirate kills grant the owner's normal currency/resource bounty");
        var generated = SkirmishSetup.Create(ArchipelagoGenerator.Create(116, 3), rules, 3);
        int fishBudget = (int)Math.Round(Math.Max(rules.Economy.MinimumResourceSpots,
            generated.Board.Tiles.Count / rules.Economy.ResourceTileInterval) * .70);
        Check(generated.FishSpots.Count >= fishBudget - generated.Treasuries.Count &&
            generated.FishSpots.Count <= fishBudget, "Reduced fish budget scales with map area; treasury sites replace overlapping fish");
        Console.WriteLine("REFINEMENT016: tower radar/veterancy, outpost targeting, AA, retreat and fish density passed");
    }
}
