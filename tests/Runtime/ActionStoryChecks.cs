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

/// <summary>Exercises the real action button, animation gate and command facade.
/// Fixtures establish eligibility through sailing/holding and owner turns.</summary>
public partial class ActionStoryChecks : Node
{
    public Main Game { get; set; } = null!;
    private int _checks;

    private void Check(bool condition, string name)
    {
        if (!condition)
            throw new InvalidOperationException("Action story: " + name);
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
            foreach (var descendant in Nodes(child))
                yield return descendant;
    }

    private async Task Capture(string suffix)
    {
        var capture = OS.GetCmdlineUserArgs().FirstOrDefault(arg => arg.StartsWith("--capture="));
        if (capture is null || DisplayServer.GetName() == "headless")
            return;
        await Frame();
        string path = capture[10..];
        int extension = path.LastIndexOf('.');
        path = extension < 0 ? path + suffix + ".png" : path[..extension] + suffix + path[extension..];
        Check(GetViewport().GetTexture().GetImage().SavePng(path) == Error.Ok, "saved " + suffix + " screenshot");
    }

    private void CheckSideLayout(ActionPapyrus papyrus)
    {
        var heading = Nodes(Game.Hud).OfType<Label>().Single(label => label.IsVisibleInTree()
            && label.Text == Game.Hud.ShipText);
        Node? ancestor = heading.GetParent();
        while (ancestor is not null && ancestor is not PanelContainer)
            ancestor = ancestor.GetParent();
        var card = (PanelContainer)ancestor!;
        var fan = Nodes(Game.Hud).OfType<RadialPapyrus>().Single(node => node.Name == "ActionPapyrus");
        Check(fan.Position.DistanceTo(card.Position) > 100,
            "actions return to the world object rather than the ledger");
        Check(papyrus.Position.Y < fan.Position.Y,
            "the downward pictorial action hangs above the ship's upward fan");
        Check(papyrus.GetGlobalRect().Position.X >= 0
            && papyrus.GetGlobalRect().End.X <= GetViewport().GetVisibleRect().Size.X,
            "wide pictorial action stays inside the viewport");
    }

