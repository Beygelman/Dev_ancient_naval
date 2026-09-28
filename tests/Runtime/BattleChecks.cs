using System;
using System.Linq;
using System.Threading.Tasks;
using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Units;
using DevAncientNaval.Presentation;
using Godot;
using Side = DevAncientNaval.Core.Units.Side;

namespace DevAncientNaval.Tests.Runtime;

public partial class BattleChecks : Node
{
    public Main Game { get; set; } = null!;
    private int _checks;
    private void Check(bool ok, string label) { if (!ok) throw new Exception(label); _checks++; }
    public override async void _Ready()
    {
        try
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            await Run();
            var capture = OS.GetCmdlineUserArgs().FirstOrDefault(a => a.StartsWith("--capture="));
            if (capture is not null && DisplayServer.GetName() != "headless")
            {
                Game.Restart();
                Game.SelectCell(new(4, 9));
                Game.MapCamera.ZoomAt(new Vector2(640, 310), 1.45f);
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                Check(GetViewport().GetTexture().GetImage().SavePng(capture[10..]) == Error.Ok, "Screenshot");
            }
            GD.Print($"PASS: {_checks} battle runtime checks.");
            GetTree().Quit();
        }
        catch (Exception e) { GD.PushError(e.ToString()); GetTree().Quit(1); }
    }

    private async Task Run()
    {
        Check(Game.Battle.Ships.Count == 8, "Both fleets deploy");
        var original = Game.Battle.Find(2)!;
        Game.SelectCell(original.Position);
        Game.SelectCell(new(5, 9));
        Check(original.Position == new GridPosition(4, 9), "Preview does not move");
        Check(Game.BoardView.PreviewPath.Count == 2, "Path shown");
        var command = Game.ConfirmOrder();
        Check(Game.Busy, "Animation locks commands");
        await Game.ConfirmOrder();
        await command;
        Check(original.Position == new GridPosition(5, 9) && original.MovementRemaining == 4, "One movement only");
        Check(!Game.Busy, "Animation releases lock");
        Game.SelectCell(new(5, 10)); Game.CancelOrder();
        await Game.ConfirmOrder();
        Check(original.Position == new GridPosition(5, 9), "Cancel preserves movement");
        Game.FastChecks = true;
        Game.SelectCell(new(2, 9));
        int money = Game.Battle.Credits(Side.Player);
        Game.BeginBuild(ShipClass.Garrison);
        var spawn = Game.Battle.SpawnCells(1).First();
        Game.SelectCell(spawn);
        Check(Game.Battle.Credits(Side.Player) == money, "Build preview no charge");
        await Game.ConfirmOrder();
        Check(Game.Battle.Ships.Count == 9 && Game.Battle.Credits(Side.Player) == money - 40, "Build confirmed");
        Check(Game.Battle.At(spawn)!.CanMove, "First produced ship ready");
        await Game.EndPlayerTurn();
        Check(Game.Battle.ActiveSide == Side.Player && Game.Battle.Round == 2, "AI returns control");
        Check(Game.Battle.Ships.Any(s => s.Owner == Side.Enemy && s.Position.X < 15), "AI advances");
        Check(!Game.Busy && Game.Hud.StatusText.Contains("Ваш ход"), "HUD returns control");

        // Real viewport tap -> preview -> GUI confirmation -> domain command.
        Game.Restart();
        void Tap(Vector2 position)
        {
            GetViewport().PushInput(new InputEventScreenTouch { Index = 0, Position = position, Pressed = true }, true);
            GetViewport().PushInput(new InputEventScreenTouch { Index = 0, Position = position, Pressed = false }, true);
        }
        Vector2 Screen(GridPosition cell) => GetViewport().GetCanvasTransform() * Game.BoardView.Projection.GridToWorld(cell);
        Tap(Screen(new(4, 9)));
        Check(Game.SelectedShipId == 2, "Touch selects ship");
        Tap(Screen(new(5, 9)));
        Check(Game.BoardView.PreviewPath.Count == 2, "Touch previews path");
        var confirm = FindButton(Game.Hud, "Переместить");
        Check(confirm is not null && !confirm.Disabled, "Move button enabled");
        var at = confirm!.GetGlobalRect().GetCenter();
        GetViewport().PushInput(new InputEventMouseButton { Position = at, GlobalPosition = at, ButtonIndex = MouseButton.Left, Pressed = true }, true);
        GetViewport().PushInput(new InputEventMouseButton { Position = at, GlobalPosition = at, ButtonIndex = MouseButton.Left, Pressed = false }, true);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        Check(Game.Battle.Find(2)!.Position == new GridPosition(5, 9), "GUI button executes order");
        // Feed touch through Input, where Godot synthesizes GUI mouse events.
        // Input.ParseInputEvent needs a window dispatch function; headless has none.
        if (DisplayServer.GetName() != "headless")
        {
            Game.SelectCell(new(6, 9));
            at = FindButton(Game.Hud, "Переместить")!.GetGlobalRect().GetCenter();
            Godot.Input.ParseInputEvent(new InputEventScreenTouch { Index = 0, Position = at, Pressed = true });
            Godot.Input.ParseInputEvent(new InputEventScreenTouch { Index = 0, Position = at, Pressed = false });
            Godot.Input.FlushBufferedEvents();
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            Check(Game.Battle.Find(2)!.Position == new GridPosition(6, 9), "Touch activates GUI confirmation");
        }

        Game.Restart();
        bool attacked = false, repaired = false;
        for (int turn = 0; turn < 12 && !Game.Battle.IsOver; turn++)
        {
            var attacker = Game.Battle.Ships.FirstOrDefault(s => s.Owner == Side.Player && Game.Battle.Ships.Any(t => Game.Battle.CanAttack(s.Id, t.Id)));
            if (!attacked && attacker is not null)
            {
                var target = Game.Battle.Ships.First(t => Game.Battle.CanAttack(attacker.Id, t.Id));
                int previousHp = target.Health;
                Game.SelectCell(attacker.Position); Game.SelectCell(target.Position);
                Check(target.Health == previousHp, "Attack preview does not damage");
                Game.FastChecks = false;
                await Game.ConfirmOrder();
                Game.FastChecks = true;
                Check(target.Health < previousHp, "Attack command animates and damages");
                attacked = true;
            }
            var damaged = Game.Battle.Ships.FirstOrDefault(s => s.Owner == Side.Player && s.CanRepair);
            if (!repaired && damaged is not null)
            {
                int previousHp = damaged.Health;
                Game.SelectCell(damaged.Position); Game.PreviewRepair();
                Check(damaged.Health == previousHp, "Repair preview does not heal");
                Game.FastChecks = false;
                await Game.ConfirmOrder();
                Game.FastChecks = true;
                Check(damaged.Health > previousHp && damaged.IsExhausted, "Repair executes with feedback");
                repaired = true;
            }
            await Game.EndPlayerTurn();
        }
        Check(attacked && repaired, "Combat and repair covered");
        for (int turn = 0; turn < 40 && !Game.Battle.IsOver; turn++) await Game.EndPlayerTurn();
        Check(Game.Battle.Winner == Side.Enemy && Game.Hud.StatusText.Contains("ПОРАЖЕНИЕ"), "Defeat displayed");
        Check(FindButton(Game.Hud, "Закончить ход")!.Disabled, "Turn button disabled after result");
        Game.Restart();
        Check(Game.Battle.Round == 1 && Game.Battle.Ships.Count == 8 && Game.Battle.Credits(Side.Player) == 120, "Restart resets match");
    }

    private static Button? FindButton(Node root, string text)
    {
        if (root is Button button && button.Text == text) return button;
        foreach (var child in root.GetChildren())
            if (FindButton(child, text) is { } found) return found;
        return null;
    }
}
