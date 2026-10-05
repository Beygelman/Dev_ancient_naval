using DevAncientNaval.Core.Economy;
using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Units;
using DevAncientNaval.Core.Vision;
using DevAncientNaval.Core.World;

namespace DevAncientNaval.Core.Battle;
public sealed partial class BattleState
{
    private readonly List<Village> _villages = new();
    public IReadOnlyList<Village> Villages => _villages.AsReadOnly();

    public Village? VillageAt(GridPosition cell) => _villages.FirstOrDefault(v => v.Position == cell);
    public IEnumerable<Village> ObservedVillages(Side side) => _villages.Where(v => v.Owner == side || Vision.IsVisible(side, v.Position));
    public int FortificationPrice(Side side) => Creative && side == Side.Player ? 0 : Rules.VillageFortificationPrice;
    private void InitializeVillages(IEnumerable<GridPosition>? supplied, int seed)
    {
        if (supplied is not null)
        {
            foreach (var cell in supplied.Distinct())
                AddVillage(cell);
            return;
        }

        // One settlement per island: choose a reproducible coast with an orthogonal berth.
        var remaining = Board.Tiles.Where(t => t.Terrain == TerrainType.Land).Select(t => t.Position).ToHashSet();
        var random = new Random(seed ^ 0x7181);
        while (remaining.Count > 0)
        {
            var coast = new List<GridPosition>();
            var pending = new Queue<GridPosition>();
            pending.Enqueue(remaining.OrderBy(p => p.Y).ThenBy(p => p.X).First());
            while (pending.TryDequeue(out var cell))
            {
                if (!remaining.Remove(cell))
                    continue;
                if (Board.GetNeighbors(cell).Any(p => Board.GetTile(p).Terrain != TerrainType.Land))
                    coast.Add(cell);
                foreach (var next in Board.GetSurrounding(cell))
                    if (remaining.Contains(next))
                        pending.Enqueue(next);
            }

            if (coast.Count > 0)
                AddVillage(coast[random.Next(coast.Count)]);
        }

        if (Board.Mesh is not null)
        {
            var groups = Enumerable.Range(0, _factions.Count).Select(side => _villages.Where(v => Board.StartingTerritory(v.Position, _factions.Count) == side).ToArray()).ToArray();
            int perSide = groups.Min(group => group.Length);
            var keep = groups.SelectMany(g => g.Take(perSide)).Select(v => v.Id).ToHashSet();
            _villages.RemoveAll(v => !keep.Contains(v.Id));
        }
    }

    private void AddVillage(GridPosition cell)
    {
        if (!Board.Contains(cell) || Board.GetTile(cell).Terrain != TerrainType.Land
            || !(Rules.DiagonalVillageBerths ? Board.GetSurrounding(cell) : Board.GetNeighbors(cell))
                .Any(p => Board.GetTile(p).Terrain != TerrainType.Land))
            throw new ArgumentException("A village must occupy a coastal land tile.");
        _villages.Add(new Village(_nextId++, cell));
    }

    private static bool IsCapturingShip(Ship ship) => !ship.IsAirborne && !ship.IsStructure && ship.Definition.Class != ShipClass.Fishing;
    private bool IsAdjacent(Ship ship, Village village) => Board.GetSurrounding(village.Position).Contains(ship.Position);
    public bool CanCaptureVillage(Side side, int villageId)
    {
        var village = _villages.FirstOrDefault(v => v.Id == villageId);
        return !IsOver && side == ActiveSide && side != Side.Pirates && PendingUpgrade(side)is null && village is { Health: <= 0 } && village.Owner != side && CaptureCrew(village, side)is not null;
    }

    private Ship? CaptureCrew(Village village, Side side) => _captureWaits.Where(entry => entry.Key.Village == village.Id && entry.Key.Side == side).Select(entry => entry.Value).Where(crew => ReadyCrew(crew, side)).Select(crew => Find(crew.ShipId)).FirstOrDefault(ship => ship is not null && IsAdjacent(ship, village));
    public CommandResult CaptureVillage(Side requester, int villageId)
    {
        if (!CanCaptureVillage(requester, villageId))
            return CommandResult.Rejected("Reduce the town to 0 HP and keep a combat ship alongside until its next turn.");
        var village = _villages.First(v => v.Id == villageId);
        var captor = CaptureCrew(village, requester)!;
        captor.IsExhausted = true;
        InvalidateWaiting(captor.Id);
        foreach (var key in _captureWaits.Keys.Where(k => k.Village == villageId).ToArray())
            _captureWaits.Remove(key);
        village.Owner = requester;
        village.TurnsOwned = 0;
        village.HasProduced = false;
        village.Health = village.MaxHealth;
        RegisterVillageIncome(village);
        UpdateVision();
        return new(true, "Village captured.", CommandKind.Capture, TargetId: villageId);
    }

