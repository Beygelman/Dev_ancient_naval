using System.Text.Json;
using System.Text.Json.Serialization;
using DevAncientNaval.Core.Units;

namespace DevAncientNaval.Core.Battle;

public sealed class BattleRules
{
    public int StartingCredits { get; init; }
    public int IncomePerMothership { get; init; }
    public int RepairAmount { get; init; }
    public int FleetLimit { get; init; }
    public IReadOnlyList<ShipDefinition> Ships { get; init; } = Array.Empty<ShipDefinition>();
    public ShipDefinition Get(ShipClass shipClass) => Ships.Single(s => s.Class == shipClass);

    public static BattleRules FromJson(string json)
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        options.Converters.Add(new JsonStringEnumConverter());
        var rules = JsonSerializer.Deserialize<BattleRules>(json, options)
            ?? throw new ArgumentException("Missing battle rules.", nameof(json));
        if (rules.StartingCredits < 0 || rules.IncomePerMothership < 0 || rules.RepairAmount <= 0 || rules.FleetLimit < 4)
            throw new ArgumentException("Invalid economy rules.");
        if (rules.Ships.Count != Enum.GetValues<ShipClass>().Length ||
            rules.Ships.Select(s => s.Class).Distinct().Count() != rules.Ships.Count)
            throw new ArgumentException("Every ship class must have exactly one definition.");
        foreach (var ship in rules.Ships)
            if (!Enum.IsDefined(ship.Class) || !Enum.IsDefined(ship.ActionProfile) || string.IsNullOrWhiteSpace(ship.Name) ||
                ship.MaxHealth <= 0 || ship.Movement < 0 || (ship.Movement == 0 && ship.Class != ShipClass.FishingDock) || ship.Damage < 0 || ship.Armor < 0 || ship.AttackRange < 0 ||
                ship.CoastMovementCost < 1 || ship.NarrowMovementCost < 1 || ship.IncomePerTurn < 0 ||
                ship.VisualRange < 1 || ship.RadarRange < 0 || ship.RadarPrice < 0 || ship.CollectionRange < 0 || ship.Price < 0 ||
                (ship.Class is not (ShipClass.Mothership or ShipClass.Balloon) && ship.Price == 0) ||
                (ship.Class is ShipClass.Fishing or ShipClass.Balloon or ShipClass.FishingDock && (ship.Damage != 0 || ship.AttackRange != 0 || ship.RadarRange != 0)) ||
                (ship.Class is not (ShipClass.Fishing or ShipClass.Balloon or ShipClass.FishingDock) && (ship.Damage == 0 || ship.AttackRange == 0)))
                throw new ArgumentException($"Invalid ship definition: {ship.Name}.");
        return rules;
    }
}
