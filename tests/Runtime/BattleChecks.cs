using System;
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

public partial class BattleChecks : Node
{
    public Main Game { get; set; }=null!;
    private int _checks;
    private void Check(bool ok,string name) { if(!ok) throw new Exception(name); _checks++; }
    private async Task Frame() { await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame); await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame); }
    private static System.Collections.Generic.IEnumerable<Node> Descendants(Node root)
    { yield return root; foreach(var child in root.GetChildren()) foreach(var item in Descendants(child)) yield return item; }
    private Button Button(string name)=>Descendants(Game.Hud).OfType<Button>().Single(b=>b.Name==name);
    private Vector2 ClickAt(Button b)=>b is SectorButton s?s.GlobalPosition+s.IconCenter:b.GetGlobalRect().GetCenter();
    private void Click(Button b)
    {
        var p=ClickAt(b);
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
            Game.Restart(); Game.SelectCell(new(2,9)); await Game.BuyRadar(); Game.Hud.ShowMessage("");
            Game.MapCamera.ZoomAt(Screen(new(2,9)),1.2f); await Frame(); await Capture("");
            foreach(var tile in Game.Battle.Board.Tiles) Game.Battle.Vision.RevealCombat(Side.Player,tile.Position);
            Game.Battle.Vision.Recompute(Game.Battle.Ships,1); Game.CancelOrder(); Game.MapCamera.FitBoard(); Game.Refresh();
            await Capture("-archipelago");
            GD.Print($"PASS: {_checks} Thor runtime checks (direct input, sectors, collection, upgrades, radar, balloon).");
            GetTree().Quit();
        }
        catch(Exception e) { GD.PushError(e.ToString()); GetTree().Quit(1); }
    }
    private async Task Run()
    {
        var defaults=Game.Battle.Rules;
        var rules=new BattleRules { StartingCredits=30,IncomePerMothership=defaults.IncomePerMothership,RepairAmount=defaults.RepairAmount,FleetLimit=defaults.FleetLimit,Ships=defaults.Ships };
        Click(Button("Menu")); await Frame();
        Check(Game.Hud.MenuVisible&&Button("NewGame").IsVisibleInTree()&&Button("ExitGame").IsVisibleInTree(),"Menu exposes new game and exit");
        var beforeSelection=Game.SelectedShipId; Tap(new Vector2(40,180));
        Check(Game.SelectedShipId==beforeSelection&&Game.Hud.MenuVisible,"Menu blocks map input");
        Click(Button("Creative")); Check(Game.Battle.Creative&&Game.Battle.BuildPrice(Side.Player,ShipClass.Kolonel)==0,"Creative menu toggle applies");
        await Capture("-menu");
        Click(Button("Creative")); Check(!Game.Battle.Creative,"Creative toggle off");
        Click(Button("CloseMenu")); Check(!Game.Hud.MenuVisible,"Return closes menu");
        Check(Game.Battle.Ships.Count==6&&Game.Battle.OwnShips(Side.Player).Count()==3,"Three starting ships per side");
        Check(Game.Battle.OwnShips(Side.Player).All(s=>s.Definition.Class is ShipClass.Mothership or ShipClass.Garrison or ShipClass.Fishing),"Correct starting classes");
        Check(Game.Battle.Credits(Side.Player)==5&&Game.Battle.Income(Side.Player)==4,"New starting economy");
        Check(!Descendants(Game.Hud).OfType<Button>().Any(b=>b.Name=="ActionMove"||b.Name=="ActionAttack"||b.Name=="ActionClose"),"No movement/attack/cancel buttons");
        Game.SelectCell(new(4,9)); await Frame();
        var before=Game.Hud.MenuPosition; Game.MapCamera.Pan(new(40,12)); await Frame();
        Check(Game.Hud.MenuPosition.DistanceTo(before)>15,"Sector menu follows selected ship");
        Tap(Screen(new(5,9))); var task=Game.CurrentOrder;
        Check(Game.Busy,"Direct movement starts immediately");
        Game.SelectCell(new(6,9)); await task;
        Check(Game.Battle.Find(2)!.Position==new GridPosition(5,9)&&!Game.Busy,"No confirmation and no double command");
        Game.SelectCell(new(19,0)); Check(Game.SelectedShipId is null,"Invalid distant tile deselects");
        Game.FastChecks=true; Game.SelectCell(new(2,9)); await Frame();
        var sectors=Descendants(Game.Hud).OfType<SectorButton>().Where(s=>s.IsVisibleInTree()).ToArray();
        Check(sectors.Length==4&&sectors.All(s=>Mathf.IsEqualApprox(s.Sweep,Mathf.Pi/4)),"Mother menu has four compact eighth sectors");
        Check(sectors.All(s=>!s._HasPoint(SectorButton.Center)),"Sector hole passes map input");
        int money=Game.Battle.Credits(Side.Player);
        Click(Button("ActionRadar")); await Game.CurrentOrder;
        Check(Game.Battle.Find(1)!.HasRadar&&Game.Battle.Credits(Side.Player)==money-2,"Radar sector purchases immediately");
        Check(Button("ActionRadar").Disabled,"Purchased radar cannot be purchased again");
        Click(Button("ActionBuild")); await Frame();
        Check(Button("BuildFishing").IsVisibleInTree(),"Shipyard opens quartered ship icons"); await Capture("-shipyard");
        Click(Button("BuildFishing")); var spawn=Game.Battle.SpawnCells(1).First(); Game.SelectCell(spawn); await Game.CurrentOrder;
        Check(Game.Battle.At(spawn)!.Definition.Class==ShipClass.Fishing&&Game.Battle.Income(Side.Player)==6,"Spawn click buys fisher");
        Check(!Game.Battle.Find(1)!.CanMove,"Mother movement locked after building");

        var fish=new[] { new GridPosition(6,5),new GridPosition(5,6),new GridPosition(4,5),new GridPosition(5,4),new GridPosition(6,6),new GridPosition(4,4),new GridPosition(4,6),new GridPosition(6,4),new GridPosition(7,5) };
        Game.LoadScenario(new BattleState(new GameBoard(20,20,p=>p==new GridPosition(7,7)?TerrainType.Land:TerrainType.Water),rules,new (Side,ShipClass,GridPosition)[] {
            (Side.Player,ShipClass.Mothership,new(5,5)),(Side.Enemy,ShipClass.Mothership,new(18,18)),(Side.Player,ShipClass.Fishing,new(8,8)) },fish));
        Game.SelectCell(new(5,5)); await Frame();
        if(DisplayServer.GetName()!="headless")
        {
            var p=ClickAt(Button("ActionCollect"));
            Godot.Input.ParseInputEvent(new InputEventScreenTouch { Index=0,Position=p,Pressed=true });
            Godot.Input.FlushBufferedEvents(); await Frame();
            Godot.Input.ParseInputEvent(new InputEventScreenTouch { Index=0,Position=p,Pressed=false });
            Godot.Input.FlushBufferedEvents(); await Frame();
            Check(Game.Mode==OrderMode.Collect,"Native touch activates collection sector");
        }
        else Click(Button("ActionCollect"));
        Check(Game.BoardView.Collection.Count==9,"Separate collection area highlights fish");
        Game.SelectCell(fish[0]); await Game.CurrentOrder;
        Check(Game.Battle.Find(1)!.Resources==1&&Game.Battle.Credits(Side.Player)==28,"Click fish spends currency and grants resource");
        Game.BeginCollect(); Game.SelectCell(fish[1]); await Game.CurrentOrder; await Frame();
        Check(Game.Hud.UpgradeVisible&&Game.Battle.Find(1)!.Level==2,"Automatic upgrade opens centered choice");
        Check(Button("UpgradeIncome").Visible&&Button("UpgradeMobility").Visible&&!Button("UpgradeBalloon").Visible,"Correct level two choices");
        await Capture("-level2");
        var position=Game.Battle.Find(1)!.Position;
        Tap(Screen(new(5,7)));
        Check(Game.Battle.Find(1)!.Position==position,"Upgrade overlay prevents map click-through");
        Click(Button("UpgradeMobility")); await Game.CurrentOrder;
        Check(!Game.Hud.UpgradeVisible&&Game.Battle.Find(1)!.MovementAllowance==3,"Choice applies once and closes window");
        foreach(var cell in fish.Skip(2).Take(3)) { Game.BeginCollect(); Game.SelectCell(cell); await Game.CurrentOrder; }
        Check(Game.Hud.UpgradeVisible&&Game.Battle.Find(1)!.Level==3,"Third resource tier");
        await Frame(); Click(Button("UpgradeBalloon")); await Game.CurrentOrder;
        var air=Game.Battle.OwnShips(Side.Player).Single(s=>s.IsAirborne);
        var balloonPoint=GetViewport().GetCanvasTransform()*(Game.BoardView.Projection.GridToWorld(air.Position)+new Vector2(0,-62));
        Game.SelectAtScreen(balloonPoint); Check(Game.SelectedShipId==air.Id,"Airborne model can be selected above sea tile");
        Game.SelectCell(new(7,7)); await Game.CurrentOrder;
        Check(air.Position==new GridPosition(7,7)&&Game.Battle.Board.GetTile(air.Position).Terrain==TerrainType.Land,"Balloon flies over land by direct tile click");
        await Capture("-balloon");
        Game.Battle.EndTurn(Side.Player); Game.Battle.EndTurn(Side.Enemy); Game.Refresh();
        Game.SelectCell(new(5,5)); await Game.CurrentOrder;
        Check(air.Position==new GridPosition(5,5),"Balloon can fly above a friendly ship");
        Game.SelectCell(new(5,5));
        foreach(var cell in fish.Skip(5)) { Game.BeginCollect(); Game.SelectCell(cell); await Game.CurrentOrder; }
        Check(Game.Battle.Find(1)!.Level==4&&!Game.Hud.UpgradeVisible&&Game.Battle.Find(1)!.MaxHealth==70,"Fourth level has stats only");

        Game.LoadScenario(new BattleState(new GameBoard(20,20,_=>TerrainType.Water),rules,new (Side,ShipClass,GridPosition)[] {
            (Side.Player,ShipClass.Mothership,new(0,0)),(Side.Enemy,ShipClass.Mothership,new(19,19)),
            (Side.Player,ShipClass.Kolonel,new(7,7)),(Side.Enemy,ShipClass.Kolonel,new(9,7)) },Array.Empty<GridPosition>()));
        Game.FastChecks=false; Game.SelectCell(new(7,7));
        Check(Game.BoardView.Targets.Contains(new GridPosition(9,7)),"Attack targets highlighted upon selection");
        Game.SelectCell(new(9,7)); task=Game.CurrentOrder;
        await ToSignal(GetTree().CreateTimer(0.25),SceneTreeTimer.SignalName.Timeout);
        Check(Game.Fleet.ProjectilePosition is not null,"Direct attack launches projectile");
        await task;
        Check(Game.Battle.Find(3)!.Health<30&&Game.Battle.Find(4)!.Health<30,"Counterattack preserved");
        Game.Battle.EndTurn(Side.Player); Game.Battle.EndTurn(Side.Enemy); Game.Refresh(); await Frame();
        double hp=Game.Battle.Find(3)!.Health; Click(Button("ActionRepair")); await Game.CurrentOrder;
        Check(Game.Battle.Find(3)!.Health>hp&&Game.Battle.Find(3)!.Health%1==0,"Plus repair sector heals integer HP");
        Game.LoadScenario(new BattleState(new GameBoard(20,20,_=>TerrainType.Water),rules,new (Side,ShipClass,GridPosition)[] {
            (Side.Player,ShipClass.Mothership,new(5,5)),(Side.Enemy,ShipClass.Mothership,new(18,18)),(Side.Enemy,ShipClass.Kolonel,new(10,5)) },Array.Empty<GridPosition>()));
        Game.FastChecks=true; Game.SelectCell(new(5,5)); await Game.BuyRadar();
        Check(Game.Battle.FindObserved(Side.Player,3) is null&&Game.BoardView.Targets.Contains(new GridPosition(10,5)),"Radar marks target without optical identity");
        Game.SelectCell(new(10,5)); await Game.CurrentOrder;
        Check(Game.Battle.Find(3)!.Health<30,"Direct contact click fires radar attack");
        await Game.EndPlayerTurn();
        Check(Game.Battle.ActiveSide==Side.Player&&!Game.Busy,"AI returns control with new economy");
        Game.Restart(); Check(Game.Battle.Ships.Count==6&&Game.Battle.Credits(Side.Player)==5,"Restart restores new setup");
    }
}
