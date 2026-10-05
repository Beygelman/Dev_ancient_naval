using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Units;
using DevAncientNaval.Core.World;
using DevAncientNaval.Presentation;
using DevAncientNaval.Presentation.UI;
using Godot;
using Side = DevAncientNaval.Core.Units.Side;

namespace DevAncientNaval.Tests.Runtime;
public partial class PortCityChecks : Node
{
    public Main Game { get; set; } = null !;

    private int _checks;
    private void Check(bool value, string message)
    {
        if (!value)
            throw new InvalidOperationException(message);
        _checks++;
    }

    private async Task Frame()
    {
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        RenderingServer.ForceDraw();
    }

    private static IEnumerable<Node> Nodes(Node node)
    {
        yield return node;
        foreach (var child in node.GetChildren())
            foreach (var item in Nodes(child))
                yield return item;
    }

    private async Task Capture(string suffix)
    {
        var arg = OS.GetCmdlineUserArgs().FirstOrDefault(a => a.StartsWith("--capture="));
        if (arg is null || DisplayServer.GetName() == "headless")
            return;
        await Frame();
        Check(GetViewport().GetTexture().GetImage().SavePng(arg[10..].Replace(".png", "-" + suffix + ".png")) == Error.Ok, "City capture " + suffix);
    }

    public override async void _Ready()
    {
        try
        {
            await Frame();
            Game.FastChecks = true;
            var spots = Enumerable.Range(0, 5).Select(i => new GridPosition(4 + i * 4, 8)).ToArray();
            var board = new GameBoard(26, 18, p => p.Y is >= 6 and <= 8 && p.X is >= 2 and <= 22 ? TerrainType.Land : TerrainType.Water, seed: 731);
            var battle = new BattleState(board, Game.Battle.Rules, new[] { (Side.Player, ShipClass.Mothership, new GridPosition(4, 12)), (Side.Enemy, ShipClass.Mothership, new GridPosition(24, 15)), (Side.Player, ShipClass.Garrison, new GridPosition(4, 9)) }, Array.Empty<GridPosition>(), villageSpots: spots);
            battle.SetCreative(true);
            battle.SetGodEye(true);
            battle.SetDifficulty(AiDifficulty.Admiral);
            var snapshot = battle.CaptureSnapshot();
            snapshot.Villages = snapshot.Villages.Select((town, i) => town with { Owner = Side.Player, Level = i + 1, Health = (i + 1) * 5, Fortified = true }).ToArray();
            battle = BattleState.LoadJson(BattleState.SerializeSnapshot(snapshot));
            foreach (var town in battle.Villages.Where(v => v.Level >= 3))
                Check(battle.BuildPort(Side.Player, town.Id).Success, "City opens its port");
            Game.LoadScenario(battle);
            Game.MapCamera.Position = Game.BoardView.Projection.GridToWorld(new(12, 8));
            Game.MapCamera.Zoom = Vector2.One * .8f;
            Game.MapCamera.ForceUpdateScroll();
            await Frame();
            await Frame();
            var labels = Nodes(Game.Fleet).OfType<Node2D>().Where(n => n.Name.ToString().StartsWith("TownInterface")).ToArray();
            Check(labels.Length == 5 && labels.All(n => n.ZIndex >= 6), "Town names and stats sit above all world objects");
            Check(battle.TradeRoutes(Side.Player).Routes.Count == 3, "Three cities connect all their ports");
            Check(Game.BoardView.TerrainTextureIdle, "God's eye terrain raster settles");
            await Capture("cities");
            Game.SelectCell(spots[3]);
            for (int i = 0; i < 25; i++)
                await Frame();
            var sectors = Nodes(Game.Hud).OfType<SectorButton>().Where(s => s.Visible && s.GetParent().Name == "ActionPapyrus").ToArray();
            Check(!sectors.Any(s => s.Name == "ActionInformation"), "Town counsel is integrated into its card");
            Check(Game.Hud.InformationText.Contains("Port:"), "City chart describes the installed port");
            Game.MapCamera.Position = Game.BoardView.Projection.GridToWorld(spots[3]);
            Game.MapCamera.Zoom = Vector2.One * 2.1f;
            Game.MapCamera.ForceUpdateScroll();
            await Capture("city-scroll");
            int draws = Game.BoardView.ObservationDrawCount;
            Game.MapCamera.Pan(new(150, 50));
            await Frame();
            Check(Game.BoardView.ObservationDrawCount == draws, "Panning reuses range/resource commands");
            var wounded = battle.CaptureSnapshot();
            wounded.Villages = wounded.Villages.Select(t => t.Id == battle.Villages[3].Id ? t with { Health = 10, Produced = false } : t).ToArray();
            battle = BattleState.LoadJson(BattleState.SerializeSnapshot(wounded));
            Game.LoadScenario(battle);
            await Frame();
            await Frame();
            Game.SelectCell(spots[3]);
            int labelsBefore = Game.BoardView.TownInterfaceDrawCount;
            await Game.RepairSelected();
            await Frame();
            await Frame();
            Check(battle.Villages[3].Health == 15 && Game.BoardView.TownInterfaceDrawCount > labelsBefore, "Active repair updates retained town health text");
            GD.Print($"PASS: {_checks} city/port/scroll checks ({DisplayServer.GetName()}).");
            GetTree().Quit();
        }
        catch (Exception error)
        {
            GD.PushError(error.ToString());
            GetTree().Quit(1);
        }
    }
}
