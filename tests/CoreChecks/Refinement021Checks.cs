using System.Text.Json.Nodes;
using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Units;
using DevAncientNaval.Core.World;

internal static class Refinement021Checks
{
    internal static int Run(BattleRules rules)
    {
        int checks = 0;
        void Check(bool yes, string name) { if (!yes) throw new Exception("0.21: " + name); checks++; }
        BattleState Duel(ShipClass attacker = ShipClass.Kolonel) => new(new GameBoard(22,22,_=>TerrainType.Water), rules,
            new[] {(Side.Player,ShipClass.Mothership,new GridPosition(2,2)),(Side.Enemy,ShipClass.Mothership,new GridPosition(20,20)),
                (Side.Player,attacker,new GridPosition(8,8)),(Side.Enemy,ShipClass.Kolonel,new GridPosition(10,8))},
            Array.Empty<GridPosition>(), villageSpots:Array.Empty<GridPosition>());
        Check(rules.Get(ShipClass.Invader).Price == 7 && rules.Get(ShipClass.Kolonel).Price == 12, "new ship prices");
        Check(rules.Get(ShipClass.Mothership).Movement == 2 && rules.Get(ShipClass.Fishing).Movement == 3 && rules.Get(ShipClass.Garrison).AttackRange == 2, "new movement and Brig gun range");
        var battle = Duel();
        Check(battle.HasMet(Side.Enemy) && battle.Credits(Side.Player) == rules.StartingCredits+5, "first optically visible rival grants five Thors");
        int money = battle.Credits(Side.Player);
        battle.SetGodEye(true); battle.SetGodEye(false);
        Check(battle.Credits(Side.Player)==money && battle.Encounters.Count==1, "fog toggles do not pay twice");
        string saved = battle.SaveJson();
        battle = BattleState.LoadJson(saved);
        Check(battle.SaveJson()==saved && battle.Credits(Side.Player)==money && battle.HasMet(Side.Enemy), "encounter identities, rewards and totals survive exact Continue");
        foreach (var bad in new NationEncounter[][] {
            new[] {new NationEncounter(Side.Enemy2,new(10,8))},
            new[] {new NationEncounter(Side.Enemy,new(10,8)),new NationEncounter(Side.Enemy,new(11,8))},
            new[] {new NationEncounter(Side.Enemy,new(-1,-1))}, null!})
        {
            var invalid = battle.CaptureSnapshot(); invalid.Encounters = bad;
            bool rejected = false;
            try { BattleState.LoadJson(BattleState.SerializeSnapshot(invalid)); }
            catch (ArgumentException) { rejected = true; }
            Check(rejected, "malformed encounter snapshots are rejected without paying rewards");
        }
        var order = battle.Prepare(b => b.Attack(Side.Player,3,4,true));
        Check(order.Result.Success && order.Result.Shots!.Count(s=>!s.IsCounterattack)==2 && order.Result.Shots!.Count(s=>s.IsCounterattack)==1, "double salvo has two active shots and one reply");
        Check(battle.Find(4)!.Health==15 && battle.Find(3)!.AttacksUsed==0, "staged salvo has no early damage");
        order.Impact("attack");
        Check(battle.Find(4)!.Health==11 && battle.Find(3)!.Health==15, "first impact commits four damage before any reply");
        order.Impact("attack2");
        Check(battle.Find(4)!.Health==7 && battle.Find(3)!.Health==15, "second impact commits remaining four damage");
        order.Impact("counter"); order.Finish();
        Check(battle.Find(3)!.AttacksRemaining==0 && battle.Find(3)!.Health<15 && battle.Find(4)!.Health==7, "reply and two consumed charges commit once");
        Check(!battle.Attack(Side.Player,3,4,true).Success, "cannot repeat exhausted salvo");
        battle=Duel();
        var fragile=battle.CaptureSnapshot(); fragile.Ships.Single(s=>s.Id==4).Health=4;
        battle=BattleState.LoadJson(BattleState.SerializeSnapshot(fragile));
        var decisive=battle.Attack(Side.Player,3,4,true);
        Check(decisive.Success && decisive.Shots!.Count==1 && battle.Find(3)!.AttacksRemaining==0 && battle.Find(4) is null,
            "a first-shot kill skips redundant flight but reserves both salvo charges");
        battle=Duel();
        var shot=battle.Attack(Side.Player,3,4);
        Check(shot.Success && shot.Shots!.Count(s=>!s.IsCounterattack)==1 && battle.Find(3)!.AttacksRemaining==1, "tap spends only one shot");
        Check(!battle.Attack(Side.Player,3,4,true).Success, "one remaining charge cannot become a double salvo");
        battle=Duel(ShipClass.Invader);
        Check(!battle.CanDoubleSalvo(3,new(10,8)), "Galleon cannot use double salvo");
        battle = new BattleState(new GameBoard(22,22,_=>TerrainType.Water), rules,
            new[] {(Side.Player,ShipClass.Mothership,new GridPosition(8,8)),(Side.Enemy,ShipClass.Mothership,new GridPosition(20,20)),
                (Side.Enemy,ShipClass.Kolonel,new GridPosition(10,8)),(Side.Player,ShipClass.Fishing,new GridPosition(9,8))},Array.Empty<GridPosition>(),villageSpots:Array.Empty<GridPosition>());
        var snap=battle.CaptureSnapshot(); snap.Ships[0].SecondAttackUpgrade=true;
        battle=BattleState.LoadJson(BattleState.SerializeSnapshot(snap));
        Check(battle.CanDoubleSalvo(1,new(10,8)), "upgraded flagship can hold for two shots");
        var motherShot=battle.Attack(Side.Player,1,3,true);
        Check(motherShot.Success && motherShot.Shots!.Where(s=>!s.IsCounterattack).Sum(s=>s.Damage)==6, "flagship double salvo retains its own three-damage guns");
        // A radar coordinate is anonymous until it enters real optical sight.
        battle = new BattleState(new GameBoard(22,22,_=>TerrainType.Water),rules,
            new[] {(Side.Player,ShipClass.Mothership,new GridPosition(4,3)),(Side.Enemy,ShipClass.Mothership,new GridPosition(20,20)),
                (Side.Enemy,ShipClass.Invader,new GridPosition(6,3))},Array.Empty<GridPosition>(),villageSpots:Array.Empty<GridPosition>());
        Check(battle.BuyRadar(Side.Player,1).Success && battle.Vision.IsRadarContact(Side.Player,new(6,3)) && !battle.HasMet(Side.Enemy), "radar doesn't identify a nation");
        money=battle.Credits(Side.Player);
        battle.SetGodEye(true);
        Check(!battle.HasMet(Side.Enemy) && battle.Credits(Side.Player)==money, "God's eye is not a paid discovery");
        battle.SetGodEye(false);
        Check(battle.Move(Side.Player,1,new(5,3)).Success && battle.HasMet(Side.Enemy) && battle.Credits(Side.Player)==money+5, "sailing into true sight pays once");
        saved=battle.SaveJson();
        var legacy=JsonNode.Parse(saved)!;
        legacy["Rules"]!.AsObject().Remove("DoubleSalvo");
        legacy["Rules"]!.AsObject().Remove("EncounterCurrencyReward");
        legacy["Rules"]!.AsObject().Remove("LevelCurrencyRewards");
        legacy.AsObject().Remove("Encounters");
        var old=BattleState.LoadJson(legacy.ToJsonString());
        Check(!old.Rules.DoubleSalvo && old.Rules.EncounterCurrencyReward==0 && old.Rules.LevelCurrencyRewards.All(n=>n==0), "missing optional rules preserve historical balance");
        Check(old.Credits(Side.Player)==battle.Credits(Side.Player), "opening an old save creates no retroactive money");
        // Each level grant is tied to the level transition, not to opening or choosing the dialog.
        foreach (int level in Enumerable.Range(1,4))
        {
            battle=new BattleState(new GameBoard(22,22,_=>TerrainType.Water),rules,
                new[] {(Side.Player,ShipClass.Mothership,new GridPosition(3,3)),(Side.Enemy,ShipClass.Mothership,new GridPosition(20,20))},
                new[] {new GridPosition(4,3)},villageSpots:Array.Empty<GridPosition>());
            battle.SetCreative(true); snap=battle.CaptureSnapshot(); snap.Ships[0].Level=level; snap.Ships[0].Resources=level+rules.Get(ShipClass.Mothership).ResourceRequirementIncrease; snap.Ships[0].Health=15+(level-1)*5;
            battle=BattleState.LoadJson(BattleState.SerializeSnapshot(snap)); money=battle.Credits(Side.Player);
            Check(battle.Collect(Side.Player,1,new(4,3)).Success && battle.Find(1)!.Level==level+1 && battle.Credits(Side.Player)==money+rules.LevelCurrencyRewards[level-1], "exact level reward "+(level+1));
            money=battle.Credits(Side.Player); long lifetime=battle.Statistics.CurrencyEarned;
            saved=battle.SaveJson(); battle=BattleState.LoadJson(saved);
            Check(battle.Credits(Side.Player)==money && battle.Statistics.CurrencyEarned==lifetime, "level reward survives Continue without doubling "+level);
            Check(battle.ChooseUpgrade(Side.Player,1,battle.UpgradeOptions(1)[0]).Success && battle.Credits(Side.Player)==money, "choosing equipment cannot repay level reward "+level);
        }
        return checks;
    }
}
