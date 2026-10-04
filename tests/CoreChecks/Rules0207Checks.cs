using System.Text.Json.Nodes;
using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Units;
using DevAncientNaval.Core.World;

internal static class Rules0207Checks
{
    internal static int Run(BattleRules rules)
    {
        int checks = 0;
        void Check(bool condition, string message)
        {
            if (!condition) throw new Exception("v020.7 rules: " + message);
            checks++;
        }
        BattleState Restore(BattleSave saved) => BattleState.LoadJson(BattleState.SerializeSnapshot(saved));
        BattleState Fixture() => new(new GameBoard(18, 18,
            p => p == new GridPosition(8, 8) ? TerrainType.Land : TerrainType.Water), rules,
            new[] { (Side.Player, ShipClass.Mothership, new GridPosition(2, 2)),
                (Side.Enemy, ShipClass.Mothership, new GridPosition(15, 15)),
                (Side.Player, ShipClass.Garrison, new GridPosition(3, 3)),
                (Side.Player, ShipClass.Fishing, new GridPosition(5, 5)) },
            Array.Empty<GridPosition>(), villageSpots: new[] { new GridPosition(8, 8) });
        BattleSave Rich(BattleState battle, int level = 5)
        {
            var saved = battle.CaptureSnapshot();
            saved.Credits[(int)Side.Player] = 200;
            saved.Ships.Single(s => s.Owner == Side.Player && s.Kind == ShipClass.Mothership).Level = level;
            return saved;
        }

        Check(rules.WeightedFleetCapacity && rules.DiagonalVillageBerths
            && rules.ScuttleRefundFraction == .4 && rules.Balloon.CrashDamage == 2,
            "new voyages enable weighted hulls, diagonal shores, refunds and crash damage");
        var fleet = Fixture();
        var expectedCosts = new Dictionary<ShipClass, int>
        {
            [ShipClass.Mothership] = 0, [ShipClass.Garrison] = 1, [ShipClass.Fishing] = 1,
            [ShipClass.Invader] = 2, [ShipClass.Kolonel] = 4, [ShipClass.Togus] = 3,
            [ShipClass.Balloon] = 0, [ShipClass.AncientGun] = 0,
            [ShipClass.CannonTower] = 0, [ShipClass.Lighthouse] = 0, [ShipClass.FishingDock] = 0
        };
        foreach (var (kind, cost) in expectedCosts)
            Check(fleet.FleetSlotCost(kind) == cost, kind + " has the required weighted capacity");
        Check(fleet.FleetUsed(Side.Player) == 2 && fleet.FleetCapacity(Side.Player) == 4,
            "flagship grants capacity without occupying it");
        var capSave = Rich(fleet);
        capSave.Ships = capSave.Ships.Concat(new[]
        {
            new SavedShip { Id = 100, Owner = Side.Player, Kind = ShipClass.Kolonel,
                Position = new(9, 3), Health = 15, Level = 1 },
            new SavedShip { Id = 101, Owner = Side.Player, Kind = ShipClass.Kolonel,
                Position = new(10, 3), Health = 15, Level = 1 }
        }).ToArray();
        capSave.NextId = 102;
        var cap = Restore(capSave);
        Check(cap.FleetUsed(Side.Player) == 10 && cap.FleetCapacity(Side.Player) == 12,
            "fleet usage sums hull sizes rather than ship count");
        Check(!cap.CanFitFleet(Side.Player, ShipClass.Kolonel)
            && !cap.CanFitFleet(Side.Player, ShipClass.Togus)
            && cap.CanFitFleet(Side.Player, ShipClass.Invader), "two free slots accept only hulls fitting both slots");
        Check(cap.BuildBlockReason(Side.Player, 1, ShipClass.Kolonel) is not null
            && cap.BuildBlockReason(Side.Player, 1, ShipClass.Invader) is null,
            "production admission uses the complete hull cost");
        Check(cap.Build(Side.Player, 1, ShipClass.Invader, new(2, 3)).Success
            && cap.FleetUsed(Side.Player) == 12, "legal construction fills its exact remaining capacity");
        Check(cap.CanFitFleet(Side.Player, ShipClass.CannonTower), "structures remain legal at a full fleet");
        Check(Restore(cap.CaptureSnapshot()).FleetUsed(Side.Player) == 12,
            "weighted usage survives an exact saved fleet");

        var coastSave = Rich(Fixture());
        coastSave.Villages[0] = coastSave.Villages[0] with { Owner = Side.Player, Level = 3, Health = 15 };
        var coast = Restore(coastSave);
        int townId = coast.Villages[0].Id;
        Check(coast.VillageSpawnCells(townId).Contains(new(9, 9))
            && coast.VillageSpawnCells(townId).Count == 8, "shipyard includes all four diagonal shore cells");
        Check(coast.BuildFromVillage(Side.Player, townId, ShipClass.Garrison, new(9, 9)).Success,
            "diagonal shipyard command accepts the shown berth");
        Check(coast.At(new(9, 9)) is { Owner: Side.Player }, "diagonal construction uses the selected tile exactly");
        coastSave.Ships = coastSave.Ships.Append(new SavedShip
        {
            Id = 100, Owner = Side.Player, Kind = ShipClass.Lighthouse,
            Position = new(11, 11), Health = 10, Level = 1
        }).ToArray();
        coastSave.NextId = 101;
        coast = Restore(coastSave);
        string beforePreview = coast.SaveJson();
        Check(coast.PreviewPortBerth(townId) == new GridPosition(9, 9),
            "new port favors its shortest navigable approach to a friendly lighthouse");
        Check(coast.SaveJson() == beforePreview, "shoreline preview never mutates the save or random sequence");
        Check(coast.BuildPort(Side.Player, townId).Success && coast.Villages[0].PortCell == new GridPosition(9, 9),
            "port construction persists the diagonal berth");
        Check(!coast.TradeRoutes(Side.Player).IsEmpty, "a diagonal port joins normal legal navigable lanes");
        var portCopy = Restore(coast.CaptureSnapshot());
        Check(portCopy.PortBerth(portCopy.Villages[0]) == new GridPosition(9, 9),
            "Continue restores the exact built harbor location");
        var changed = coast.CaptureSnapshot();
        changed.Ships = changed.Ships.Append(new SavedShip
        {
            Id = 101, Owner = Side.Player, Kind = ShipClass.Lighthouse,
            Position = new(6, 6), Health = 10, Level = 1
        }).ToArray();
        changed.NextId = 102;
        portCopy = Restore(changed);
        Check(portCopy.PortBerth(portCopy.Villages[0]) == new GridPosition(9, 9),
            "later network nodes cannot move an existing port");

        var refund = Restore(Rich(Fixture()));
        Check(refund.ScuttleRefund(Side.Player, 3) == 0, "free initial escorts cannot be farmed for currency");
        Check(refund.Build(Side.Player, 1, ShipClass.Invader, new(2, 3)).Success, "refund fixture buys a Galleon");
        var purchased = refund.At(new(2, 3))!;
        Check(purchased.ConstructionPrice == 7 && refund.ScuttleRefund(Side.Player, purchased.Id) == 3,
            "forty percent of seven rounds to three, using the purchase receipt");
        int bank = refund.Credits(Side.Player);
        var refundOrder = refund.Scuttle(Side.Player, purchased.Id);
        Check(refundOrder.Success && refundOrder.Amount == 3 && refund.Credits(Side.Player) == bank + 3,
            "dismantling pays the displayed refund once");
        Check(!refund.Scuttle(Side.Player, purchased.Id).Success && refund.Credits(Side.Player) == bank + 3,
            "a repeated demolition cannot pay twice");
        Check(!refund.Scuttle(Side.Player, 1).Success, "the flagship cannot be dismantled");
        var discounted = Rich(Fixture());
        discounted.Villages[0] = discounted.Villages[0] with
            { Owner = Side.Player, Level = 3, Health = 15, Port = true, PortCell = new(9, 9) };
        refund = Restore(discounted);
        Check(refund.BuildFromVillage(Side.Player, townId, ShipClass.Invader, new(8, 9)).Success,
            "discounted harbor can buy a Galleon");
        purchased = refund.At(new(8, 9))!;
        Check(purchased.ConstructionPrice == 5 && refund.ScuttleRefund(Side.Player, purchased.Id) == 2,
            "refund uses discounted paid currency rather than the full catalog price");
        refund = Restore(refund.CaptureSnapshot());
        Check(refund.ScuttleRefund(Side.Player, purchased.Id) == 2, "purchase receipt survives Continue");
        var receiptCopy = refund.CaptureSnapshot();
        receiptCopy.Ships.Single(s => s.Id == purchased.Id).ConstructionPrice = null;
        Check(Restore(receiptCopy).ScuttleRefund(Side.Player, purchased.Id) == 3,
            "absent historical receipt has the documented active-price fallback");
        var creative = Restore(Rich(Fixture()));
        creative.SetCreative(true);
        Check(creative.Build(Side.Player, 1, ShipClass.Invader, new(2, 3)).Success
            && creative.ScuttleRefund(Side.Player, creative.At(new(2, 3))!.Id) == 0,
            "creative hulls retain their actual zero price");

        BattleState CrashFixture() => new(new GameBoard(18, 18,
            p => p == new GridPosition(5, 5) ? TerrainType.Land : TerrainType.Water), rules,
            new[] { (Side.Player, ShipClass.Mothership, new GridPosition(2, 2)),
                (Side.Enemy, ShipClass.Mothership, new GridPosition(15, 15)),
                (Side.Player, ShipClass.Kolonel, new GridPosition(4, 4)),
                (Side.Enemy, ShipClass.Balloon, new GridPosition(5, 4)),
                (Side.Player, ShipClass.Fishing, new GridPosition(5, 3)),
                (Side.Enemy, ShipClass.Garrison, new GridPosition(6, 4)),
                (Side.Player, ShipClass.Garrison, new GridPosition(4, 5)),
                (Side.Enemy, ShipClass.CannonTower, new GridPosition(6, 5)),
                (Side.Enemy, ShipClass.Garrison, new GridPosition(7, 4)) },
            Array.Empty<GridPosition>(), villageSpots: new[] { new GridPosition(5, 5) });
        var crashSave = CrashFixture().CaptureSnapshot();
        crashSave.Ships.Single(s => s.Id == 6).Health = 1;
        crashSave.Ships.Single(s => s.Id == 7).Health = 1;
        var crash = Restore(crashSave);
        Check(crash.CanAttack(3, 4), "anti-air shot is legal before the crash");
        var result = crash.Attack(Side.Player, 3, 4);
        Check(result.Success && result.BalloonCrashes.Count == 1 && result.Shots!.Count == 1,
            "one shot generates one crash and no recursive replies");
        Check(crash.Find(3)!.Health == 13 && crash.Find(5)!.Health == 3
            && crash.Find(8)!.Health == 8, "crash damages shooter, friendly builder and opposing tower equally");
        Check(crash.Find(6) is null && crash.Find(7) is null && crash.Find(9)!.Health == 5,
            "all adjacent low-health hulls sink while outside the nine-cell footprint is unchanged");
        Check(crash.Villages[0].Health == 3 && result.BalloonCrashes[0].Villages.Single().Damage == 2,
            "underlying land settlement takes the same fixed two damage");
        Check(crash.Statistics.EnemyShipsDestroyed == 2 && crash.DirectShipKills(Side.Player) == 2,
            "crash enemy hull kills are attributed once; friendly damage is not a kill reward");
        Check(Restore(crash.CaptureSnapshot()).DirectShipKills(Side.Player) == 2,
            "per-captain combat totals survive Continue");
        var staged = Restore(crashSave);
        var order = staged.Prepare(b => b.Attack(Side.Player, 3, 4));
        Check(staged.Find(4) is not null && staged.Find(3)!.Health == 15,
            "preparing the shot cannot apply crash damage early");
        order.Impact("attack");
        Check(staged.Find(4) is null && staged.Find(3)!.Health == 15,
            "gun impact removes the balloon before its fall hurts the ground");
        order.Impact(order.Result.BalloonCrashes.Single().ImpactKey);
        Check(staged.Find(3)!.Health == 13 && staged.Find(6) is null,
            "crash boundary applies the simultaneous area exactly once");
        order.Impact(order.Result.BalloonCrashes.Single().ImpactKey);
        order.Finish();
        order.Finish();
        Check(staged.Find(3)!.Health == 13 && staged.DirectShipKills(Side.Player) == 2,
            "duplicate native impact/final completion never repeats crash damage or statistics");
        var scuttleSave = CrashFixture().CaptureSnapshot();
        scuttleSave.Ships.Single(s => s.Id == 4).Owner = Side.Player;
        var scuttled = Restore(scuttleSave);
        var falling = scuttled.Scuttle(Side.Player, 4);
        Check(falling.Success && falling.BalloonCrashes.Count == 1 && scuttled.Find(3)!.Health == 13,
            "voluntary balloon descent has the same physical area effect");
        Check(!scuttled.Scuttle(Side.Player, 4).Success && scuttled.Find(3)!.Health == 13,
            "repeated balloon dismantling cannot repeat its blast");

        var strongholds = Restore(Rich(Fixture()));
        Check(strongholds.Build(Side.Player, 1, ShipClass.CannonTower, new(2, 3)).Success
            && strongholds.Build(Side.Player, 4, ShipClass.Lighthouse, new(5, 6)).Success,
            "stronghold evidence counts paid towers and beacons from distinct builders");
        Check(strongholds.Statistics.StructuresBuilt == 2 && strongholds.Statistics.SeaStrongholdsBuilt == 2
            && Restore(strongholds.CaptureSnapshot()).Statistics.SeaStrongholdsBuilt == 2,
            "stronghold evidence survives staged copies and Continue");
        var dockSave = Rich(Fixture());
        dockSave.Shoals = new[] { new GridPosition(2, 3) };
        var docks = Restore(dockSave);
        Check(docks.BuildDock(Side.Player, 1, new(2, 3)).Success
            && docks.Statistics.StructuresBuilt == 1 && docks.Statistics.SeaStrongholdsBuilt == 0,
            "fishing docks grow economic construction totals without qualifying as military strongholds");

        var legacyNode = JsonNode.Parse(Fixture().SaveJson())!.AsObject();
        var legacyRules = legacyNode["Rules"]!.AsObject();
        legacyRules.Remove("WeightedFleetCapacity");
        legacyRules.Remove("DiagonalVillageBerths");
        legacyRules.Remove("ScuttleRefundFraction");
        legacyRules["Balloon"]!.AsObject().Remove("CrashDamage");
        legacyNode.Remove("DirectShipKills");
        legacyNode["Statistics"]!.AsObject().Remove("StructuresBuilt");
        legacyNode["Statistics"]!.AsObject().Remove("SeaStrongholdsBuilt");
        foreach (var ship in legacyNode["Ships"]!.AsArray()) ship!.AsObject().Remove("ConstructionPrice");
        foreach (var town in legacyNode["Villages"]!.AsArray()) town!.AsObject().Remove("PortCell");
        var legacy = BattleState.LoadJson(legacyNode.ToJsonString());
        Check(!legacy.Rules.WeightedFleetCapacity && !legacy.Rules.DiagonalVillageBerths
            && legacy.Rules.ScuttleRefundFraction == 0 && legacy.Rules.Balloon.CrashDamage == 0,
            "absent optional rules preserve released voyages");
        Check(legacy.FleetUsed(Side.Player) == 3 && !legacy.VillageSpawnCells(legacy.Villages[0].Id).Contains(new(9, 9)),
            "legacy fleets retain one slot per hull including their flagship and orthogonal shores");
        Check(legacy.ScuttleRefund(Side.Player, 3) == 0 && legacy.DirectShipKills(Side.Enemy) == 0,
            "old saves start new counters at zero without inventing currency");
        var legacyCrashRules = JsonNode.Parse(System.Text.Json.JsonSerializer.Serialize(rules))!.AsObject();
        legacyCrashRules["Balloon"]!.AsObject().Remove("CrashDamage");
        var noCrash = CrashFixture().CaptureSnapshot();
        noCrash.Rules = BattleRules.FromJson(legacyCrashRules.ToJsonString());
        var oldCombat = Restore(noCrash);
        Check(oldCombat.Attack(Side.Player, 3, 4).BalloonCrashes.Count == 0 && oldCombat.Find(3)!.Health == 15,
            "older balloon destruction retains its no-area-damage combat contract");
        var invalid = Fixture().CaptureSnapshot();
        invalid.Ships[0].ConstructionPrice = -1;
        bool rejected = false;
        try { Restore(invalid); } catch (ArgumentException) { rejected = true; }
        Check(rejected, "negative construction receipts cannot enter a save");
        invalid = Fixture().CaptureSnapshot();
        invalid.DirectShipKills = new long[] { -1, 0, 0, 0, 0, 0 };
        rejected = false;
        try { Restore(invalid); } catch (ArgumentException) { rejected = true; }
        Check(rejected, "invalid rival kill counters cannot enter a save");
        return checks;
    }
}
