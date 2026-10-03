using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Units;
using DevAncientNaval.Core.World;
using DevAncientNaval.Core.Economy;
using DevAncientNaval.Core.Vision;

namespace DevAncientNaval.Core.Battle;
public sealed partial class BattleState
{
<<<<<<< Updated upstream
    public bool HasAntiAir(Ship ship) => ship.IsMothership || Rules.Balloon.KolonelAntiAir && ship.Definition.Class == ShipClass.Kolonel;
    public bool AntiAirCovers(Ship ship, GridPosition origin, GridPosition target) => HasAntiAir(ship) && Board.InRadius(origin, target, Rules.Balloon.AntiAirRange) && Board.InRadius(origin, target, ship.CannonRange);
    public bool UsesMortar(Ship ship, GridPosition cell) => ship.Definition.Class == ShipClass.AncientGun || ship.HasMortar && (ship.Definition.Class == ShipClass.Togus || !Board.InRadius(ship.Position, cell, ship.CannonRange));
    private bool WeaponCoversFrom(Ship ship, GridPosition origin, GridPosition cell, bool counter = false) => ship.IsArmed && (ship.Definition.Class == ShipClass.AncientGun ? !counter && Board.InRadius(origin, cell, 5) && (!Rules.Mortar.TowerDeadZone || !Board.InRadius(origin, cell, Rules.Mortar.DeadZone)) : (ship.Definition.Class != ShipClass.Togus && Board.InRadius(origin, cell, ship.CannonRange) || !counter && ship.HasMortar && !Board.InRadius(origin, cell, Rules.Mortar.DeadZone) && Board.InRadius(origin, cell, ship.MortarRange)));
    public bool WeaponCovers(Ship ship, GridPosition cell, bool counter = false) => WeaponCoversFrom(ship, ship.Position, cell, counter);
    public double Damage(Ship attacker, Ship target, bool counter = false) => attacker.IsArmed && (!target.IsAirborne || AntiAirCovers(attacker, attacker.Position, target.Position)) ? Ship.Whole(Math.Max(1, (UsesMortar(attacker, target.Position) && !counter ? attacker.CurrentMortarDamage : attacker.CurrentDamage) + (counter ? attacker.CounterDamageBonus : attacker.ShotDamageBonus) - target.Definition.Armor)) : 0;
=======
    public bool UsesMortar(Ship ship, GridPosition cell) => ship.Definition.Class == ShipClass.AncientGun || ship.HasMortar && !Board.InRadius(ship.Position, cell, 3);
    private bool WeaponCoversFrom(Ship ship, GridPosition origin, GridPosition cell, bool counter = false) => ship.IsArmed && (ship.Definition.Class == ShipClass.AncientGun ? !counter && Board.InRadius(origin, cell, 5) : (ship.Definition.Class != ShipClass.Togus && Board.InRadius(origin, cell, ship.Definition.AttackRange) || !counter && ship.HasMortar && !Board.InRadius(origin, cell, 3) && Board.InRadius(origin, cell, ship.MortarRange)));
    public bool WeaponCovers(Ship ship, GridPosition cell, bool counter = false) => WeaponCoversFrom(ship, ship.Position, cell, counter);
    public double Damage(Ship attacker, Ship target, bool counter = false) => attacker.IsArmed && (!target.IsAirborne || attacker.IsMothership && Board.InRadius(attacker.Position, target.Position, 3)) ? Ship.Whole(Math.Max(1, (UsesMortar(attacker, target.Position) && !counter ? attacker.CurrentMortarDamage : attacker.CurrentDamage) + (counter ? attacker.CounterDamageBonus : attacker.ShotDamageBonus) - target.Definition.Armor)) : 0;
>>>>>>> Stashed changes
    public bool CanAttack(int id, int targetId)
    {
        var ship = Find(id);
        var target = ship is null ? null : Find(targetId);
<<<<<<< Updated upstream
        return !IsOver && ship is not null && target is not null && (!target.IsAirborne || AntiAirCovers(ship, ship.Position, target.Position)) && ship.Owner == ActiveSide && ship.Owner != target.Owner && (Vision.IsVisible(ship.Owner, target.Position) || Vision.IsRadarContact(ship.Owner, target.Position)) && ship.AttacksRemaining > 0 && WeaponCovers(ship, target.Position);
=======
        return !IsOver && ship is not null && target is not null && (!target.IsAirborne || ship.IsMothership && Board.InRadius(ship.Position, target.Position, 3)) && ship.Owner == ActiveSide && ship.Owner != target.Owner && (Vision.IsVisible(ship.Owner, target.Position) || ship.HasRadar && Vision.Contacts(ship.Owner).Contains(target.Position)) && ship.AttacksRemaining > 0 && WeaponCovers(ship, target.Position);
>>>>>>> Stashed changes
    }

