using System;
using System.Linq;
using System.Threading.Tasks;
using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Units;
using DevAncientNaval.Core.World;
using DevAncientNaval.Presentation;
using Godot;
using Side = DevAncientNaval.Core.Units.Side;

namespace DevAncientNaval.Tests.Runtime;

public partial class BattleChecks : Node
{
    public Main Game { get; set; }=null!;
    private int _checks;
    private void Check(bool ok,string label) { if(!ok) throw new Exception(label); _checks++; }
    private async Task Frame() { await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame); await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame); }
    private Button Button(string name)=>Descendants(Game.Hud).OfType<Button>().Single(b=>b.Name==name);
    private static System.Collections.Generic.IEnumerable<Node> Descendants(Node root)
    {
        yield return root; foreach(var child in root.GetChildren()) foreach(var item in Descendants(child)) yield return item;
    }
    private void Click(Button button)
    {
        var p=button.GetGlobalRect().GetCenter();
        GetViewport().PushInput(new InputEventMouseButton { Position=p,GlobalPosition=p,ButtonIndex=MouseButton.Left,Pressed=true },true);
        GetViewport().PushInput(new InputEventMouseButton { Position=p,GlobalPosition=p,ButtonIndex=MouseButton.Left,Pressed=false },true);
    }
    private void Tap(Vector2 p)
    {
        GetViewport().PushInput(new InputEventScreenTouch { Index=0,Position=p,Pressed=true },true);
        GetViewport().PushInput(new InputEventScreenTouch { Index=0,Position=p,Pressed=false },true);
    }
    private Vector2 Screen(GridPosition p)=>GetViewport().GetCanvasTransform()*Game.BoardView.Projection.GridToWorld(p);
    private async Task Capture(string suffix)
    {
        var arg=OS.GetCmdlineUserArgs().FirstOrDefault(a=>a.StartsWith("--capture="));
        if(arg is null||DisplayServer.GetName()=="headless") return;
        await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
        Check(GetViewport().GetTexture().GetImage().SavePng(arg[10..].Replace(".png",suffix+".png"))==Error.Ok,"Screenshot "+suffix);
    }
    public override async void _Ready()
    {
        try
        {
            await Frame(); await Run();
            Game.Restart(); Game.SelectCell(new(4,9)); Game.MapCamera.ZoomAt(Screen(new(4,9)),1.22f);
            Game.Hud.ShowMessage(""); await Frame(); await Capture("");
            GD.Print($"PASS: {_checks} battle runtime checks (radial UI, immediate orders, fog, radar, combat, veteran, mouse/touch).");
            GetTree().Quit();
        }
        catch(Exception e) { GD.PushError(e.ToString()); GetTree().Quit(1); }
    }
    private async Task Run()
    {
        var rules=Game.Battle.Rules;
        Check(Game.Battle.ObservedShips(Side.Player).Count()==4,"Enemy fleet initially hidden");
        Game.SelectCell(new(17,9));
        Check(Game.SelectedShipId is null&&Game.Hud.SelectionText.Contains("Неизведанные"),"Hidden enemy cannot be inspected");
        Check(!Descendants(Game.Hud).OfType<Label>().Any(l=>l.Text.Contains("X:")||l.Text.Contains("Y:")||l.Text.Contains("Ваш ход")),"No debug coordinates or turn ownership label");
        Check(!Descendants(Game.Hud).OfType<Button>().Any(b=>b.Text.Contains("Подтверд")||b.Text=="+"||b.Text=="−"||b.Text=="Обзор"),"No confirmation or camera buttons");

        Game.SelectCell(new(4,9)); await Frame();
        Check(Button("ActionMove").IsVisibleInTree()&&!Button("ActionMove").Disabled,"Radial actions visible");
        var beforeMenu=Game.Hud.MenuPosition;
        Game.MapCamera.Pan(new(60,10)); await Frame();
        Check(Game.Hud.MenuPosition.DistanceTo(beforeMenu)>30,"Actions follow camera");
        Click(Button("ActionMove"));
        Check(Game.Mode==OrderMode.Move&&Game.BoardView.Reachable.Count>0,"Move icon chooses targets without click-through");
        Game.SelectCell(new(5,9)); var task=Game.CurrentOrder;
        Check(Game.Busy,"Movement animation locks repeated input");
        Game.SelectCell(new(6,9)); await task;
        Check(Game.Battle.Find(2)!.Position==new GridPosition(5,9)&&Game.Battle.Find(2)!.MovementRemaining==4&&!Game.Busy,"Cell click moves once with no confirmation");
        Game.BeginMove(); Game.CancelOrder(); Game.SelectCell(new(5,10));
        Check(Game.Battle.Find(2)!.Position==new GridPosition(5,9),"Cancel mode does not move");

        Game.FastChecks=true; Game.SelectCell(new(2,9)); await Frame();
        Click(Button("ActionBuild")); await Frame();
        Check(Button("BuildFishing").IsVisibleInTree(),"Mother opens production icons");
        int money=Game.Battle.Credits(Side.Player); Click(Button("BuildFishing"));
        Check(Game.Mode==OrderMode.Build&&Game.Battle.Credits(Side.Player)==money,"Choose class without charging");
        var spawn=Game.Battle.SpawnCells(1).First(); Game.SelectCell(spawn); await Game.CurrentOrder;
        Check(Game.Battle.At(spawn)!.Definition.Class==ShipClass.Fishing&&Game.Battle.Credits(Side.Player)==money-20,"Spawn click builds immediately");
        Check(Game.Battle.Income(Side.Player)==33&&!Game.Battle.Find(1)!.CanMove,"Fishing income and mother lock");
        Check(Button("ActionAttack").Disabled&&Game.Hud.ShipText.Contains("Рыболов"),"Fishing attack icon disabled");
        Game.SelectCell(new(2,9)); Check(Button("ActionMove").Disabled,"Mother movement icon disabled after build");
        Game.FastChecks=false;
        task=Game.EndPlayerTurn();
        Check(Game.Hud.BannerText=="Ходит: Капитан Анат"&&Game.Busy,"Named opponent banner at start");
        await task;
        Check(Game.Battle.ActiveSide==Side.Player&&Game.Battle.Round==2&&!Game.Busy&&Game.Hud.BannerText=="","Control returns and banner clears");

        // Real native map touch plus GUI mouse/touch events.
        Game.FastChecks=true; Game.Restart(); Tap(Screen(new(4,9))); await Frame();
        Check(Game.SelectedShipId==2,"Map touch selects ship");
        if(DisplayServer.GetName()!="headless")
        {
            var p=Button("ActionMove").GetGlobalRect().GetCenter();
            Godot.Input.ParseInputEvent(new InputEventScreenTouch { Index=0,Position=p,Pressed=true });
            Godot.Input.ParseInputEvent(new InputEventScreenTouch { Index=0,Position=p,Pressed=false });
            Godot.Input.FlushBufferedEvents(); await Frame();
            Check(Game.Mode==OrderMode.Move&&Game.SelectedShipId==2,$"Native touch activates radial button without click-through: mode={Game.Mode}, selected={Game.SelectedShipId}, at={p}, button={Button("ActionMove").GetGlobalRect()}");
        }
        else Click(Button("ActionMove"));
        Tap(Screen(new(5,9))); await Game.CurrentOrder;
        Check(Game.Battle.Find(2)!.Position==new GridPosition(5,9),"Touch destination executes directly");
        Game.MapCamera.Pan(new(-1000,-1000)); await Frame();
        foreach(var button in Descendants(Game.Hud).OfType<Button>().Where(b=>b.IsVisibleInTree()))
            Check(new Rect2(Vector2.Zero,GetViewport().GetVisibleRect().Size).Encloses(button.GetGlobalRect()),"Visible buttons fit viewport");
        Game.MapCamera.FitBoard(); Game.SelectCell(new(2,9)); await Frame(); Click(Button("ActionBuild")); await Frame(); await Capture("-shipyard");

        var setup=new (Side,ShipClass,GridPosition)[] {
            (Side.Player,ShipClass.Mothership,new(0,0)),(Side.Enemy,ShipClass.Mothership,new(19,19)),
            (Side.Player,ShipClass.Garrison,new(7,7)),(Side.Enemy,ShipClass.Invader,new(13,7)) };
        Game.LoadScenario(new BattleState(new GameBoard(20,20,_=>TerrainType.Water),rules,setup));
        Game.SelectCell(new(13,7));
        Check(Game.SelectedShipId is null&&Game.Hud.SelectionText.Contains("Радар: неизвестный"),"Radar contact has no identity");
        Game.SelectCell(new(7,7)); Game.BeginMove(); Game.SelectCell(new(9,7)); await Game.CurrentOrder;
        Check(Game.Battle.FindObserved(Side.Player,4) is not null,"Scout identifies contact");
        Game.SelectCell(new(13,7)); Check(Game.SelectedShipId==4&&Game.Hud.ShipText.Contains("Invader"),"Visible target inspectable");

        setup[2]=(Side.Player,ShipClass.Kolonel,new(7,7)); setup[3]=(Side.Enemy,ShipClass.Kolonel,new(9,7));
        Game.LoadScenario(new BattleState(new GameBoard(20,20,_=>TerrainType.Water),rules,setup));
        Game.FastChecks=false; Game.SelectCell(new(7,7)); await Frame(); Click(Button("ActionAttack"));
        Check(Game.Mode==OrderMode.Attack&&Game.BoardView.AttackArea.Count>0,"Attack icon displays range");
        Game.SelectCell(new(9,7)); task=Game.CurrentOrder;
        await ToSignal(GetTree().CreateTimer(0.25),SceneTreeTimer.SignalName.Timeout);
        var midpoint=Game.BoardView.Projection.GridToWorld(new(8,7));
        Check(Game.Fleet.ProjectilePosition is { } shot&&shot.Y<midpoint.Y-20,"Ballistic projectile above straight line");
        await Capture("-combat"); await task;
        Check(Game.Battle.Find(3)!.Health<23&&Game.Battle.Find(4)!.Health<23&&!Game.Busy,"Attack and counterattack complete");
        Game.BeginAttack(); Game.SelectCell(new(9,7)); await Game.CurrentOrder;
        Check(Game.Battle.Find(3)!.AttacksRemaining==0&&Game.Battle.Find(4)!.AttacksUsed==0,"Every attack countered without consuming normal attacks");
        Game.Battle.EndTurn(Side.Player); Game.Battle.EndTurn(Side.Enemy); Game.Refresh();
        Game.SelectCell(new(7,7)); double hp=Game.Battle.Find(3)!.Health; await Frame(); Click(Button("ActionRepair")); await Game.CurrentOrder;
        Check(Game.Battle.Find(3)!.Health>hp&&Game.Battle.Find(3)!.IsExhausted,"Repair icon heals immediately");

        Game.FastChecks=true;
        Game.LoadScenario(new BattleState(new GameBoard(20,20,_=>TerrainType.Water),rules,new (Side,ShipClass,GridPosition)[] {
            (Side.Player,ShipClass.Mothership,new(0,0)),(Side.Enemy,ShipClass.Mothership,new(19,19)),
            (Side.Player,ShipClass.Kolonel,new(7,7)),(Side.Enemy,ShipClass.Fishing,new(8,7)),
            (Side.Enemy,ShipClass.Fishing,new(7,8)),(Side.Enemy,ShipClass.Fishing,new(6,7)) }));
        foreach(var cell in new[] { new GridPosition(8,7),new GridPosition(7,8),new GridPosition(6,7) })
        {
            if(Game.Battle.Find(3)!.AttacksRemaining==0) { Game.Battle.EndTurn(Side.Player); Game.Battle.EndTurn(Side.Enemy); }
            Game.SelectCell(new(7,7)); Game.BeginAttack(); Game.SelectCell(cell); await Game.CurrentOrder;
        }
        Check(Game.Battle.Find(3)!.IsVeteran&&Game.Hud.ShipText.Contains("ВЕТЕРАН")&&Game.Hud.ShipText.Contains(Game.Battle.Find(3)!.MaxHealth.ToString("0.##")),"Veteran stats and pennant");
        Game.MapCamera.ZoomAt(Screen(new(7,7)),1.6f); await Frame(); await Capture("-veteran");
        var sudden=new BattleRules { StartingCredits=120,IncomePerMothership=25,RepairAmount=4,FleetLimit=12,
            Ships=rules.Ships.Select(s=>s.Class==ShipClass.Mothership?s with { MaxHealth=1 }:s).ToArray() };
        Game.LoadScenario(new BattleState(new GameBoard(20,20,_=>TerrainType.Water),sudden,new (Side,ShipClass,GridPosition)[] {
            (Side.Player,ShipClass.Mothership,new(3,3)),(Side.Enemy,ShipClass.Mothership,new(19,19)),(Side.Enemy,ShipClass.Kolonel,new(5,3)) }));
        await Game.EndPlayerTurn();
        Check(Game.Battle.Winner==Side.Enemy&&Game.Hud.StatusText=="ПОРАЖЕНИЕ"&&Button("EndTurn").Disabled,"Defeat banner blocks commands");
        Game.Restart(); Check(Game.Battle.Round==1&&Game.Battle.Ships.Count==8&&Game.Battle.Credits(Side.Player)==120,"Restart resets match");
    }
}
