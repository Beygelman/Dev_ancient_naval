using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Units;
using DevAncientNaval.Core.World;

internal static partial class BattleScenarios
{
    private static void MovementPreviews()
    {
        void Compare(BattleState battle, int id, string context)
        {
            var preview = battle.PreviewMovement(id);
            var costs = battle.Reachable(id);
            Check(preview.Costs.Count == costs.Count && costs.All(pair => preview.Costs.TryGetValue(pair.Key, out int value) && value == pair.Value), context + ": preview costs equal uncached reachable cells");
            foreach (var tile in battle.Board.Tiles)
                Check(preview.PathTo(tile.Position).SequenceEqual(battle.PathTo(id, tile.Position)), context + ": preview preserves each destination and route tie order");
        }

        var battle = new BattleState(new GameBoard(20, 20, _ => TerrainType.Water), Funded, new[] { (Side.Player, ShipClass.Mothership, new GridPosition(1, 1)), (Side.Enemy, ShipClass.Mothership, new GridPosition(18, 18)), (Side.Player, ShipClass.Garrison, new GridPosition(8, 8)), (Side.Player, ShipClass.Fishing, new GridPosition(9, 8)), (Side.Enemy, ShipClass.Garrison, new GridPosition(10, 8)), (Side.Player, ShipClass.Balloon, new GridPosition(8, 8)) }, Array.Empty<GridPosition>(), villageSpots: Array.Empty<GridPosition>());
        Compare(battle, 3, "Friendly transit and enemy threats");
        var preview = battle.PreviewMovement(3);
        Check(!preview.Costs.ContainsKey(new(9, 8)) && !preview.Costs.ContainsKey(new(10, 8)) && preview.PathTo(new(10, 7)).Count > 1, "Occupied cells cannot be destinations while nearby transit remains possible");
        Check(battle.StepCost(3, new(8, 8), new(9, 7)) == 20, "Known enemy adjacency adds threat movement cost to previews");
        Compare(battle, 6, "Airborne movement over occupied sea cells");
        Check(battle.PreviewMovement(6).Costs.ContainsKey(new(9, 8)) && battle.PreviewMovement(6).Costs.ContainsKey(new(10, 8)), "Flight previews can land over friendly and enemy sea units");
        var oldPreview = battle.PreviewMovement(3);
        var oldCosts = oldPreview.Costs.ToDictionary(pair => pair.Key, pair => pair.Value);
        battle.Find(5)!.Position = new(15, 15);
        battle.Vision.Recompute(battle.Ships, battle.TurnSerial);
        Compare(battle, 3, "New visibility and enemy departure");
        Check(oldPreview.Costs.Count == oldCosts.Count && oldCosts.All(pair => oldPreview.Costs[pair.Key] == pair.Value), "A cached route snapshot stays fixed after battle mutations");
        Check(battle.PreviewMovement(3).Costs.ContainsKey(new(10, 8)), "A fresh route snapshot observes the vacated enemy tile");
        battle = Fixture(ShipClass.Garrison, target: new(15, 15), terrain: cell => cell == new GridPosition(12, 8) ? TerrainType.Land : TerrainType.Water);
        Check(!battle.Vision.IsExplored(Side.Player, new(12, 8)), "Fog obstacle is initially undiscovered");
        Compare(battle, 3, "Unknown terrain estimate");
        Check(battle.PreviewMovement(3).PathTo(new(12, 8)).Count > 1, "Preview does not reveal a hidden land cell");
        var moved = battle.Move(Side.Player, 3, new(12, 8));
        Check(moved.Success && battle.Find(3)!.Position != new GridPosition(12, 8), "Actual movement still stops before the undiscovered obstacle");
        Compare(battle, 3, "After actual movement and newly revealed coast");
        battle.Find(3)!.IsExhausted = true;
        Compare(battle, 3, "Exhausted ship");
        Check(battle.PreviewMovement(999).Costs.Count == 0, "A missing ship has an empty preview");
        var organic = SkirmishSetup.Create(ArchipelagoGenerator.Create(731, 1), Rules);
        Compare(organic, organic.OwnShips(Side.Player).First(ship => ship.Definition.Class == ShipClass.Garrison).Id, "Organic mesh route snapshot");
        foreach (int rivals in new[]
        {
            1,
            2,
            3,
            4
        }

        )
        {
            var world = ArchipelagoGenerator.Create(731, rivals);
            var mesh = world.Mesh!;
            foreach (var origin in Enumerable.Range(0, rivals + 1).Select(index => world.FleetAnchor(index, rivals + 1)))
                foreach (int radius in new[]
                {
                    0,
                    1,
                    2,
                    3,
                    5,
                    12,
                    64
                }

                )
                {
                    var expected = world.Tiles.Where(tile => world.InRadius(origin, tile.Position, radius)).Select(tile => tile.Position).ToHashSet();
                    Check(expected.SetEquals(mesh.Within(origin, radius)), "Bounded vision search matches independent full-board distance coverage");
                }

            Check(mesh.Within(new(-1, -1), 3).Count == 0 && mesh.Within(world.CentralCell, -1).Count == 0, "Invalid optical coverage queries return no cells");
        }
    }
}