    private void Click(ActionPapyrus papyrus)
    {
        var point = papyrus.GlobalPosition + new Vector2(papyrus.Size.X / 2, 112);
        GetViewport().PushInput(new InputEventMouseButton
        {
            ButtonIndex = MouseButton.Left,
            Pressed = true,
            Position = point, GlobalPosition = point
        }, true);
        GetViewport().PushInput(new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = false, Position = point, GlobalPosition = point }, true);
    }

    private async Task CaptureTown()
    {
        var location = new GridPosition(9, 8);
        var battle = new BattleState(new GameBoard(20, 20,
                p => p == location ? TerrainType.Land : TerrainType.Water), Game.Battle.Rules,
            new[]
            {
                (Side.Player, ShipClass.Mothership, new GridPosition(2, 2)),
                (Side.Enemy, ShipClass.Mothership, new GridPosition(18, 18)),
                (Side.Player, ShipClass.Garrison, new GridPosition(8, 8))
            }, Array.Empty<GridPosition>(), villageSpots: new[] { location });
        var snapshot = battle.CaptureSnapshot();
        snapshot.Villages = snapshot.Villages.Select(town => town with { Health = 0 }).ToArray();
        battle = BattleState.LoadJson(BattleState.SerializeSnapshot(snapshot));
        Check(!battle.CanCaptureVillage(Side.Player, battle.Villages.Single().Id),
            "zero health alone does not reveal a claim action");
        Check(battle.EndTurn(Side.Player).Success && battle.EndTurn(Side.Enemy).Success,
            "the alongside crew waits a full owner turn");
        battle.SetGodEye(true);
        Game.LoadScenario(battle);
        Game.FastChecks = false;
        var town = battle.Villages.Single();
        Check(Game.SelectedShipId is null && Game.Hud.ClaimPapyrus.Visible,
            "a ready claim unfolds automatically without selecting the town");
        Game.SelectCell(location);
        await Frame();
        await ToSignal(GetTree().CreateTimer(.4), SceneTreeTimer.SignalName.Timeout);
        var papyrus = Game.Hud.ClaimPapyrus;
        Check(battle.CanCaptureVillage(Side.Player, town.Id) && papyrus.IsVisibleInTree()
            && !Game.Hud.TreasuryPapyrus.IsVisibleInTree(), "only the eligible harbor scene unfolds");
        CheckSideLayout(papyrus);
        Check(papyrus.HasArtwork,
            "harbor action has its brush illustration");
        await Capture("-claim-ready");
        var stats = battle.Statistics;
        int credits = battle.Credits(Side.Player);
        Click(papyrus);
        var order = Game.CurrentOrder;
        Check(Game.Busy && papyrus.IsConsuming && town.Owner is null
            && !battle.Find(3)!.IsExhausted, "claim click starts burning before ownership or crew actions change");
        Click(papyrus);
        Check(ReferenceEquals(order, Game.CurrentOrder), "repeat click during burning cannot enqueue another claim");
        await Game.CaptureVillage();
        await Game.EndPlayerTurn();
        await ToSignal(GetTree().CreateTimer(.25), SceneTreeTimer.SignalName.Timeout);
        Check(Game.Busy && papyrus.IsConsuming && town.Owner is null && town.Health == 0
            && battle.Credits(Side.Player) == credits && battle.Statistics == stats,
            "ownership, treasury and totals stay unchanged while the parchment burns");
        await Capture("-claim-burning");
        await order;
        await Frame();
        Check(!Game.Busy && town.Owner == Side.Player && town.Health == town.MaxHealth
            && battle.Find(3)!.IsExhausted, "the completed story commits capture and spends its waiting crew");
        Check(!papyrus.Visible && !papyrus.IsConsuming && !papyrus.IsProcessing(),
            "consumed harbor parchment stays closed and idle");
        await Game.CaptureVillage();
        Check(town.Owner == Side.Player && battle.Credits(Side.Player) == credits
            && battle.Statistics == stats, "an already owned harbor cannot commit a second capture");
        await Capture("-claim-complete");
    }

    private async Task PlunderTreasury()
    {
        var battle = new BattleState(new GameBoard(20, 20, _ => TerrainType.Water), Game.Battle.Rules,
            new[]
            {
                (Side.Player, ShipClass.Mothership, new GridPosition(2, 2)),
                (Side.Enemy, ShipClass.Mothership, new GridPosition(18, 18)),
                (Side.Player, ShipClass.Garrison, new GridPosition(6, 6))
            }, Array.Empty<GridPosition>(), villageSpots: Array.Empty<GridPosition>());
        var snapshot = battle.CaptureSnapshot();
        int id = snapshot.NextId++;
        snapshot.Treasuries = new[] { new Treasury(id, new(7, 6)) };
        snapshot.Outcomes = new[] { new SavedOutcome(id, TreasuryReward.Currency) };
        battle = BattleState.LoadJson(BattleState.SerializeSnapshot(snapshot));
        Check(battle.Move(Side.Player, 3, new(7, 6)).Success && !battle.CanLootTreasury(Side.Player, 3),
            "arrival at a treasury does not skip the crew wait");
        Check(battle.EndTurn(Side.Player).Success && battle.EndTurn(Side.Enemy).Success,
            "the plunder crew waits to the next player turn");
        battle.SetGodEye(true);
        Game.LoadScenario(battle);
        Game.FastChecks = false;
        Game.SelectCell(new(7, 6));
        await Frame();
        await ToSignal(GetTree().CreateTimer(.4), SceneTreeTimer.SignalName.Timeout);
        var papyrus = Game.Hud.TreasuryPapyrus;
        Check(papyrus.IsVisibleInTree() && battle.CanLootTreasury(Side.Player, 3)
            && !Game.Hud.ClaimPapyrus.Visible, "only the ready sunken artifact scene unfolds");
        CheckSideLayout(papyrus);
        Check(papyrus.HasArtwork,
            "treasury action has its underwater brush illustration");
        await Capture("-treasury-ready");
        var stats = battle.Statistics;
        int credits = battle.Credits(Side.Player);
        Click(papyrus);
        var order = Game.CurrentOrder;
        Check(Game.Busy && papyrus.IsConsuming && battle.Treasuries.Single().Id == id,
            "plunder starts with a consuming story while the treasure remains");
        Click(papyrus);
        Check(ReferenceEquals(order, Game.CurrentOrder), "repeat plunder click cannot enqueue another command");
        await Game.LootTreasury();
        await Game.EndPlayerTurn();
        await ToSignal(GetTree().CreateTimer(.25), SceneTreeTimer.SignalName.Timeout);
        Check(Game.Busy && papyrus.IsConsuming && battle.Treasuries.Single().Id == id
            && battle.Credits(Side.Player) == credits && battle.Statistics == stats
            && !battle.Find(3)!.IsExhausted, "artifact and reward remain untouched during the burn");
        await Capture("-treasury-burning");
        await order;
        await Frame();
        Check(!Game.Busy && battle.Treasuries.Count == 0 && battle.Find(3)!.IsExhausted
            && battle.LastTreasuryReward == TreasuryReward.Currency,
            "burn completion commits discovery and spends the plunder crew");
        Check(battle.Credits(Side.Player) == credits + battle.Rules.Treasury.CurrencyReward
            && battle.Statistics.CurrencyEarned == stats.CurrencyEarned + battle.Rules.Treasury.CurrencyReward,
            "currency and lifetime receipts are credited exactly once afterwards");
        var committed = battle.Statistics;
        await Game.LootTreasury();
        Check(battle.Statistics == committed && !papyrus.Visible && !papyrus.IsProcessing(),
            "consumed treasury has no second reward or idle animation");
        await Capture("-treasury-complete");
    }

    private void AmphoraTransitions()
    {
        var amphora = new AmphoraHealthAnimation();
        amphora.Observe(100, 100, 0);
        Check(amphora.Stage == 0 && !amphora.Active(0), "whole vessel initializes without a fictional damage effect");
        for (int stage = 1; stage <= 3; stage++)
        {
            amphora.Observe(100 - stage * 25, 100, stage);
            var hit = amphora.Motion(stage + .02f);
            Check(hit.PreviousStage == stage - 1 && hit.Stage == stage && !hit.Healing
                && hit.Shake != Vector2.Zero, "damage crosses the " + (100 - stage * 25) + "% clay threshold with a brittle impulse");
            Check(!amphora.Active(stage + .6f) && amphora.Motion(stage + .6f).Shake == Vector2.Zero,
                "damage impulse settles instead of becoming an idle tremor");
        }
        amphora.Observe(76, 100, 4);
        var healing = amphora.Motion(4.1f);
        Check(healing.PreviousStage == 3 && healing.Stage == 0 && healing.Healing
            && healing.Shake == Vector2.Zero && healing.Restore is > 0 and < 1,
            "repair reassembles fragments gently across multiple thresholds");
        Check(amphora.Motion(4.10625f).Flash > .9f && amphora.Motion(4.31875f).Flash < .01f
            && amphora.Motion(4.53125f).Flash > .7f, "healed clay shines in two distinct pulses");
        amphora.Observe(76, 100, 4.4f);
        Check(amphora.Motion(4.5f).Progress > .5f,
            "unchanged snapshots cannot restart the shared repair animation");
        Check(!amphora.Active(5) && amphora.Motion(5).Restore == 1,
            "the repaired clay animation finishes completely");
    }

    public override async void _Ready()
    {
        try
        {
            await Frame();
            await CaptureTown();
            await PlunderTreasury();
            AmphoraTransitions();
            GD.Print($"PASS: {_checks} capture/treasury story gates and clay transitions ({DisplayServer.GetName()}).");
            GetTree().Quit();
        }
        catch (Exception error)
        {
            GD.PushError(error.ToString());
            GetTree().Quit(1);
        }
    }
}
