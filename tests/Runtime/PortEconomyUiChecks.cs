using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
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

/// <summary>Current connected-port economics, native localized controls and enemy information boundaries.</summary>
public partial class PortEconomyUiChecks : Node
{
    public Main Game { get; set; } = null!;
    private int _checks;
    private void Check(bool good, string text)
    {
        if (!good) throw new InvalidOperationException("Port economy UI: " + text);
        _checks++;
    }
    private static IEnumerable<Node> Nodes(Node root)
    {
        yield return root;
        foreach (Node child in root.GetChildren()) foreach (Node node in Nodes(child)) yield return node;
    }
    private async Task Frames(int count = 3)
    {
        for (int frame = 0; frame < count; frame++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    }
    private async Task Capture(string suffix)
    {
        string? path = OS.GetCmdlineUserArgs().FirstOrDefault(arg => arg.StartsWith("--capture="))?[10..];
        if (path is null || DisplayServer.GetName() == "headless") return;
        await Frames();
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        Check(GetViewport().GetTexture().GetImage().SavePng(path.Replace(".png", "-" + suffix + ".png")) == Error.Ok,
            "native capture " + suffix);
    }
    private BattleState Fixture(int ports, bool beacons, Side owner = Side.Player, bool legacy = false)
    {
        var board = new GameBoard(35, 20, cell => cell.Y == 5 && cell.X is >= 3 and <= 29
            ? TerrainType.Land : TerrainType.Water, seed: 20731);
        var ships = new List<(Side, ShipClass, GridPosition)>
        {
            (Side.Player, ShipClass.Mothership, new(6, 7)),
            (Side.Enemy, ShipClass.Mothership, new(32, 18))
        };
        if (beacons)
        {
            ships.Add((owner, ShipClass.Lighthouse, new(16, 6)));
            ships.Add((owner, ShipClass.Lighthouse, new(21, 6)));
        }
        var battle = new BattleState(board, Game.Battle.Rules, ships,
            Array.Empty<GridPosition>(), villageSpots: new[] { new GridPosition(6, 5), new(11, 5), new(26, 5) });
        var snapshot = battle.CaptureSnapshot();
        snapshot.Credits[0] = 500;
        snapshot.Ships.Single(ship => ship.Owner == Side.Player && ship.Kind == ShipClass.Mothership).Level = 5;
        snapshot.Villages = snapshot.Villages.Select((town, index) => town with
        {
            Owner = owner, Level = 3, Health = 15, Port = index < ports,
            PortCell = index < ports ? new GridPosition(town.Position.X, 6) : null
        }).ToArray();
        string json = BattleState.SerializeSnapshot(snapshot);
        if (legacy)
        {
            var stored = JsonNode.Parse(json)!;
            stored["Rules"]!["Ports"]!.AsObject().Remove("ConnectedCityIncome");
            stored["Rules"]!["Ports"]!["Income"] = 1;
            json = stored.ToJsonString();
        }
        battle = BattleState.LoadJson(json);
        foreach (var award in battle.PendingAwards.ToArray()) battle.ClaimAward(award.Owner, award.Id);
        if (owner == Side.Player) battle.SetGodEye(true);
        return battle;
    }
    private async Task Load(BattleState battle)
    {
        Game.Home.Hide();
        Game.FastChecks = true;
        Game.Tutorial.Clear();
        Game.LoadScenario(battle);
        Game.MapCamera.Position = Game.BoardView.Projection.GridToWorld(new(6, 6));
        Game.MapCamera.Zoom = Vector2.One * 1.5f;
        Game.MapCamera.ForceUpdateScroll();
        Game.Refresh();
        await Frames();
    }
    private SectorButton Sector(string name) => Nodes(Game.Hud).OfType<SectorButton>().Single(button => button.Name == name);
    private void CloseInformation()
    {
        if (!Game.Hud.InformationVisible) return;
        var close = Nodes(Game.Hud).OfType<Button>().SingleOrDefault(button => button.Name == "CloseInformation");
        if (close is not null) close.EmitSignal(BaseButton.SignalName.Pressed);
        else Game.Hud.CloseMenus(); // The newer compact card has no utility close button.
    }
    private static string[] Numbers(string source) => Regex.Matches(source, @"\d+")
        .Select(match => match.Value).OrderBy(value => value).ToArray();
    private void Localized(string source, string locale, bool requireChanged = true)
    {
        string translated = Language.Translate(source);
        Check(locale == "en" || !requireChanged || translated != source, "economic text is localized in " + locale);
        Check(Numbers(source).SequenceEqual(Numbers(translated)), "localization preserves every economic number");
        Check(!translated.Contains("[[plural:") && !Regex.IsMatch(translated, @"\{\d+\}"),
            "economic text resolves all template and plural fields");
    }
    private void PortFacts(BattleState battle, Village town, int expected)
    {
        Check(battle.PortIncome(town) == expected, "Core contributes the expected connected-city income");
        var page = AncientLore.Village(battle, town);
        var rows = page.Sections.SelectMany(section => section.Rows).ToArray();
        Check(rows.Single(row => row.Label == "Port").Value == $"+{expected} Thors per turn",
            "installed port row shows its actual live contribution");
        Check(rows.Single(row => row.Label == "Income").Value == $"+{battle.VillageIncome(town) + expected} Thors per turn",
            "town total includes Core's current contribution exactly once");
        Check(rows.Any(row => row.Label == "Trade income"), "new policy describes city links separately from numeric income");
        Game.SelectCell(town.Position);
        Check(Sector("ActionPort").TooltipText.Contains("+1 income per other connected city")
            && !Sector("ActionPort").TooltipText.Contains("+0 income"),
            "port tooltip explains the connection rule instead of advertising a fixed zero bonus");
    }
    private async Task OwnPortCases()
    {
        foreach (var choice in new[] { (Ports: 1, Beacons: false, Income: 0),
                     (Ports: 2, Beacons: false, Income: 1), (Ports: 3, Beacons: true, Income: 2) })
        {
            var battle = Fixture(choice.Ports, choice.Beacons);
            await Load(battle);
            foreach (var town in battle.Villages.Where(town => town.HasPort)) PortFacts(battle, town, choice.Income);
            if (choice.Beacons)
                Check(!battle.TradeRoutes(Side.Player).Routes.Any(route => route.First() == new GridPosition(6, 6)
                    && route.Last() == new GridPosition(26, 6)),
                    "distant income is provided by a beacon chain rather than an unlimited direct link");
        }
    }
    private async Task PrivateEnemyIncome()
    {
        string? previous = null;
        foreach (int ports in new[] { 1, 3 })
        {
            var battle = Fixture(ports, true, Side.Enemy);
            var town = battle.Villages.Single(village => village.Position == new GridPosition(6, 5));
            Check(battle.Vision.IsVisible(Side.Player, town.Position)
                && !battle.Vision.IsVisible(Side.Player, new GridPosition(26, 5)),
                "the enemy frontage is observed while its other cities remain hidden");
            Check(battle.PortIncome(town) == (ports == 1 ? 0 : 2), "enemy Core income really differs between hidden networks");
            var page = AncientLore.Village(battle, town);
            var rows = page.Sections.SelectMany(section => section.Rows).ToArray();
            Check(rows.Single(row => row.Label == "Settlement income").Value == $"+{battle.VillageIncome(town)} Thors per turn"
                && rows.Single(row => row.Label == "Port").Value == "Trade income is not observed",
                "enemy counsel exposes known settlement income and conceals the network count");
            Check(previous is null || previous == page.PlainText, "changing a hidden enemy network cannot change visible counsel");
            previous = page.PlainText;
            await Load(battle);
            CloseInformation();
            Game.Hud.ShowInformation(battle, town.Position);
            Check(Game.Hud.InformationText.Contains("Trade income is not observed")
                && !Game.Hud.InformationText.Contains("Income: +5 Thors per turn"),
                "actual native information panel does not disclose hidden total income");
        }
        var legacy = Fixture(3, true, Side.Enemy, legacy: true);
        Check(!legacy.Rules.Ports.ConnectedCityIncome, "missing saved policy keeps historical fixed-income rules");
        var oldTown = legacy.Villages.Single(village => village.Position == new GridPosition(6, 5));
        Check(AncientLore.Village(legacy, oldTown).Sections.SelectMany(section => section.Rows)
            .Single(row => row.Label == "Port").Value == "+1 Thors per turn",
            "the already-public historical fixed port bonus retains its old behavior");
    }
    private async Task LanguagesAndPrices()
    {
        var battle = Fixture(3, true);
        await Load(battle);
        var mother = battle.Mothership(Side.Player)!;
        var town = battle.Villages.Single(village => village.Position == new GridPosition(6, 5));
        var prices = new[] { (ShipClass.Fishing, 5), (ShipClass.Garrison, 6), (ShipClass.Invader, 9),
            (ShipClass.Kolonel, 16), (ShipClass.Togus, 22) };
        Check(prices.All(item => battle.BuildPrice(Side.Player, item.Item1) == item.Item2),
            "current hull prices rise progressively while free ships are unchanged");
        Check(battle.Rules.Get(ShipClass.CannonTower).Price == 6 && battle.Rules.Get(ShipClass.Lighthouse).Price == 6
            && battle.Rules.Get(ShipClass.FishingDock).Price == 8 && battle.PortPrice(Side.Player) == 6,
            "stationary construction prices remain unchanged");
        Check(mother.Definition.RadarPrice == 4 && battle.MortarPrice == 14,
            "current scanner and mortar purchases use their requested prices");
        foreach (string locale in new[] { "en", "uk", "nl" })
        {
            Language.Set(locale, persist: false);
            CloseInformation();
            Game.SelectCell(mother.Position);
            Game.Refresh();
            foreach (var name in new[] { "ActionRadar", "ActionMortar" })
            {
                var button = Sector(name);
                Check(button.Cost == (name == "ActionRadar" ? 4 : 14), "equipment icon shows its actual debit");
                Localized(button.TooltipText, locale);
                Check(button.Tr(button.TooltipText).ToString() == Language.Translate(button.TooltipText),
                    "native economic tooltip uses the active catalog");
            }
            foreach (var item in prices)
            {
                var button = Sector("Build" + item.Item1);
                Check(button.Cost == item.Item2, "shipyard price is sourced from current Core rules");
                Localized(button.TooltipText, locale, requireChanged: false);
            }
            Game.SelectCell(town.Position);
            Localized(Sector("ActionPort").TooltipText, locale);
            Localized("Trade income is not observed", locale);
            Localized("1 Thor per other connected friendly city; lighthouses relay the route", locale);
            Localized("Each port earns 1 Thor per other connected friendly city. A lone port earns nothing; lighthouses carry trade between cities.", locale);
            Game.Hud.ShowInformation(battle, town.Position);
            await Frames();
            var values = Nodes(Game.Hud).OfType<Label>().Where(label => label.Name == "InformationFieldValue").ToArray();
            Check(values.Any(value => value.Text == "+2 Thors per turn"), "native panel renders actual connected income in " + locale);
            foreach (var value in values)
                Check(value.Tr(value.Text).ToString() == Language.Translate(value.Text), "native information value uses the active locale");
            var portValue = values.Single(value => value.Text == "+2 Thors per turn");
            Nodes(Game.Hud).OfType<ScrollContainer>().Single(scroll => scroll.Name == "InformationContentScroll")
                .EnsureControlVisible(portValue);
            await Capture("owned-port-" + locale);
        }
    }
    public override async void _Ready()
    {
        try
        {
            await Frames();
            Check(Game.Battle.Rules.Ports.ConnectedCityIncome && Game.Battle.Rules.Ports.Income == 0,
                "new-voyage data opts into connected income with no fixed base port bonus");
            await OwnPortCases();
            await PrivateEnemyIncome();
            await LanguagesAndPrices();
            GD.Print($"PASS: {_checks} native connected-port economics, hidden-income and EN/UK/NL price checks.");
            GetTree().Quit();
        }
        catch (Exception error) { GD.PushError(error.ToString()); GetTree().Quit(1); }
    }
}
