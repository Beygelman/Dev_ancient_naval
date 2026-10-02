using DevAncientNaval.Core.Units;

namespace DevAncientNaval.Core.Battle;
public sealed record FactionIdentity(Side Side, string Name);
/// <summary>Cosmetic names have their own seeded stream and persist with a voyage.</summary>
public static class WorldNames
{
    internal static IReadOnlyList<string> LegacyCaptains { get; } = Array.AsReadOnly(new[] { "Anat", "Tarek", "Selene", "Amun", "Nefira", "Hanno", "Iset", "Baalon", "Thales", "Meryt", "Damon", "Ashara", "Khepri", "Melqart", "Zenobia", "Ariston", "Naram", "Saphira", "Tanit", "Leontes" });
    internal static IReadOnlyList<string> LegacyTowns { get; } = Array.AsReadOnly(new[] { "Aster Bay", "Whitehaven", "Nacre", "Pelagos", "Saffron Quay", "Tamaris", "Cedar Reach", "Lapis Port", "Ilyra", "Golden Shoal", "Ostara", "Byblion", "Sunward", "Pearl Steps", "Myrra", "Talassa", "Amber Cove", "Nysa", "Azura", "Serene Harbor", "Oriel", "Palmwatch", "Alabaster", "Elion", "Coral Gate", "Vesper", "Ormos", "Dawnmarket", "Istria", "Cypress Quay", "Saltwind", "Aruna", "Bronzehaven", "Maris", "Lily Coast", "Temara", "Moonwater", "Sundome", "Opal Reach", "Kallista" });

    public static IReadOnlyList<string> Captains { get; } = Array.AsReadOnly(LegacyCaptains.Concat(CultureNamePools.AllCaptains).Distinct(StringComparer.Ordinal).ToArray());
    public static IReadOnlyList<string> Towns { get; } = Array.AsReadOnly(LegacyTowns.Concat(CultureNamePools.AllTowns).Distinct(StringComparer.Ordinal).ToArray());
    public static IReadOnlyList<string> CaptainsFor(FleetColor color) => CultureNamePools.For(color).Captains;
    public static IReadOnlyList<string> TownsFor(FleetColor color) => CultureNamePools.For(color).Towns;
    public static IReadOnlyList<string> PirateTowns => CultureNamePools.PirateTowns;

    internal static string[] Shuffle(IReadOnlyList<string> names, int seed)
    {
        var result = names.ToArray();
        var random = new Random(seed);
        for (int i = result.Length - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            (result[i], result[j]) = (result[j], result[i]);
        }

        return result;
    }
}

public sealed partial class BattleState
{
    private readonly Dictionary<Side, string> _factionNames = new();
    public string FactionName(Side side) => side == Side.Player ? "Your fleet" : side == Side.Pirates ? "Pirates" : "Captain " + _factionNames.GetValueOrDefault(side, "Anat");
    private bool _worldNamesRestored;
    private void RethemeUnplayedWorld()
    {
        if (!Rules.ThemedWorldNames || _worldNamesRestored || Round != 1 || TurnSerial != 0) return;
        _factionNames.Clear();
        foreach (var town in _villages) town.Name = "";
        AssignWorldNames();
    }
    private void AssignWorldNames()
    {
        var usedCaptains = _factionNames.Values.ToHashSet(StringComparer.Ordinal);
        var legacyCaptains = WorldNames.Shuffle(WorldNames.LegacyCaptains, Board.Seed ^ 0x4193);
        int captainOrdinal = 0;
        foreach (var side in _factions.Where(side => side != Side.Player))
        {
            int legacyIndex = captainOrdinal++;
            if (_factionNames.ContainsKey(side)) continue;
            var candidates = Rules.ThemedWorldNames
                ? WorldNames.Shuffle(WorldNames.CaptainsFor(ColorFor(side)), Board.Seed ^ 0x4193 ^ (int)side * 997)
                : legacyCaptains.Skip(legacyIndex).Concat(legacyCaptains.Take(legacyIndex)).ToArray();
            var name = candidates.First(n => !usedCaptains.Contains(n));
            _factionNames.Add(side, name);
            usedCaptains.Add(name);
        }
        var usedTowns = _villages.Where(v => !string.IsNullOrWhiteSpace(v.Name)).Select(v => v.Name).ToHashSet(StringComparer.Ordinal);
        var legacyTowns = WorldNames.Shuffle(WorldNames.LegacyTowns, Board.Seed ^ 0x5718);
        int ordinal = 0;
        foreach (var village in _villages.OrderBy(v => v.Id))
        {
            if (string.IsNullOrWhiteSpace(village.Name))
            {
                if (!Rules.ThemedWorldNames) village.Name = legacyTowns[ordinal % legacyTowns.Length];
                else
                {
                    var nearest = _ships.Where(s => s.IsMothership)
                        .OrderBy(s => Board.Distance(s.Position, village.Position)).ThenBy(s => s.Id).FirstOrDefault();
                    var pool = village.Owner == Side.Pirates ? WorldNames.PirateTowns : WorldNames.TownsFor(nearest is null ? PlayerColor : ColorFor(nearest.Owner));
                    var candidates = WorldNames.Shuffle(pool, Board.Seed ^ 0x5718 ^ village.Id * 313);
                    var name = candidates.FirstOrDefault(n => !usedTowns.Contains(n));
                    village.Name = name ?? candidates[0] + " " + village.Id;
                }
                usedTowns.Add(village.Name);
            }
            ordinal++;
        }
    }
}