    private void EndVillageTurn(Side side)
    {
        foreach (var village in _villages)
        {
            if (village.Health <= 0 && village.Owner != side && side != Side.Pirates)
            {
                foreach (var ship in OwnShips(side).Where(s => IsCapturingShip(s) && IsAdjacent(s, village)))
                    _captureWaits[(village.Id, side, ship.Id)] = new(ship.Id, ship.Position, TurnSerial);
            }

            if (village.Owner == side && village.Health > 0)
            {
                if (Rules.FrozenUnownedVillages && side == Side.Pirates) continue;
                village.TurnsOwned++;
                if (!Rules.PaidVillageUpgrades && village.TurnsOwned % 2 == 0 && village.Level < 5)
                {
                    village.Level++;
                    village.Health += 5;
                    RegisterVillageIncome(village);
                }

                continue;
            }
        }
    }

    private void StartVillageTurn(Side side)
    {
        foreach (var village in _villages)
        {
            if (village.Owner == side)
            {
                village.HasProduced = false;
                village.HasRepaired = false;
                village.HasAttacked = false;
            }
        }
    }

    private void RegisterVillageIncome(Village village)
    {
        RemoveIncomeSource($"village:{village.Id}");
        if (village.Owner is { } owner && village.Health > 0)
            SetIncomeSource(new IncomeSource($"village:{village.Id}", owner, VillageIncome(village) + (village.HasPort ? Rules.Ports.Income : 0)));
    }

    public IReadOnlyList<GridPosition> VillageSpawnCells(int villageId) => _villages.FirstOrDefault(v => v.Id == villageId)is { } village
        ? VillageBerths(village).Where(p => IsFreeWater(p) && (!Rules.EmptyOuterRim || !Board.IsOuterCell(p))).ToArray()
        : Array.Empty<GridPosition>();
    private string? ValidateVillage(Side side, int villageId, out Village? village)
    {
        village = _villages.FirstOrDefault(v => v.Id == villageId);
        if (PendingPresentation is not null)
            return "A projectile is still in flight.";
        if (IsOver)
            return "The battle is over.";
        if (side != ActiveSide)
            return "It is the other side's turn.";
        if (village?.Owner != side)
            return "Select one of your villages.";
        if (village.Health <= 0)
            return "This town is defeated and its shipyard is inactive.";
        if (village.HasRepaired)
            return "This town has already repaired this turn.";
        return PendingUpgrade(side)is null ? null : "Choose the Mothership upgrade first.";
    }

    public static int VillageRequiredLevel(ShipClass kind) => kind == ShipClass.Garrison ? 2 : kind == ShipClass.Lighthouse ? 3 : RequiredLevel(kind);
    public string? VillageBuildBlockReason(Side requester, int villageId, ShipClass kind)
    {
        var error = ValidateVillage(requester, villageId, out var village);
        if (error is not null)
            return error;
        if (kind is not (ShipClass.Garrison or ShipClass.Fishing or ShipClass.Invader or ShipClass.Kolonel or ShipClass.Togus or ShipClass.Lighthouse))
            return "This class cannot be built by a village.";
        if (kind == ShipClass.Lighthouse && Rules.FishingLighthouses)
            return "Villages cannot build lighthouses.";
        if (kind == ShipClass.Lighthouse && !Rules.LighthousesEnabled)
            return "Lighthouse construction is unavailable in this voyage.";
        if (village!.Level < VillageRequiredLevel(kind))
            return $"Available at village level {VillageRequiredLevel(kind)}.";
        if (village.HasProduced)
            return "This village has already built a ship this turn.";
        if (!CanFitFleet(requester, kind))
            return $"Fleet limit: {FleetCapacity(requester)}.";
        if (Credits(requester) < VillageBuildPrice(villageId, kind))
            return "Not enough Thors.";
        return VillageSpawnCells(villageId).Count == 0 ? "No adjacent water tile is free." : null;
    }

    public CommandResult BuildFromVillage(Side requester, int villageId, ShipClass kind, GridPosition spawn)
    {
        var error = VillageBuildBlockReason(requester, villageId, kind);
        if (error is not null)
            return CommandResult.Rejected(error);
        if (!VillageSpawnCells(villageId).Contains(spawn))
            return CommandResult.Rejected("Choose a free water tile beside the village.");
        var village = _villages.First(v => v.Id == villageId);
        int price = VillageBuildPrice(villageId, kind);
        var ship = new Ship(_nextId++, requester, Rules.Get(kind), spawn)
        {
            IsExhausted = true,
            ConstructionPrice = price
        };
        _ships.Add(ship);
        RecordShipConstruction(ship);
        RegisterShipIncome(ship);
        ClearRuinsForConstruction(kind, spawn);
        _credits[(int)requester] -= price;
        _everProduced[(int)requester] = true;
        village.HasProduced = true;
        UpdateVision();
        return new(true, $"{ship.Definition.Name} built. Ready next turn.", CommandKind.Build, villageId, ship.Id);
    }

