using System;
using System.Linq;
using System.Threading.Tasks;
using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Units;
using DevAncientNaval.Core.World;
using DevAncientNaval.Presentation;
using Godot;
using Side = DevAncientNaval.Core.Units.Side;

namespace DevAncientNaval.Tests.Runtime;

/// <summary>Native geometry and retained-art checks; never writes the player's save.</summary>
public partial class WorldVisual0203Checks : Node
{
    public Main Game { get; set; } = null!;
    private int _checks;
    private void Check(bool value, string description)
    {
        if (!value) throw new InvalidOperationException("World visual v020.3: " + description);
        _checks++;
    }
    private async Task Frame()
    {
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        RenderingServer.ForceDraw();
    }
    private async Task Capture(string suffix)
    {
        var arg = OS.GetCmdlineUserArgs().FirstOrDefault(a => a.StartsWith("--capture="));
        if (arg is null || DisplayServer.GetName() == "headless") return;
        await Frame();
        Check(GetViewport().GetTexture().GetImage().SavePng(arg[10..].Replace(".png", "-" + suffix + ".png")) == Error.Ok,
            "capture " + suffix);
    }
    public override async void _Ready()
    {
        try
        {
            await Frame();
            Game.FastChecks = true;
            var board = new GameBoard(40, 28, p =>
                Math.Pow((p.X - 20) / 12.0, 2) + Math.Pow((p.Y - 13) / 8.0, 2) < 1 ||
                p.X is >= 4 and <= 5 && p.Y is >= 18 and <= 21 ? TerrainType.Land : TerrainType.Water, seed: 927);
            var battle = new BattleState(board, Game.Battle.Rules,
                new[] { (Side.Player, ShipClass.Mothership, new GridPosition(2, 2)),
                    (Side.Enemy, ShipClass.Mothership, new GridPosition(38, 25)),
                    (Side.Player, ShipClass.Garrison, new GridPosition(2, 8)) },
                Array.Empty<GridPosition>(), villageSpots: Array.Empty<GridPosition>());
            var save = battle.CaptureSnapshot();
            var cells = new[] { new GridPosition(2, 8), new(2, 13), new(36, 7), new(36, 14) };
            save.Treasuries = cells.Select(p => new Treasury(save.NextId++, p)).ToArray();
            save.Outcomes = save.Treasuries.Select(t => new SavedOutcome(t.Id, TreasuryReward.Currency)).ToArray();
            battle = BattleState.LoadJson(BattleState.SerializeSnapshot(save));
            Check(battle.Rules.PersistTreasuryRuins, "current rules preserve discovered ruin records");
            Check(battle.EndTurn(Side.Player).Success && battle.EndTurn(Side.Enemy).Success,
                "the ruin crew waits through a complete turn");
            battle.SetGodEye(true);
            Game.LoadScenario(battle);
            Game.Home.Hide();
            Game.Refresh();
            Game.MapCamera.FitBoard();
            string untouched = battle.SaveJson();
            for (int i = 0; i < 14; i++) await Frame();
            var view = Game.BoardView;
            Check(view.CosmeticRiverCount > 0, "land islands contain cosmetic river illustrations");
            Check(view.CosmeticRiverDiagnostics.All(r => r.IslandCells >= 8 && r.MinimumWidth is > 0 and < .2f &&
                    r.MaximumWidth > r.MinimumWidth && r.Centerline.Length >= 3 &&
                    r.Centerline.All(p => p.IsFinite()) && r.Cells.Length >= 2),
                "rivers remain thin ribbons with finite paths spanning at least two cells and variable banks");
            Check(view.CosmeticRiverDiagnostics.All(r => r.Centerline.Zip(r.Centerline.Skip(1))
                    .All(pair => pair.First.DistanceTo(pair.Second) < 100)),
                "curved river paths remain connected through their mouths and endpoints");
            var water = view.CosmeticRiverWaterShapes.ToArray();
            Check(water.Length > 0, "river bodies contain clipped drawable water");
            bool validWater = true;
            foreach (var (_, polygon) in water)
            {
                var triangles = Geometry2D.TriangulatePolygon(polygon);
                validWater &= polygon.All(p => p.IsFinite()) && triangles.Length >= 3;
                for (int i = 0; i < triangles.Length; i += 3)
                    validWater &= view.VisualLandContains((polygon[triangles[i]] + polygon[triangles[i + 1]] +
                        polygon[triangles[i + 2]]) / 3);
            }
            Check(validWater, "every river triangle is valid and stays inside the island contour");
            var heights = view.MountainHeights.OrderBy(h => h).ToArray();
            Check(heights.Length >= 3 && heights[^1] - heights[0] > 12,
                "summits have visibly varied projected heights");
            float snowThreshold = Math.Max(58, heights[(int)((heights.Length - 1) * .82f)]);
            Check(view.SnowyMountainHeights.All(h => h >= snowThreshold),
                "snow is restricted to the tallest summits");
            Check(view.DecorativeRuinCount >= 4 && view.DecorativeRuinVariants.Distinct().Count() == 4,
                "passive inland ruins contain four distinct destroyed settlement motifs");
            Check(view.TreasuryRuinModelCount == 4 && view.TreasuryRuinVariants.Distinct().Count() == 4,
                "sunken treasuries use tower, colonnade, prow and fort motifs");
            Check(battle.SaveJson() == untouched, "building cosmetic scenery leaves terrain, ships and event RNG untouched");
            Check(battle.CanLootTreasury(Side.Player, 3) && Game.Ambience.TreasureGlowVertexCount == 28,
                "only the ready treasury carries its seven retained vertical light rays");
            await Capture("islands-rivers-and-ruins");
            foreach (var treasury in battle.Treasuries)
            {
                Game.MapCamera.Position = view.Projection.GridToWorld(treasury.Position) + new Vector2(0, -15);
                Game.MapCamera.Zoom = Vector2.One * 3;
                Game.MapCamera.ForceUpdateScroll();
                await Frame();
                await Capture("sunken-motif-" + ((board.Seed + treasury.Id * 17) % 4));
            }
            int atlas = view.SceneryAtlasBuildCount, waves = Game.Ambience.WaveMeshBuildCount,
                glows = Game.Ambience.TreasureGlowBuildCount;
            for (int i = 0; i < 6; i++)
            {
                Game.MapCamera.Pan(new Vector2(7, -3));
                view.Select(new GridPosition(i + 8, 10));
                Game.Ambience.RefreshVisibility();
                await Frame();
            }
            Check(view.SceneryAtlasBuildCount == atlas && view.SceneryAtlasIdle &&
                    Game.Ambience.WaveMeshBuildCount == waves && Game.Ambience.TreasureGlowBuildCount == glows,
                "pan, selection and repeated visibility refresh retain all scenery, wave and light meshes");
            var record = battle.TreasuryAt(cells[0])!;
            Check(battle.LootTreasury(Side.Player, 3).Success, "ready crew can collect the illustrated ruin");
            Game.Refresh();
            await Frame();
            Check(battle.TreasuryAt(cells[0]) is null && battle.TreasuryRuins.Single(t => t.Id == record.Id).IsCollected &&
                    view.TreasuryRuinModelCount == 4 && view.SceneryAtlasBuildCount == atlas,
                "collection removes the action while preserving the original retained ruin artwork");
            Check(Game.Ambience.ActiveTreasurePulses == 1, "one collection starts one bounded light pulse");
            await Capture("collected-light");
            await ToSignal(GetTree().CreateTimer(.85), SceneTreeTimer.SignalName.Timeout);
            Check(Game.Ambience.ActiveTreasurePulses == 0, "collection light fades within three quarters of a second");
            var resumed = BattleState.LoadJson(battle.SaveJson());
            Check(resumed.TreasuryRuins.Single(t => t.Id == record.Id).IsCollected && resumed.Treasuries.Count == 3,
                "save reload preserves the emptied ruin without restoring loot");
            Game.LoadScenario(resumed);
            Game.Refresh();
            await Frame();
            Check(Game.Ambience.ActiveTreasurePulses == 0, "loading an already collected ruin does not replay its reward light");
            resumed.SetGodEye(false);
            Game.Refresh();
            await Frame();
            Check(Game.Ambience.TreasureGlowVertexCount == 0,
                "hidden and collected ruins have no ready light that can reveal their locations");
            GD.Print($"PASS: {_checks} v020.3 seeded cosmetic rivers, summit variance, four passive/sunken motifs, persistent loot art and retained light/wave meshes.");
            GetTree().Quit();
        }
        catch (Exception error) { GD.PushError(error.ToString()); GetTree().Quit(1); }
    }
}
