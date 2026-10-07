using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Units;
using Side = DevAncientNaval.Core.Units.Side;

namespace DevAncientNaval.Presentation.UI;

/// <summary>Small optional presentation-side history, separate from rules and battle serialization.</summary>
internal sealed class TutorialAdvice
{
    private sealed record Progress(int Version, string Voyage, string[] Shown);
    // Existing IDs and the v1 sidecar remain stable on Continue. Append topics,
    // rather than using a UI redesign as a reason to repeat acknowledged advice.
    private static readonly string[] Topics = { "resources", "kolonel", "trade", "repair", "radar", "village", "veterancy", "commands" };
    private readonly TutorialHud _hud;
    private readonly string? _path;
    private readonly HashSet<string> _shown = new(StringComparer.Ordinal);
    private readonly Queue<string> _queue = new();
    private string _voyage = "";
    private BattleState? _battle;
    private bool _resourceWanted;
    internal int ShownCount => _shown.Count;
    internal IReadOnlyCollection<string> Shown => _shown;
    internal TutorialAdvice(TutorialHud hud, string? savePath)
    {
        _hud = hud;
        _path = savePath is null ? null : Path.GetFullPath(savePath) + ".tutorials.json";
    }
    internal void Begin(BattleState battle, bool newGame)
    {
        _battle = battle;
        _voyage = $"{battle.Board.Seed}:{battle.Board.Width}:{battle.Board.Height}:{battle.Board.MapSize}:{battle.PlayerColor}";
        _queue.Clear();
        _shown.Clear();
        _hud.Clear();
        _resourceWanted = newGame;
        if (!newGame) Read();
        else Write();
    }
    internal void Suspend() => _hud.SetAllowed(false);
    internal void Observe(BattleState battle, bool allowed, bool ownCommandsVisible = false)
    {
        if (!ReferenceEquals(_battle, battle)) return;
        if (!UiHints.Enabled || !allowed)
        {
            _hud.SetAllowed(false);
            return;
        }
        if (_resourceWanted) Enqueue("resources");
        if (battle.Rules.DoubleSalvo && battle.OwnShips(Side.Player).Any(s =>
            s.Definition.Class == ShipClass.Kolonel || s.IsMothership && s.SecondAttackUpgrade)) Enqueue("kolonel");
        if (battle.Villages.Any(v => v.Owner == Side.Player && v.Health > 0)) Enqueue("village");
        if (battle.OwnShips(Side.Player).Any(s => s.CanEarnVeterancy && (s.Kills > 0 || s.IsVeteran))) Enqueue("veterancy");
        if (ownCommandsVisible) Enqueue("commands");
        var towns = battle.Villages.Where(v => v.Owner == Side.Player && v.HasPort && v.Health > 0).ToArray();
        if (towns.Length >= 2)
        {
            if (towns.Any(town => battle.ConnectedPortCityCount(town) > 0)) Enqueue("trade");
        }
        if (battle.OwnShips(Side.Player).Any(s => s.CanRepair)) Enqueue("repair");
        if (battle.OwnShips(Side.Player).Any(s => s.HasRadar && s.Definition.Class is ShipClass.Mothership or ShipClass.Kolonel)) Enqueue("radar");
        if (_hud.Topic is not null)
        {
            _hud.SetAllowed(true);
            return;
        }
        if (_queue.Count == 0) return;
        string topic = _queue.Dequeue();
        _shown.Add(topic);
        Write();
        var text = Content(topic, battle);
        _hud.Present(topic, text.Title, text.Body, ScreenshotPath(topic));
    }
    private void Enqueue(string topic)
    {
        if (!_shown.Contains(topic) && !_queue.Contains(topic)) _queue.Enqueue(topic);
    }
    private void Read()
    {
        if (_path is null || !File.Exists(_path)) return;
        try
        {
            if (new FileInfo(_path).Length > 16_384) return;
            var saved = JsonSerializer.Deserialize<Progress>(File.ReadAllText(_path));
            if (saved is not { Version: 1 } || saved.Voyage != _voyage || saved.Shown is null) return;
            foreach (string topic in saved.Shown.Take(Topics.Length).Where(Topics.Contains)) _shown.Add(topic);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or JsonException)
        { Godot.GD.PushWarning("Tutorial history: " + error.Message); }
    }
    private void Write()
    {
        if (_path is null) return;
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            string temporary = _path + ".tmp";
            File.WriteAllText(temporary, JsonSerializer.Serialize(new Progress(1, _voyage, Topics.Where(_shown.Contains).ToArray())));
            File.Move(temporary, _path, overwrite: true);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        { Godot.GD.PushWarning("Tutorial history: " + error.Message); }
    }
    internal static string ScreenshotPath(string topic) => topic is "resources" or "kolonel" or "village" or "veterancy" or "commands"
        ? $"res://assets/ui/tutorial-{topic}-0207b.png" : $"res://assets/ui/tutorial-{topic}-0206.png";

