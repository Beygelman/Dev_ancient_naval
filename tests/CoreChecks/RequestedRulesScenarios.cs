using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Units;
using DevAncientNaval.Core.World;

internal static partial class BattleScenarios
{
    private static void RequestedRules()
    {
        ExactHullBalance();
        UpgradeAlternatives();
        TransitAndCollection();
        BalloonLifecycle();
        VillageLifecycle();
    }

    private static void ExactHullBalance()
    {
        foreach (var (kind, hp) in new[]
        {
            (ShipClass.Mothership, 20), (ShipClass.Kolonel, 15), (ShipClass.Invader, 10),
            (ShipClass.Garrison, 5), (ShipClass.Togus, 5), (ShipClass.Fishing, 5),
            (ShipClass.FishingDock, 10)
        }) Check(Rules.Get(kind).MaxHealth == hp, $"Requested hull for {kind}");
        var mother = Fixture().Find(1)!;
        for (int level = 1; level <= 5; level++)
        {
            mother.Level = level;
            Check(mother.MaxHealth == 20 + 5 * (level - 1), "Each mothership level adds five HP");
        }
    }

    private static Ship QueueUpgrade(BattleState battle, int level)
    {
        var mother = battle.Find(1)!;
        mother.Level = level;
        mother.Health = mother.MaxHealth;
        mother.PendingUpgradeLevel = level;
        return mother;
    }

    private static void UpgradeAlternatives()
    {
        var b = Fixture();
        var mother = QueueUpgrade(b, 2);
        int credits = b.Credits(Side.Player);
        int income = b.Income(Side.Player);
        Check(b.ChooseUpgrade(Side.Player, mother.Id, UpgradeChoice.FishingBoat).Success,
            "Fishing alternative creates a boat");
        var fishing = b.OwnShips(Side.Player).Single(s => s.Definition.Class == ShipClass.Fishing);
        Check(fishing.Health == 5 && b.Credits(Side.Player) == credits && b.Income(Side.Player) == income + 2,
            "Bonus fishing boat is free and earns its normal income");
        var lake = new GridPosition(4, 4);
        var land = new HashSet<GridPosition> { new(3,4), new(4,3), new(5,4), new(4,5) };
        b = new BattleState(new GameBoard(20,20,p => land.Contains(p) ? TerrainType.Land : TerrainType.Water),
            Funded, new[] {
                (Side.Player,ShipClass.Mothership,new GridPosition(5,5)),
                (Side.Enemy,ShipClass.Mothership,new GridPosition(18,18)),
                (Side.Player,ShipClass.Garrison,new GridPosition(6,5)),
                (Side.Player,ShipClass.Garrison,new GridPosition(5,6))
            }, Array.Empty<GridPosition>());
        QueueUpgrade(b,2);
        Check(b.ChooseUpgrade(Side.Player,1,UpgradeChoice.FishingBoat).Success &&
            b.OwnShips(Side.Player).Single(s => s.Definition.Class == ShipClass.Fishing).Position != lake,
            "Fishing reward finds connected water when the nearest empty tile is an isolated lake");

        foreach (bool radarFirst in new[] { false, true })
        {
            b = Fixture(); mother = b.Find(1)!;
            if (radarFirst) Check(b.BuyRadar(Side.Player, 1).Success, "Install radar before sight upgrade");
            QueueUpgrade(b, 3);
            Check(b.ChooseUpgrade(Side.Player, 1, UpgradeChoice.Vision).Success && mother.VisualRange == 4,
                "Sight upgrade adds two visual tiles");
            if (!radarFirst)
            {
                Check(mother.RadarRange == 0, "Sight upgrade does not grant free radar");
                Check(b.BuyRadar(Side.Player, 1).Success, "Install radar after sight upgrade");
            }
            Check(mother.RadarRange == 7, "Sight bonus applies to existing and future radar");
        }

        b = Fixture(); mother = QueueUpgrade(b, 3); mother.Health = 20;
        Check(b.ChooseUpgrade(Side.Player, 1, UpgradeChoice.Restoration).Success &&
            mother.MaxHealth == 35 && mother.Health == 25, "Hull upgrade preserves existing damage");
        Check(b.EndTurn(Side.Player).Success && mother.Health == 27, "Unused repair heals two at turn end");
        Check(b.EndTurn(Side.Enemy).Success && mother.Health == 29 && mother.CanMove,
            "Passive repair restores two without consuming actions");
        mother.Health = 34; Round(b);
        Check(mother.Health == 35, "Passive repair never exceeds maximum hull");

        b = Fixture(); mother = QueueUpgrade(b, 4);
        Check(b.ChooseUpgrade(Side.Player, 1, UpgradeChoice.SecondAttack).Success &&
            mother.AttacksRemaining == 2, "Level four can grant one extra shot");
        b = Fixture(); mother = QueueUpgrade(b, 5);
        Check(b.UpgradeOptions(1).SequenceEqual(new[] { UpgradeChoice.Shipwright, UpgradeChoice.Firepower }),
            "Level five offers the final two upgrades");
        Check(b.ChooseUpgrade(Side.Player, 1, UpgradeChoice.Firepower).Success,
            "Firepower upgrade is available at level five");
        var target = b.Find(4)!; target.Position = new(6, 5);
        Check(b.Damage(mother, target) == 6 && b.Damage(mother, target, true) == 5,
            "Firepower separately adds three to shots and two to counterattacks");
    }

