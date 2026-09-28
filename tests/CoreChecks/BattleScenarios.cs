using DevAncientNaval.Core.AI;
using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Units;
using DevAncientNaval.Core.World;
using DevAncientNaval.Core.Vision;
using DevAncientNaval.Core.Economy;

internal static class BattleScenarios
{
    private static int _checks;
    private static readonly BattleRules Rules = BattleRules.FromJson(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "balance.json")));
    private static void Check(bool ok, string name) { if (!ok) throw new Exception("FAIL: " + name); _checks++; }
    private static bool Near(double a, double b) => Math.Abs(a - b) < 0.001;
    private static BattleRules With(Func<ShipDefinition, ShipDefinition> change) => new()
    {
        StartingCredits = Rules.StartingCredits, IncomePerMothership = Rules.IncomePerMothership,
        RepairAmount = Rules.RepairAmount, FleetLimit = Rules.FleetLimit, Ships = Rules.Ships.Select(change).ToArray()
    };
    private static BattleState Duel(ShipClass kind = ShipClass.Invader, ShipClass defender = ShipClass.Kolonel,
        GridPosition? target = null, Func<GridPosition, TerrainType>? terrain = null, BattleRules? rules = null) => new(
        new GameBoard(20, 20, terrain ?? (_ => TerrainType.Water)), rules ?? Rules,
        new (Side, ShipClass, GridPosition)[] {
            (Side.Player, ShipClass.Mothership, new(0, 0)), (Side.Enemy, defender == ShipClass.Mothership ? ShipClass.Fishing : ShipClass.Mothership, new(19, 19)),
            (Side.Player, kind, new(7, 7)), (Side.Enemy, defender, target ?? new(9, 7)) });
    private static void NextRound(BattleState b)
    {
        Check(b.EndTurn(Side.Player).Success, "End player turn");
        Check(b.EndTurn(Side.Enemy).Success, "End enemy turn");
    }
    public static void Run()
    {
        Combat(); Navigation(); Vision(); Economy(); Opponent();
        Console.WriteLine($"PASS: {_checks} battle/exploration checks; 8 complete AI matches.");
    }

    private static void Combat()
    {
        var b = Duel(); var a = b.Find(3)!; var d = b.Find(4)!;
        a.Health = a.MaxHealth / 2;
        Check(Near(a.CurrentDamage, a.Definition.Damage * 0.75), "Half HP gives 75% damage before armor");
        Check(Near(b.Damage(a, d), 2.5), "Armor follows health modifier");
        a.Health = a.MaxHealth / 4;
        Check(a.MovementAllowance == 4, "Exactly 25% HP has full movement");
        a.Health -= 0.01; a.MovementSpentUnits = 20;
        Check(a.MovementAllowance == 3 && a.MovementRemainingUnits == 10, "Below 25% removes one point from shared budget");
        a.Health = a.MaxHealth; a.MovementSpentUnits = 0;
        double predicted = b.PreviewCounterDamage(a, d);
        var attack = b.Attack(Side.Player, 3, 4);
        Check(attack.Success && attack.Shots!.Count == 2, "One attack plus exactly one reply");
        Check(!attack.Shots![0].IsCounterattack && attack.Shots[1].IsCounterattack, "Reply order");
        Check(Near(attack.Shots[1].Damage, predicted), "Preview uses defender HP after primary hit");
        Check(a.Health < a.MaxHealth && d.Health < d.MaxHealth, "Both sides take damage");
        Check(d.AttacksUsed == 0 && !d.HasMoved, "Reply does not spend defender turn actions");

        b = Duel(ShipClass.Kolonel); d = b.Find(4)!;
        Check(b.Attack(Side.Player, 3, 4).Shots!.Count == 2, "First incoming shot countered");
        Check(b.Attack(Side.Player, 3, 4).Shots!.Count == 2, "Second incoming shot countered in same turn");
        Check(!b.Attack(Side.Player, 3, 4).Success, "Heavy cannot fire a third normal attack");
        Check(d.AttacksRemaining == 2, "Replies do not use heavy's two normal attacks");
        Check(!b.Move(Side.Player, 3, new(7, 8)).Success, "Heavy cannot move after firing");

        foreach (var kind in Enum.GetValues<ShipClass>().Where(k => k != ShipClass.Fishing))
        {
            b = Duel(defender: kind, target: new(8, 7));
            b.Find(4)!.IsExhausted = true; b.Find(4)!.AttacksUsed = 2;
            Check(b.Attack(Side.Player, 3, 4).Shots!.Count == 2, $"Every surviving armed class replies even exhausted: {kind}");
        }
        b = Duel(defender: ShipClass.Fishing);
        Check(b.Attack(Side.Player, 3, 4).Shots!.Count == 1, "Fishing never counterattacks");
        b = Duel(ShipClass.Fishing);
        Check(!b.Attack(Side.Player, 3, 4).Success && b.AttackCells(3).Count == 0, "Fishing cannot attack");
        b = Duel(ShipClass.Kolonel, ShipClass.Garrison, new(10, 7));
        Check(b.Attack(Side.Player, 3, 4).Shots!.Count == 1, "Out-of-range defender cannot counter");
        b = Duel(); b.Find(4)!.Health = 1;
        Check(b.Attack(Side.Player, 3, 4).Shots!.Count == 1 && b.Find(4) is null, "Sunk defender cannot counter");

        foreach (var profile in new[] { ShipClass.Garrison, ShipClass.Invader, ShipClass.Kolonel })
        {
            b = Duel(profile, ShipClass.Fishing);
            Check(b.Move(Side.Player, 3, new(8, 7)).Success, "Move before attack");
            Check(b.Attack(Side.Player, 3, 4).Success, "Attack after move");
            Check(b.Move(Side.Player, 3, new(8, 8)).Success == (profile == ShipClass.Garrison), "Movement after attack profile");
            Check(b.Find(3)!.AttacksRemaining == 0, "One attack when moved");
        }
        b = Duel(ShipClass.Invader, ShipClass.Fishing);
        Check(b.Attack(Side.Player, 3, 4).Success && b.Move(Side.Player, 3, new(7, 8)).Success, "Standard attack then move");
        Check(b.Move(Side.Player, 3, new(7, 9)).Success, "Standard can split remaining movement");

        b = new BattleState(new GameBoard(20, 20, _ => TerrainType.Water), Rules,
            new (Side, ShipClass, GridPosition)[] {
                (Side.Player, ShipClass.Mothership, new(0, 0)), (Side.Enemy, ShipClass.Mothership, new(19, 19)),
                (Side.Player, ShipClass.Kolonel, new(7, 7)),
                (Side.Enemy, ShipClass.Fishing, new(8, 7)), (Side.Enemy, ShipClass.Fishing, new(7, 8)),
                (Side.Enemy, ShipClass.Fishing, new(6, 7)), (Side.Enemy, ShipClass.Fishing, new(7, 6)) });
        a = b.Find(3)!; a.Health = 10;
        foreach (int id in new[] { 4, 5, 6, 7 }) b.Find(id)!.Health = 1;
        b.Attack(Side.Player, 3, 4); b.Attack(Side.Player, 3, 5);
        Check(a.Kills == 2 && !a.IsVeteran && a.Health == 10, "No early veteran bonus");
        NextRound(b);
        Check(b.Attack(Side.Player, 3, 6).Shots![0].Promoted, "Third actual kill promotes");
        Check(a.IsVeteran && a.Kills == 3 && Near(a.MaxHealth, 28.75) && Near(a.Health, 28.75) && a.FullDamage == 10, "Veteran heals and grants exact 25% HP/damage");
        a.Health = 15;
        Check(!b.Attack(Side.Player, 3, 7).Shots![0].Promoted && a.Health == 15 && a.MaxHealth == 28.75, "Bonus and healing never stack on fourth kill");
        NextRound(b); Check(b.Repair(Side.Player, 3).Success && a.Health == 19, "Veteran repair respects new max");
        Check(!a.CanMove && a.AttacksRemaining == 0, "Repair uses actions");
        b = Duel(ShipClass.Garrison); a = b.Find(3)!; d = b.Find(4)!;
        a.Health = 1; d.Kills = 2; d.Health = 10;
        attack = b.Attack(Side.Player, 3, 4);
        Check(attack.Shots!.Count == 2 && attack.Shots[1].Promoted && b.Find(3) is null && d.Health == d.MaxHealth, "Counter kill can promote and heal");
        foreach (var winningSide in Enum.GetValues<Side>())
        {
            b = new BattleState(new GameBoard(8, 8, _ => TerrainType.Water), Rules,
                new (Side, ShipClass, GridPosition)[] { (Side.Player, ShipClass.Mothership, new(3, 3)), (Side.Enemy, ShipClass.Mothership, new(4, 3)) });
            if (winningSide == Side.Enemy) b.EndTurn(Side.Player);
            int target = winningSide == Side.Player ? 2 : 1, actor = 3 - target;
            b.Find(target)!.Health = 1;
            Check(b.Attack(winningSide, actor, target).Success && b.Winner == winningSide, "Mothership loss decides winner");
            Check(!b.EndTurn(winningSide).Success && !b.Move(winningSide, actor, new(3, 4)).Success, "Battle ends immediately");
        }
    }

    private static void Navigation()
    {
        var b = Duel(ShipClass.Garrison, target: new(17, 17));
        Check(b.StepCost(3, new(7, 7), new(8, 8)) == 14, "Diagonal costs 1.4");
        Check(b.Reachable(3).ContainsKey(new(10, 10)) && !b.Reachable(3).ContainsKey(new(11, 11)), "Rounded movement area");
        var route = b.PathTo(3, new(10, 10));
        Check(Near(b.PathCost(3, route), 4.2) && b.Move(Side.Player, 3, new(10, 10)).Success && Near(b.Find(3)!.MovementRemaining, 0.8), "Weighted route consumes actual budget");
        Check(!b.Find(3)!.CanMove, "Fraction smaller than one cannot move");

        TerrainType Coast(GridPosition p) => p == new GridPosition(8, 6) ? TerrainType.Land : TerrainType.Water;
        foreach (var (kind, cost) in new[] { (ShipClass.Garrison,10), (ShipClass.Invader,20), (ShipClass.Kolonel,30), (ShipClass.Fishing,10) })
        {
            b = Duel(kind, target: new(17, 17), terrain: Coast);
            Check(b.Board.GetTile(new(8, 7)).Terrain == TerrainType.Coast && b.StepCost(3, new(7, 7), new(8, 7)) == cost, $"Coastal penalty: {kind}");
        }
        TerrainType Narrow(GridPosition p) => p == new GridPosition(6, 8) || p == new GridPosition(8, 8) ? TerrainType.Land : TerrainType.Water;
        b = Duel(ShipClass.Garrison, target: new(17,17), terrain: Narrow);
        Check(b.Board.IsNarrowPassage(new(7,8)) && b.StepCost(3,new(7,7),new(7,8))==10,"Light enters one-cell strait freely");
        b = Duel(ShipClass.Kolonel,target:new(17,17),terrain:Narrow);
        Check(b.StepCost(3,new(7,7),new(7,8))==30 && b.Move(Side.Player,3,new(7,8)).Success && b.Find(3)!.MovementRemaining==0,"Heavy spends whole turn entering strait");
        b = new BattleState(new GameBoard(20,20,Narrow), Rules, new (Side,ShipClass,GridPosition)[] {
            (Side.Player,ShipClass.Mothership,new(7,7)),(Side.Enemy,ShipClass.Mothership,new(19,19)) });
        Check(b.StepCost(1,new(7,7),new(7,8)) is null && !b.PathTo(1,new(7,8)).Any(),"Mother cannot enter one-cell strait");
        b = Duel(ShipClass.Garrison,target:new(17,17),terrain:p=>p==new GridPosition(8,7)?TerrainType.Land:TerrainType.Water);
        Check(b.StepCost(3,new(7,7),new(8,8)) is null,"No diagonal cutting through island corner");
        Check(b.Move(Side.Player,3,new(9,7)).Success,"Weighted detour goes around island");
        Check(!b.Move(Side.Enemy,3,new(9,8)).Success && !b.Move(Side.Player,4,new(17,18)).Success,"Ownership enforced");
        Check(!b.Move(Side.Player,3,new(-1,0)).Success,"Board bounds enforced");

        var shortSight = With(s => s with { VisualRange = 1, RadarRange = 0 });
        var clear = Duel(ShipClass.Garrison,target:new(17,17),rules:shortSight);
        var island = Duel(ShipClass.Garrison,target:new(17,17),rules:shortSight,
            terrain:p=>p.X==10&&p.Y==7?TerrainType.Land:TerrainType.Water);
        Check(clear.PathTo(3,new(12,7)).SequenceEqual(island.PathTo(3,new(12,7))),"Unknown terrain never leaks through path preview");
        var move = island.Move(Side.Player,3,new(12,7));
        Check(move.Success && island.Find(3)!.Position==new GridPosition(9,7),"Exploration stops before newly found land");
        Check(island.Vision.IsExplored(Side.Player,new(10,7)),"New island is discovered");
        Check(move.Path!.All(p=>island.Board.GetTile(p).Terrain!=TerrainType.Land),"Executed path remains legal");
        var hidden = Duel(ShipClass.Garrison,target:new(10,7),rules:shortSight);
        Check(clear.PathTo(3,new(12,7)).SequenceEqual(hidden.PathTo(3,new(12,7))),"Hidden ship never leaks through path preview");
        Check(hidden.Move(Side.Player,3,new(12,7)).Success && hidden.Find(3)!.Position==new GridPosition(9,7),"Exploration stops before unseen occupied tile");
    }

    private static void Vision()
    {
        var b = Duel(ShipClass.Garrison, target:new(13,7));
        Check(b.Vision.State(Side.Player,new(13,7))==VisibilityState.RadarContact,"Radar detects beyond sight");
        Check(b.FindObserved(Side.Player,4) is null && b.Vision.KnownTerrain(Side.Player,new(13,7)) is null,"Radar reveals neither class nor terrain");
        Check(!b.CanAttack(3,4),"Radar alone cannot be fired upon");
        Check(b.Vision.IsVisible(Side.Player,new(10,11)) && !b.Vision.IsVisible(Side.Player,new(11,11)),"Optical sight is Euclidean circle");
        Check(b.Vision.KnownTerrain(Side.Player,new(19,19)) is null,"Enemy spawn remains unknown");
        Check(b.Vision.KnownTerrain(Side.Enemy,new(0,0)) is null,"Separate knowledge for each side");
        b.Move(Side.Player,3,new(9,7));
        Check(b.FindObserved(Side.Player,4) is not null,"Closing distance identifies contact");
        Check(b.Vision.IsExplored(Side.Player,new(3,7)) && !b.Vision.IsVisible(Side.Player,new(3,7)),"Terrain memory persists outside sight");
        b = Duel(ShipClass.Fishing,target:new(10,7));
        Check(b.Vision.Contacts(Side.Player).Count==0 && b.FindObserved(Side.Player,4) is null,"Fishing has no radar");
        Check(b.Vision.IsVisible(Side.Player,new(9,7)) && !b.Vision.IsVisible(Side.Player,new(10,7)),"Fishing short sight");
        b = Duel(ShipClass.Garrison,target:new(13,7));
        b.Find(4)!.Position=new(17,17); b.Vision.Recompute(b.Ships,4);
        Check(b.Vision.Contacts(Side.Player).Count==0,"Moving beyond radar removes contact");
        var rules = With(s => s.Class == ShipClass.Garrison ? s with { VisualRange=1,RadarRange=0 } : s);
        b = Duel(ShipClass.Kolonel,ShipClass.Garrison,new(10,7),rules:rules);
        Check(b.FindObserved(Side.Enemy,3) is null,"Shooter initially unseen to defender");
        b.Attack(Side.Player,3,4);
        Check(b.FindObserved(Side.Enemy,3) is not null,"Muzzle fire reveals shooter to defender");
        b.EndTurn(Side.Player);
        Check(b.FindObserved(Side.Enemy,3) is null && b.Vision.IsExplored(Side.Enemy,new(7,7)),"Combat reveal clears next turn but terrain remains known");
    }

    private static void Economy()
    {
        var b=Duel(); var mother=b.Find(1)!; int funds=b.Credits(Side.Player);
        Check(Rules.Get(ShipClass.Fishing).Price==Rules.Ships.Where(s=>s.Class!=ShipClass.Mothership).Min(s=>s.Price),"Fishing cheapest class");
        Check(!b.Build(Side.Player,1,ShipClass.Fishing,new(4,4)).Success && mother.CanMove && b.Credits(Side.Player)==funds,"Invalid build has no side effects");
        var built=b.Build(Side.Player,1,ShipClass.Fishing,new(1,0));
        Check(built.Success && b.Find(built.TargetId)!.CanMove && !mother.CanMove,"First fisher acts, mother locks movement");
        Check(b.Credits(Side.Player)==funds-20 && b.Income(Side.Player)==33,"Fishing cost and income");
        Check(!b.Build(Side.Player,1,ShipClass.Fishing,new(0,1)).Success,"One production per turn");
        NextRound(b);
        Check(mother.CanMove && b.Credits(Side.Player)==funds-20+33,"Movement restored and income paid once");
        var second=b.Build(Side.Player,1,ShipClass.Fishing,new(0,1));
        Check(second.Success && b.Find(second.TargetId)!.IsExhausted,"Second production waits until next own turn");
        funds=b.Credits(Side.Player);
        Check(!b.EndTurn(Side.Enemy).Success && b.Credits(Side.Player)==funds,"Duplicate turn cannot farm money");
        NextRound(b); Check(b.Find(second.TargetId)!.CanMove,"New fisher wakes next own turn");
        Check(!b.Build(Side.Player,1,ShipClass.Mothership,new(1,1)).Success,"No second mother");
        b.SetIncomeSource(new IncomeSource("station",Side.Player,7));
        Check(b.Income(Side.Player)==48,"Income combines all sources");
        b.RemoveIncomeSource("station"); Check(b.Income(Side.Player)==41,"Source removal");
        b=Duel(ShipClass.Kolonel,ShipClass.Fishing); b.Find(4)!.Health=1;
        Check(b.Income(Side.Enemy)==33,"Enemy fisher income registered");
        b.Attack(Side.Player,3,4);
        Check(b.Income(Side.Enemy)==25 && b.IncomeSources.All(s=>s.BoundShipId!=4),"Sinking removes fisher income");

        b=Duel(); b.Build(Side.Player,1,ShipClass.Kolonel,new(1,0)); NextRound(b); funds=b.Credits(Side.Player);
        Check(!b.Build(Side.Player,1,ShipClass.Invader,new(0,1)).Success && b.Credits(Side.Player)==funds && !b.Find(1)!.HasProduced,"Insufficient funds preserve slot");
        b=Duel(); Check(b.Move(Side.Player,1,new(0,1)).Success && b.Build(Side.Player,1,ShipClass.Fishing,new(0,2)).Success && !b.Find(1)!.CanMove,"Mother may move before production");
    }

    private static void Opponent()
    {
        // Identical observations with a different hidden enemy position must produce the same choice.
        var a=Duel(ShipClass.Garrison,target:new(17,16)); var b=Duel(ShipClass.Garrison,target:new(16,17));
        for(int step=0;step<20&&a.ActiveSide==Side.Player;step++)
        {
            Check(!a.ObservedShips(Side.Player).Any(s=>s.Owner==Side.Enemy) && !b.ObservedShips(Side.Player).Any(s=>s.Owner==Side.Enemy),"AI fixture has no observed enemy");
            var ra=SimpleOpponent.Step(a); var rb=SimpleOpponent.Step(b);
            Check(ra.Kind==rb.Kind && ra.ActorId==rb.ActorId && ra.TargetId==rb.TargetId &&
                a.OwnShips(Side.Player).Select(s=>s.Position).SequenceEqual(b.OwnShips(Side.Player).Select(s=>s.Position)),"AI production and exploration ignore hidden enemy location");
        }
        Check(a.ActiveSide==Side.Enemy && b.ActiveSide==Side.Enemy,"Observation-only AI completes whole turn");
        for(int scenario=0;scenario<8;scenario++)
        {
            int seed=scenario;
            var map=new GameBoard(20,20,p=>p.X>=7&&p.X<=9&&p.Y>=3+seed&&p.Y<=7+seed?TerrainType.Land:TerrainType.Water);
            var match=new BattleState(map,Rules,new (Side,ShipClass,GridPosition)[] {
                (Side.Player,ShipClass.Mothership,new(2,9)),(Side.Player,ShipClass.Garrison,new(4,9)),
                (Side.Player,ShipClass.Invader,new(3,11)),(Side.Player,ShipClass.Kolonel,new(2,7)),
                (Side.Enemy,ShipClass.Mothership,new(17,9)),(Side.Enemy,ShipClass.Garrison,new(15,9)),
                (Side.Enemy,ShipClass.Invader,new(16,7)),(Side.Enemy,ShipClass.Kolonel,new(17,11)) });
            int count=0, turnCount=0;
            while(!match.IsOver&&match.Round<160&&count++<10000)
            {
                var side=match.ActiveSide;
                var result=SimpleOpponent.Step(match);
                Check(result.Success,"AI legal command");
                Check(match.Ships.All(s=>s.Health>0&&s.Health<=s.MaxHealth&&s.MovementRemaining>=0),"Health and movement invariants");
                Check(match.Ships.Select(s=>s.Position).Distinct().Count()==match.Ships.Count,"Unique occupancy");
                Check(match.Ships.All(s=>map.GetTile(s.Position).Terrain!=TerrainType.Land),"Ships remain at sea");
                if(side!=match.ActiveSide) turnCount=0; else Check(++turnCount<256,"AI turn finishes within budget");
            }
            Check(match.IsOver,$"AI match {scenario} finishes within 160 rounds");
            Console.WriteLine($"AI scenario {scenario}: winner {match.Winner}, round {match.Round}, commands {count}");
        }
    }
}
