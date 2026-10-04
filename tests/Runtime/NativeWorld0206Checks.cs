using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Units;
using DevAncientNaval.Core.World;
using DevAncientNaval.Presentation;
using DevAncientNaval.Presentation.Map;
using DevAncientNaval.Presentation.UI;
using Godot;
using Side = DevAncientNaval.Core.Units.Side;

namespace DevAncientNaval.Tests.Runtime;

public partial class NativeWorld0206Checks : Node
{
    public Main Game { get; set; } = null!;
    private int _checks;
    private void Check(bool good, string message)
    {
        if (!good) throw new InvalidOperationException("World v020.6: " + message);
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
    private async Task Show(BattleState battle)
    {
        battle.SetGodEye(true);
        foreach (var award in battle.PendingAwards.ToArray())
            Check(battle.ClaimAward(Side.Player,award.Id).Success,"settle fixture encounter before displaying scenery");
        Game.LoadScenario(battle);
        Game.Home.Hide();
        Game.Refresh();
        Game.MapCamera.FitBoard();
        for (int i = 0; i < 10; i++) await Frame();
    }
    private static IEnumerable<Node> Nodes(Node root)
    {
        yield return root;
        foreach(var child in root.GetChildren())foreach(var nested in Nodes(child))yield return nested;
    }
    private async Task PickTown(Village town,bool owned)
    {
        Game.MapCamera.Position=Game.BoardView.VillageWorldAnchor(town);
        Game.MapCamera.Zoom=Vector2.One*3;
        Game.MapCamera.ForceUpdateScroll();
        await Frame();
        var point=GetViewport().GetCanvasTransform()*Game.BoardView.ToGlobal(Game.BoardView.VillageWorldAnchor(town));
        string untouched=Game.Battle.SaveJson();
        GetViewport().PushInput(new InputEventMouseMotion {Position=point,GlobalPosition=point},true);
        foreach(bool pressed in new[] {true,false})
            GetViewport().PushInput(new InputEventMouseButton {Position=point,GlobalPosition=point,
                ButtonIndex=MouseButton.Left,Pressed=pressed},true);
        await Frame();
        Check(Game.SelectedVillageId==town.Id,"actual pointer picks inland artwork as its original village identity");
        Check(Game.Battle.SaveJson()==untouched,"picking shifted artwork cannot issue a simulation command");
        if(owned)
        {
            var paper=Nodes(Game.Hud).OfType<RadialPapyrus>().Single(node=>node.Name=="ActionPapyrus");
            Check(paper.IsVisibleInTree() && (paper.GetGlobalTransformWithCanvas()*SectorButton.Center).DistanceTo(point)<1,
                "owned town action parchment remains centered on its real shifted artwork");
        }
    }
    public override async void _Ready()
    {
        try
        {
            await Frame();
            Game.FastChecks = true;
            var rules = Game.Battle.Rules;
            bool matrixOnly=OS.GetCmdlineUserArgs().Contains("--town-matrix-only");
            foreach (var world in matrixOnly ? Array.Empty<WorldKind>() : new[] { WorldKind.Oceans, WorldKind.Pangaea })
            {
                var board = ArchipelagoGenerator.Create(2063, 1, world, MapSize.Sea);
                var battle = SkirmishSetup.Create(board, rules, 1);
                await Show(battle);
                string untouched = battle.SaveJson();
                foreach (var town in battle.Villages)
                {
                    var fit = Game.BoardView.VillagePlacement(town);
                    Check(MathF.Abs(fit.Scale - .64f) < .0001f,
                        $"every generated town has the same medium house scale: {world} #{town.Id} at {town.Position}, scale={fit.Scale}");
                    Check(Game.BoardView.VillageFootprintPoints(town).All(p => Game.BoardView.VillageSoilContains(town,p)),
                        "uniform town ground footprint remains strictly inland " + town.Id);
                }
                var deltas = Game.BoardView.CosmeticRiverDeltas;
                Check(deltas.All(d => d.OceanColor.IsEqualApprox(Game.BoardView.RiverOceanColor(d.OceanCell))),
                    "delta end color uses the destination ocean tint");
                Check(Game.BoardView.CosmeticRiverDiagnostics.Where(r => r.EndKind == "Lake")
                    .All(r => r.Cells.Length <= 13), "dead-end lake rivers are local, not whole-island scars");
                int builds = Game.BoardView.TownRasterBuildCount;
                int rivers = Game.BoardView.CosmeticRiverBuildCount;
                int shores = Game.BoardView.RiverShoreSplitBuildCount;
                for (int frame = 0; frame < 20; frame++)
                {
                    Game.MapCamera.Pan(new Vector2(3,2));
                    Game.BoardView.AnimateTowns();
                    await Frame();
                }
                Check(Game.BoardView.TownRasterBuildCount == builds && Game.BoardView.CosmeticRiverBuildCount == rivers &&
                    Game.BoardView.RiverShoreSplitBuildCount == shores, "church and camera animation do not rebuild retained geometry");
                Check(battle.SaveJson() == untouched,"cosmetic scenery never mutates simulation state");
                await Capture(world.ToString().ToLowerInvariant());
                foreach (var town in battle.Villages.Take(2))
                {
                    Game.MapCamera.Position = Game.BoardView.Projection.GridToWorld(town.Position) +
                        Game.BoardView.VillagePlacement(town).Offset + new Vector2(0,-15);
                    Game.MapCamera.Zoom = Vector2.One * 3.5f;
                    Game.MapCamera.ForceUpdateScroll();
                    await Capture(world.ToString().ToLowerInvariant()+"-town-"+town.Id);
                }
            }
            var flat = new GameBoard(26,20,p => p.X is >= 5 and <= 20 && p.Y is >= 5 and <= 15 ?
                TerrainType.Land : TerrainType.Water,seed:2064);
            foreach (var color in matrixOnly ? Array.Empty<FleetColor>() : Enum.GetValues<FleetColor>())
            {
                var battle = new BattleState(flat,rules,new[] { (Side.Player,ShipClass.Mothership,new GridPosition(2,2)),
                    (Side.Enemy,ShipClass.Mothership,new GridPosition(24,18)) },Array.Empty<GridPosition>(),
                    villageSpots:new[] {new GridPosition(7,5)});
                battle.SetPlayerColor(color);
                var snapshot=battle.CaptureSnapshot();
                snapshot.Villages=snapshot.Villages.Select(v => v with {Owner=Side.Player,Level=3,Fortified=true,Health=15}).ToArray();
                battle=BattleState.LoadJson(BattleState.SerializeSnapshot(snapshot));
                await Show(battle);
                var town=battle.Villages[0];
                Game.MapCamera.Position=Game.BoardView.Projection.GridToWorld(town.Position)+Game.BoardView.VillagePlacement(town).Offset+new Vector2(0,-24);
                Game.MapCamera.Zoom=Vector2.One*4;
                Game.MapCamera.ForceUpdateScroll();
                await Capture("sanctuary-"+color.ToString().ToLowerInvariant());
                await PickTown(town,true);
                int builds=Game.BoardView.TownRasterBuildCount;
                for(int i=0;i<18;i++)await Frame();
                Check(builds==Game.BoardView.TownRasterBuildCount,"faction sanctuary light never rebuilds its static raster " + color);
            }
            foreach (var size in Enum.GetValues<MapSize>())
                foreach (var world in Enum.GetValues<WorldKind>())
                {
                    var board=ArchipelagoGenerator.Create(2063,4,world,size);
                    var battle=SkirmishSetup.Create(board,rules,4);
                    await Show(battle);
                    foreach(var town in battle.Villages)
                    {
                        var fit=Game.BoardView.VillagePlacement(town);
                        if(fit.Scale==0)
                        {
                            Game.MapCamera.Position=Game.BoardView.Projection.GridToWorld(town.Position);
                            Game.MapCamera.Zoom=Vector2.One*6;
                            Game.MapCamera.ForceUpdateScroll();
                            await Capture("failed-fit-"+size+"-"+world+"-"+town.Id);
                            GD.Print("FIT noart " + string.Join(";", board.GetNeighbors(town.Position).Select(p=>p+":"+board.GetTile(p).Terrain)));
                        }
                        Check(MathF.Abs(fit.Scale-.64f)<.0001f,
                            $"size/world town matrix: {size}/{world} town {town.Id} at {town.Position}, scale={fit.Scale}");
                        var transform=fit.Transform(Vector2.Zero);
                        Check(MathF.Abs(transform.X.X-.64f)<.0001f && transform.Y.X==0 &&
                            MathF.Abs(transform.Y.Y-.64f)<.0001f && transform.Determinant()>0,
                            "compact floor alignment preserves medium house width, upright walls and positive winding");
                        var bad=Game.BoardView.VillageFootprintPoints(town).Where(p=>!Game.BoardView.VillageSoilContains(town,p)).ToArray();
                        Check(bad.Length==0,
                            $"size/world footprint matrix: {size}/{world} town {town.Id}: " +
                            string.Join(";",bad.Take(2).Select(p=>Game.BoardView.VillageSoilDiagnostic(town,p))));
                    }
                    Check(Game.BoardView.TownRasterBounded && Game.BoardView.TownRasterIdle,
                        "inland relocation and shear keep every town raster bounded and idle");
                    var moved=battle.Villages.FirstOrDefault(t=>Game.BoardView.VillagePlacement(t).Offset.Length()>2);
                    if(moved is not null)await PickTown(moved,false);
                }
            GD.Print($"PASS: {_checks} v020.6 uniform coastal town scale, local rivers, matching delta tint and retained faction sanctuary animation.");
            GetTree().Quit();
        }
        catch(Exception error){GD.PushError(error.ToString());GetTree().Quit(1);}
    }
}
