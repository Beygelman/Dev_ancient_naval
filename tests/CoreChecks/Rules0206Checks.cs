using System.Text.Json.Nodes;
using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Units;
using DevAncientNaval.Core.World;

internal static class Rules0206Checks
{
    internal static int Run(BattleRules rules)
    {
        int checks = 0;
        void Check(bool condition, string message)
        {
            if (!condition) throw new Exception("v020.6 rules: " + message);
            checks++;
        }
        BattleState Fixture() => new(new GameBoard(24, 24,
            p => p == new GridPosition(12, 12) ? TerrainType.Land : TerrainType.Water), rules,
            new[] { (Side.Player, ShipClass.Mothership, new GridPosition(2, 2)),
                (Side.Enemy, ShipClass.Mothership, new GridPosition(21, 21)),
                (Side.Player, ShipClass.Fishing, new GridPosition(5, 5)),
                (Side.Player, ShipClass.Kolonel, new GridPosition(8, 8)),
                (Side.Enemy, ShipClass.Kolonel, new GridPosition(9, 8)) },
            new[] { new GridPosition(2, 3) }, villageSpots: new[] { new GridPosition(12, 12) });
        BattleState Restore(BattleSave saved) => BattleState.LoadJson(BattleState.SerializeSnapshot(saved));
        BattleSave Rich(BattleState battle, int level = 2)
        {
            var saved = battle.CaptureSnapshot();
            saved.Credits[(int)Side.Player] = 200;
            saved.Ships.Single(s => s.Kind == ShipClass.Mothership && s.Owner == Side.Player).Level = level;
            return saved;
        }
        Check(rules.Get(ShipClass.Togus).Movement == 3 && rules.Get(ShipClass.FishingDock).VisualRange == 2,
            "Granado moves farther and fishing docks see one tile farther");
        var mortar = Restore(Rich(Fixture(), 5));
        var mortarSave = mortar.CaptureSnapshot();
        mortarSave.Ships.Single(s => s.Kind == ShipClass.Kolonel && s.Owner == Side.Player).Kind = ShipClass.Togus;
        mortarSave.Ships.Single(s => s.Kind == ShipClass.Togus).Health = 5;
        mortarSave.Ships.Single(s => s.Kind == ShipClass.Togus).HasMortar = true;
        mortarSave.Ships.Single(s => s.Kind == ShipClass.Togus).HasRadar = true;
        mortar = Restore(mortarSave);
        var granado = mortar.OwnShips(Side.Player).Single(s => s.Definition.Class == ShipClass.Togus);
        Check(mortar.MortarDeadZone(granado) == 1 && !mortar.WeaponCovers(granado, new(9, 8))
            && mortar.WeaponCovers(granado, new(10, 8)) && mortar.WeaponCovers(granado, new(13, 8))
            && !mortar.WeaponCovers(granado, new(14, 8)), "Granado fires from distance two through five only");
        Check(mortar.MortarDeadZone(mortar.Mothership(Side.Player)!) == 2,
            "the Granado-specific minimum does not alter flagship or tower mortar safety");
        var economy = Restore(Rich(Fixture()));
        int income = economy.Income(Side.Player);
        Check(economy.Upkeep(Side.Player) == 0 && income == economy.GrossIncome(Side.Player), "no hull upkeep in new voyages");
        Check(economy.Build(Side.Player, economy.Mothership(Side.Player)!.Id, ShipClass.Garrison, new(3, 2)).Success
            && economy.Income(Side.Player) == income, "building the third combat hull no longer reduces turn income");
        Check(economy.VillageIncome(economy.Villages.Single()) == 2, "a level-one town gains the extra income");
        var townSave = Rich(Fixture());
        townSave.Villages[0] = townSave.Villages[0] with { Owner = Side.Player, Level = 2, Health = 10 };
        var townBattle = Restore(townSave);
        Check(townBattle.UpgradeVillage(Side.Player, townBattle.Villages.Single().Id).Success
            && townBattle.VillageIncome(townBattle.Villages.Single()) == 3,
            "paid progression registers the larger city income");
        var salvo = Restore(Rich(Fixture()));
        var result = salvo.Attack(Side.Player, 4, 5, true);
        Check(result.Success && result.Shots!.Where(s => !s.IsCounterattack).Select(s => s.Damage).SequenceEqual(new[] { 4d, 4d })
            && result.Shots!.Count(s => s.IsCounterattack) == 1, "salvo carries exactly two equal one-shot charges and one reply");
        var fortified = Rich(Fixture());
        fortified.Ships.Single(s => s.Id == 4).Position = new(11, 12);
        fortified.Ships.Single(s => s.Id == 5).Position = new(18, 18);
        fortified.Villages[0] = fortified.Villages[0] with { Level = 3, Health = 15, Fortified = true };
        var townSalvo = Restore(fortified);
        Check(townSalvo.AttackVillage(Side.Player, 4, townSalvo.Villages.Single().Id, true).Amount == 6,
            "fortification rounds one-shot damage before multiplying the salvo");
        var treasurySave = Rich(Fixture());
        treasurySave.Treasuries = new[] { new Treasury(200, new(5, 6)) };
        treasurySave.NextId = 201;
        var treasury = Restore(treasurySave);
        Check(treasury.Build(Side.Player, 3, ShipClass.Lighthouse, new(5, 6)).Success
            && treasury.TreasuryRuins.Count == 0 && treasury.Credits(Side.Player) == 194,
            "lighthouse removes the underlying ruin without awarding plunder");
        Check(Restore(treasury.CaptureSnapshot()).TreasuryRuins.Count == 0,
            "cleared ruins cannot return after Continue");
        treasury = Restore(treasurySave);
        Check(treasury.Build(Side.Player, 3, ShipClass.CannonTower, new(5, 6)).Success
            && treasury.TreasuryRuins.Count == 0, "cannon tower also clears the treasury");
        var edgeGiftSave = Rich(Fixture(), 2);
        var giftMother = edgeGiftSave.Ships.Single(s => s.Id == 1);
        giftMother.Position = new(1, 1);
        giftMother.PendingUpgradeLevel = 2;
        var edgeGift = Restore(edgeGiftSave);
        Check(edgeGift.ChooseUpgrade(Side.Player, 1, UpgradeChoice.FishingBoat).Success
            && edgeGift.OwnShips(Side.Player).Where(s => s.Definition.Class == ShipClass.Fishing)
                .All(s => !edgeGift.Board.IsOuterCell(s.Position)),
            "free Support Brig delivery skips nearby outer-edge water");
        edgeGiftSave = Rich(Fixture(), 4);
        giftMother = edgeGiftSave.Ships.Single(s => s.Id == 1);
        giftMother.Position = new(0, 1);
        giftMother.PendingUpgradeLevel = 4;
        edgeGiftSave.Ships = edgeGiftSave.Ships.Append(new SavedShip
        {
            Id = 201, Owner = Side.Player, Kind = ShipClass.Balloon,
            Position = new(1, 1), Health = 1, Level = 1
        }).ToArray();
        edgeGiftSave.NextId = 202;
        edgeGift = Restore(edgeGiftSave);
        Check(edgeGift.ChooseUpgrade(Side.Player, 1, UpgradeChoice.Balloon).Success
            && edgeGift.Find(202) is { IsAirborne: true } delivered
            && !edgeGift.Board.IsOuterCell(delivered.Position) && delivered.Position != new GridPosition(1, 1),
            "an edge flagship delivers its new Balloon to a free interior air berth");
        Check(Restore(edgeGift.CaptureSnapshot()).Find(202)!.Position == edgeGift.Find(202)!.Position,
            "relocated airborne reward survives Continue");
        var oldGiftJson = JsonNode.Parse(BattleState.SerializeSnapshot(edgeGiftSave))!.AsObject();
        oldGiftJson["Rules"]!["EmptyOuterRim"] = false;
        var oldGift = BattleState.LoadJson(oldGiftJson.ToJsonString());
        Check(oldGift.ChooseUpgrade(Side.Player, 1, UpgradeChoice.Balloon).Success
            && oldGift.Find(202)!.Position == new GridPosition(0, 1),
            "legacy voyages retain airborne reward above the edge flagship");
        var previewSave = Rich(Fixture());
        previewSave.Ships.Single(s => s.Id == 1).Position = new(11, 10);
        previewSave.Villages[0] = previewSave.Villages[0] with { Owner = Side.Player, Level = 3, Health = 15, Port = true };
        var preview = Restore(previewSave);
        string original = preview.SaveJson();
        var port = preview.PortBerth(preview.Villages.Single());
        var prospective = preview.PreviewLighthouseTradeRoutes(Side.Player, new(11, 10));
        Check(prospective.IsEmpty, "an occupied lighthouse preview has no paths");
        prospective = preview.PreviewLighthouseTradeRoutes(Side.Player, new(10, 11));
        Check(prospective.Routes.Any(route => route.First() == port && route.Last() == new GridPosition(10, 11)
                || route.Last() == port && route.First() == new GridPosition(10, 11)),
            "a legal lighthouse preview connects to the nearby owned port");
        Check(preview.SaveJson() == original, "trade preview never mutates topology, credits, RNG or snapshot");
        Check(preview.PreviewLighthouseTradeRoutes(Side.Player, new(0, 0)).IsEmpty,
            "lighthouse preview never searches unexplored terrain");
        var ready = Fixture();
        var rows = ready.ReadyActions(Side.Player);
        Check(rows.Select(r => r.Id).Distinct().Count() == rows.Count && rows.All(r => r.ShipClass is not null),
            "multiple ship orders qualify a single object and neutral towns are excluded");
        var exhausted = Rich(Fixture());
        foreach (var s in exhausted.Ships.Where(s => s.Owner == Side.Player)) s.IsExhausted = true;
        Check(Restore(exhausted).ReadyActions(Side.Player).Count == 0, "spent hulls contribute no remaining actions");
        var scarce = Rich(Fixture(), 1);
        foreach (var s in scarce.Ships.Where(s => s.Owner == Side.Player))
        { s.MovementLocked = true; s.AttacksUsed = 2; s.HasProduced = true; }
        scarce.Credits[0] = 1;
        ready = Restore(scarce);
        Check(ready.ReadyActions(Side.Player).Count == 0, "unaffordable collection does not inflate the counter");
        scarce.Credits[0] = 2;
        ready = Restore(scarce);
        Check(ready.ReadyActions(Side.Player).Single().Id == 1
            && ready.ReadyActions(Side.Player).Single().Actions == ReadyActionKind.Collect,
            "an affordable active fish spot qualifies the flagship once");
        scarce.Credits[0] = 4; scarce.Fish = Array.Empty<GridPosition>();
        scarce.Ships.Single(s => s.Id == 1).HasProduced = false;
        ready = Restore(scarce);
        Check(ready.BuildBlockReason(Side.Player, 1, ShipClass.Fishing) is null
            && ready.ReadyActions(Side.Player).Count == 0, "Support Brig production, radar and other excluded purchases are not counted");
        var fullBuilderSave = Rich(Fixture());
        fullBuilderSave.Fish = Array.Empty<GridPosition>();
        fullBuilderSave.Shoals = Array.Empty<GridPosition>();
        fullBuilderSave.Credits[0] = 6;
        foreach (var hull in fullBuilderSave.Ships.Where(s => s.Owner == Side.Player)) hull.IsExhausted = true;
        var fullMother = fullBuilderSave.Ships.Single(s => s.Id == 1);
        fullMother.IsExhausted = false;
        fullMother.MovementLocked = true;
        fullMother.AttacksUsed = 1;
        fullBuilderSave.Ships = fullBuilderSave.Ships.Concat(Enumerable.Range(201, 3).Select(id => new SavedShip
        {
            Id = id, Owner = Side.Player, Kind = ShipClass.Garrison,
            Position = new GridPosition(id - 190, 5), Health = 5, Level = 1, IsExhausted = true
        })).ToArray();
        fullBuilderSave.NextId = 204;
        ready = Restore(fullBuilderSave);
        Check(ready.FleetUsed(Side.Player) == ready.FleetCapacity(Side.Player)
            && ready.BuildBlockReason(Side.Player, 1, ShipClass.Garrison) is not null
            && ready.ReadyActions(Side.Player).Single() is { Id: 1, Actions: ReadyActionKind.Build },
            "an immobile spent-gun flagship still counts affordable structures at full fleet capacity");
        fullBuilderSave.Credits[0] = 5;
        Check(Restore(fullBuilderSave).ReadyActions(Side.Player).Count == 0,
            "unaffordable structures do not qualify an otherwise spent full-fleet flagship");
        var unsafeSave = Rich(Fixture());
        foreach (var s in unsafeSave.Ships.Where(s => s.Owner == Side.Player)) s.IsExhausted = true;
        var shooter = unsafeSave.Ships.Single(s => s.Id == 4);
        shooter.IsExhausted = false; shooter.MovementLocked = true; shooter.Health = 1;
        ready = Restore(unsafeSave);
        Check(ready.CanAttack(4, 5) && ready.ReadyActions(Side.Player).Count == 0,
            "a lethal reply removes the otherwise legal attack from guidance");
        unsafeSave.Ships.Single(s => s.Id == 5).Health = 4;
        ready = Restore(unsafeSave);
        Check(ready.ReadyActions(Side.Player).Single().Actions == ReadyActionKind.Attack,
            "a decisive double salvo is safe when a single shot would invite a lethal reply");
        ready = Restore(Rich(Fixture()));
        string beforeQuery = ready.SaveJson();
        ready.ReadyActions(Side.Player);
        Check(ready.SaveJson() == beforeQuery && ready.ReadyActions(Side.Enemy).Count == 0,
            "guidance is read-only and cannot list another nation's orders");
        var old = JsonNode.Parse(ready.SaveJson())!.AsObject();
        var oldRules = old["Rules"]!.AsObject();
        foreach (string field in new[] { "ConstructionClearsRuins", "EmptyOuterRim", "SeparatedStartingEscorts",
            "MapSizePirateSettlements", "EqualDoubleSalvoDamage" }) oldRules.Remove(field);
        oldRules["Mortar"]!.AsObject().Remove("GranadoDeadZone");
        oldRules["Economy"]!.AsObject().Remove("VillageIncomeBonus");
        oldRules["Economy"]!.AsObject().Remove("ResourceDensityMultiplier");
        oldRules["Economy"]!["CombatShipsPerUpkeep"] = 2;
        var legacy = BattleState.LoadJson(old.ToJsonString());
        Check(!legacy.Rules.ConstructionClearsRuins && !legacy.Rules.EmptyOuterRim
            && !legacy.Rules.SeparatedStartingEscorts && !legacy.Rules.MapSizePirateSettlements
            && !legacy.Rules.EqualDoubleSalvoDamage && legacy.Rules.Mortar.GranadoDeadZone is null
            && legacy.Rules.Economy.VillageIncomeBonus == 0 && legacy.Rules.Economy.ResourceDensityMultiplier == .7,
            "absent settings retain released voyage mechanics");
        Check(legacy.Build(Side.Player, 1, ShipClass.Garrison, new(3, 2)).Success && legacy.Upkeep(Side.Player) == 1,
            "existing voyages retain their explicit upkeep policy");
        foreach (var size in Enum.GetValues<MapSize>())
        {
            int expectedPirates = size switch { MapSize.Lake => 2, MapSize.Bay => 3, MapSize.Sea => 4, _ => 5 };
            foreach (int rivals in new[] { 1, 4 })
            {
                var board = ArchipelagoGenerator.Create(206, rivals, WorldKind.Oceans, size);
                var match = SkirmishSetup.Create(board, rules, rivals);
                Check(match.Villages.Count(v => v.Owner == Side.Pirates) == expectedPirates,
                    "pirate settlement count is controlled by map size rather than rivals");
                Check(match.Villages.Where(v => v.Owner == Side.Pirates).All(v => v.Level < 5)
                    && match.Villages.Count(v => v.Owner == Side.Pirates && v.Level == 3 && v.IsFortified) >= 2,
                    "two fortified level-three bays remain guaranteed; pirates never start at level five");
                foreach (var side in match.Factions)
                {
                    var mother = match.Mothership(side)!;
                    Check(match.OwnShips(side).Where(s => !s.IsMothership).All(s =>
                        !board.GetSurrounding(mother.Position).Contains(s.Position)), "starting escorts have a separating tile");
                }
                Check(match.Villages.All(v => board.GetNeighbors(v.Position).Any(p => board.GetTile(p).Terrain == TerrainType.Land)),
                    "new map towns never occupy isolated single-cell islets");
                Check(match.Ships.All(s => !board.IsOuterCell(s.Position))
                    && match.FishSpots.All(p => !board.IsOuterCell(p)) && match.Shoals.All(p => !board.IsOuterCell(p))
                    && match.Villages.All(v => !board.IsOuterCell(v.Position))
                    && match.TreasuryRuins.All(t => !board.IsOuterCell(t.Position)), "the outermost ring has no generated objects");
                Check(Restore(match.CaptureSnapshot()).SaveJson() == match.SaveJson(), "new settings and generated deployment survive exact Continue");
            }
        }
        foreach (var kind in Enum.GetValues<WorldKind>())
        {
            var broad = ArchipelagoGenerator.Create(2063, 4, kind, MapSize.Lake);
            var towns = SkirmishSetup.Create(broad, rules, 4);
            Check(towns.Villages.Count == 15 && towns.Villages.All(v => broad.GetNeighbors(v.Position)
                .Any(p => broad.GetTile(p).Terrain == TerrainType.Land)),
                "the smallest five-fleet world retries terrain while retaining fifteen suitable towns");
        }
        var isolatedCells = new[] { new GridPosition(6, 6), new GridPosition(6, 14), new GridPosition(6, 22),
            new GridPosition(22, 6), new GridPosition(22, 14), new GridPosition(22, 22) }.ToHashSet();
        var isolatedBoard = new GameBoard(32, 32, p => isolatedCells.Contains(p) ? TerrainType.Land : TerrainType.Water,
            mapSize: MapSize.Sea);
        Check(WorldSettlementPlacement.Create(isolatedBoard, 2).Count == 6,
            "the optional legacy placement policy still accepts isolated historic town cells");
        bool isolatedRejected = false;
        try { WorldSettlementPlacement.Create(isolatedBoard, 2, requireLandNeighbor: true); }
        catch (InvalidOperationException) { isolatedRejected = true; }
        Check(isolatedRejected, "new town placement rejects single-cell land before creating an undersized town");
        var legacyPolicy = JsonNode.Parse(Fixture().SaveJson())!["Rules"]!.AsObject();
        legacyPolicy.Remove("MapSizePirateSettlements");
        var legacyPlacementRules = BattleRules.FromJson(legacyPolicy.ToJsonString());
        Check(SkirmishSetup.Create(isolatedBoard, legacyPlacementRules, 1).Villages.Count == 6,
            "absent optional new-world rules retain historical settlement eligibility");
        return checks;
    }
}
