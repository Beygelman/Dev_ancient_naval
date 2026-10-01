using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Units;
using DevAncientNaval.Core.World;
using DevAncientNaval.Core.Economy;
using DevAncientNaval.Core.Vision;

namespace DevAncientNaval.Core.Battle;
public sealed partial class BattleState
{
    public bool UsesMortar(Ship ship, GridPosition cell) => ship.Definition.Class == ShipClass.AncientGun || ship.HasMortar && !Board.InRadius(ship.Position, cell, 3);
    private bool WeaponCoversFrom(Ship ship, GridPosition origin, GridPosition cell, bool counter = false) => ship.IsArmed && (ship.Definition.Class == ShipClass.AncientGun ? !counter && Board.InRadius(origin, cell, 5) : (ship.Definition.Class != ShipClass.Togus && Board.InRadius(origin, cell, ship.Definition.AttackRange) || !counter && ship.HasMortar && !Board.InRadius(origin, cell, 3) && Board.InRadius(origin, cell, ship.MortarRange)));
    public bool WeaponCovers(Ship ship, GridPosition cell, bool counter = false) => WeaponCoversFrom(ship, ship.Position, cell, counter);
    public double Damage(Ship attacker, Ship target, bool counter = false) => attacker.IsArmed && (!target.IsAirborne || attacker.IsMothership && Board.InRadius(attacker.Position, target.Position, Rules.Balloon.AntiAirRange)) ? Ship.Whole(Math.Max(1, (UsesMortar(attacker, target.Position) && !counter ? attacker.CurrentMortarDamage : attacker.CurrentDamage) + (counter ? attacker.CounterDamageBonus : attacker.ShotDamageBonus) - target.Definition.Armor)) : 0;
    public bool CanAttack(int id, int targetId)
    {
        var ship = Find(id);
        var target = ship is null ? null : Find(targetId);
        return !IsOver && ship is not null && target is not null && (!target.IsAirborne || ship.IsMothership && Board.InRadius(ship.Position, target.Position, Rules.Balloon.AntiAirRange)) && ship.Owner == ActiveSide && ship.Owner != target.Owner && (Vision.IsVisible(ship.Owner, target.Position) || ship.HasRadar && Vision.Contacts(ship.Owner).Contains(target.Position)) && ship.AttacksRemaining > 0 && WeaponCovers(ship, target.Position);
    }

    public IReadOnlyCollection<GridPosition> AttackCells(int id)
    {
        var ship = Find(id);
        if (ship is null || !ship.IsArmed || IsOver || ship.Owner != ActiveSide || ship.AttacksRemaining == 0)
            return Array.Empty<GridPosition>();
        return Board.Tiles.Where(t => (Vision.IsVisible(ship.Owner, t.Position) || ship.HasRadar) && WeaponCovers(ship, t.Position)).Select(t => t.Position).ToArray();
    }

    public bool CanCounterattack(Ship defender, Ship attacker) => defender.Health > 0 && defender.IsArmed && (!attacker.IsAirborne || defender.IsMothership && Board.InRadius(defender.Position, attacker.Position, Rules.Balloon.AntiAirRange)) && WeaponCovers(defender, attacker.Position, true);
    public double PreviewCounterDamage(Ship attacker, Ship defender)
    {
        double remaining = Math.Max(0, defender.Health - Damage(attacker, defender));
        return remaining == 0 || !CanCounterattack(defender, attacker) ? 0 : Math.Max(1, Ship.Whole(defender.FullDamage * (0.5 + 0.5 * remaining / defender.MaxHealth)) + defender.CounterDamageBonus - attacker.Definition.Armor);
    }

    public CommandResult AttackAt(Side requester, int id, GridPosition cell)
    {
        var target = _ships.FirstOrDefault(s => s.Position == cell && CanAttack(id, s.Id));
        if (target is not null)
            return Attack(requester, id, target.Id);
        return VillageAt(cell)is { } village ? AttackVillage(requester, id, village.Id) : CommandResult.Rejected("There is no available target here.");
    }

    public IReadOnlyCollection<GridPosition> TargetCells(int id)
    {
        var ship = Find(id);
        return ship is null ? Array.Empty<GridPosition>() : _ships.Where(t => CanAttack(id, t.Id)).Select(t => t.Position).Concat(_villages.Where(v => CanAttackVillage(id, v.Id)).Select(v => v.Position)).ToArray();
    }

    public CommandResult Attack(Side requester, int id, int targetId)
    {
        var error = ValidateActor(requester, id, out var ship);
        if (error is not null)
            return CommandResult.Rejected(error);
        if (!CanAttack(id, targetId))
            return CommandResult.Rejected("An observed target must be within weapon range, with an attack remaining.");
        var target = Find(targetId)!;
        // A gunshot is not optical reconnaissance. Radar never reveals a ship's identity.
        ship!.AttacksUsed++;
        if (ship.Definition.ActionProfile == ActionProfile.Standard && ship.HasMoved)
            ship.MovementLocked = true;
        var shots = new List<CombatShot>
        {
            Fire(ship, target, false)
        };
        var splash = shots[0].IsMortar ? MortarSplash(ship, target.Position, target.Id) : Array.Empty<CombatShot>();
        var area = shots[0].IsMortar ? MortarVillageSplash(ship, target.Position) : Array.Empty<AreaHit>();
        RecordImpact("attack");
        // One reply to each incoming attack; replies never recursively trigger replies.
        if (!IsOver && CanCounterattack(target, ship))
        {
            shots.Add(Fire(target, ship, true));
            RecordImpact("counter");
        }

        UpdateVision();
        var first = shots[0];
        string message = first.TargetVisibleToPlayer ? $"{(first.AttackerVisibleToPlayer ? ship.Definition.Name : "Unknown ship")}: {first.Damage:0} damage" : "Fired at a radar contact · outcome not visible";
        if (shots.Count > 1 && shots[1].TargetVisibleToPlayer)
            message += $" · counterattack: {shots[1].Damage:0}";
        if (shots.Any(s => s.Promoted && s.AttackerVisibleToPlayer))
            message += " · VETERAN!";
        if (shots.Any(s => s.TargetSunk && s.TargetVisibleToPlayer))
            message += " · ship sunk";
        return new(true, message, CommandKind.Attack, id, targetId, shots[0].Damage, Shots: shots, Splash: splash, AreaHits: area);
    }

    private CombatShot Fire(Ship attacker, Ship target, bool counter, double? fixedDamage = null)
    {
        var from = ShipSnapshot.From(attacker);
        var to = ShipSnapshot.From(target);
        double damage = Math.Min(target.Health, fixedDamage ?? Damage(attacker, target, counter));
        target.Health = Ship.Whole(target.Health - damage);
        bool sunk = target.Health <= 0, promoted = false;
        if (sunk)
        {
            RewardPirateDefeat(attacker, target);
            RemoveDestroyedShip(target);
            if (attacker.CanEarnVeterancy && !target.IsStructure)
                attacker.Kills++;
            if (attacker.CanEarnVeterancy && !attacker.IsVeteran && attacker.Kills >= 3)
            {
                attacker.IsVeteran = true;
                attacker.Health = attacker.MaxHealth;
                promoted = true;
            }
        }

        return new(from, to, damage, counter, sunk, promoted, !counter && UsesMortar(attacker, target.Position), attacker.Owner == Side.Player || Vision.IsVisible(Side.Player, attacker.Position), target.Owner == Side.Player || Vision.IsVisible(Side.Player, target.Position));
    }
}
