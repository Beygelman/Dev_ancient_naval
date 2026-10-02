using System.Text.Json;
using System.Text.Json.Nodes;
using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Units;
using DevAncientNaval.Core.Vision;
using DevAncientNaval.Core.World;

internal static class Rules0202Checks
{
    internal static int Run(BattleRules rules)
    {
        int checks = 0;
        void Check(bool value, string message)
        {
            if (!value) throw new Exception("0.20.2 rules: " + message);
            checks++;
        }
        var mother = new Ship(1, Side.Player, rules.Get(ShipClass.Mothership), new(2,2));
        for (int level = 1; level <= 4; level++)
        {
            mother.Level = level;
            Check(mother.ResourcesRequired == level + 2, "each level needs one additional resource");
        }
        var board = new GameBoard(24,24, p => p.X is >= 8 and <= 12 && p.Y is >= 3 and <= 20 ? TerrainType.Land : TerrainType.Water, 71);
        var mountain = TerrainFeatures.For(board).MountainCells.OrderBy(p => p.X).ThenBy(p => p.Y).First();
        var from = new GridPosition(7,mountain.Y);
        var behind = new GridPosition(13,mountain.Y);
        var observer = new Ship(1,Side.Player,rules.Get(ShipClass.Mothership) with { VisualRange = 10, RadarRange = 10 },from) { HasRadar = true };
        var enemy = new Ship(2,Side.Enemy,rules.Get(ShipClass.Invader),behind);
        var vision = new BattleVision(board,true,true);
        vision.Recompute(new[] {observer,enemy},1);
        Check(!vision.IsOpticallyVisible(Side.Player,behind) && vision.IsRadarContact(Side.Player,behind), "mountains cast optical shadow while radar passes through");
        Check(vision.IsOpticallyVisible(Side.Player,mountain), "the first mountain itself remains visible");
        Check(vision.IsOpticallyVisible(Side.Player,new(6,mountain.Y)), "open sea beside the observer remains visible");
        var historic = new BattleVision(board);
        historic.Recompute(new[] {observer,enemy},1);
        Check(historic.IsVisible(Side.Player,behind), "optional missing mountain policy preserves historical sight");
        var organic = ArchipelagoGenerator.Create(731,1,WorldKind.Pangaea);
        var organicMountain = TerrainFeatures.For(organic).MountainCells.First();
        var shoreObserver = organic.Tiles.Where(t=>t.Terrain!=TerrainType.Land)
            .MinBy(t=>System.Numerics.Vector2.DistanceSquared(organic.Center(t.Position),organic.Center(organicMountain)))!.Position;
        var organicShip = new Ship(7,Side.Player,rules.Get(ShipClass.Mothership) with { VisualRange=8 },shoreObserver);
        var actualSight = new BattleVision(organic,true,true);
        var oldSight = new BattleVision(organic);
        actualSight.Recompute(new[] {organicShip},1); oldSight.Recompute(new[] {organicShip},1);
        Check(organic.Tiles.Any(t=>oldSight.IsVisible(Side.Player,t.Position) && !actualSight.IsVisible(Side.Player,t.Position)),
            "deformed organic cell polygons cast real shadows rather than address-row shadows");
        Check(organic.Tiles.All(t=>!actualSight.IsVisible(Side.Player,t.Position) || oldSight.IsVisible(Side.Player,t.Position)),
            "polygon occlusion can remove coverage but never expand the original radius");
        var city = new Village(3,new(8,mountain.Y)) { Owner = Side.Player, IsFortified = true };
        vision.Recompute(new[] {enemy},2,new[] {city});
        Check(!vision.IsOpticallyVisible(Side.Player,behind), "town vision also respects mountain shadows");
        var sea = new GameBoard(24,24,_=>TerrainType.Water);
        observer.Position = new(3,3);
        var brig = new Ship(3,Side.Enemy,rules.Get(ShipClass.Garrison),new(7,3));
        var fishing = new Ship(4,Side.Enemy,rules.Get(ShipClass.Fishing),new(7,4));
        observer = new Ship(1,Side.Player,rules.Get(ShipClass.Mothership),new(3,3)) { HasRadar = true };
        enemy.Position = new(7,2);
        vision = new BattleVision(sea,true,true);
        vision.Recompute(new[] {observer,brig,fishing,enemy},1);
        Check(!vision.IsRadarContact(Side.Player,brig.Position) && !vision.IsRadarContact(Side.Player,fishing.Position)
            && vision.IsRadarContact(Side.Player,enemy.Position), "only tiny hulls evade radar, ordinary ships remain contacts");
        brig.Position = new(4,3);
        vision.Recompute(new[] {observer,brig,fishing,enemy},2);
        Check(vision.IsOpticallyVisible(Side.Player,brig.Position), "stealth Brig remains visible in actual sight");

        var battle = new BattleState(new GameBoard(24,24,p=>p==new GridPosition(10,10)?TerrainType.Land:TerrainType.Water),rules,
            new[] {(Side.Player,ShipClass.Mothership,new GridPosition(2,2)),(Side.Enemy,ShipClass.Mothership,new GridPosition(21,21))},
            Array.Empty<GridPosition>(),villageSpots:new[] {new GridPosition(10,10)});
        battle.Villages[0].Owner=Side.Player;
        for (int round=2; round<=4; round++)
        {
            Check(battle.EndTurn(Side.Player).Success,"advance player clock");
            var turn=battle.EndTurn(Side.Enemy);
            Check(turn.Success && turn.HeavenlyReceipts!.Count==0,"no blessing before fifth personal start");
        }
        string saved=battle.SaveJson();
        battle=BattleState.LoadJson(saved);
        Check(battle.SaveJson()==saved && battle.PersonalTurnStarts(Side.Player)==4,"personal clock survives Continue exactly");
        battle.EndTurn(Side.Player);
        int money=battle.Credits(Side.Player);
        long lifetime=battle.Statistics.CurrencyEarned;
        var blessing=battle.EndTurn(Side.Enemy);
        var receipt=blessing.HeavenlyReceipts!.Single();
        Check(receipt.Owner==Side.Player && receipt.IsReligiousBlessing && receipt.Amount==4 && receipt.Beneficiaries==2,
            "fifth start grants two per living captured town and flagship");
        Check(battle.Credits(Side.Player)==money+battle.Income(Side.Player)+4
            && battle.Statistics.CurrencyEarned==lifetime+battle.GrossIncome(Side.Player)+4,"blessing enters currency and gross lifetime ledger once");
        saved=battle.SaveJson(); battle=BattleState.LoadJson(saved);
        Check(battle.SaveJson()==saved && !battle.EndTurn(Side.Enemy).Success,"Continue and rejected duplicate start cannot repay blessing");
        battle=new BattleState(sea,rules,new[] {
            (Side.Player,ShipClass.Mothership,new GridPosition(2,2)),
            (Side.Enemy,ShipClass.Mothership,new GridPosition(20,20)),
            (Side.Enemy2,ShipClass.Mothership,new GridPosition(10,8)),
            (Side.Enemy,ShipClass.Kolonel,new GridPosition(9,8)),
            (Side.Enemy2,ShipClass.Garrison,new GridPosition(15,15))},Array.Empty<GridPosition>(),villageSpots:Array.Empty<GridPosition>());
        battle.Find(3)!.Health=1;
        battle.EndTurn(Side.Player);
        var staged=battle.Prepare(b=>b.Attack(Side.Enemy,4,3));
        Check(staged.Result.Success && battle.FlagshipsDestroyedBy(Side.Enemy)==0,"kill bounty clock waits for projectile impact");
        staged.Impact("attack");
        Check(battle.FlagshipsDestroyedBy(Side.Enemy)==1,"direct rival flagship kill is registered at impact");
        staged.Impact("attack"); staged.Finish();
        Check(battle.FlagshipsDestroyedBy(Side.Enemy)==1 && battle.Find(5) is null,"repeat impact and automatic follower scuttling add no flagship kills");
        battle.EndTurn(Side.Enemy);
        while (battle.PersonalTurnStarts(Side.Enemy)<4)
        {
            battle.EndTurn(Side.Player); battle.EndTurn(Side.Enemy);
        }
        battle=BattleState.LoadJson(battle.SaveJson());
        money=battle.Credits(Side.Enemy);
        var assistance=battle.EndTurn(Side.Player);
        receipt=assistance.HeavenlyReceipts!.Single();
        Check(receipt.Owner==Side.Enemy && !receipt.IsReligiousBlessing && receipt.Amount==2
            && battle.Credits(Side.Enemy)==money+battle.Income(Side.Enemy)+2,"rival fifth start pays for persisted direct flagship destruction");
        var invalid=battle.CaptureSnapshot(); invalid.FlagshipKills[(int)Side.Pirates]=1;
        bool rejected=false;
        try { BattleState.LoadJson(BattleState.SerializeSnapshot(invalid)); } catch (ArgumentException) { rejected=true; }
        Check(rejected,"invalid pirate assistance counters rejected");
        invalid=battle.CaptureSnapshot(); invalid.FlagshipKills[(int)Side.Enemy4]=1;
        rejected=false;
        try { BattleState.LoadJson(BattleState.SerializeSnapshot(invalid)); } catch (ArgumentException) { rejected=true; }
        Check(rejected,"assistance counters outside the saved roster rejected");
        var legacy=JsonNode.Parse(battle.SaveJson())!.AsObject();
        legacy.Remove("PersonalTurnStarts"); legacy.Remove("FlagshipKills");
        foreach (var field in new[] {"MountainSightShadows","SmallHullRadarStealth","HeavenlyAssistance","ThemedWorldNames"})
            legacy["Rules"]!.AsObject().Remove(field);
        foreach (var definition in legacy["Rules"]!["Ships"]!.AsArray())
            definition!.AsObject().Remove("ResourceRequirementIncrease");
        var restored=BattleState.LoadJson(legacy.ToJsonString());
        Check(!restored.Rules.HeavenlyAssistance && !restored.Rules.MountainSightShadows && !restored.Rules.SmallHullRadarStealth
            && restored.Mothership(Side.Player)!.ResourcesRequired==2,"missing optional rules keep old thresholds and policies");
        Check(restored.Credits(Side.Enemy)==battle.Credits(Side.Enemy),"opening old data creates no invented heavenly payment");
        return checks;
    }
}
