using System.Text.Json;
using System.Text.Json.Nodes;
using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Units;
using DevAncientNaval.Core.World;

internal static class V0209x2CombatChecks
{
    internal static int Run(BattleRules rules)
    {
        int checks = 0;
        void Check(bool value, string message)
        {
            if (!value) throw new Exception("v020.9x2 combat: " + message);
            checks++;
        }
        BattleRules Policy(bool experimental)
        {
            var json = JsonNode.Parse(JsonSerializer.Serialize(rules))!.AsObject();
            if (experimental) json["ExperimentalCombat"] = true;
            else json.Remove("ExperimentalCombat");
            return BattleRules.FromJson(json.ToJsonString());
        }
        BattleState Fixture(ShipClass kind, bool experimental = true) => new(
            new GameBoard(24, 24, _ => TerrainType.Water), Policy(experimental),
            new[] { (Side.Player, ShipClass.Mothership, new GridPosition(2, 2)),
                (Side.Enemy, ShipClass.Mothership, new GridPosition(21, 21)),
                (Side.Player, kind, new GridPosition(8, 8)),
                (Side.Enemy, ShipClass.Fishing, new GridPosition(kind == ShipClass.Togus ? 11 : 9, 8)) },
            Array.Empty<GridPosition>());

        var brig = Fixture(ShipClass.Garrison);
        Check(brig.Attack(Side.Player, 3, 4).Success && brig.Find(3)!.MovementRemainingUnits == 10,
            "brig can shoot before moving but receives only its remaining one-point retreat");
        var resumed = BattleState.LoadJson(brig.SaveJson());
        Check(resumed.Find(3)!.MovementRemainingUnits == 10 && resumed.Rules.ExperimentalCombat,
            "Continue retains the charged retreat budget and experimental rule");
        Check(resumed.Move(Side.Player, 3, new(8, 9)).Success,
            "brig can spend the permitted retreat point");
        Check(!resumed.Move(Side.Player, 3, new(8, 10)).Success,
            "splitting retreat into orders does not renew the budget");
        Check(brig.Move(Side.Player, 3, new(8, 9)).Success
            && !brig.PathTo(3, new(8, 10)).Any(), "movement preview agrees with issued orders");
        var nearlySpent = Fixture(ShipClass.Garrison);
        nearlySpent.Find(3)!.MovementSpentUnits = 45;
        Check(nearlySpent.Attack(Side.Player, 3, 4).Success
            && nearlySpent.Find(3)!.MovementRemainingUnits == 5,
            "firing never grants a fresh retreat point to a nearly spent brig");
        var exhausted = Fixture(ShipClass.Garrison);
        exhausted.Find(3)!.MovementSpentUnits = 50;
        Check(exhausted.Attack(Side.Player, 3, 4).Success && !exhausted.Find(3)!.CanMove,
            "a brig may finish movement and fire, without receiving new movement");
        var promotion = Fixture(ShipClass.Garrison);
        promotion.Find(3)!.Health = 1;
        promotion.Find(3)!.Kills = 2;
        promotion.Find(3)!.MovementSpentUnits = 35;
        promotion.Find(4)!.Health = 1;
        Check(promotion.Attack(Side.Player, 3, 4).Success && promotion.Find(3)!.IsVeteran
            && promotion.Find(3)!.MovementRemainingUnits == 5,
            "promotion and full healing cannot replenish the pre-shot retreat remainder");
        var legacyBrig = Fixture(ShipClass.Garrison, false);
        Check(!legacyBrig.Rules.ExperimentalCombat && legacyBrig.Attack(Side.Player, 3, 4).Success
            && legacyBrig.Find(3)!.MovementRemainingUnits == legacyBrig.Find(3)!.MovementAllowance * 10,
            "absent combat flag retains historical Scout movement after fire");

        var artillery = Fixture(ShipClass.Togus);
        var shot = artillery.Attack(Side.Player, 3, 4);
        Check(shot.Success && shot.Shots![0].Damage == 6 && shot.Shots[0].IsMortar,
            "Granado's real mortar uses the configured six damage rather than the old constant");
        Check(!artillery.Find(3)!.CanMove && !artillery.Move(Side.Player, 3, new(8, 9)).Success,
            "Granado cannot fire then retreat");
        var artillerySave = BattleState.LoadJson(artillery.SaveJson());
        Check(!artillerySave.Find(3)!.CanMove && artillerySave.Find(3)!.MortarRange == 5,
            "artillery lock and five-tile siege range survive Continue");
        var movingArtillery = Fixture(ShipClass.Togus);
        Check(movingArtillery.Move(Side.Player, 3, new(8, 9)).Success
            && movingArtillery.Find(3)!.AttacksRemaining == 0
            && !movingArtillery.Attack(Side.Player, 3, 4).Success,
            "Granado cannot move and then fire in the same turn");
        var historicalArtillery = Fixture(ShipClass.Togus, false);
        Check(historicalArtillery.Find(3)!.CurrentMortarDamage == 8,
            "old voyages preserve hard-coded mortar damage even with a non-eight definition");
        Check(historicalArtillery.Attack(Side.Player, 3, 4).Success
            && historicalArtillery.Find(3)!.CanMove,
            "old Standard artillery keeps fire-then-move behavior");

        var galleon = Fixture(ShipClass.Invader);
        Check(galleon.Attack(Side.Player, 3, 4).Success && galleon.Find(3)!.CanMove,
            "Galleon retains Standard fire-then-withdraw mobility");
        var advancingGalleon = Fixture(ShipClass.Invader);
        Check(advancingGalleon.Move(Side.Player, 3, new(8, 9)).Success
            && advancingGalleon.Attack(Side.Player, 3, 4).Success
            && !advancingGalleon.Find(3)!.CanMove,
            "Galleon cannot move-fire-retreat");
        var kolonel = Fixture(ShipClass.Kolonel);
        Check(kolonel.Find(3)!.AttacksRemaining == 2 && kolonel.Attack(Side.Player, 3, 4).Success
            && !kolonel.Find(3)!.CanMove && kolonel.Find(3)!.AttacksRemaining == 1,
            "Kolonel retains two shots and commits its movement on the first");
        var counterSave = Fixture(ShipClass.Invader).CaptureSnapshot();
        counterSave.Ships.Single(s => s.Id == 4).Kind = ShipClass.Garrison;
        var counterBattle = BattleState.LoadJson(BattleState.SerializeSnapshot(counterSave));
        var counterResult = counterBattle.Attack(Side.Player, 3, 4);
        Check(counterResult.Success && counterResult.Shots!.Any(s => s.IsCounterattack)
            && counterBattle.Find(4)!.MovementSpentUnits == 0 && counterBattle.Find(4)!.AttacksUsed == 0,
            "counterattacking brig does not spend its next turn's movement or attack");

        var mother = new Ship(50, Side.Player, rules.Get(ShipClass.Mothership), new(4, 4))
            { HasMortar = true, Health = 10 };
        var oldMother = new Ship(50, Side.Player, Policy(false).Get(ShipClass.Mothership), new(4, 4))
            { HasMortar = true, Health = 10 };
        mother.ConsumeAttackMovement(); oldMother.ConsumeAttackMovement();
        Check(mother.CurrentMortarDamage == oldMother.CurrentMortarDamage && mother.MortarRange == 5
            && mother.MovementRemainingUnits == oldMother.MovementRemainingUnits
            && mother.CanRepair == oldMother.CanRepair && mother.CanMove == oldMother.CanMove,
            "experimental ship handling leaves maternal mortar, movement and repair unchanged");
        Check(!JsonSerializer.Serialize(rules.Get(ShipClass.Garrison)).Contains("ExperimentalCombat"),
            "per-ship derived policy cannot be stored as an independent nested save flag");
        return checks;
    }
}