    public IReadOnlyCollection<GridPosition> AttackCells(int id)
    {
        var ship = Find(id);
        if (ship is null || !ship.IsArmed || IsOver || ship.Owner != ActiveSide || ship.AttacksRemaining == 0)
            return Array.Empty<GridPosition>();
<<<<<<< Updated upstream
        return Board.Tiles.Where(t => (Vision.IsVisible(ship.Owner, t.Position) || Vision.IsRadarContact(ship.Owner, t.Position)) && WeaponCovers(ship, t.Position)).Select(t => t.Position).ToArray();
    }

    public bool CanCounterattack(Ship defender, Ship attacker) => defender.Health > 0 && defender.IsArmed && (!attacker.IsAirborne || AntiAirCovers(defender, defender.Position, attacker.Position)) && WeaponCovers(defender, attacker.Position, true);
=======
        return Board.Tiles.Where(t => (Vision.IsVisible(ship.Owner, t.Position) || ship.HasRadar) && WeaponCovers(ship, t.Position)).Select(t => t.Position).ToArray();
    }

    public bool CanCounterattack(Ship defender, Ship attacker) => defender.Health > 0 && defender.IsArmed && (!attacker.IsAirborne || defender.IsMothership && Board.InRadius(defender.Position, attacker.Position, 3)) && WeaponCovers(defender, attacker.Position, true);
>>>>>>> Stashed changes
    public double PreviewCounterDamage(Ship attacker, Ship defender)
    {
        double remaining = Math.Max(0, defender.Health - Damage(attacker, defender));
        return remaining == 0 || !CanCounterattack(defender, attacker) ? 0 : Math.Max(1, Ship.Whole(defender.FullDamage * (0.5 + 0.5 * remaining / defender.MaxHealth)) + defender.CounterDamageBonus - attacker.Definition.Armor);
    }

