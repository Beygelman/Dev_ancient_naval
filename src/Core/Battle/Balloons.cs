using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Units;

namespace DevAncientNaval.Core.Battle;
public sealed partial class BattleState
{
    public bool CanDropBomb(int id) => !IsOver && Find(id)is { IsAirborne: true, BombCooldown: 0, HasMoved: true } ship && ship.Owner == ActiveSide && !ship.IsExhausted && PendingUpgrade(ship.Owner)is null;
    public CommandResult DropBomb(Side requester, int id)
    {
        var error = ValidateActor(requester, id, out var balloon);
        if (error is not null)
            return CommandResult.Rejected(error);
        if (!CanDropBomb(id))
            return CommandResult.Rejected($"Move the Balloon first. Its bomb recharges every {Rules.Balloon.CooldownTurns} owner turns.");
        balloon!.BombCooldown = Rules.Balloon.CooldownTurns;
        balloon.AttacksUsed++;
        var cells = Board.GetSurrounding(balloon.Position).Append(balloon.Position).ToHashSet();
        var shots = _ships.Where(s => !s.IsAirborne && cells.Contains(s.Position)).ToArray().Select(target => BombHit(balloon, target, target.Position == balloon.Position ? Rules.Balloon.BombDamage : Rules.Balloon.SplashDamage)).ToArray();
        foreach (var village in _villages.Where(v => cells.Contains(v.Position) && v.Health > 0))
        {
            double amount = (village.Position == balloon.Position ? Rules.Balloon.BombDamage : Rules.Balloon.SplashDamage) * (village.IsFortified ? .75 : 1);
            village.Health = Math.Max(0, village.Health - amount);
            RegisterVillageIncome(village);
        }

        RecordImpact("bomb");
        RecomputeVictory();
        UpdateVision();
        return new(true, $"Bomb: {Rules.Balloon.BombDamage} direct damage, {Rules.Balloon.SplashDamage} splash. Ready again in {Rules.Balloon.CooldownTurns} turns.", CommandKind.Bomb, id, Amount: shots.Sum(s => s.Damage), Path: new[] { balloon.Position }, Shots: shots);
    }

    private CombatShot BombHit(Ship balloon, Ship target, int amount)
    {
        var from = ShipSnapshot.From(balloon);
        var to = ShipSnapshot.From(target);
        double damage = Math.Min(target.Health, amount);
        target.Health = Math.Max(0, target.Health - damage);
        bool sunk = target.Health <= 0;
        if (sunk)
        {
            RewardPirateDefeat(balloon, target);
            RecordEnemyLoss(balloon.Owner, target);
            RemoveDestroyedShip(target);
        }

        return new(from, to, damage, false, sunk, false, false, balloon.Owner == Side.Player || Vision.IsVisible(Side.Player, balloon.Position), target.Owner == Side.Player || Vision.IsVisible(Side.Player, target.Position));
    }
}
