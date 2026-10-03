using System.Text.Json;
using System.Text.Json.Nodes;
using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Units;
using DevAncientNaval.Core.World;

internal static class Rules0203Checks
{
    internal static int Run(BattleRules rules)
    {
        int checks = 0;
        void Check(bool value, string message)
        {
            if (!value) throw new Exception("v020.3 rules: " + message);
            checks++;
        }
        BattleState Create(params (Side Owner, ShipClass Class, GridPosition Position)[] extra) => new(
            new GameBoard(32,32,_=>TerrainType.Water),rules,
            new[] {(Side.Player,ShipClass.Mothership,new GridPosition(1,1)),
                (Side.Enemy,ShipClass.Mothership,new GridPosition(28,28))}.Concat(extra),
            Array.Empty<GridPosition>(),villageSpots:Array.Empty<GridPosition>());

        Check(rules.StartingCredits == 8 && rules.RepairAmount == 4 && rules.AutoRepairAmount == 4
            && rules.AncientAutoRepairAmount == 2,"starting treasury and active/passive healing balance");
        Check(rules.Get(ShipClass.Garrison).Price == 5 && rules.Get(ShipClass.Fishing).Price == 4
            && rules.Get(ShipClass.Fishing).IncomePerTurn == 1 && rules.Get(ShipClass.Togus).Price == 16,
            "Brig, fishing income and Granado construction balance");
        Check(new[] {ShipClass.Mothership,ShipClass.Invader,ShipClass.Kolonel}.All(c=>rules.Get(c).AttackRange==2)
            && rules.Balloon.AntiAirRange==2,"short base cannon and anti-air ranges");
        Check(rules.DynamicFleetCapacity && rules.DeferredRewards && rules.FishingRadarVisible
            && rules.FishingLighthouses && rules.FrozenUnownedVillages && rules.PersistTreasuryRuins,
            "new voyage policies enabled explicitly");

        var fleet = Create((Side.Player,ShipClass.Fishing,new(2,1)),
            (Side.Player,ShipClass.Garrison,new(1,2)),(Side.Player,ShipClass.Fishing,new(2,2)),
            (Side.Player,ShipClass.Balloon,new(1,1)),(Side.Player,ShipClass.Lighthouse,new(5,5)),
            (Side.Player,ShipClass.CannonTower,new(6,6)),(Side.Player,ShipClass.AncientGun,new(7,7)));
        Check(fleet.FleetUsed(Side.Player)==4 && fleet.FleetCapacity(Side.Player)==4,
            "flagship, armed hulls and fishing vessels count; air and buildings do not");
        Check(fleet.BuildBlockReason(Side.Player,1,ShipClass.Fishing)?.StartsWith("Fleet limit:")==true
            && fleet.BuildBlockReason(Side.Player,1,ShipClass.Garrison)?.StartsWith("Fleet limit:")==true,
            "full capacity blocks both fishing and combat construction");
        Check(fleet.BuildBlockReason(Side.Player,1,ShipClass.Lighthouse) is null,
            "a lighthouse remains constructible at full fleet capacity");
        fleet.Find(1)!.Level=3;
        Check(fleet.FleetCapacity(Side.Player)==8,"flagship levels add two fleet slots each");
        var coast = new GameBoard(32,32,p=>p==new GridPosition(15,15)?TerrainType.Land:TerrainType.Water);
        var town = new BattleState(coast,rules,new[] {
            (Side.Player,ShipClass.Mothership,new GridPosition(1,1)),
            (Side.Enemy,ShipClass.Mothership,new GridPosition(28,28))},
            Array.Empty<GridPosition>(),villageSpots:new[] {new GridPosition(15,15)});
        var village=town.Villages.Single(); village.Owner=Side.Player; village.Level=3; village.Health=village.MaxHealth;
        Check(town.FleetCapacity(Side.Player)==8,"living level-three town contributes four slots");
        village.Health=0;
        Check(town.FleetCapacity(Side.Player)==4,"destroyed town no longer contributes capacity");

        var gift=Create((Side.Player,ShipClass.Fishing,new(2,1)),(Side.Player,ShipClass.Fishing,new(3,1)),
            (Side.Player,ShipClass.Fishing,new(4,1)),(Side.Player,ShipClass.Fishing,new(5,1)),
            (Side.Player,ShipClass.Fishing,new(6,1)));
        gift.Find(1)!.Level=2; gift.Find(1)!.PendingUpgradeLevel=2;
        var fullGift=gift.CaptureSnapshot();
        Check(!gift.ChooseUpgrade(Side.Player,1,UpgradeChoice.FishingBoat).Success
            && gift.FleetUsed(Side.Player)==6 && gift.Find(1)!.PendingUpgradeLevel==2
            && gift.CaptureSnapshot().NextId==fullGift.NextId,
            "free fishing reward cannot overflow capacity or consume pending choice/identifier");
        Check(gift.ChooseUpgrade(Side.Player,1,UpgradeChoice.Mobility).Success && gift.FleetUsed(Side.Player)==6,
            "full fleet can still choose the alternative level reward");
        var availableGift=Create((Side.Player,ShipClass.Fishing,new(2,1)));
        availableGift.Find(1)!.Level=2; availableGift.Find(1)!.PendingUpgradeLevel=2;
        Check(availableGift.ChooseUpgrade(Side.Player,1,UpgradeChoice.FishingBoat).Success
            && availableGift.FleetUsed(Side.Player)==3,"free fishing reward is delivered when capacity remains");

        var scuttle=Create((Side.Player,ShipClass.Fishing,new(4,4)));
        int money=scuttle.Credits(Side.Player), used=scuttle.FleetUsed(Side.Player), income=scuttle.GrossIncome(Side.Player);
        Check(!scuttle.Scuttle(Side.Player,1).Success && !scuttle.Scuttle(Side.Player,2).Success,
            "flagship and opposing hull cannot be dismantled");
        Check(scuttle.Scuttle(Side.Player,3).Success && scuttle.Find(3) is null
            && scuttle.Credits(Side.Player)==money && scuttle.FleetUsed(Side.Player)==used-1
            && scuttle.GrossIncome(Side.Player)==income-1,"dismantling frees capacity and bound income without refund");
        Check(!scuttle.Scuttle(Side.Player,3).Success,"dismantling cannot be repeated");

        foreach(var kind in new[] {ShipClass.Invader,ShipClass.Kolonel})
        {
            var combat=Create((Side.Player,kind,new(5,5)),(Side.Enemy,ShipClass.Fishing,new(6,5)),
                (Side.Enemy,ShipClass.Fishing,new(8,5)),(Side.Player,ShipClass.Fishing,new(8,4)));
            var gun=combat.Find(3)!; gun.Kills=2; combat.Find(4)!.Health=1;
            Check(!combat.WeaponCovers(gun,new(8,5)),kind+" has no veteran range before third kill");
            Check(combat.Attack(Side.Player,3,4).Success && gun.IsVeteran && gun.Kills==3
                && gun.Health==gun.MaxHealth && gun.CannonRange==3,kind+" third kill promotes, heals and extends cannon range");
            if(gun.AttacksRemaining==0) { combat.EndTurn(Side.Player); combat.EndTurn(Side.Enemy); }
            Check(combat.CanAttack(3,5) && combat.WeaponCovers(gun,new(8,5),true),kind+" veteran attack and reply both reach third tile");
            var restored=BattleState.LoadJson(combat.SaveJson());
            Check(restored.Find(3)!.IsVeteran && restored.Find(3)!.CannonRange==3,kind+" range survives Continue");
        }

        var mortars=Create((Side.Player,ShipClass.Togus,new(6,6)),(Side.Player,ShipClass.AncientGun,new(16,6)));
        foreach(var id in new[] {3,4})
        {
            var gun=mortars.Find(id)!;
            Check(!mortars.WeaponCovers(gun,new(gun.Position.X+2,gun.Position.Y))
                && mortars.WeaponCovers(gun,new(gun.Position.X+3,gun.Position.Y))
                && mortars.WeaponCovers(gun,new(gun.Position.X+5,gun.Position.Y))
                && !mortars.WeaponCovers(gun,new(gun.Position.X+6,gun.Position.Y)),
                "mortar-only hull and ancient tower exclude inner two tiles and stop at five");
            Check(!mortars.WeaponCovers(gun,new(gun.Position.X+3,gun.Position.Y),true),"mortar-only weapons never counterattack");
        }
        var mother=mortars.Find(1)!; mother.HasMortar=true;
        Check(mortars.WeaponCovers(mother,new(3,1)) && !mortars.UsesMortar(mother,new(3,1))
            && mortars.WeaponCovers(mother,new(4,1)) && mortars.UsesMortar(mother,new(4,1)),
            "flagship keeps cannons in the mortar dead zone");

        var radar=new BattleState(new GameBoard(32,32,_=>TerrainType.Water),rules,new[] {
            (Side.Player,ShipClass.Mothership,new GridPosition(28,28)),
            (Side.Enemy,ShipClass.Mothership,new GridPosition(2,2)),
            (Side.Player,ShipClass.Fishing,new GridPosition(6,2)),
            (Side.Player,ShipClass.Garrison,new GridPosition(6,3))},Array.Empty<GridPosition>(),villageSpots:Array.Empty<GridPosition>());
        radar.Find(2)!.HasRadar=true; radar.Vision.Recompute(radar.Ships,radar.TurnSerial);
        Check(radar.Vision.IsRadarContact(Side.Enemy,new(6,2)) && !radar.Vision.IsVisible(Side.Enemy,new(6,2)),
            "fishing vessel produces radar contact without optical identity");
        Check(!radar.Vision.IsRadarContact(Side.Enemy,new(6,3)),"Brig continues to evade radar");

        var light=Create((Side.Player,ShipClass.Fishing,new(4,4)));
        Check(light.BuildLighthouse(Side.Player,3,new(4,5)).Success,"fishing vessel can construct adjacent lighthouse");
        var beacon=light.Ships.Single(s=>s.Definition.Class==ShipClass.Lighthouse);
        Check(beacon.VisualRange==4 && beacon.IsStructure && !beacon.IsArmed && light.FleetUsed(Side.Player)==2,
            "lighthouse supplies sight without occupying a fleet slot");

        var healing=Create((Side.Player,ShipClass.Invader,new(5,5)),(Side.Player,ShipClass.AncientGun,new(6,6)));
        healing.Find(3)!.Health=4; healing.Find(4)!.Health=4;
        Check(!healing.Repair(Side.Player,4).Success,"ancient tower has no active repair action");
        Check(healing.EndTurn(Side.Player).Success && healing.Find(3)!.Health==8 && healing.Find(4)!.Health==6,
            "unused ship repairs four and unused ancient tower repairs two");
        var activeHeal=Create((Side.Player,ShipClass.Invader,new(5,5))); activeHeal.Find(3)!.Health=4;
        Check(activeHeal.Repair(Side.Player,3).Success && activeHeal.Find(3)!.Health==8,"active repair restores four");
        activeHeal.EndTurn(Side.Player);
        Check(activeHeal.Find(3)!.Health==8,"active repair cannot also receive passive repair that turn");

        var awards=Create((Side.Player,ShipClass.Garrison,new(5,5)),(Side.Enemy,ShipClass.Fishing,new(6,5)));
        var nation=awards.PendingAwards.Single(a=>a.Kind==AwardKind.Nation);
        Check(awards.HasMet(Side.Enemy) && awards.Credits(Side.Player)==rules.StartingCredits,
            "optical encounter queues its five Thors instead of crediting early");
        awards=BattleState.LoadJson(awards.SaveJson());
        Check(awards.PendingAwards.Single().Id==nation.Id && awards.Credits(Side.Player)==rules.StartingCredits,
            "unclaimed encounter banner survives Continue");
        void RejectAward(BattleState source, Action<BattleSave> mutate, string description)
        {
            var invalid=source.CaptureSnapshot(); mutate(invalid);
            bool rejected=false;
            try { BattleState.LoadJson(BattleState.SerializeSnapshot(invalid)); }
            catch(ArgumentException) { rejected=true; }
            Check(rejected,description);
        }
        RejectAward(awards,save=>save.PendingAwards[0]=save.PendingAwards[0] with { Position=null },
            "encounter receipt cannot omit its actual discovery position");
        RejectAward(awards,save=>save.PendingAwards[0]=save.PendingAwards[0] with { Position=new(7,5) },
            "encounter receipt cannot substitute another map position");
        RejectAward(awards,save=>save.PendingAwards[0]=save.PendingAwards[0] with { Nation=Side.Enemy2 },
            "encounter receipt cannot invent a different nation");
        RejectAward(awards,save=>save.PendingAwards[0]=save.PendingAwards[0] with { Amount=6 },
            "encounter receipt cannot inflate currency");
        RejectAward(awards,save=>save.PendingAwards[0]=save.PendingAwards[0] with { Id="nation:01" },
            "encounter receipt identifiers are canonical");
        Check(!awards.ClaimAward(Side.Enemy,nation.Id).Success && awards.ClaimAward(Side.Player,nation.Id).Success
            && awards.Credits(Side.Player)==rules.StartingCredits+5,"only owner can claim encounter exactly once");
        Check(!awards.ClaimAward(Side.Player,nation.Id).Success && awards.PendingAwards.Count==0,
            "repeated reward activation cannot duplicate coins");
        Check(BattleState.LoadJson(awards.SaveJson()).PendingAwards.Count==0,"claimed encounter is not recreated by Continue");
        var heavens=Create();
        for(int i=0;i<4;i++) { heavens.EndTurn(Side.Player); heavens.EndTurn(Side.Enemy); }
        var blessing=heavens.PendingAwards.Single(a=>a.Kind==AwardKind.Heavenly); money=heavens.Credits(Side.Player);
        Check(blessing.Amount==2 && heavens.PersonalTurnStarts(Side.Player)==5,"fifth personal start queues two for living flagship");
        Check(BattleState.LoadJson(heavens.SaveJson()).PendingAwards.Single().Id==blessing.Id,
            "valid unclaimed heavenly receipt survives Continue");
        foreach(string forgedId in new[] {"heavenly:0:garbage","heavenly:0:6","heavenly:1:5",
            "heavenly:0:10","heavenly:0:05","heavenly:0:+5","heavenly:0:5:extra"})
            RejectAward(heavens,save=>save.PendingAwards[0]=save.PendingAwards[0] with { Id=forgedId },
                "fabricated heavenly receipt identifier rejected: "+forgedId);
        RejectAward(heavens,save=>save.PendingAwards[0]=save.PendingAwards[0] with { Amount=4 },
            "heavenly receipt cannot exceed all possible city and flagship beneficiaries");
        RejectAward(heavens,save=>save.PendingAwards[0]=save.PendingAwards[0] with { Amount=1 },
            "heavenly receipt is an even beneficiary reward");
        RejectAward(heavens,save=>save.PendingAwards[0]=save.PendingAwards[0] with { Position=new(1,1) },
            "heavenly receipt cannot carry a forged map destination");
        RejectAward(heavens,save=>save.PendingAwards=save.PendingAwards.Concat(save.PendingAwards).ToArray(),
            "duplicate heavenly receipt cannot be claimed twice");
        RejectAward(heavens,save=>save.PersonalTurnStarts=Array.Empty<int>(),
            "heavenly receipt requires a persisted personal clock");
        Check(heavens.ClaimAward(Side.Player,blessing.Id).Success && heavens.Credits(Side.Player)==money+2
            && !heavens.ClaimAward(Side.Player,blessing.Id).Success,"heavenly blessing is claimed once");
        var lostCity=new BattleState(coast,rules,new[] {
            (Side.Player,ShipClass.Mothership,new GridPosition(1,1)),
            (Side.Enemy,ShipClass.Mothership,new GridPosition(28,28))},
            Array.Empty<GridPosition>(),villageSpots:new[] {new GridPosition(15,15)});
        lostCity.Villages.Single().Owner=Side.Player;
        for(int i=0;i<4;i++) { lostCity.EndTurn(Side.Player); lostCity.EndTurn(Side.Enemy); }
        var cityReceipt=lostCity.PendingAwards.Single(a=>a.Kind==AwardKind.Heavenly);
        var lostSnapshot=lostCity.CaptureSnapshot();
        lostSnapshot.Villages[0]=lostSnapshot.Villages[0] with { Owner=null };
        lostCity=BattleState.LoadJson(BattleState.SerializeSnapshot(lostSnapshot));
        Check(cityReceipt.Amount==4 && lostCity.PendingAwards.Single().Amount==4,
            "loss of a town preserves an already-earned blessing amount on Continue");
        var missingAwards=JsonNode.Parse(lostCity.SaveJson())!.AsObject(); missingAwards.Remove("PendingAwards");
        Check(BattleState.LoadJson(missingAwards.ToJsonString()).PendingAwards.Count==0,
            "historical missing reward collection defaults to empty");

        var ruins=Create((Side.Player,ShipClass.Garrison,new(5,5)));
        var snapshot=ruins.CaptureSnapshot(); int treasuryId=snapshot.NextId++;
        snapshot.Treasuries=new[] {new Treasury(treasuryId,new(5,5))};
        snapshot.Outcomes=new[] {new SavedOutcome(treasuryId,TreasuryReward.Currency)};
        snapshot.TreasuryWaits=new[] {new SavedWait(treasuryId,Side.Player,3,new(5,5),0)}; snapshot.TurnSerial=1;
        ruins=BattleState.LoadJson(BattleState.SerializeSnapshot(snapshot)); money=ruins.Credits(Side.Player);
        Check(ruins.LootTreasury(Side.Player,3).Success && ruins.Credits(Side.Player)==money+rules.Treasury.CurrencyReward,
            "ready crew collects fixed currency reward");
        Check(ruins.Treasuries.Count==0 && ruins.TreasuryRuins.Single().IsCollected && ruins.TreasuryAt(new(5,5)) is null
            && !ruins.LootTreasury(Side.Player,3).Success,"collected ruin remains on map but cannot be harvested again");
        ruins=BattleState.LoadJson(ruins.SaveJson());
        Check(ruins.TreasuryRuins.Single().IsCollected && ruins.Treasuries.Count==0,"ruin state survives Continue");

        var bay=new BattleState(coast,rules,new[] {
            (Side.Player,ShipClass.Mothership,new GridPosition(1,1)),
            (Side.Enemy,ShipClass.Mothership,new GridPosition(28,28))},
            Array.Empty<GridPosition>(),villageSpots:new[] {new GridPosition(15,15)});
        var pirate=bay.Villages.Single(); pirate.Owner=Side.Pirates; pirate.Level=3; pirate.Health=pirate.MaxHealth; pirate.IsFortified=true;
        for(int i=0;i<4;i++)
            Check(bay.EndTurn(Side.Player).Success && bay.EndTurn(Side.Enemy).Success && bay.EndTurn(Side.Pirates).Success,
                "pirate settlement has legal turn cycle");
        Check(pirate.Level==3 && pirate.TurnsOwned==0 && pirate.IsFortified,"unclaimed pirate bay keeps generated level and wall");
        pirate.Owner=Side.Player;
        bay.EndTurn(Side.Player); bay.EndTurn(Side.Enemy); bay.EndTurn(Side.Player);
        Check(pirate.Level==(rules.PaidVillageUpgrades?3:4) && pirate.TurnsOwned==2,
            "captured town progresses only through the active rules' paid or historic policy");

        var oldRules=JsonNode.Parse(JsonSerializer.Serialize(rules))!.AsObject();
        foreach(string field in new[] {"DynamicFleetCapacity","DeferredRewards","FishingRadarVisible","FishingLighthouses",
            "FrozenUnownedVillages","PersistTreasuryRuins","AncientAutoRepairAmount"}) oldRules.Remove(field);
        var legacyRules=BattleRules.FromJson(oldRules.ToJsonString());
        Check(!legacyRules.DynamicFleetCapacity && !legacyRules.DeferredRewards && !legacyRules.FishingRadarVisible
            && !legacyRules.FishingLighthouses && !legacyRules.FrozenUnownedVillages && !legacyRules.PersistTreasuryRuins
            && legacyRules.AncientAutoRepairAmount==0,"absent optional policies preserve historic rules");
        var legacy=new BattleState(new GameBoard(32,32,_=>TerrainType.Water),legacyRules,new[] {
            (Side.Player,ShipClass.Mothership,new GridPosition(1,1)),(Side.Enemy,ShipClass.Mothership,new GridPosition(28,28)),
            (Side.Player,ShipClass.Garrison,new GridPosition(5,5)),(Side.Enemy,ShipClass.Fishing,new GridPosition(6,5))},
            Array.Empty<GridPosition>(),villageSpots:Array.Empty<GridPosition>());
        Check(legacy.FleetCapacity(Side.Player)==legacyRules.FleetLimit && legacy.PendingAwards.Count==0
            && legacy.Credits(Side.Player)==legacyRules.StartingCredits+legacyRules.EncounterCurrencyReward,
            "legacy voyage keeps fixed capacity and immediate encounter income");
        return checks;
    }
}
