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
public partial class Scuttle0207bChecks : Node
{
    public Main Game { get; set; } = null!;
    private int _checks;
    private void Check(bool pass, string message)
    { if (!pass) throw new InvalidOperationException("Scuttle0207b: " + message); _checks++; }
    private static IEnumerable<Node> Nodes(Node node)
    { yield return node; foreach (var child in node.GetChildren()) foreach (var nested in Nodes(child)) yield return nested; }
    private async Task Frames(int count = 5)
    { for (int i = 0; i < count; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); }
    private void Click(string name)
    {
        var button = Nodes(Game).OfType<Button>().Single(b => b.Name == name);
        var point = button.GetGlobalTransformWithCanvas() * (button is SectorButton sector ? sector.IconCenter : button.Size * .5f);
        GetViewport().PushInput(new InputEventMouseMotion { Position = point, GlobalPosition = point }, true);
        foreach (bool down in new[] { true, false })
            GetViewport().PushInput(new InputEventMouseButton { Position = point, GlobalPosition = point,
                ButtonIndex = MouseButton.Left, Pressed = down, ButtonMask = down ? MouseButtonMask.Left : 0 }, true);
    }
    private void KeyPress(Key key)
    { foreach (bool down in new[] { true, false }) GetViewport().PushInput(new InputEventKey { Keycode = key, PhysicalKeycode = key, Pressed = down }, true); }
    private void Load(bool hints)
    {
        var battle = new BattleState(new GameBoard(16, 16, _ => TerrainType.Water), Game.Battle.Rules,
            new[] { (Side.Player, ShipClass.Mothership, new GridPosition(4, 4)),
                (Side.Player, ShipClass.Garrison, new GridPosition(5, 4)),
                (Side.Enemy, ShipClass.Mothership, new GridPosition(14, 14)) }, Array.Empty<GridPosition>());
        var save = battle.CaptureSnapshot();
        save.Ships.Single(s => s.Kind == ShipClass.Garrison).ConstructionPrice = 8;
        Game.LoadScenario(BattleState.LoadJson(BattleState.SerializeSnapshot(save)));
        Game.Home.Hide(); Game.FastChecks = true;
        UiHints.Set(hints, persist: false);
        Game.MapCamera.Position = Game.BoardView.Projection.GridToWorld(new(5, 4));
        Game.MapCamera.Zoom = Vector2.One;
        Game.MapCamera.ForceUpdateScroll();
        Game.SelectCell(new(5, 4)); Game.Refresh();
    }
    public override async void _Ready()
    {
        try
        {
            await Frames();
            foreach (bool hints in new[] { false, true })
            {
                UiScale.Set(hints ? 1.25f : .8f, persist: false);
                Language.Set(hints ? "nl" : "en", persist: false);
                Load(hints); await Frames();
                string before = Game.Battle.SaveJson();
                int id = Game.SelectedShipId!.Value;
                int expectedRefund = Game.Battle.ScuttleRefund(Side.Player, id);
                Click("ActionScuttle"); await Frames();
                Check(Game.ScuttleConfirmationVisible, "confirmation opens via real scuttle sector hints=" + hints);
                Check(Game.Battle.SaveJson() == before, "opening never changes Core");
                Check(Game.MapInput.PointerEnabled?.Invoke() == false && Game.MapInput.KeyboardEnabled?.Invoke() == false,
                    "modal blocks map pointer, pan and gameplay keys");
                var camera = Game.MapCamera.Position;
                KeyPress(Key.Key1); KeyPress(Key.R);
                Game.SelectCell(new(4, 4)); await Frames();
                Check(Game.SelectedShipId == id && Game.Battle.SaveJson() == before,
                    "underlying keyboard/selection cannot act");
                Check(Game.MapCamera.Position == camera, "modal does not move camera");
                Click("CancelScuttle"); await Game.CurrentOrder; await Frames();
                Check(!Game.ScuttleConfirmationVisible && !Game.Busy && Game.Battle.SaveJson() == before, "cancel leaves identical voyage");
                Click("ActionScuttle"); await Frames(); KeyPress(Key.Escape);
                await Game.CurrentOrder; await Frames();
                Check(!Game.ScuttleConfirmationVisible && !Game.Hud.MenuVisible && Game.Battle.SaveJson() == before,
                    "Escape cancels without opening session menu");
                Click("ActionScuttle"); await Frames();
                string? capture = OS.GetCmdlineUserArgs().FirstOrDefault(a => a.StartsWith("--capture="))?[10..];
                if (capture is not null)
                {
                    await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                    Check(GetViewport().GetTexture().GetImage().SavePng(capture.Replace(".png", "-hints-" + hints + ".png")) == Error.Ok,
                        "native confirmation capture");
                }
                int money = Game.Battle.Credits(Side.Player);
                Click("ConfirmScuttle"); Click("ConfirmScuttle");
                await Game.CurrentOrder; await Frames();
                Check(Game.Battle.Find(id) is null, "accepted ship is removed");
                Check(Game.Battle.Credits(Side.Player) == money + expectedRefund, "actual-price refund committed exactly once");
                Check(Game.Battle.PendingPresentation is null && !Game.Busy, "staged scuttle completes before commands resume");
            }
            UiScale.Set(1, persist: false); Language.Set("en", persist: false);
            Load(false); await Frames();
            var pending = Game.RequestScuttle(); await Frames();
            Game.LoadScenario(new BattleState(new GameBoard(12, 12, _ => TerrainType.Water), Game.Battle.Rules,
                new[] { (Side.Player, ShipClass.Mothership, new GridPosition(2, 2)),
                    (Side.Enemy, ShipClass.Mothership, new GridPosition(10, 10)) }, Array.Empty<GridPosition>()));
            string replacement = Game.Battle.SaveJson();
            await pending; await Frames();
            Check(!Game.ScuttleConfirmationVisible && Game.Battle.SaveJson() == replacement, "replacement cancels stale prompt safely");
            foreach (string locale in new[] { "uk", "nl" })
            {
                Check(LocalizedMessages.Translate("Scuttle this ship?", locale) != "Scuttle this ship?", locale + " prompt translated");
                Check(LocalizedMessages.Translate("Dismantling returns 3 Thors.", locale) != "Dismantling returns 3 Thors.", locale + " actual refund translated");
            }
            GD.Print($"PASS: {_checks} v020.7b native always-confirm scuttle, input isolation and exact-once refund checks.");
            GetTree().Quit();
        }
        catch (Exception error) { GD.PushError(error.ToString()); GetTree().Quit(1); }
    }
}
