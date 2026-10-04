using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Units;

namespace DevAncientNaval.Core.Battle;
/// <summary>A coastal settlement occupies one land tile and has its own shipyard.</summary>
public sealed class Village
{
    public string Name { get; internal set; } = "";
    public int Id { get; }
    public GridPosition Position { get; }
    public Side? Owner { get; internal set; }
    public int Level { get; internal set; } = 1;
    public double MaxHealth => Level * 5;
    public double Health { get; internal set; } = 5;
    public bool HasPort { get; internal set; }
    public bool IsFortified { get; internal set; }
    public int TurnsOwned { get; internal set; }
    public bool HasProduced { get; internal set; }
    public bool HasRepaired { get; internal set; }
    public bool HasAttacked { get; internal set; }
    public int VisualRange => IsFortified ? 5 : 3;
    public int IncomePerTurn => Owner is null || Health <= 0 ? 0 : Level;

    internal Village(int id, GridPosition position)
    {
        Id = id;
        Position = position;
    }
}
