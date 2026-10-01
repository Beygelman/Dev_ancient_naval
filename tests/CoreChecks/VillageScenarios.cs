using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Units;
using DevAncientNaval.Core.World;

internal static partial class BattleScenarios
{
    private static BattleState VillageArena(ShipClass visitor = ShipClass.Garrison,
        ShipClass opponent = ShipClass.Kolonel, GridPosition? opponentPosition = null)
    {
        var village = new GridPosition(9, 8);
        return new BattleState(new GameBoard(20, 20, p => p == village ? TerrainType.Land : TerrainType.Water),
            Funded, new[]
            {
                (Side.Player, ShipClass.Mothership, new GridPosition(1, 1)),
                (Side.Enemy, ShipClass.Mothership, new GridPosition(18, 18)),
                (Side.Player, visitor, new GridPosition(8, 8)),
                (Side.Enemy, opponent, opponentPosition ?? new GridPosition(12, 8))
            }, Array.Empty<GridPosition>(), villageSpots: new[] { village });
    }

    private static void VillageLifecycle()
    {
        var b = VillageArena(); var village = b.Villages.Single();
        Check(village.Owner is null && village.Level == 1 && village.Health == 5 && village.MaxHealth == 5,
            "Coastal villages begin neutral with five HP");
        Check(!b.CanCaptureVillage(Side.Player, village.Id) && !b.CaptureVillage(Side.Player, village.Id).Success,
            "Arriving beside a village does not capture it immediately");
        Round(b);
        Check(!b.CanCaptureVillage(Side.Player, village.Id), "Waiting never bypasses healthy defenses");
        Check(b.CanAttackVillage(3,village.Id) && b.AttackVillage(Side.Player,3,village.Id).Success && village.Health==2,
            "Neutral town defenses can be attacked");
        Round(b);
        Check(b.AttackVillage(Side.Player,3,village.Id).Success && village.Health==0 && b.VillageAt(village.Position)==village,
            "Lethal damage leaves the town on the map at zero HP");
        Check(!b.CanCaptureVillage(Side.Player,village.Id) && !b.CanAttackVillage(3,village.Id),
            "Defeated town waits a turn and cannot be damaged further");
        Round(b); Check(b.CanCaptureVillage(Side.Player,village.Id),"Holding alongside unlocks capture next turn");
        Check(b.Move(Side.Player,3,new(7,8)).Success && !b.CanCaptureVillage(Side.Player,village.Id),
            "Capturing still requires an adjacent naval ship");
        Check(b.Move(Side.Player,3,new(8,8)).Success && !b.CanCaptureVillage(Side.Player,village.Id),
            "Moving away and returning restarts capture wait");
        Round(b);
        int credits = b.Credits(Side.Player);
        Check(b.CaptureVillage(Side.Player, village.Id).Success && village.Owner == Side.Player &&
            b.Credits(Side.Player) == credits && b.Income(Side.Player) == 3 && village.Health==5,
            "Clicking the ready flag captures freely and adds one income");
        Check(!b.CaptureVillage(Side.Player, village.Id).Success, "Owned villages cannot be captured repeatedly");

        village.Health = 4;
        Round(b);
        Check(village.Level == 1 && village.Health == 5 && b.Credits(Side.Player) == credits + 3,
            "First owned turn adds income but no village level");
        for (int level = 2; level <= 5; level++)
        {
            Round(b);
            Check(village.Level == level && village.MaxHealth == level * 5 && village.Health == level * 5 &&
                b.Income(Side.Player) == 2 + level, "Every second owned turn adds one level, five HP and one income");
            Check(b.PendingUpgrade(Side.Player) is null, "Automatic village growth never asks for a ship upgrade");
            if (level < 5) Round(b);
        }
        Round(b); Round(b);
        Check(village.Level == 5 && village.MaxHealth == 25 && b.Income(Side.Player) == 7,
            "Village level, hull and income stop growing at level five");

        foreach (var excluded in new[] { ShipClass.Fishing, ShipClass.Balloon, ShipClass.FishingDock, ShipClass.AncientGun })
        {
            b = VillageArena(excluded); village = b.Villages.Single(); village.Health=0; Round(b); Round(b);
            Check(!b.CanCaptureVillage(Side.Player, village.Id), $"{excluded} cannot capture a village");
        }

        b = VillageArena(); village = b.Villages.Single(); village.Health=0; Round(b); b.CaptureVillage(Side.Player, village.Id);
        credits = b.Credits(Side.Player);
        var spawn = b.VillageSpawnCells(village.Id).First();
        var built = b.BuildFromVillage(Side.Player, village.Id, ShipClass.Fishing, spawn);
        Check(built.Success && b.Find(built.TargetId) is { IsExhausted: true } &&
            b.Find(built.TargetId)!.Definition.Class == ShipClass.Fishing &&
            b.Credits(Side.Player) == credits - Rules.Get(ShipClass.Fishing).Price,
            "Village builds a fishing boat on adjacent water at the normal price");
        Check(!b.BuildFromVillage(Side.Player, village.Id, ShipClass.Garrison,
            b.VillageSpawnCells(village.Id).First()).Success, "A village produces at most one ship each turn");
        Round(b); village.Level=2;
        Check(b.BuildFromVillage(Side.Player, village.Id, ShipClass.Garrison,
            b.VillageSpawnCells(village.Id).First()).Success, "Village production resets next turn and supports garrisons");
        Round(b); village.Level = 5; village.Health = village.MaxHealth;
        foreach (var unsupported in new[] { ShipClass.Mothership, ShipClass.FishingDock, ShipClass.Balloon, ShipClass.AncientGun, ShipClass.PirateSchooner })
            Check(b.VillageBuildBlockReason(Side.Player, village.Id, unsupported) is not null,
                $"Village shipyard does not produce {unsupported}");
        for (int level = 1; level <= 5; level++)
        {
            village.Level = level;
            foreach (var kind in new[] { ShipClass.Fishing, ShipClass.Garrison, ShipClass.Invader, ShipClass.Kolonel, ShipClass.Togus })
                Check((b.VillageBuildBlockReason(Side.Player, village.Id, kind) is null) ==
                    (level >= (kind == ShipClass.Garrison ? 2 : BattleState.RequiredLevel(kind))), "Village unlocks Fishing at one, Brig at two and larger ships at three through five");
        }

        b = VillageArena(opponentPosition: new(11, 8)); village = b.Villages.Single();
        village.Health=0; Round(b); b.CaptureVillage(Side.Player, village.Id);
        village.Level = 5; village.Health = village.MaxHealth;
        credits = b.Credits(Side.Player);
        Check(b.FortifyVillage(Side.Player, village.Id).Success && village.IsFortified &&
            b.Credits(Side.Player) == credits - Rules.VillageFortificationPrice,
            "Fortification charges once and marks the village");
        credits = b.Credits(Side.Player);
        Check(!b.FortifyVillage(Side.Player, village.Id).Success && b.Credits(Side.Player) == credits,
            "Duplicate fortification is rejected without spending");
        b.EndTurn(Side.Player);
        Check(b.TargetCells(4).Contains(village.Position), "Enemy village is offered as a valid attack target");
        var attack = b.AttackVillage(Side.Enemy, 4, village.Id);
        Check(attack.Success && attack.Amount == 3 && village.Health == 22 && b.Find(4)!.Health == 12,
            "Fortification resists exactly 25 percent and counters for three inside range three");

        b = VillageArena(opponent: ShipClass.Togus, opponentPosition: new(13, 8)); village = b.Villages.Single();
        village.Health=0; Round(b); b.CaptureVillage(Side.Player, village.Id);
        village.Level = 5; village.Health = village.MaxHealth; b.FortifyVillage(Side.Player, village.Id);
        b.EndTurn(Side.Player);
        Check(!b.CanAttackVillage(4, village.Id), "Radar alone does not expose a village beyond sight");
        b.Vision.RevealCombat(Side.Enemy, village.Position); b.Vision.Recompute(b.Ships, b.Round);
        attack = b.AttackVillage(Side.Enemy, 4, village.Id);
        Check(attack.Success && attack.Amount == 7.5 && village.Health == 17.5 && b.Find(4)!.Health == 5,
            "Distant mortar takes village resistance but no out-of-range counterattack");

        b = VillageArena(opponentPosition: new(11, 8)); village = b.Villages.Single();
        village.Health=0; Round(b); b.CaptureVillage(Side.Player, village.Id); village.Health = 1; village.HasRepaired=true;
        b.EndTurn(Side.Player);
        Check(b.AttackAt(Side.Enemy, 4, village.Position).Success && b.VillageAt(village.Position)==village && village.Health==0 &&
            b.Income(Side.Player) == 2, "Defeated town persists and stops earning income");
        Check(!b.CanAttackVillage(4,village.Id), "Zero-HP town cannot be targeted repeatedly");
        b.EndTurn(Side.Enemy); b.EndTurn(Side.Player);
        Check(village.Health==0 && village.Level==1 && b.VillageBuildBlockReason(Side.Player,village.Id,ShipClass.Garrison) is not null,
            "Defeated town does not regenerate, grow or produce");
        Check(b.Move(Side.Enemy,4,new(10,8)).Success && !b.CanCaptureVillage(Side.Enemy,village.Id),
            "Approaching a defeated town begins the capture wait");
        b.EndTurn(Side.Enemy); b.EndTurn(Side.Player);
        Check(b.CanCaptureVillage(Side.Enemy,village.Id),"Enemy capture becomes available next turn");
        Check(b.CaptureVillage(Side.Enemy,village.Id).Success && village.Owner==Side.Enemy && village.Health==village.MaxHealth &&
            b.Income(Side.Player)==2 && b.Income(Side.Enemy)==3, "Capture restores health and transfers income exactly once");
    }
}
