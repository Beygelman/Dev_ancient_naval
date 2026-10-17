using System.Text.Json;
using System.Text.Json.Nodes;
using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Economy;
using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Units;
using DevAncientNaval.Core.World;

internal static class Rules0209Checks
{
    internal static int Run(BattleRules current)
    {
        int checks = 0;
        void Check(bool value, string name) { if (!value) throw new Exception("0209: " + name); checks++; }
        BattleRules Policy(bool early, int cap)
        {
            var json = JsonNode.Parse(JsonSerializer.Serialize(current))!.AsObject();
            json["CounterBetweenSalvoShots"] = early;
            json["Ports"]!["ConnectedCityIncomeCap"] = cap;
            return BattleRules.FromJson(json.ToJsonString());
        }
        BattleState Combat(bool early, double attackerHealth = 15)
        {
            var battle = new BattleState(new GameBoard(20, 20, _ => TerrainType.Water), Policy(early, 0),
                new[] { (Side.Player, ShipClass.Mothership, new GridPosition(2, 2)),
                    (Side.Enemy, ShipClass.Mothership, new GridPosition(17, 17)),
                    (Side.Player, ShipClass.Kolonel, new GridPosition(8, 8)),
                    (Side.Enemy, ShipClass.Invader, new GridPosition(9, 8)) }, Array.Empty<GridPosition>());
            var saved = battle.CaptureSnapshot();
            saved.Ships.Single(s => s.Id == 3).Health = attackerHealth;
            saved.Ships.Single(s => s.Id == 4).Health = 6;
            return BattleState.LoadJson(BattleState.SerializeSnapshot(saved));
        }
        Check(current.CounterBetweenSalvoShots && current.Ports.ConnectedCityIncomeCap == 2,
            "new voyage enables independently saved combat and trade experiments");
        var active = Combat(true);
        var attack = active.Attack(Side.Player, 3, 4, true);
        Check(attack.Success && attack.Shots!.Select(s => s.IsCounterattack).SequenceEqual(new[] { false, true, false }),
            "surviving target replies between active shots");
        Check(attack.Shots![2].Damage == Math.Min(attack.Shots[2].Target.Health, attack.Shots[0].Damage) && attack.Shots.Count(s => s.IsCounterattack) == 1,
            "second shot keeps configured damage and there is one reply");
        Check(active.Find(3)!.Health < 15 && active.Find(4) is null, "target replies even when second shot sinks it");
        var historical = Combat(false);
        var oldAttack = historical.Attack(Side.Player, 3, 4, true);
        Check(oldAttack.Shots!.Count == 2 && oldAttack.Shots.All(s => !s.IsCounterattack) && historical.Find(3)!.Health == 15,
            "historical double salvo still sinks before reply");
        var safety = Combat(true, 1).CaptureSnapshot();
        safety.Ships.Single(s => s.Id == 4).Health = 4;
        safety.Ships.Single(s => s.Id == 3).MovementLocked = true;
        var unsafeReply = BattleState.LoadJson(BattleState.SerializeSnapshot(safety));
        Check(!unsafeReply.ReadyActions(Side.Player).Any(r => r.Id == 3 && (r.Actions & ReadyActionKind.Attack) != 0),
            "readiness rejects double-shot kills prevented by a lethal middle reply");
        safety.Rules = Policy(false, 0);
        var legacySafe = BattleState.LoadJson(BattleState.SerializeSnapshot(safety));
        Check(legacySafe.ReadyActions(Side.Player).Any(r => r.Id == 3 && (r.Actions & ReadyActionKind.Attack) != 0),
            "historical readiness retains its decisive double-shot kill");
        var fragile = Combat(true, 3);
        var interrupted = fragile.Attack(Side.Player, 3, 4, true);
        Check(interrupted.Shots!.Count == 2 && interrupted.Shots[1].IsCounterattack && fragile.Find(3) is null,
            "lethal reply cancels second shot");
        var staged = Combat(true);
        var prepared = staged.Prepare(b => b.Attack(Side.Player, 3, 4, true));
        prepared.Impact("attack");
        Check(staged.Find(4)!.Health == 2 && staged.Find(3)!.Health == 15, "first staged impact");
        prepared.Impact("counter");
        Check(staged.Find(3)!.Health < 15 && staged.Find(4)!.Health == 2, "middle staged reply");
        prepared.Impact("attack2");
        Check(staged.Find(4) is null, "last staged shot");
        prepared.Finish();
        string committed = staged.SaveJson();
        prepared.Impact("attack"); prepared.Finish();
        Check(staged.SaveJson() == committed && committed == active.SaveJson(), "skip/replay applies final aggregate exactly once");
        var resumed = BattleState.LoadJson(committed);
        Check(resumed.Rules.CounterBetweenSalvoShots && resumed.SaveJson() == committed, "combat rule survives Continue");

        BattleState Ports(int count, int cap)
        {
            var towns = Enumerable.Range(0, count).Select(i => new GridPosition(5 + 2 * i, 5)).ToArray();
            var battle = new BattleState(new GameBoard(20, 20, p => towns.Contains(p) ? TerrainType.Land : TerrainType.Water),
                Policy(false, cap), new[] { (Side.Player, ShipClass.Mothership, new GridPosition(2, 16)),
                    (Side.Enemy, ShipClass.Mothership, new GridPosition(17, 16)) },
                Array.Empty<GridPosition>(), villageSpots: towns);
            var saved = battle.CaptureSnapshot();
            for (int i = 0; i < count; i++)
            {
                var town = saved.Villages[i];
                saved.Villages[i] = town with { Owner = Side.Player, Level = 3, Health = 15,
                    Port = true, PortCell = new(town.Position.X, 6) };
                saved.Income = saved.Income.Append(new IncomeSource($"village:{town.Id}", Side.Player, 3)).ToArray();
            }
            return BattleState.LoadJson(BattleState.SerializeSnapshot(saved));
        }
        foreach (int count in new[] { 1, 2, 3, 4 })
        {
            var ports = Ports(count, 2);
            int each = Math.Min(2, count - 1);
            Check(ports.Villages.All(v => ports.PortIncome(v) == each), "capped connected income " + count);
            Check(ports.GrossIncome(Side.Player) == current.IncomePerMothership + count * (3 + each),
                "cap reaches credited gross income " + count);
            string saved = ports.SaveJson();
            Check(BattleState.LoadJson(saved).SaveJson() == saved, "trade policy/state resumes exactly");
        }
        var credited = Ports(4, 2);
        int balance = credited.Credits(Side.Player);
        credited.EndTurn(Side.Player);
        var nextTurn = credited.EndTurn(Side.Enemy);
        Check(nextTurn.IncomeReceipts!.Where(r => r.SourceId.StartsWith("village:")).All(r => r.Amount == 5)
            && credited.Credits(Side.Player) == balance + credited.Income(Side.Player),
            "turn receipts and actual wallet credit apply the cap");
        var four = Ports(4, 0);
        Check(four.Villages.All(v => four.PortIncome(v) == 3), "zero cap preserves old uncapped network");
        var missing = JsonNode.Parse(JsonSerializer.Serialize(current))!.AsObject();
        missing.Remove("CounterBetweenSalvoShots");
        missing["Ports"]!.AsObject().Remove("ConnectedCityIncomeCap");
        var oldRules = BattleRules.FromJson(missing.ToJsonString());
        Check(!oldRules.CounterBetweenSalvoShots && oldRules.Ports.ConnectedCityIncomeCap == 0,
            "absent fields preserve historical policies with generated JSON defaults");

        var land = new GridPosition(9, 8);
        foreach (double hp in new[] { 15d, 3d })
        {
            var townBattle = new BattleState(new GameBoard(20, 20, p => p == land ? TerrainType.Land : TerrainType.Water),
                Policy(true, 0), new[] { (Side.Player, ShipClass.Mothership, new GridPosition(2, 2)),
                    (Side.Enemy, ShipClass.Mothership, new GridPosition(17, 17)),
                    (Side.Player, ShipClass.Kolonel, new GridPosition(8, 8)) },
                Array.Empty<GridPosition>(), villageSpots: new[] { land });
            var saved = townBattle.CaptureSnapshot();
            saved.Ships.Single(s => s.Id == 3).Health = hp;
            saved.Villages[0] = saved.Villages[0] with { Owner = Side.Enemy, Level = 2, Health = 6, Fortified = true };
            townBattle = BattleState.LoadJson(BattleState.SerializeSnapshot(saved));
            var result = townBattle.AttackVillage(Side.Player, 3, townBattle.Villages[0].Id, true);
            Check(result.Success && result.StructureHit!.ReplyBetweenShots && result.StructureHit.CounterDamage > 0,
                "fortified town replies between shots");
            Check(hp == 3 ? result.StructureHit!.SecondAttacker is null && townBattle.Find(3) is null
                : result.StructureHit!.SecondAttacker is not null, "town reply can prevent second shot");
        }
        checks += CityInfrastructureChecks.Run(current);
        return checks;
    }
}

