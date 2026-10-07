using System.Text.Json.Nodes;
using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Units;
using DevAncientNaval.Core.World;

internal static class Rules0207bChecks
{
    internal static int Run(BattleRules rules)
    {
        // This retained suite exercises the released 0207b catalog. The next
        // version's requested speed is covered separately by Rules0208bChecks.
        var released = JsonNode.Parse(System.Text.Json.JsonSerializer.Serialize(rules))!.AsObject();
        foreach (var definition in released["Ships"]!.AsArray())
            if (definition!["Class"]!.GetValue<int>() == (int)ShipClass.Togus) definition["Movement"] = 4;
        rules = BattleRules.FromJson(released.ToJsonString());
        int checks = 0;
        void Check(bool condition, string name) { if (!condition) throw new Exception(name); checks++; }
        Check(rules.Get(ShipClass.Togus).Movement == 4, "released 0207b Granado retains four movement tiles");
        Check(rules.Get(ShipClass.CannonTower).Damage == 5, "0207b built cannon tower damage increases by two");
        Check(rules.Get(ShipClass.AncientGun).Damage == 5, "ancient tower damage unchanged");
        Check(rules.CannonTowersIgnoreWalls, "new voyage tower wall policy enabled");
        var townCell = new GridPosition(8, 8);
        BattleState Fixture(BattleRules active, ShipClass gunClass, bool wall = true)
        {
            var battle = new BattleState(new GameBoard(20, 20,
                p => p == townCell ? TerrainType.Land : TerrainType.Water), active, new[] {
                    (Side.Player, ShipClass.Mothership, new GridPosition(3, 3)),
                    (Side.Enemy, ShipClass.Mothership, new GridPosition(17, 17)),
                    (Side.Player, gunClass, new GridPosition(7, 8)),
                    (Side.Player, ShipClass.Garrison, new GridPosition(8, 7)) },
                Array.Empty<GridPosition>(), villageSpots: new[] { townCell });
            var saved = battle.CaptureSnapshot();
            saved.Villages[0] = saved.Villages[0] with { Level = 5, Health = 25, Fortified = wall };
            saved.Credits[0] = 100;
            return BattleState.LoadJson(BattleState.SerializeSnapshot(saved));
        }
        foreach (bool wall in new[] { false, true })
        {
            var battle = Fixture(rules, ShipClass.CannonTower, wall);
            var gun = battle.Find(3)!;
            double expected = gun.CurrentDamage;
            Check(battle.VillageShotDamage(gun, battle.Villages[0]) == expected, "tower prediction ignores wall only");
            string initial = battle.SaveJson();
            var staged = battle.Prepare(b => b.AttackVillage(Side.Player, 3, b.Villages[0].Id));
            Check(staged.Result.Success && staged.Result.Amount == expected, "prepared tower result matches forecast");
            Check(battle.Villages[0].Health == 25, "preparing tower salvo does not mutate live health");
            staged.Impact("village");
            Check(battle.Villages[0].Health == 25 - expected, "tower damage commits at village impact");
            staged.Impact("village");
            Check(battle.Villages[0].Health == 25 - expected, "duplicate tower impact never applies twice");
            staged.Finish(); staged.Finish();
            Check(battle.Villages[0].Health == 25 - expected && battle.PendingPresentation is null, "finish preserves exact-once damage");
            var resumed = BattleState.LoadJson(battle.SaveJson());
            Check(resumed.Rules.CannonTowersIgnoreWalls && resumed.SaveJson() == battle.SaveJson(), "Continue keeps new policy and deterministic state");
            var replay = BattleState.LoadJson(initial);
            Check(replay.AttackVillage(Side.Player, 3, replay.Villages[0].Id).Amount == expected, "immediate and staged facade outcomes agree");
        }
        var ordinary = Fixture(rules, ShipClass.Invader);
        var ordinaryGun = ordinary.Find(3)!;
        Check(ordinary.VillageShotDamage(ordinaryGun, ordinary.Villages[0]) == Ship.Whole(ordinaryGun.CurrentDamage * .75), "ordinary ship still respects walls");

        var legacy = JsonNode.Parse(Fixture(rules, ShipClass.CannonTower).SaveJson())!.AsObject();
        var embedded = legacy["Rules"]!.AsObject();
        embedded.Remove("CannonTowersIgnoreWalls");
        foreach (var definition in embedded["Ships"]!.AsArray())
        {
            string kind = definition!["Class"]!.GetValue<string>();
            if (kind == "CannonTower") definition["Damage"] = 3;
            if (kind == "Togus") definition["Movement"] = 3;
        }
        var old = BattleState.LoadJson(legacy.ToJsonString());
        Check(!old.Rules.CannonTowersIgnoreWalls, "absent rule preserves historical wall resistance");
        Check(old.Rules.Get(ShipClass.Togus).Movement == 3 && old.Rules.Get(ShipClass.CannonTower).Damage == 3, "historical embedded catalog preserved");
        var oldTower = old.Find(3)!;
        Check(old.VillageShotDamage(oldTower, old.Villages[0]) == Ship.Whole(oldTower.CurrentDamage * .75), "historical tower damage remains reduced");
        var oldAgain = BattleState.LoadJson(old.SaveJson());
        Check(!oldAgain.Rules.CannonTowersIgnoreWalls && oldAgain.SaveJson() == old.SaveJson(), "legacy policy survives another roundtrip");
        return checks;
    }
}

