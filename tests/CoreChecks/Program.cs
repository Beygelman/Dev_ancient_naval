using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.World;

int checks = 0;
void Check(bool condition, string name)
{
    if (!condition) throw new Exception(name);
    checks++;
}
void Reject(Action action, string name)
{
    try { action(); }
    catch (ArgumentOutOfRangeException) { checks++; return; }
    throw new Exception(name);
}
var board = new GameBoard(20, 20, p => p.X == 4 ? TerrainType.Land : TerrainType.Water);
Check(board.Tiles.Count == 400, "400 cells");
Check(board.Tiles.Select(t => t.Position).Distinct().Count() == 400, "Unique coordinates");
Check(board.GetTile(new(4, 8)).Terrain == TerrainType.Land, "Terrain retained");
Check(board.GetNeighbors(new(0, 0)).Count() == 2, "Corner neighbors");
Check(board.GetNeighbors(new(0, 8)).Count() == 3, "Edge neighbors");
Check(board.GetNeighbors(new(8, 8)).Count() == 4, "Interior neighbors");
Check(board.GetNeighbors(new(-1, 0)).Count() == 0, "Outside has no neighbors");
foreach (var tile in board.Tiles)
foreach (var neighbor in board.GetNeighbors(tile.Position))
    Check(Math.Abs(neighbor.X - tile.Position.X) + Math.Abs(neighbor.Y - tile.Position.Y) == 1,
        "No diagonal neighbors");
Check(!board.TryGetTile(new(-1, 0), out var missing) && missing is null, "Negative coordinate rejected");
Check(!board.TryGetTile(new(20, 0), out _), "Right edge excluded");
Check(!board.TryGetTile(new(0, 20), out _), "Bottom edge excluded");
Reject(() => board.GetTile(new(-1, 0)), "Invalid tile access");
Reject(() => new GameBoard(0, 20, _ => TerrainType.Water), "Zero width");
Reject(() => new GameBoard(20, -1, _ => TerrainType.Water), "Negative height");
Reject(() => new GameBoard(1, 1, _ => (TerrainType)42), "Invalid terrain");
Check(typeof(GameBoard).Assembly.GetReferencedAssemblies().All(a => !a.Name!.StartsWith("Godot")),
    "Core must not reference Godot");
Console.WriteLine($"PASS: {checks} core checks");
BattleScenarios.Run();
var persistenceRules = DevAncientNaval.Core.Battle.BattleRules.FromJson(
    File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "balance.json")));
int persistenceChecks = PersistenceChecks.Run(persistenceRules,
    Environment.GetEnvironmentVariable("ANCIENT_NAVAL_LEGACY_FIXTURES"));
Console.WriteLine($"PASS: {persistenceChecks} save compatibility and validation checks.");

Console.WriteLine($"PASS: {WorldGenerationScenarios.Run(persistenceRules)} four-world generation checks.");
Console.WriteLine($"PASS: {VoyageStatisticsChecks.Run(persistenceRules)} voyage statistics checks.");
Console.WriteLine($"PASS: {Refinement020Checks.Run(persistenceRules)} free-coast, anti-air, inland Pangaea and red-save checks.");
Console.WriteLine($"PASS: {Refinement021Checks.Run(persistenceRules)} salvo, encounters, level rewards and compatibility checks.");
Console.WriteLine($"PASS: {Rules0202Checks.Run(persistenceRules)} v0.20.2 progression, mountain shadows, radar and heavenly aid checks.");
Console.WriteLine($"PASS: {World0202Checks.Run(persistenceRules)} v0.20.2 world placement and deterministic terrain checks.");
Console.WriteLine($"PASS: {Culture0202Checks.Run(persistenceRules)} v0.20.2 themed Latin names and Continue checks.");

Console.WriteLine($"PASS: {Lighthouse0202Checks.Run(persistenceRules)} lighthouse and merged trade checks.");
Console.WriteLine($"PASS: {Admiral0203Checks.Run(persistenceRules)} v0.20.3 coordinated Admiral tactics and fog fairness checks.");
