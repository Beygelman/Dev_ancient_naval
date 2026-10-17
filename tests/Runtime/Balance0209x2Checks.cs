using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Economy;
using DevAncientNaval.Core.Units;
using DevAncientNaval.Core.World;
using DevAncientNaval.Presentation;
using DevAncientNaval.Presentation.UI;
using Godot;
using Side = DevAncientNaval.Core.Units.Side;

namespace DevAncientNaval.Tests.Runtime;

/// <summary>Actual native city/role cards and orders. Captures are rendered by Godot, never drawn test mockups.</summary>
public partial class Balance0209x2Checks : Node
{
    public Main Game { get; set; } = null!;
    private int _checks;
    private void Check(bool good, string text)
    {
        if (!good) throw new InvalidOperationException("v020.9x2 native UI: " + text);
        _checks++;
    }
    private static IEnumerable<Node> Nodes(Node root)
    {
        yield return root;
        foreach (Node child in root.GetChildren()) foreach (Node node in Nodes(child)) yield return node;
    }
    private async Task Frames(int count = 5)
    { for (int i = 0; i < count; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); }
    private async Task Capture(string suffix)
    {
        string? path = OS.GetCmdlineUserArgs().FirstOrDefault(arg => arg.StartsWith("--capture="))?[10..];
        if (path is null || DisplayServer.GetName() == "headless") return;
        await Frames();
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        Check(GetViewport().GetTexture().GetImage().SavePng(path.Replace(".png", "-" + suffix + ".png")) == Error.Ok,
            "rendered capture " + suffix);
    }
    private void Click(Control control)
    {
        Vector2 point = control.GetGlobalTransformWithCanvas() * (control is SectorButton sector ? sector.IconCenter : control.Size * .5f);
        GetViewport().PushInput(new InputEventMouseMotion { Position = point, GlobalPosition = point }, true);
        foreach (bool down in new[] { true, false })
            GetViewport().PushInput(new InputEventMouseButton { Position = point, GlobalPosition = point,
                ButtonIndex = MouseButton.Left, Pressed = down, ButtonMask = down ? MouseButtonMask.Left : 0 }, true);
    }
    private BattleState Fixture(bool raider)
    {
        // A real one-cell sea channel permits an unambiguous blockade; the southern sea hosts the combat roster.
        var board = new GameBoard(24, 18, cell => cell.Y == 6 || cell.Y >= 9 ? TerrainType.Water : TerrainType.Land, seed: 20922);
        var ships = new List<(Side, ShipClass, GridPosition)>
        {
            (Side.Player, ShipClass.Mothership, new(3, 13)),
            (Side.Enemy, ShipClass.Mothership, new(21, 16)),
            (Side.Player, ShipClass.Garrison, new(7, 11)),
            (Side.Player, ShipClass.Invader, new(9, 12)),
            (Side.Player, ShipClass.Kolonel, new(11, 13)),
            (Side.Player, ShipClass.Togus, new(13, 14)),
            (Side.Player, ShipClass.Fishing, new(5, 12)),
            (Side.Enemy, ShipClass.Garrison, raider ? new(8, 6) : new(20, 12))
        };
        var battle = new BattleState(board, Game.Battle.Rules, ships, Array.Empty<GridPosition>(),
            villageSpots: new[] { new GridPosition(5, 5), new(11, 5) });
        var snapshot = battle.CaptureSnapshot();
        snapshot.Credits[0] = 80;
        snapshot.PiratesEnabled = false;
        snapshot.Ships.Single(ship => ship.Owner == Side.Player && ship.Kind == ShipClass.Mothership).Level = 5;
        snapshot.Villages = snapshot.Villages.Select((town, index) => town with
        {
            Owner = Side.Player, Level = index == 0 ? 3 : 1, Health = index == 0 ? 11 : 15,
            Port = index == 0, PortCell = index == 0 ? new GridPosition(5, 6) : null,
            Name = index == 0 ? "Aster Harbour" : "Pelagos Market"
        }).ToArray();
        // Snapshot ownership changes must include the same registered sources a real capture creates.
        // Core derives the live port contribution from each city source rather than its stored amount.
        snapshot.Income = snapshot.Income.Where(source => !source.Id.StartsWith("village:", StringComparison.Ordinal))
            .Concat(snapshot.Villages.Where(town => town.Owner is not null && town.Health > 0)
                .Select(town => new IncomeSource($"village:{town.Id}", town.Owner!.Value, 2))).ToArray();
        battle = BattleState.LoadJson(BattleState.SerializeSnapshot(snapshot));
        foreach (var award in battle.PendingAwards.ToArray()) battle.ClaimAward(award.Owner, award.Id);
        return battle;
    }
    private async Task Load(BattleState battle, GridPosition centre)
    {
        Game.FastChecks = true;
        Game.Home.Hide();
        Game.Tutorial.Clear();
        Game.LoadScenario(battle);
        Game.MapCamera.Position = Game.BoardView.Projection.GridToWorld(centre);
        Game.MapCamera.Zoom = Vector2.One * 1.25f;
        Game.MapCamera.ForceUpdateScroll();
        Game.Refresh();
        await Frames();
    }
    private async Task ScrollTo(string value)
    {
        var label = Nodes(Game.Hud).OfType<Label>().FirstOrDefault(node => node.Name == "InformationFieldValue"
            && node.Text.Contains(value, StringComparison.Ordinal));
        Check(label is not null, "native card contains " + value);
        Nodes(Game.Hud).OfType<ScrollContainer>().Single(node => node.Name == "InformationContentScroll")
            .EnsureControlVisible(label!);
        await Frames();
    }
    private async Task ConnectedAndDevelopment()
    {
        var battle = Fixture(false);
        await Load(battle, new(7, 6));
        var town = battle.Villages.Single(city => city.HasPort);
        Check(battle.TradeStatusFor(Side.Player, town.Id) == PortTradeStatus.Connected
            && battle.PortIncome(town) == 2 && battle.VillageIncome(town) == 2,
            "level-3 port trades with a level-1 market without a second port");
        Check(battle.GrossIncome(Side.Player) == 8 && battle.Income(Side.Player) == 8,
            "treasury income includes the flagship, both local city sources and connected trade exactly once");
        foreach (string locale in new[] { "en", "uk", "nl" })
        {
            Language.Set(locale, persist: false);
            Game.SelectCell(town.Position);
            Game.Refresh();
            await Frames();
            Check(Game.Hud.InformationText.Contains("Income: +4 Thors per turn"), "city card uses actual total income");
            Check(Nodes(Game.Hud).OfType<Label>().Any(label => label.Name == "InformationSectionHeading"
                && label.Text == Language.Translate("City decisions").ToUpperInvariant()),
                "rendered uppercase city heading is localized in " + locale);
            var port = Nodes(Game.Hud).OfType<SectorButton>().Single(button => button.Name == "ActionPort");
            Check(port.TooltipText.Contains("no shipyard discount") && !port.TooltipText.Contains("Trade cap"),
                "port explanation uses the new rule rather than the historical cap/discount");
            foreach (string source in new[] { EconomyUiFacts.TradeStatus(battle, town),
                "City decisions", "Pay once for two idle city works. Other orders pause development; completed work is kept. Level increases do not heal or increase fleet capacity." })
                Check(locale == "en" || Language.Translate(source) != source, "new city explanations localized in " + locale);
            Check(!Nodes(Game.Hud).OfType<SectorButton>().Any(button => button.Name == "ActionInfrastructure" && button.Visible),
                "new cities expose no infrastructure-slot command");
            await Capture("city-connected-" + locale);
        }
        Language.Set("en", persist: false);
        Game.SelectCell(town.Position); Game.Refresh(); await Frames();
        var upgrade = Nodes(Game.Hud).OfType<SectorButton>().Single(button => button.Name == "ActionVillageUpgrade");
        Check(upgrade.Visible && !upgrade.Disabled, "development is a live native action");
        var camera = Game.MapCamera.Position;
        Click(upgrade);
        await Frames();
        await Game.CurrentOrder;
        Check(town.UpgradeWorkRemaining == 2 && town.Level == 3, "native click funds a project without instantly upgrading");
        Check(Game.MapCamera.Position == camera, "own development does not move the camera");
        Game.SelectCell(town.Position); Game.Refresh(); await Frames();
        Check(Game.Hud.InformationText.Contains("2/2 works remaining"), "native city card shows funded project progress");
        await ScrollTo("2/2 works remaining");
        await Capture("city-development");
        Click(upgrade); await Frames(); await Game.CurrentOrder;
        Check(town.DevelopmentPaused && town.UpgradeWorkRemaining == 2 && !town.HasProduced,
            "native development command pauses without spending work or funded progress");
        Game.SelectCell(town.Position); Game.Refresh(); await Frames();
        await ScrollTo("Development paused");
        await Capture("city-development-paused");
        Click(upgrade); await Frames(); await Game.CurrentOrder;
        Check(!town.DevelopmentPaused && town.UpgradeWorkRemaining == 2,
            "native development command resumes the same funded project");
    }
    private async Task InterruptedTrade()
    {
        var battle = Fixture(true);
        for (int i = 0; i < 4; i++) Check(battle.EndTurn(battle.ActiveSide).Success, "advance real interception stance");
        var town = battle.Villages.Single(city => city.HasPort);
        Check(battle.TradeStatusFor(Side.Player, town.Id) == PortTradeStatus.Interrupted
            && battle.PortIncome(town) == 0 && battle.VillageIncome(town) == 2,
            "confirmed interception removes trade while preserving local income");
        Check(battle.GrossIncome(Side.Player) == 6 && battle.Income(Side.Player) == 6,
            "interrupted treasury income retains the flagship and both local city sources");
        await Load(battle, new(7, 6));
        Game.SelectCell(town.Position); Game.Refresh(); await Frames();
        Check(Game.Hud.InformationText.Contains("Trade interrupted") && !Game.Hud.InformationText.Contains("8,6"),
            "interruption card describes income loss without raider coordinates");
        await ScrollTo("Trade interrupted");
        await Capture("trade-interrupted");
    }
    private async Task CombatRoles()
    {
        var battle = Fixture(false);
        await Load(battle, new(10, 12));
        foreach (var kind in new[] { ShipClass.Garrison, ShipClass.Invader, ShipClass.Kolonel, ShipClass.Togus, ShipClass.Fishing })
        {
            var ship = battle.OwnShips(Side.Player).Single(item => item.Definition.Class == kind);
            Game.SelectCell(ship.Position); Game.Refresh(); await Frames();
            string role = EconomyUiFacts.ShipRole(ship);
            Check(role.Length > 0 && Game.Hud.InformationText.Contains(role), "native role card for " + kind);
            foreach (string locale in new[] { "uk", "nl" })
                Check(LocalizedMessages.Translate(role, locale) != role, "role localized in " + locale);
            await ScrollTo(role);
            await Capture("role-" + kind.ToString().ToLowerInvariant());
        }
    }
    public override async void _Ready()
    {
        try
        {
            await Frames();
            Check(Game.Battle.Rules.ExperimentalEconomy && Game.Battle.Rules.ExperimentalCombat
                && !Game.Battle.Rules.Infrastructure.Enabled, "new voyage opts into redesigned rules");
            await ConnectedAndDevelopment();
            await InterruptedTrade();
            await CombatRoles();
            GD.Print($"PASS: {_checks} v020.9x2 native economy, development, interception, role and EN/UK/NL checks.");
            GetTree().Quit();
        }
        catch (Exception error) { GD.PushError(error.ToString()); GetTree().Quit(1); }
    }
}