<<<<<<< Updated upstream
    public bool CanDoubleSalvo(int id, GridPosition cell) => Rules.DoubleSalvo && Find(id) is { AttacksRemaining: >= 2 } ship
        && (ship.Definition.Class == ShipClass.Kolonel || ship.IsMothership && ship.SecondAttackUpgrade)
        && !UsesMortar(ship, cell) && TargetCells(id).Contains(cell);

    public CommandResult AttackAt(Side requester, int id, GridPosition cell, bool doubleSalvo = false)
    {
        var target = _ships.FirstOrDefault(s => s.Position == cell && CanAttack(id, s.Id));
        if (target is not null)
            return Attack(requester, id, target.Id, doubleSalvo);
        return VillageAt(cell)is { } village ? AttackVillage(requester, id, village.Id, doubleSalvo) : CommandResult.Rejected("There is no available target here.");
=======
    public CommandResult AttackAt(Side requester, int id, GridPosition cell)
    {
        var target = _ships.FirstOrDefault(s => s.Position == cell && CanAttack(id, s.Id));
        if (target is not null)
            return Attack(requester, id, target.Id);
        return VillageAt(cell) is { } village ? AttackVillage(requester, id, village.Id) : CommandResult.Rejected("There is no available target here.");
>>>>>>> Stashed changes
    }

    public IReadOnlyCollection<GridPosition> TargetCells(int id)
    {
        var ship = Find(id);
        return ship is null ? Array.Empty<GridPosition>() : _ships.Where(t => CanAttack(id, t.Id)).Select(t => t.Position).Concat(_villages.Where(v => CanAttackVillage(id, v.Id)).Select(v => v.Position)).ToArray();
    }

<<<<<<< Updated upstream
    public CommandResult Attack(Side requester, int id, int targetId, bool doubleSalvo = false)
=======
    public CommandResult Attack(Side requester, int id, int targetId)
>>>>>>> Stashed changes
    {
        var error = ValidateActor(requester, id, out var ship);
        if (error is not null)
            return CommandResult.Rejected(error);
        if (!CanAttack(id, targetId))
            return CommandResult.Rejected("An observed target must be within weapon range, with an attack remaining.");
        var target = Find(targetId)!;
<<<<<<< Updated upstream
        if (doubleSalvo && !CanDoubleSalvo(id, target.Position))
            return CommandResult.Rejected("A double salvo needs two cannon shots remaining.");
        // A gunshot is not optical reconnaissance. Radar never reveals a ship's identity.
        ship!.AttacksUsed += doubleSalvo ? 2 : 1;
=======
        // A gunshot is not optical reconnaissance. Radar never reveals a ship's identity.
        ship!.AttacksUsed++;
>>>>>>> Stashed changes
        if (ship.Definition.ActionProfile == ActionProfile.Standard && ship.HasMoved)
            ship.MovementLocked = true;
        var shots = new List<CombatShot>
        {
            Fire(ship, target, false)
        };
        var splash = shots[0].IsMortar ? MortarSplash(ship, target.Position, target.Id) : Array.Empty<CombatShot>();
        var area = shots[0].IsMortar ? MortarVillageSplash(ship, target.Position) : Array.Empty<AreaHit>();
<<<<<<< Updated upstream
        RecordImpact("attack");
        if (doubleSalvo && !IsOver && target.Health > 0)
        {
            shots.Add(Fire(ship, target, false));
            RecordImpact("attack2");
        }
        // One reply to each incoming attack; replies never recursively trigger replies.
        if (!IsOver && CanCounterattack(target, ship))
        {
            shots.Add(Fire(target, ship, true));
            RecordImpact("counter");
        }

        UpdateVision();
        var first = shots[0];
        string message = first.TargetVisibleToPlayer ? $"{(first.AttackerVisibleToPlayer ? ship.Definition.Name : "Unknown ship")}: {first.Damage:0} damage" : "Fired at a radar contact · outcome not visible";
        if (doubleSalvo && first.TargetVisibleToPlayer)
            message = $"Double salvo: {shots.Where(s => !s.IsCounterattack).Sum(s => s.Damage):0} damage";
        if (shots.LastOrDefault() is { IsCounterattack: true, TargetVisibleToPlayer: true } reply)
            message += $" · −{reply.Damage:0}";
=======
        // One reply to each incoming attack; replies never recursively trigger replies.
        if (!IsOver && CanCounterattack(target, ship))
            shots.Add(Fire(target, ship, true));
        UpdateVision();
        var first = shots[0];
        string message = first.TargetVisibleToPlayer ? $"{(first.AttackerVisibleToPlayer ? ship.Definition.Name : "Unknown ship")}: {first.Damage:0} damage" : "Fired at a radar contact · outcome not visible";
        if (shots.Count > 1 && shots[1].TargetVisibleToPlayer)
            message += $" · counterattack: {shots[1].Damage:0}";
>>>>>>> Stashed changes
        if (shots.Any(s => s.Promoted && s.AttackerVisibleToPlayer))
            message += " · VETERAN!";
        if (shots.Any(s => s.TargetSunk && s.TargetVisibleToPlayer))
            message += " · ship sunk";
<<<<<<< Updated upstream
        return new(true, message, CommandKind.Attack, id, targetId, shots[0].Damage, Shots: shots, Splash: splash, AreaHits: area)
            { SalvoCharges = doubleSalvo ? 2 : 1 };
=======
        return new(true, message, CommandKind.Attack, id, targetId, shots[0].Damage, Shots: shots, Splash: splash, AreaHits: area);
>>>>>>> Stashed changes
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
<<<<<<< Updated upstream
            RecordEnemyLoss(attacker.Owner, target);
            RemoveDestroyedShip(target);
            if (attacker.CanEarnVeterancy && !target.IsStructure)
                attacker.Kills++;
            if (attacker.CanEarnVeterancy && !attacker.IsVeteran && attacker.Kills >= 3)
=======
            RemoveDestroyedShip(target);
            if (!attacker.IsMothership && !attacker.IsStructure && !target.IsStructure)
                attacker.Kills++;
            if (!attacker.IsMothership && !attacker.IsStructure && !attacker.IsVeteran && attacker.Kills >= 3)
>>>>>>> Stashed changes
            {
                attacker.IsVeteran = true;
                attacker.Health = attacker.MaxHealth;
                promoted = true;
            }
<<<<<<< Updated upstream
=======


>>>>>>> Stashed changes
        }

        return new(from, to, damage, counter, sunk, promoted, !counter && UsesMortar(attacker, target.Position), attacker.Owner == Side.Player || Vision.IsVisible(Side.Player, attacker.Position), target.Owner == Side.Player || Vision.IsVisible(Side.Player, target.Position));
    }
}
