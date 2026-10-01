using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Units;

namespace DevAncientNaval.Core.Battle;
/// <summary>Stored state excludes all derived stats, which come from the balance definitions.</summary>
public sealed class SavedShip
{
    public int Id { get; set; }
    public Side Owner { get; set; }
    public ShipClass Kind { get; set; }
    public GridPosition Position { get; set; }
    public double Health { get; set; }
    public int Kills { get; set; }
    public bool IsVeteran { get; set; }
    public bool IsAncient { get; set; }
    public int BombCooldown { get; set; }
    public bool HasRepaired { get; set; }
    public int Level { get; set; }
    public int Resources { get; set; }
    public int PendingUpgradeLevel { get; set; }
    public bool IncomeUpgrade { get; set; }
    public bool MobilityUpgrade { get; set; }
    public bool SecondAttackUpgrade { get; set; }
    public bool HasMortar { get; set; }
    public bool FortificationUpgrade { get; set; }
    public bool ShipwrightUpgrade { get; set; }
    public bool VisionUpgrade { get; set; }
    public bool RestorationUpgrade { get; set; }
    public bool FirepowerUpgrade { get; set; }
    public bool HasRadar { get; set; }
    public int MovementSpentUnits { get; set; }
    public int AttacksUsed { get; set; }
    public bool HasMoved { get; set; }
    public bool IsExhausted { get; set; }
    public bool HasProduced { get; set; }
    public bool MovementLocked { get; set; }

    internal static SavedShip From(Ship s) => new()
    {
        Id = s.Id,
        Owner = s.Owner,
        Kind = s.Definition.Class,
        Position = s.Position,
        Health = s.Health,
        Kills = s.Kills,
        IsVeteran = s.IsVeteran,
        IsAncient = s.IsAncient,
        BombCooldown = s.BombCooldown,
        HasRepaired = s.HasRepaired,
        Level = s.Level,
        Resources = s.Resources,
        PendingUpgradeLevel = s.PendingUpgradeLevel,
        IncomeUpgrade = s.IncomeUpgrade,
        MobilityUpgrade = s.MobilityUpgrade,
        SecondAttackUpgrade = s.SecondAttackUpgrade,
        HasMortar = s.HasMortar,
        FortificationUpgrade = s.FortificationUpgrade,
        ShipwrightUpgrade = s.ShipwrightUpgrade,
        VisionUpgrade = s.VisionUpgrade,
        RestorationUpgrade = s.RestorationUpgrade,
        FirepowerUpgrade = s.FirepowerUpgrade,
        HasRadar = s.HasRadar,
        MovementSpentUnits = s.MovementSpentUnits,
        AttacksUsed = s.AttacksUsed,
        HasMoved = s.HasMoved,
        IsExhausted = s.IsExhausted,
        HasProduced = s.HasProduced,
        MovementLocked = s.MovementLocked,
    };
    internal Ship Restore(BattleRules rules)
    {
        var ship = new Ship(Id, Owner, rules.Get(Kind), Position);
        Apply(ship);
        return ship;
    }

    internal void Apply(Ship ship)
    {
        ship.Position = Position;
        ship.Health = Health;
        ship.Kills = Kills;
        ship.IsVeteran = IsVeteran;
        ship.IsAncient = IsAncient;
        ship.BombCooldown = BombCooldown;
        ship.HasRepaired = HasRepaired;
        ship.Level = Level;
        ship.Resources = Resources;
        ship.PendingUpgradeLevel = PendingUpgradeLevel;
        ship.IncomeUpgrade = IncomeUpgrade;
        ship.MobilityUpgrade = MobilityUpgrade;
        ship.SecondAttackUpgrade = SecondAttackUpgrade;
        ship.HasMortar = HasMortar;
        ship.FortificationUpgrade = FortificationUpgrade;
        ship.ShipwrightUpgrade = ShipwrightUpgrade;
        ship.VisionUpgrade = VisionUpgrade;
        ship.RestorationUpgrade = RestorationUpgrade;
        ship.FirepowerUpgrade = FirepowerUpgrade;
        ship.HasRadar = HasRadar;
        ship.MovementSpentUnits = MovementSpentUnits;
        ship.AttacksUsed = AttacksUsed;
        ship.HasMoved = HasMoved;
        ship.IsExhausted = IsExhausted;
        ship.HasProduced = HasProduced;
        ship.MovementLocked = MovementLocked;
    }
}