    private static void TransitAndCollection()
    {
        var corridor = new GameBoard(20, 20, p =>
            p.Y == 8 || p.X < 4 || p.X > 15 ? TerrainType.Water : TerrainType.Land);
        var b = new BattleState(corridor, Funded, new[]
        {
            (Side.Player, ShipClass.Mothership, new GridPosition(1, 1)),
            (Side.Enemy, ShipClass.Mothership, new GridPosition(18, 18)),
            (Side.Player, ShipClass.Garrison, new GridPosition(8, 8)),
            (Side.Player, ShipClass.Garrison, new GridPosition(9, 8)),
            (Side.Enemy, ShipClass.Garrison, new GridPosition(10, 8))
        }, Array.Empty<GridPosition>());
        Check(b.PathTo(3,new(11,8)).Count==0, "Enemy blocks the single-width channel");
        Check(b.StepCost(3,new(8,8),new(9,8))==20,"Enemy-adjacent cell costs two movement points");
        Check(!b.Move(Side.Player,3,new(9,8)).Success,"Allied ships permit transit, not overlapping destinations");
        Check(!b.Move(Side.Player,3,new(10,8)).Success,"Enemy-occupied tile cannot be entered");
        b.Find(5)!.Position=new(18,16); b.Vision.Recompute(b.Ships,b.Round);
        Check(b.PathTo(3,new(11,8)).Contains(new(9,8)),"Route passes through an allied ship");
        Check(b.Move(Side.Player,3,new(11,8)).Success && b.Find(3)!.MovementSpentUnits==30,"Friendly transit has no extra cost");
        Check(b.Find(4)!.Position==new GridPosition(9,8),"Transit does not displace the ally");

        var resource = new GridPosition(9, 8);
        b = Fixture(ShipClass.Fishing, fish: new[] { resource, new GridPosition(0, 19) });
        Check(b.CollectionCells(Side.Player).Contains(resource), "Side collection discovers any nearby collector");
        Check(b.Collect(Side.Player, resource).Success && b.Find(1)!.Resources == 1,
            "Collection works without a selected ship identifier");
        Check(!b.Collect(Side.Player, new GridPosition(0, 19)).Success,
            "Selection-free collection still enforces range and visibility");
        b.EndTurn(Side.Player);
        Check(b.CollectionCells(Side.Player).Count == 0, "Collection unavailable outside owner's turn");
    }

