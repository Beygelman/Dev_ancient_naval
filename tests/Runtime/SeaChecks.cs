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
            await Frame(); var seen=new HashSet<TreasuryReward>();
            for(int seed=0;seed<50 && seen.Count<5;seed++)
            {
                var b=new BattleState(new GameBoard(20,20,_=>TerrainType.Water),Game.Battle.Rules,new[]{
                    (Side.Player,ShipClass.Mothership,new GridPosition(1,1)),(Side.Enemy,ShipClass.Mothership,new GridPosition(18,18)),
                    (Side.Player,ShipClass.Kolonel,new GridPosition(2,2))},Array.Empty<GridPosition>(),resourceSeed:seed,seaEvents:true);
                Game.LoadScenario(b);Game.FastChecks=true;Reveal();
                var ship=b.Find(3)!;
                var treasure=b.Treasuries.OrderBy(t=>b.Board.Distance(ship.Position,t.Position)).First();
                int limit=20;
                while(ship.Position!=treasure.Position && limit-->0)
                {
                    var route=b.RouteToward(ship.Id,treasure.Position);
                    var end=b.AffordableDestination(ship.Id,route);
                    Check(end!=ship.Position && b.Move(Side.Player,ship.Id,end).Success,"Treasury can be reached by ordinary movement");
                    if(ship.Position!=treasure.Position) NextTurn();
                }
                Check(ship.Position==treasure.Position && !b.CanLootTreasury(Side.Player,3),"Loot waits on arrival");
                Game.MapCamera.Position=Game.BoardView.Projection.GridToWorld(ship.Position);
                Game.MapCamera.Zoom=Vector2.One*1.4f;Game.MapCamera.ForceUpdateScroll();
                Game.SelectCell(ship.Position); await Frame();
<<<<<<< Updated upstream
                var button=Game.Hud.TreasuryPapyrus;
                Check(!button.IsVisibleInTree(),"Treasury scroll waits until crew is ready");
                NextTurn(); Game.SelectCell(ship.Position);await Frame();
                Check(button.IsVisibleInTree(),"Treasury scroll appears next turn");
                await ToSignal(GetTree().CreateTimer(.42),SceneTreeTimer.SignalName.Timeout);
                var point=button.GlobalPosition+button.Size/2;
=======
                var button=Descendants(Game.Hud).OfType<SectorButton>().Single(n=>n.Name=="ActionLoot");
                Check(button.IsVisibleInTree() && button.Disabled,"Treasury action displays waiting state");
                NextTurn(); Game.SelectCell(ship.Position);await Frame();
                Check(!button.Disabled,"Treasury action enables next turn");
                var point=button.GlobalPosition+button.IconCenter;
>>>>>>> Stashed changes
                GetViewport().PushInput(new InputEventMouseButton{Position=point,GlobalPosition=point,ButtonIndex=MouseButton.Left,Pressed=true},true);
                GetViewport().PushInput(new InputEventMouseButton{Position=point,GlobalPosition=point,ButtonIndex=MouseButton.Left,Pressed=false},true);
                await Game.CurrentOrder; await Frame();
                Check(b.LastTreasuryReward is not null && b.TreasuryAt(treasure.Position) is null,"Treasury sector executes loot through the interface");
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
