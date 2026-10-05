using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Units;
using DevAncientNaval.Core.World;
using DevAncientNaval.Presentation;
using DevAncientNaval.Presentation.UI;
using Godot;
using Side = DevAncientNaval.Core.Units.Side;

namespace DevAncientNaval.Tests.Runtime;

public partial class SeaChecks : Node
{
    public Main Game { get; set; } = null!;
    private int _checks;
    private void Check(bool ok,string name) {if(!ok) throw new Exception(name);_checks++;}
    private static IEnumerable<Node> Descendants(Node node)
    {yield return node;foreach(var child in node.GetChildren()) foreach(var item in Descendants(child)) yield return item;}
    private async Task Frame()=>await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
    private async Task WaitForUi(Func<bool> ready,string name)
    {
        ulong started=Time.GetTicksMsec();
        while(!ready() && Time.GetTicksMsec()-started<6000) await Frame();
        Check(ready(),name);
    }
    private void Click(Vector2 point)
    {
        GetViewport().PushInput(new InputEventMouseMotion{Position=point,GlobalPosition=point},true);
        GetViewport().PushInput(new InputEventMouseButton{Position=point,GlobalPosition=point,ButtonIndex=MouseButton.Left,Pressed=true,ButtonMask=MouseButtonMask.Left},true);
        GetViewport().PushInput(new InputEventMouseButton{Position=point,GlobalPosition=point,ButtonIndex=MouseButton.Left,Pressed=false},true);
    }
    private async Task AcceptPendingAwards()
    {
        var reward=Descendants(Game).OfType<RewardPapyrusHud>().Single();
        for(int attempts=0;reward.IsOpen && attempts<20;attempts++)
        {
            var claim=Descendants(reward).OfType<Button>().Single(node=>node.Name=="ClaimReward");
            await WaitForUi(()=>claim.IsVisibleInTree() && !claim.Disabled
                && !Descendants(reward).OfType<RollingModalPaper>().Any(paper=>paper.IsAnimating),
                "Current reward paper finishes opening before its real claim click");
            var award=Game.Battle.PendingAwards.Single(item=>item.Id==reward.PendingId);
            int credits=Game.Battle.Credits(Side.Player);
            Click(claim.GetGlobalTransformWithCanvas()*(claim.Size/2));
            await WaitForUi(()=>Game.Battle.PendingAwards.All(item=>item.Id!=award.Id),
                "Real claim stamp and fold accept the original reward");
            await Game.CurrentOrder;await Frame();
            Check(Game.Battle.Credits(Side.Player)==credits+award.Amount,
                "Real reward claim credits its exact Core amount once");
        }
        Check(!reward.IsOpen && !Game.Battle.PendingAwards.Any(award=>award.Owner==Side.Player),
            "Fixture rewards are acknowledged before sea-chart input");
    }
    private void Reveal()
    {
        foreach(var tile in Game.Battle.Board.Tiles) Game.Battle.Vision.RevealCombat(Side.Player,tile.Position);
        Game.Battle.Vision.Recompute(Game.Battle.Ships,Game.Battle.TurnSerial,Game.Battle.Villages);Game.Refresh();
    }
    private void NextTurn()
    {
        var b=Game.Battle;
        b.EndTurn(Side.Player); b.EndTurn(Side.Enemy);
        if(b.ActiveSide==Side.Pirates) b.EndTurn(Side.Pirates);
        Reveal();
    }
    private async Task Capture(string suffix)
    {
        string? arg=OS.GetCmdlineUserArgs().FirstOrDefault(a=>a.StartsWith("--capture="));
        if(arg is null || DisplayServer.GetName()=="headless") return;
        await Frame();await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
        Check(GetViewport().GetTexture().GetImage().SavePng(arg[10..].Replace(".png","-"+suffix+".png"))==Error.Ok,"Capture "+suffix);
    }
    public override async void _Ready()
    {
        try
        {
            await Frame(); UiScale.Set(1, false); var seen=new HashSet<TreasuryReward>();
            for(int seed=0;seed<50 && seen.Count<5;seed++)
            {
                var b=new BattleState(new GameBoard(20,20,_=>TerrainType.Water),Game.Battle.Rules,new[]{
                    (Side.Player,ShipClass.Mothership,new GridPosition(1,1)),(Side.Enemy,ShipClass.Mothership,new GridPosition(18,18)),
                    (Side.Player,ShipClass.Kolonel,new GridPosition(2,2))},Array.Empty<GridPosition>(),resourceSeed:seed,seaEvents:true);
                Game.LoadScenario(b);Game.FastChecks=true;Reveal();await AcceptPendingAwards();
                var ship=b.Find(3)!;
                var treasure=b.Treasuries.OrderBy(t=>b.Board.Distance(ship.Position,t.Position)).First();
                int limit=20;
                while(ship.Position!=treasure.Position && limit-->0)
                {
                    var route=b.RouteToward(ship.Id,treasure.Position);
                    var end=b.AffordableDestination(ship.Id,route);
                    Check(end!=ship.Position && b.Move(Side.Player,ship.Id,end).Success,"Treasury can be reached by ordinary movement");
                    if(ship.Position!=treasure.Position) {NextTurn();await AcceptPendingAwards();}
                }
                Check(ship.Position==treasure.Position && !b.CanLootTreasury(Side.Player,3),"Loot waits on arrival");
                Game.MapCamera.Position=Game.BoardView.Projection.GridToWorld(ship.Position);
                Game.MapCamera.Zoom=Vector2.One*1.4f;Game.MapCamera.ForceUpdateScroll();
                Game.SelectCell(ship.Position); await Frame();
                var button=Game.Hud.TreasuryPapyrus;
                Check(!button.IsVisibleInTree(),"Treasury scroll waits until crew is ready");
                NextTurn();await AcceptPendingAwards();Game.SelectCell(ship.Position);await Frame();
                Check(button.IsVisibleInTree(),"Treasury scroll appears next turn");
                await ToSignal(GetTree().CreateTimer(.42),SceneTreeTimer.SignalName.Timeout);
                await Frame();
                var point=button.GetGlobalTransformWithCanvas()*(button.Size/2);
                Check(button._HasPoint(button.Size/2),"Revealed treasury paper exposes its native hit area");
                GetViewport().PushInput(new InputEventMouseMotion{Position=point,GlobalPosition=point},true);
                await Frame();
                GD.Print($"SEA_CLICK seed {seed}, point {point}, hovered {GetViewport().GuiGetHoveredControl()?.Name}, pending awards {b.PendingAwards.Count}");
                Check(GetViewport().GuiGetHoveredControl()==button,"Native treasury click reaches the revealed world paper");
                await Capture("treasury-ready-"+seed);
                Click(point);
                await Game.CurrentOrder; await Frame();
                Check(b.LastTreasuryReward is not null && b.TreasuryAt(treasure.Position) is null,$"Treasury sector executes loot through the interface (point {point}, reveal {button.Reveal}, consuming {button.IsConsuming}, busy {Game.Busy})");
                var committed=b.SaveJson();
                Click(point);await Game.CurrentOrder;await Frame();
                Check(b.SaveJson()==committed,"Repeated native treasury clicks cannot apply a second discovery or reward");
                var reward=b.LastTreasuryReward!.Value;
                if(reward!=TreasuryReward.Resources) Game.CancelOrder();
                if(seen.Add(reward)) {await ToSignal(GetTree().CreateTimer(.8),SceneTreeTimer.SignalName.Timeout);await Capture(reward.ToString());}
            }
            Check(seen.Count==5,"All five discoveries exercised in the real game engine");
            GD.Print($"PASS: {_checks} sea-event runtime checks.");GetTree().Quit();
        }
        catch(Exception e) {GD.PushError(e.ToString());GetTree().Quit(1);}
    }
}
