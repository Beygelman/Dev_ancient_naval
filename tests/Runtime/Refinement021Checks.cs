using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Units;
using DevAncientNaval.Core.World;
using DevAncientNaval.Presentation;
using DevAncientNaval.Presentation.Map;
using DevAncientNaval.Presentation.UI;
using DevAncientNaval.Presentation.Camera;
using Godot;
using Side = DevAncientNaval.Core.Units.Side;

namespace DevAncientNaval.Tests.Runtime;

public partial class Refinement021Checks : Node
{
    public Main Game { get; set; } = null!;
    private int _checks;
    private void Check(bool yes, string name) { if (!yes) throw new Exception("0.21 UI: " + name); _checks++; }
    private async Task Wait(double seconds) => await ToSignal(GetTree().CreateTimer(seconds), SceneTreeTimer.SignalName.Timeout);
    private async Task Frame() => await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    private static IEnumerable<Node> Nodes(Node node)
    {
        yield return node;
        foreach (var child in node.GetChildren()) foreach (var n in Nodes(child)) yield return n;
    }
    private Vector2 Screen(GridPosition p) => GetViewport().GetCanvasTransform()*Game.BoardView.Projection.GridToWorld(p);
    private async Task Capture(string suffix)
    {
        var arg=OS.GetCmdlineUserArgs().FirstOrDefault(a=>a.StartsWith("--capture="));
        if (arg is null || DisplayServer.GetName()=="headless") return;
        await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
        Check(GetViewport().GetTexture().GetImage().SavePng(arg[10..].Replace(".png","-"+suffix+".png"))==Error.Ok,"screenshot "+suffix);
    }
    private BattleState Duel() => new(new GameBoard(24,24,_=>TerrainType.Water), Game.Battle.Rules,
        new[] {(Side.Player,ShipClass.Mothership,new GridPosition(2,2)),(Side.Enemy,ShipClass.Mothership,new GridPosition(22,22)),
            (Side.Player,ShipClass.Kolonel,new GridPosition(8,8)),(Side.Enemy,ShipClass.Kolonel,new GridPosition(10,8)),
            (Side.Player,ShipClass.Garrison,new GridPosition(6,8)),(Side.Player,ShipClass.Fishing,new GridPosition(5,9))},
        Array.Empty<GridPosition>(), villageSpots:Array.Empty<GridPosition>());
    private async Task Salvo(bool touch)
    {
        var battle=Duel(); Game.LoadScenario(battle); Game.FastChecks=false;
        Game.MapCamera.Position=Game.BoardView.Projection.GridToWorld(new(8,8));
        Game.MapCamera.Zoom=Vector2.One*1.2f; Game.MapCamera.ForceUpdateScroll();
        Game.SelectCell(new(8,8)); await Wait(.4); await Frame();
        var fan=Nodes(Game.Hud).OfType<RadialPapyrus>().Single(n=>n.Name=="ActionPapyrus");
        var origin=fan.GlobalPosition+SectorButton.Center;
        Check(Nodes(fan).OfType<SectorButton>().Where(n=>n.IsVisibleInTree()).All(n=>n.GlobalPosition.Y+n.IconCenter.Y>Screen(new(8,8)).Y+ShipVisualProfile.ProgressY(ShipClass.Kolonel)*1.2f+10),"fan buttons lie compactly below progress cells");
        Check(Nodes(fan).OfType<SectorButton>().Where(n=>n.IsVisibleInTree()).All(n=>n.IconCenter.Y>SectorButton.Center.Y),"all command sectors face downward symmetrically");
        Check(ShipVisualProfile.ProgressY(ShipClass.Fishing)<ShipVisualProfile.ProgressY(ShipClass.Garrison)
            && ShipVisualProfile.ProgressY(ShipClass.Garrison)<ShipVisualProfile.ProgressY(ShipClass.Mothership),"smaller hull progress sits closer to its center");
        await Capture(touch ? "touch-ready" : "salvo-ready");
        var p=Screen(new(10,8));
        int heldEvents=0,tapEvents=0;
        Action<Vector2> held=p=>heldEvents++;
        Action<Vector2> tapped=p=>tapEvents++;
        Game.MapInput.Held+=held; Game.MapInput.Tapped+=tapped;
        if (touch) GetViewport().PushInput(new InputEventScreenTouch {Index=5,Position=p,Pressed=true},true);
        else GetViewport().PushInput(new InputEventMouseButton {ButtonIndex=MouseButton.Left,Position=p,GlobalPosition=p,Pressed=true},true);
        ulong holdStart=Time.GetTicksMsec();
        while (Time.GetTicksMsec()-holdStart<520) await Frame();
        Check(!Game.Busy && battle.Find(4)!.Health==15,"holding alone causes no early shot");
        if (touch) GetViewport().PushInput(new InputEventScreenTouch {Index=5,Position=p,Pressed=false},true);
        else GetViewport().PushInput(new InputEventMouseButton {ButtonIndex=MouseButton.Left,Position=p,GlobalPosition=p,Pressed=false},true);
        ulong holdElapsed=Time.GetTicksMsec()-holdStart;
        var order=Game.CurrentOrder;
        Game.MapInput.Held-=held; Game.MapInput.Tapped-=tapped;
        Check(Game.Busy && battle.PendingPresentation is not null,$"release begins a staged double salvo: held={heldEvents}, taps={tapEvents}, elapsed_ms={holdElapsed}");
        await Wait(.08);
        Check(battle.Find(4)!.Health==15 && Game.Fleet.CompletedSalvos.Count==0,"camera travels before projectile or damage");
        await order;
        Check(!Game.Busy && battle.Find(4)!.Health==7 && battle.Find(3)!.AttacksRemaining==0,$"real hold yields eight damage and consumes both shots: HP={battle.Find(4)!.Health}, shots={battle.Find(3)!.AttacksRemaining}, flights={Game.Fleet.CompletedSalvos.Count}");
        Check(Game.Fleet.CompletedSalvos.Count==3,"two attacks and one reply have separate flights");
        await Capture(touch ? "touch-complete" : "salvo-complete");
    }
    private async Task TownSalvo()
    {
        var cell=new GridPosition(10,8);
        var battle=new BattleState(new GameBoard(24,24,p=>p==cell ? TerrainType.Land : TerrainType.Water), Game.Battle.Rules,
            new[] {(Side.Player,ShipClass.Mothership,new GridPosition(2,2)),(Side.Enemy,ShipClass.Mothership,new GridPosition(22,22)),
                (Side.Player,ShipClass.Kolonel,new GridPosition(8,8))}, Array.Empty<GridPosition>(),villageSpots:new[] {cell});
        var saved=battle.CaptureSnapshot();
        saved.Villages[0]=saved.Villages[0] with {Owner=Side.Enemy,Level=3,Health=15,Fortified=true};
        battle=BattleState.LoadJson(BattleState.SerializeSnapshot(saved));
        Game.LoadScenario(battle); Game.FastChecks=false;
        Game.MapCamera.Zoom=Vector2.One; Game.MapCamera.Position=Game.BoardView.Projection.GridToWorld(new(8,8)); Game.MapCamera.ForceUpdateScroll();
        Game.SelectCell(new(8,8)); await Wait(.4); await Frame();
        var point=Screen(cell);
        GetViewport().PushInput(new InputEventMouseButton {ButtonIndex=MouseButton.Left,Position=point,GlobalPosition=point,Pressed=true},true);
        ulong held=Time.GetTicksMsec(); while(Time.GetTicksMsec()-held<520) await Frame();
        GetViewport().PushInput(new InputEventMouseButton {ButtonIndex=MouseButton.Left,Position=point,GlobalPosition=point,Pressed=false},true);
        var order=Game.CurrentOrder;
        Check(Game.Busy && battle.Villages.Single().Health==15,"town salvo leaves health pending during camera approach");
        await order;
        Check(battle.Villages.Single().Health==9 && battle.Find(3)!.AttacksRemaining==0,"two four-damage town shots respect its 25% fortification resistance");
        Check(battle.Find(3)!.Health==11 && Game.Fleet.CompletedSalvos.Count==2,"town replies once after both visible salvos");
        Check(Game.MapCamera.Position.DistanceTo(Game.BoardView.Projection.GridToWorld(new(8,8)))<.01f,"camera returns to attacked ship before the town's reply");
        await Capture("town-salvo");
        Game.CancelOrder(); await Frame();
        point=Screen(new(8,8));
        GetViewport().PushInput(new InputEventMouseButton {ButtonIndex=MouseButton.Left,Position=point,GlobalPosition=point,Pressed=true},true);
        held=Time.GetTicksMsec(); while(Time.GetTicksMsec()-held<520) await Frame();
        GetViewport().PushInput(new InputEventMouseButton {ButtonIndex=MouseButton.Left,Position=point,GlobalPosition=point,Pressed=false},true);
        Check(Game.SelectedShipId==3,"holding an own ship with no selection still selects it normally");
    }
    private async Task CameraChecks()
    {
        var camera=Game.MapCamera;
        Check(MapCamera.FlightEase(0)==0 && MapCamera.FlightEase(1)==1,"camera ease has exact endpoints");
        float previous=0;
        for(int i=1;i<=100;i++) {float next=MapCamera.FlightEase(i/100f); Check(next>=previous,"camera motion is monotonic");previous=next;}
        Check(MapCamera.FlightEase(.01f)<.00002f && 1-MapCamera.FlightEase(.99f)<.00002f,"ease starts and ends at zero speed");
        foreach(double seconds in new[] {.25,.5})
        {
            var from=Game.BoardView.Projection.GridToWorld(new(7,7));
            var to=Game.BoardView.Projection.GridToWorld(new(15,12));
            camera.Position=from; camera.ForceUpdateScroll();
            ulong start=Time.GetTicksMsec(); var travel=camera.FocusAsync(to,seconds);
            await Wait(seconds*.4);
            Check(camera.Position.DistanceTo(from)>1 && camera.Position.DistanceTo(to)>1,"camera actually traverses intermediate points");
            await travel;
            double elapsed=(Time.GetTicksMsec()-start)/1000.0;
            Check(camera.Position.DistanceTo(to)<.01f && Math.Abs(elapsed-seconds)<.09,"camera lands in fixed requested duration "+seconds);
        }
        var flight=camera.FocusAsync(Game.BoardView.Projection.GridToWorld(new(8,8)),.5);
        camera.Pan(new(10,0)); await flight;
        Check(flight.IsCompleted,"manual panning cancels automatic flight without trapping an order");
        Game.Hud.ShowOpponentTurn(Side.Enemy2);
        var nation=Nodes(Game.Hud).OfType<PanelContainer>().Single(n=>n.Name=="ActingNation");
        Check(Nodes(nation).OfType<Label>().Single().Text=="Other nations are taking their turns","unmet nation identity is concealed");
        Game.Hud.ShowOpponentTurn(Side.Enemy);
        Check(Nodes(nation).OfType<Label>().Single().Text.Contains(Game.Battle.FactionName(Side.Enemy)),"met nation's captain is identified");
        await Frame();
        Check(nation.Position.Y<140 && nation.IsVisibleInTree() && Game.Hud.BannerText=="","opponent status occupies small top plaque only");
        await Capture("turn-status");
        Game.Hud.ShowPlayerTurn();
    }
    private async Task Discovery()
    {
        var battle=new BattleState(new GameBoard(24,24,_=>TerrainType.Water),Game.Battle.Rules,
            new[] {(Side.Player,ShipClass.Mothership,new GridPosition(2,2)),(Side.Enemy,ShipClass.Mothership,new GridPosition(22,22)),
                (Side.Player,ShipClass.Garrison,new GridPosition(4,3)),(Side.Enemy,ShipClass.Garrison,new GridPosition(8,3))},
            Array.Empty<GridPosition>(),villageSpots:Array.Empty<GridPosition>());
        Game.LoadScenario(battle); Game.FastChecks=false;
        Game.MapCamera.Zoom=Vector2.One;
        Game.MapCamera.Position=Game.BoardView.Projection.GridToWorld(new(4,3)); Game.MapCamera.ForceUpdateScroll();
        Check(!battle.HasMet(Side.Enemy),"fixture rival begins outside optical sight");
        int credits=battle.Credits(Side.Player);
        Game.SelectCell(new(4,3)); Game.SelectCell(new(5,3)); await Game.CurrentOrder;
        Check(battle.HasMet(Side.Enemy) && battle.Credits(Side.Player)==credits+5,"actual sailing discovers and pays five Thors");
        var target=Game.BoardView.Projection.GridToWorld(new(8,3));
        Check(Game.MapCamera.Position.DistanceTo(target)<.01f && Game.Hud.MessageText.Contains("Met "),"discovery camera finishes on the first seen rival");
        await Capture("first-encounter");
    }
    public override async void _Ready()
    {
        try
        {
            await Frame(); await Salvo(false); await Salvo(true); await CameraChecks(); await Discovery(); await TownSalvo();
            GD.Print($"PASS: {_checks} 0.21 input, staged salvo, lower fans, camera timing and nation privacy checks.");
            GetTree().Quit();
        }
        catch(Exception error) { GD.PushError(error.ToString()); GetTree().Quit(1); }
    }
}
