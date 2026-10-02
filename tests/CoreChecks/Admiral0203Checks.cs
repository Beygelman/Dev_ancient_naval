using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using DevAncientNaval.Core.AI;
using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Units;
using DevAncientNaval.Core.World;

internal static class Admiral0203Checks
{
    internal static int Run(BattleRules rules)
    {
        int checks=0;
        void Check(bool value,string message)
        {
            if (!value) throw new Exception("0.20.3 Admiral: "+message);
            checks++;
        }
        var tacticalJson=JsonNode.Parse(JsonSerializer.Serialize(rules))!.AsObject();
        tacticalJson["StartingCredits"]=0; tacticalJson["EncounterCurrencyReward"]=0;
        var tactical=BattleRules.FromJson(tacticalJson.ToJsonString());
        BattleState Create(params (Side Owner,ShipClass Class,GridPosition Position)[] extra)
        {
            var state=new BattleState(new GameBoard(30,30,_=>TerrainType.Water),tactical,
                new[] {(Side.Player,ShipClass.Mothership,new GridPosition(1,1)),(Side.Enemy,ShipClass.Mothership,new GridPosition(28,28))}.Concat(extra),
                Array.Empty<GridPosition>(),villageSpots:Array.Empty<GridPosition>());
            state.SetDifficulty(AiDifficulty.Admiral);
            return state;
        }
        var battle=Create((Side.Player,ShipClass.Garrison,new(8,8)),(Side.Player,ShipClass.Garrison,new(7,9)),
            (Side.Enemy,ShipClass.Invader,new(10,8)),(Side.Player,ShipClass.Fishing,new(9,8)));
        battle.Find(5)!.Health=5;
        var decisions=new List<CommandResult>();
        for (int step=0; step<4 && battle.Find(5) is not null; step++)
        {
            var result=SimpleOpponent.Step(battle); decisions.Add(result);
            Check(result.Success,"coordinated orders are legal");
        }
        Check(battle.Find(5) is null && battle.ActiveSide==Side.Player,"two Brigs finish one target in the same personal turn");
        Check(decisions.Count(r=>r.Kind==CommandKind.Attack && r.TargetId==5)==2
            && decisions.Any(r=>r.Kind==CommandKind.Move && r.ActorId==4),"second gun makes a legal affordable advance before contributing");
        Check(battle.Find(3) is not null && battle.Find(4) is not null,"forecast accounts for return fire without sacrificing ready hulls");

        battle=Create((Side.Player,ShipClass.Kolonel,new(8,8)),(Side.Enemy,ShipClass.Invader,new(10,8)),
            (Side.Player,ShipClass.Fishing,new(9,8)));
        battle.Find(4)!.Health=8;
        var salvo=SimpleOpponent.Step(battle);
        Check(salvo.Success && salvo.Kind==CommandKind.Attack && salvo.Shots!.Count(s=>!s.IsCounterattack)==2
            && salvo.Shots!.All(s=>!s.IsCounterattack) && battle.Find(4) is null,"Admiral uses a decisive double salvo and avoids a return shot");
        battle=Create((Side.Player,ShipClass.Kolonel,new(8,8)),(Side.Enemy,ShipClass.Invader,new(10,8)),
            (Side.Player,ShipClass.Fishing,new(9,8)));
        battle.Find(4)!.Health=3;
        salvo=SimpleOpponent.Step(battle);
        Check(salvo.Success && salvo.Shots!.Count(s=>!s.IsCounterattack)==1 && battle.Find(3)!.AttacksRemaining==1,
            "a one-shot kill preserves the second charge");
        battle=Create((Side.Player,ShipClass.Kolonel,new(8,8)),(Side.Enemy,ShipClass.Kolonel,new(10,8)),
            (Side.Player,ShipClass.Fishing,new(9,8)));
        battle.Find(3)!.Health=3;
        var caution=SimpleOpponent.Step(battle);
        Check(caution.Success && caution.Kind!=CommandKind.Attack && battle.Find(3)!.AttacksUsed==0,
            "a damaged gun does not take predictable lethal counterfire");
        battle=Create((Side.Player,ShipClass.Garrison,new(8,8)),(Side.Enemy,ShipClass.Kolonel,new(10,8)),
            (Side.Player,ShipClass.Fishing,new(9,8)));
        battle.Find(1)!.Position=new(4,8);
        battle.Vision.Recompute(battle.Ships,1);
        var regroup=SimpleOpponent.Step(battle);
        Check(regroup.Success && regroup.Kind==CommandKind.Move && regroup.ActorId==3
            && battle.Board.Distance(battle.Find(3)!.Position,battle.Find(1)!.Position)<4,
            "an understrength escort regroups toward allied support instead of making a losing trade");

        battle=Create((Side.Enemy,ShipClass.Garrison,new(25,25)));
        battle.EndTurn(Side.Player);
        string unseenSave=battle.SaveJson();
        var altered=battle.CaptureSnapshot(); altered.Ships.Single(s=>s.Owner==Side.Player).Position=new(2,25);
        altered.Ships.Single(s=>s.Owner==Side.Player).Health=1;
        var hidden=BattleState.LoadJson(BattleState.SerializeSnapshot(altered));
        var normal=BattleState.LoadJson(unseenSave);
        var hiddenOrder=SimpleOpponent.Step(hidden); var normalOrder=SimpleOpponent.Step(normal);
        Check(Signature(hiddenOrder)==Signature(normalOrder),"unseen flagship location and HP do not change the Admiral's decision");
        var repeat=BattleState.LoadJson(unseenSave);
        Check(Signature(SimpleOpponent.Step(repeat))==Signature(normalOrder),"identical observed states produce deterministic orders");

        // Profile and finish small, actual tactical voyages. These reports are
        // command latency observations, not a rendering/FPS claim.
        foreach (int offset in new[] {0,1})
        {
            var sample=Create((Side.Player,ShipClass.Kolonel,new(11,12+offset)),(Side.Enemy,ShipClass.Kolonel,new(15,12+offset)),
                (Side.Player,ShipClass.Garrison,new(12,14)),(Side.Enemy,ShipClass.Garrison,new(16,14)));
            var clock=Stopwatch.StartNew(); int orders=0;
            while (!sample.IsOver && sample.Round<200 && orders<6000)
            {
                var order=SimpleOpponent.Step(sample);
                Check(order.Success,"complete Admiral tactical voyage issues legal orders");
                orders++;
            }
            Check(sample.IsOver,"Admiral completes a tactical voyage rather than permanently stalling");
            Console.WriteLine($"ADMIRAL0203 fixture={offset} rounds={sample.Round} orders={orders} elapsed_ms={clock.Elapsed.TotalMilliseconds:F1}");
        }
        return checks;
    }

    private static string Signature(CommandResult command)=>$"{command.Kind}/{command.ActorId}/{command.TargetId}/"+
        string.Join(';',command.Path?.Select(p=>$"{p.X},{p.Y}")??Array.Empty<string>());
}