    public string? FortifyBlockReason(Side requester, int villageId)
    {
        var error = ValidateVillage(requester, villageId, out var village);
        if (error is not null)
            return error;
        if (village!.IsFortified)
            return "This village is already fortified.";
        return Credits(requester) < FortificationPrice(requester) ? "Not enough Thors." : null;
    }

    public CommandResult FortifyVillage(Side requester, int villageId)
    {
        var error = FortifyBlockReason(requester, villageId);
        if (error is not null)
            return CommandResult.Rejected(error);
        _credits[(int)requester] -= FortificationPrice(requester);
        _villages.First(v => v.Id == villageId).IsFortified = true;
        UpdateVision();
        return new(true, Rules.VillageCombat.AutomaticAttack ? "Outpost raised: 25% resistance, sight +2, automatic level-scaled guns from level 2." : "Village fortified: 25% damage resistance and a 3-damage counterattack within 3 tiles.", CommandKind.Fortify, TargetId: villageId);
    }

    public bool CanAttackVillage(int shipId, int villageId)
    {
        var ship = Find(shipId);
        var village = _villages.FirstOrDefault(v => v.Id == villageId);
        return !IsOver && ship is not null && village is not null && ship.Owner == ActiveSide && village.Health > 0 && village.Owner != ship.Owner && ship.AttacksRemaining > 0 && Vision.IsVisible(ship.Owner, village.Position) && WeaponCovers(ship, village.Position);
    }

    public CommandResult AttackVillage(Side requester, int shipId, int villageId, bool doubleSalvo = false)
    {
        var error = ValidateActor(requester, shipId, out var ship);
        if (error is not null)
            return CommandResult.Rejected(error);
        if (!CanAttackVillage(shipId, villageId))
            return CommandResult.Rejected("A visible village must be within weapon range.");
        var village = _villages.First(v => v.Id == villageId);
        if (doubleSalvo && !CanDoubleSalvo(shipId, village.Position))
            return CommandResult.Rejected("A double salvo needs two cannon shots remaining.");
        var attackerBefore = ShipSnapshot.From(ship!);
        bool attackerVisible = ship!.Owner == Side.Player || Vision.IsVisible(Side.Player, ship.Position);
        bool townVisible = Vision.IsVisible(Side.Player, village.Position);
        bool mortar = UsesMortar(ship, village.Position);
        double counterDamage = 0;
        double raw = (mortar ? ship.CurrentMortarDamage + Rules.Mortar.VillageDamageBonus : ship.CurrentDamage) + ship.ShotDamageBonus;
        double oneShot = raw * (village.IsFortified ? .75 : 1);
        if (Rules.EqualDoubleSalvoDamage) oneShot = Ship.Whole(oneShot);
        double damage = Math.Min(village.Health, oneShot * (doubleSalvo ? 2 : 1));
        village.Health = Math.Max(0, village.Health - damage);
        ship.AttacksUsed += doubleSalvo ? 2 : 1;
        var splash = mortar ? MortarSplash(ship, village.Position) : Array.Empty<CombatShot>();
        var area = mortar ? MortarVillageSplash(ship, village.Position, village.Id) : Array.Empty<AreaHit>();
        if (ship.Definition.ActionProfile == ActionProfile.Standard && ship.HasMoved)
            ship.MovementLocked = true;
        if (village.Health <= 0)
        {
            RegisterVillageIncome(village);
        }

        RecordImpact("village");
        if (village.Health > 0 && village.IsFortified && (!Rules.VillageCombat.AutomaticAttack || village.Level >= 2) && Board.InRadius(village.Position, ship.Position, Rules.VillageCombat.CounterRange))
        {
            counterDamage = Math.Min(ship.Health, Math.Max(1, VillageCounterDamage(village) - ship.Definition.Armor));
            ship.Health -= counterDamage;
            if (ship.Health <= 0)
            {
                if (village.Owner is { } owner)
                {
                    RewardPirateDefeat(owner, ship);
                    RecordEnemyLoss(owner, ship);
                }
                RemoveDestroyedShip(ship);
            }
        }

        RecordImpact("village-counter");
        UpdateVision();
        return new(true, $"Town hit for {damage:0.##} damage." + (village.Health <= 0 ? " Defenses defeated: hold alongside until next turn to capture." : ""), CommandKind.Attack, shipId, villageId, damage, Path: new[] { ship.Position, village.Position }, StructureHit: new(attackerBefore, village.Position, counterDamage, mortar, attackerVisible, townVisible, doubleSalvo ? 2 : 1), Splash: splash, AreaHits: area);
    }
}