    internal static (string Title, string Body) Content(string topic, BattleState battle) => topic switch
    {
        "resources" => ("Grow the floating city", ResourceAdvice(battle)),
        "kolonel" => ("One charge or a broadside",
            (battle.Rules.EqualDoubleSalvoDamage
                ? "Choose the charge\nSelect your Kolonel, then a target. The target's parchment offers one shot or a double salvo. Two charges spend both attacks, fly together and deal twice a single shot's damage.\n\nWatch the reply\nA surviving enemy in range answers only once. Choose before firing; the picture shows this choice."
                : "Choose the charge\nSelect your Kolonel, then a target. The target's parchment offers one shot or a double salvo. Two charges spend both attacks and fly together.\n\nWatch the reply\nA surviving enemy in range answers only once. Choose before firing; the picture shows this choice.")
                .Replace("Select your Kolonel", battle.Mothership(Side.Player)?.SecondAttackUpgrade == true
                    ? "Select your Mothership or Kolonel" : "Select your Kolonel")),
        "village" => ("Raise a village into a city", VillageAdvice(battle)),
        "veterancy" => ("The crew earns its silver", VeteranAdvice(battle)),
        "commands" => ("Read the numbered parchment",
            "Choose an order\nThe visible commands are numbered 1–9 from left to right. Press a number or touch its symbol. Grey commands keep their number but cannot act.\n\nOpen a new page\nOpening the shipyard assigns fresh numbers to its own commands. Right-click closes the parchment and clears selection.\n\nFind the next crew\nTouch the number beneath your nation’s relic to visit the next object with a useful action. Touch the relic itself to end your turn."),
        "trade" => ("Carry the sea lanes forward",
            (battle.Rules.Ports.MaximumRouteLength > 0
                ? $"Your port towns are linked by white dashed sea lanes. Direct links reach up to {battle.Rules.Ports.MaximumRouteLength} tiles. Build forward lighthouses with a Mothership or Support Brig to extend your network and its lookout coverage."
                : "Your port towns share white dashed routes across navigable sea. Build forward lighthouses with a Mothership or Support Brig to extend your network and its lookout coverage.")
            + (battle.Rules.Ports.ConnectedCityIncome
                ? "\n\nEach port earns 1 Thor per other connected friendly city. A lone port earns nothing; lighthouses carry trade between cities."
                : "")),
        "repair" => ("Repair before the next battle",
            $"Select a damaged ship and press R or the repair symbol to restore up to {battle.Rules.RepairAmount} HP. Repair spends all of that ship's remaining movement, attacks and construction for this turn."),
        _ => ("A contact beyond the lookout",
            "Radar marks an enemy's tile without revealing its hull, health or identity. Firing at a radar contact keeps that enemy hidden. Ordinary lookouts reveal ships directly; mountains block their sight but do not block radar.")
    };

    private static string ResourceAdvice(BattleState battle)
    {
        var mother = battle.Mothership(Side.Player);
        if (mother is null || mother.Level >= 5)
            return "The city is grown\nThe Mothership has reached level 5. Its resource row is complete; further catches are no longer needed for leveling.";
        return $"Gather for the flagship\nCollect a fish site within reach of your Mothership or Support Brig for {battle.CollectionCost(Side.Player)} Thors. Each catch adds 1 resource to the Mothership, even when the Support Brig gathers it."
            + $"\n\nComplete the row\nLevel {mother.Level}: {mother.Resources}/{mother.ResourcesRequired} resources. Fill the progress cells beneath the hull to reach the next level, then choose one blessing on its parchment."
            + $"\n\nA lasting harvest\nFrom level 2, a fishing dock costs {battle.DockPrice(Side.Player)} Thors and grants {battle.Rules.DockResourceReward} flagship resources when built.";
    }

    private static string VillageAdvice(BattleState battle)
    {
        var village = battle.Villages.Where(v => v.Owner == Side.Player && v.Health > 0).OrderBy(v => v.Id).FirstOrDefault();
        string growth = village is { Level: >= 5 }
            ? "The highest walls\nThis city is already level 5, the highest settlement level."
            : battle.Rules.PaidVillageUpgrades && village is not null
                ? $"Invest in the settlement\nSelect your village and choose its level-up symbol. Its next level costs {battle.VillageUpgradePrice(Side.Player, village.Id)} Thors. The upgrade uses this settlement's work for the turn."
                : "Let the settlement grow\nIn this saved voyage, living owned villages gain one level after every two of their own turns. No paid level-up order is needed.";
        return growth + "\n\nMore hands, stronger walls\nEach new level adds 5 maximum HP. Level 2 opens Brig construction; level 3 opens Galleons and ports. A port connects the city to your maritime network.";
    }

    private static string VeteranAdvice(BattleState battle)
    {
        var ship = battle.OwnShips(Side.Player).Where(s => s.CanEarnVeterancy && (s.Kills > 0 || s.IsVeteran))
            .OrderBy(s => s.IsVeteran).ThenBy(s => s.Id).FirstOrDefault();
        string example = ship is null ? "" : $"\n\n{ship.Name}: {ship.Definition.MaxHealth} → {Ship.Whole(ship.Definition.MaxHealth * 1.25)} maximum HP; {ship.Definition.Damage} → {Ship.Whole(ship.Definition.Damage * 1.25)} base damage."
            + (ship.Definition.VeteranRangeBonus > 0 ? $"\nIts cannon range also gains {ship.Definition.VeteranRangeBonus} tiles." : "\nIts cannon range stays unchanged.");
        return "Three victories\nAn armed ship or gun tower earns veterancy after sinking 3 enemy vessels. Destroyed structures do not count; a victorious counterattack does."
            + "\n\nA seasoned crew\nPromotion restores full health. Maximum HP and base weapon strength rise by 25%, rounded to whole numbers. A silver mark honors the veteran crew." + example;
    }
}
