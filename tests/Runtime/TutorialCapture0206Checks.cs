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

/// <summary>Captures actual rendered, public fixture scenes for the tutorial photographs.</summary>
public partial class TutorialCapture0206Checks : Node
{
    public Main Game { get;
    set; } = null!;
    private int _checks;
    private static IEnumerable<Node> Nodes(Node node)
    {
        yield return node;
        foreach (Node child in node.GetChildren()) foreach (Node item in Nodes(child)) yield return item;
    }
    internal static BattleState Fixture(BattleRules rules, string topic)
    {
        bool trade = topic == "trade";
        bool Land(GridPosition p) => trade && (p.Y >= 5 && p.Y <= 9 && (p.X >= 3 && p.X <= 7 || p.X >= 11 && p.X <= 15)
            || p.Y >= 12 && p.Y <= 16 && p.X >= 7 && p.X <= 11);
        var townCells = trade ? new[] { new GridPosition(7, 7), new GridPosition(11, 7), new GridPosition(9, 12) } : Array.Empty<GridPosition>();
        var setup = new List<(Side Owner, ShipClass Class, GridPosition Position)> {
            (Side.Player, ShipClass.Mothership, trade ? new GridPosition(8, 11) : new GridPosition(7, 7)),
            (Side.Enemy, ShipClass.Mothership, new GridPosition(21, 21)) };
        if (topic == "resources") setup.Add((Side.Player, ShipClass.Fishing, new GridPosition(9, 7)));
        if (topic == "kolonel")
        {
            setup.Add((Side.Player, ShipClass.Kolonel, new GridPosition(9, 7)));
            setup.Add((Side.Enemy, ShipClass.Invader, new GridPosition(11, 7)));
        }
        if (topic == "repair") setup.Add((Side.Player, ShipClass.Garrison, new GridPosition(9, 7)));
        if (topic == "radar") setup.Add((Side.Enemy, ShipClass.Invader, new GridPosition(11, 7)));
        if (trade) setup.Add((Side.Player, ShipClass.Lighthouse, new GridPosition(9, 7)));
        var battle = new BattleState(new GameBoard(24, 24, p => Land(p) ? TerrainType.Land : TerrainType.Water, seed: 2006), rules,
            setup, topic == "resources" ? new[] { new GridPosition(6, 7), new GridPosition(7, 8) } : Array.Empty<GridPosition>(),
            villageSpots: townCells);
        var save = battle.CaptureSnapshot();
        save.Credits[0] = 80;
        if (topic == "repair") save.Ships.Single(s => s.Kind == ShipClass.Garrison).Health = 2;
        if (topic == "radar") save.Ships[0].HasRadar = true;
        for (int i = 0; i < save.Villages.Length; i++)
            save.Villages[i] = save.Villages[i] with { Owner = Side.Player, Level = 3, Health = 15, Port = true };
        battle = BattleState.LoadJson(BattleState.SerializeSnapshot(save));
        foreach (var award in battle.PendingAwards.ToArray()) battle.ClaimAward(Side.Player, award.Id);
        return battle;
    }
    private async Task Frames(int count)
    {
        for (int i = 0; i < count; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    }
    public override async void _Ready()
    {
        try
        {
            if (DisplayServer.GetName() == "headless") throw new InvalidOperationException("Tutorial images require the native renderer.");
            await Frames(5);
            UiHints.Set(false, persist: false);
            Game.FastChecks = true;
            foreach (string topic in new[] { "resources", "kolonel", "trade", "repair", "radar" })
            {
                var battle = Fixture(Game.Battle.Rules, topic);
                Game.LoadScenario(battle);
                foreach (var canvas in Nodes(Game).OfType<CanvasLayer>()) canvas.Hide();
                var focus = new GridPosition(topic == "trade" || topic == "radar" ? 9 : topic == "kolonel" ? 10 : 8, topic == "trade" ? 8 : 7);
                Game.MapCamera.Position = Game.BoardView.Projection.GridToWorld(focus);
                Game.MapCamera.Zoom = Vector2.One * (topic == "trade" ? 1.12f : topic == "radar" ? 1.05f : 1.6f);
                Game.MapCamera.ForceUpdateScroll();
                if (topic == "radar")
                {
                    Game.SelectCell(new GridPosition(7, 7));
                    Game.BoardView.Reachable = Array.Empty<GridPosition>();
                    Game.BoardView.RefreshOverlays();
                }
                await Frames(18);
                System.Threading.Tasks.Task? salvo = null;
                PresentedCommand? presentation = null;
                var originalFocus = Game.Fleet.FocusTarget;
                if (topic == "kolonel")
                {
                    Game.Fleet.FocusTarget = null;
                    var gun = battle.OwnShips(Side.Player).Single(s => s.Definition.Class == ShipClass.Kolonel);
                    var target = battle.Ships.Single(s => s.Owner == Side.Enemy && s.Definition.Class == ShipClass.Invader);
                    presentation = battle.Prepare(b => b.Attack(Side.Player, gun.Id, target.Id, doubleSalvo: true));
                    if (!presentation.Result.Success) throw new InvalidOperationException(presentation.Result.Message);
                    salvo = Game.Fleet.Animate(presentation.Result, presentation: presentation);
                    ulong started = Time.GetTicksMsec();
                    int expectedCharges = DevAncientNaval.Presentation.Map.ShipVisualProfile.For(ShipClass.Kolonel).Cannonballs * 2;
                    while (Game.Fleet.ActiveProjectileCount < expectedCharges && Time.GetTicksMsec() - started < 10_000) await Frames(1);
                    if (Game.Fleet.ActiveProjectileCount != expectedCharges || Game.Fleet.LastSalvoLaunchSpread != 0) throw new InvalidOperationException("A real double salvo must have both charges in flight.");
                    await Frames(6);
                }
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                var image = GetViewport().GetTexture().GetImage();
                var size = image.GetSize();
                var crop = image.GetRegion(new Rect2I((size.X - 512) / 2, (size.Y - 256) / 2, 512, 256));
                string path = ProjectSettings.GlobalizePath($"res://assets/ui/tutorial-{topic}-0206.png");
                if (crop.SavePng(path) != Error.Ok) throw new InvalidOperationException("Could not save tutorial capture: " + topic);
                _checks++;
                if (salvo is not null) await salvo;
                presentation?.Finish();
                Game.Fleet.FocusTarget = originalFocus;
            }
            GD.Print($"PASS: {_checks} native rendered tutorial close-up captures.");
            GetTree().Quit();
        }
        catch (Exception error)
        {
            GD.PushError(error.ToString());
            GetTree().Quit(1);
        }
    }
}
