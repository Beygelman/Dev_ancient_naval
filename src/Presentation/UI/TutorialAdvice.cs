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
    private static readonly string[] Topics = { "resources", "kolonel", "trade", "repair", "radar" };
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
    internal void Observe(BattleState battle, bool allowed)
    {
        if (!ReferenceEquals(_battle, battle)) return;
        if (!UiHints.Enabled || !allowed)
        {
            _hud.SetAllowed(false);
            return;
        }
        if (_resourceWanted) Enqueue("resources");
        if (battle.Rules.DoubleSalvo && battle.OwnShips(Side.Player).Any(s => s.Definition.Class == ShipClass.Kolonel)) Enqueue("kolonel");
        var towns = battle.Villages.Where(v => v.Owner == Side.Player && v.HasPort && v.Health > 0).ToArray();
        if (towns.Length >= 2)
        {
            var berths = towns.Select(battle.PortBerth).ToHashSet();
            if (battle.TradeRoutes(Side.Player).Routes.Any(route => route.Count > 1
                    && berths.Contains(route[0]) && berths.Contains(route[^1]))) Enqueue("trade");
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
        _hud.Present(topic, text.Title, text.Body, $"res://assets/ui/tutorial-{topic}-0206.png");
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
    private static (string Title, string Body) Content(string topic, BattleState battle) => topic switch
    {
        "resources" => ("Fish and the floating city",
            $"Collect a glowing fish site with your Mothership or Support Brig for {battle.CollectionCost(Side.Player)} Thors. Each site gives one flagship resource. The next level needs {battle.Mothership(Side.Player)?.ResourcesRequired ?? 0} resources. Unlocks: level 2 docks, 3 Galleons, 4 Kolonels, 5 mortars. Towns use paid upgrades on their own parchment."),
        "kolonel" => ("One charge or a broadside",
            battle.Rules.EqualDoubleSalvoDamage
                ? "Select your Kolonel, then a target. Choose one cannonball or two on the target's parchment. Two charges fly together, deal twice one shot's damage and provoke only one counterattack from a surviving enemy in range."
                : "Select your Kolonel, then a target. Choose one cannonball or two on the target's parchment. Two charges fly together and provoke only one counterattack from a surviving enemy in range."),
        "trade" => ("Carry the sea lanes forward",
            battle.Rules.Ports.MaximumRouteLength > 0
                ? $"Your port towns are linked by white dashed sea lanes. Direct links reach up to {battle.Rules.Ports.MaximumRouteLength} tiles. Build forward lighthouses with a Mothership or Support Brig to extend your network and its lookout coverage."
                : "Your port towns share white dashed routes across navigable sea. Build forward lighthouses with a Mothership or Support Brig to extend your network and its lookout coverage."),
        "repair" => ("Repair before the next battle",
            $"Select a damaged ship and press R or the repair symbol to restore up to {battle.Rules.RepairAmount} HP. Repair spends all of that ship's remaining movement, attacks and construction for this turn."),
        _ => ("A contact beyond the lookout",
            "Radar marks an enemy's tile without revealing its hull, health or identity. Firing at a radar contact keeps that enemy hidden. Ordinary lookouts reveal ships directly; mountains block their sight but do not block radar.")
    };
}
