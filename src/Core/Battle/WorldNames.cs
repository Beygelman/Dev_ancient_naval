using DevAncientNaval.Core.Units;

namespace DevAncientNaval.Core.Battle;
public sealed record FactionIdentity(Side Side, string Name);
/// <summary>Cosmetic names have their own seeded stream and persist with a voyage.</summary>
public static class WorldNames
{
    public static IReadOnlyList<string> Captains { get; } = Array.AsReadOnly(new[] { "Anat", "Tarek", "Selene", "Amun", "Nefira", "Hanno", "Iset", "Baalon", "Thales", "Meryt", "Damon", "Ashara", "Khepri", "Melqart", "Zenobia", "Ariston", "Naram", "Saphira", "Tanit", "Leontes" });
    public static IReadOnlyList<string> Towns { get; } = Array.AsReadOnly(new[] { "Aster Bay", "Whitehaven", "Nacre", "Pelagos", "Saffron Quay", "Tamaris", "Cedar Reach", "Lapis Port", "Ilyra", "Golden Shoal", "Ostara", "Byblion", "Sunward", "Pearl Steps", "Myrra", "Talassa", "Amber Cove", "Nysa", "Azura", "Serene Harbor", "Oriel", "Palmwatch", "Alabaster", "Elion", "Coral Gate", "Vesper", "Ormos", "Dawnmarket", "Istria", "Cypress Quay", "Saltwind", "Aruna", "Bronzehaven", "Maris", "Lily Coast", "Temara", "Moonwater", "Sundome", "Opal Reach", "Kallista" });

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
    private void AssignWorldNames()
    {
        var captains = WorldNames.Shuffle(WorldNames.Captains, Board.Seed ^ 0x4193);
        int index = 0;
        foreach (var side in _factions.Where(side => side != Side.Player))
            _factionNames.TryAdd(side, captains[index++]);
        var towns = WorldNames.Shuffle(WorldNames.Towns, Board.Seed ^ 0x5718);
        index = 0;
        foreach (var village in _villages.OrderBy(v => v.Id))
        {
            if (string.IsNullOrWhiteSpace(village.Name))
                village.Name = towns[index % towns.Length];
            index++;
        }
    }
}