    private static BattleState BalloonArena(bool blastGrid)
    {
        var setup = new List<(Side, ShipClass, GridPosition)>
        {
            (Side.Player, ShipClass.Mothership, new(1, 1)),
            (Side.Enemy, ShipClass.Mothership, new(18, 18)),
            (Side.Player, ShipClass.Balloon, new(8, 8))
        };
        if (blastGrid)
        {
            for (int y = 7; y <= 9; y++) for (int x = 7; x <= 9; x++)
                setup.Add(((x + y) % 2 == 0 ? Side.Player : Side.Enemy, ShipClass.Garrison, new(x, y)));
            setup.Add((Side.Enemy, ShipClass.Garrison, new(10, 8)));
        }
        else setup.Add((Side.Enemy, ShipClass.Kolonel, new(9, 8)));
        return new BattleState(new GameBoard(20, 20, _ => TerrainType.Water), Funded,
            setup, Array.Empty<GridPosition>());
    }

    private static void BalloonLifecycle()
    {
        var b=BalloonArena(false); var balloon=b.Find(3)!;
        Check(balloon.VisualRange==8 && balloon.MovementAllowance==Rules.Get(ShipClass.Invader).Movement,"Balloon sight and Galleon movement");
        Check(!b.DropBomb(Side.Player,3).Success,"Balloon moves before bombing");
        Check(b.Move(Side.Player,3,new(9,8)).Success && b.DropBomb(Side.Player,3).Success,"Balloon bombs below after movement");
        Check(b.Find(4)!.Health==9 && balloon.BombCooldown==3 && !b.DropBomb(Side.Player,3).Success,"Direct damage six and immediate repeat blocked");
        b.EndTurn(Side.Player);
        Check(!b.CanAttack(4,3) && !b.Attack(Side.Enemy,4,3).Success,"Balloon cannot be attacked");
        b.EndTurn(Side.Enemy);
        Check(balloon.BombCooldown==2,"First owner turn of recharge");
        Round(b); Check(balloon.BombCooldown==1 && !b.CanDropBomb(3),"Second recharge turn cannot bomb");
        Round(b); Check(balloon.BombCooldown==0 && b.Find(3)==balloon,"Third recharge turn retains balloon");
        Check(b.Move(Side.Player,3,new(8,8)).Success && b.DropBomb(Side.Player,3).Success,"Recharged balloon can bomb again");
        for(int i=0;i<7;i++) Round(b);
        Check(b.Find(3)==balloon,"Balloon persists beyond ten turns");

        b=BalloonArena(true);
        Check(b.Move(Side.Player,3,new(8,7)).Success && b.Move(Side.Player,3,new(8,8)).Success,"Air can cross occupied water");
        var blast=b.DropBomb(Side.Player,3);
        Check(blast.Success && blast.Shots!.Count==9,"Bomb affects center and eight adjacent cells");
        Check(b.At(new(8,8)) is null && b.Ships.Where(s=>s.Id is >=4 and <=12).All(s=>s.Health==3),"Six direct and two splash damage include friendly fire");
        Check(b.Find(13)!.Health==5,"Splash excludes second ring");

        b=new BattleState(new GameBoard(20,20,_=>TerrainType.Water),Funded,new[]{
            (Side.Player,ShipClass.Mothership,new GridPosition(7,8)),
            (Side.Enemy,ShipClass.Mothership,new GridPosition(9,8)),
            (Side.Player,ShipClass.Balloon,new GridPosition(8,7))},Array.Empty<GridPosition>());
        b.Find(1)!.Health=2; b.Find(2)!.Health=2;
        b.Move(Side.Player,3,new(8,8)); b.DropBomb(Side.Player,3);
        Check(b.IsOver && b.IsDraw && b.Winner is null,"Simultaneous splash sinking both flagships is a draw");
    }
}
