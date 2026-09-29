using System;
using System.Linq;
using System.Threading.Tasks;
using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Units;
using DevAncientNaval.Core.World;
using DevAncientNaval.Core.Vision;
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
    private async Task BuyResource(GridPosition cell,bool touch=false)
    {
        Game.SelectCell(cell); await Frame();
        Check(Button("TileResource").IsVisibleInTree(),$"Tile opens resource sector: cell {cell}, selected {Game.SelectedShipId}, level {Game.Battle.Mothership(Side.Player)!.Level}, pending {Game.Battle.PendingUpgrade(Side.Player)?.Level}, sites {string.Join(';',Game.BoardView.Collection)}");
        if(touch) Tap(ClickAt(Button("TileResource"))); else Click(Button("TileResource"));
        await Game.CurrentOrder; await Frame();
    }
    public override async void _Ready()
    {
        try
        {
            await Frame(); await Run();
            Game.Restart(); var anchor=Game.Battle.Mothership(Side.Player)!.Position;
            Game.SelectCell(anchor); await Game.BuyRadar(); Game.Hud.ShowMessage("");
            Game.MapCamera.ZoomAt(Screen(anchor),1.8f); await Frame(); await Capture("");
            foreach(var tile in Game.Battle.Board.Tiles) Game.Battle.Vision.RevealCombat(Side.Player,tile.Position);
            Game.Battle.Vision.Recompute(Game.Battle.Ships,1); Game.CancelOrder(); Game.MapCamera.FitBoard(); Game.Refresh();
            await Capture("-archipelago");
            GD.Print($"PASS: {_checks} naval runtime checks (tile purchases, unlocks, stealth animation, pentagon, controls)."); GetTree().Quit();
        }
        catch(Exception e) { GD.PushError(e.ToString()); GetTree().Quit(1); }
    }
    private async Task Run()
    {
        var defaults=Game.Battle.Rules;
        var rules=new BattleRules { StartingCredits=80,IncomePerMothership=defaults.IncomePerMothership,RepairAmount=defaults.RepairAmount,FleetLimit=defaults.FleetLimit,Ships=defaults.Ships };
        Click(Button("Menu")); await Frame();
        Check(Game.Hud.MenuVisible&&Button("NewGame").IsVisibleInTree()&&Button("ExitGame").IsVisibleInTree(),"Menu exposes new game and exit");
        var beforeSelection=Game.SelectedShipId; Tap(new Vector2(40,180));
        Check(Game.SelectedShipId==beforeSelection&&Game.Hud.MenuVisible,"Menu blocks map input");
        Click(Button("Creative")); Check(Game.Battle.Creative&&Game.Battle.BuildPrice(Side.Player,ShipClass.Kolonel)==0,"Creative toggle");
        await Capture("-menu"); Click(Button("Creative")); Click(Button("CloseMenu"));
        Check(!Game.Hud.MenuVisible&&!Game.Battle.Creative,"Menu closes and creative off");
        Check(Game.Battle.Ships.Count==6&&Game.Battle.Credits(Side.Player)==5&&Game.Battle.Income(Side.Player)==4,"Starting fleet and economy");
        Check(!Descendants(Game.Hud).OfType<Button>().Any(b=>new[] {"ActionMove","ActionAttack","ActionClose","ActionCollect","ActionDock"}.Contains(b.Name.ToString())),"No obsolete ship actions");
        var garrison=Game.Battle.OwnShips(Side.Player).Single(s=>s.Definition.Class==ShipClass.Garrison);
        Game.SelectCell(garrison.Position); await Frame();
        var before=Game.Hud.MenuPosition; Game.MapCamera.Pan(new(40,12)); await Frame();
        Check(Game.Hud.MenuPosition.DistanceTo(before)>15,"Ship sectors follow camera");
        var destination=new GridPosition(garrison.Position.X+1,garrison.Position.Y);
        Tap(Screen(destination)); var order=Game.CurrentOrder; Check(Game.Busy,"Movement starts on tile click"); await order;
        Check(garrison.Position==destination&&!Game.Busy,"Direct movement complete");
        Game.SelectCell(new(0,0)); Check(Game.SelectedShipId is null,"Pentagon exterior deselects");
        Game.FastChecks=true; var mother=Game.Battle.Mothership(Side.Player)!; Game.SelectCell(mother.Position); await Frame();
        var sectors=Descendants(Game.Hud).OfType<SectorButton>().Where(s=>s.IsVisibleInTree()).ToArray();
        Check(sectors.Length==4&&sectors.All(s=>Mathf.IsEqualApprox(s.Sweep,Mathf.Pi/4)&&!s._HasPoint(SectorButton.Center)),"Four compact eighth sectors with transparent center");
        int money=Game.Battle.Credits(Side.Player); Click(Button("ActionRadar")); await Game.CurrentOrder;
        Check(mother.HasRadar&&Game.Battle.Credits(Side.Player)==money-2,"Radar purchase");
        Click(Button("ActionBuild")); await Frame();
        Check(!Button("BuildFishing").Disabled&&Button("BuildInvader").Disabled&&Button("BuildKolonel").Disabled&&Button("BuildTogus").Disabled,"Starting yard unlocks only fishing and garrison");
        await Capture("-shipyard"); Click(Button("BuildFishing")); var spawn=Game.Battle.SpawnCells(mother.Id).First(); Game.SelectCell(spawn); await Game.CurrentOrder;
        Check(Game.Battle.At(spawn)!.Definition.Class==ShipClass.Fishing&&!mother.CanMove,"Build works and locks mother movement");

        var fish=(from y in Enumerable.Range(3,5) from x in Enumerable.Range(3,5) let p=new GridPosition(x,y) where p!=new GridPosition(5,5)&&BattleVision.InRadius(p,new(5,5),2) select p).Take(14).ToArray();
        Game.LoadScenario(new BattleState(new GameBoard(20,20,p=>p==new GridPosition(7,7)?TerrainType.Land:TerrainType.Water),rules,new[] {
            (Side.Player,ShipClass.Mothership,new GridPosition(5,5)),(Side.Enemy,ShipClass.Mothership,new GridPosition(18,18)),
            (Side.Player,ShipClass.Fishing,new GridPosition(8,8)) },fish));
        mother=Game.Battle.Find(1)!; Game.SelectCell(mother.Position); await Frame();
        Check(Game.BoardView.Collection.Count==14&&Game.BoardView.DockSites.Count==0&&Game.BoardView.AttackArea.Count==0,"Automatic fish contours, no attack fill or level-one docks");
        Game.SelectCell(fish[0]); await Frame();
        Check(Game.Battle.Credits(Side.Player)==80&&mother.Resources==0,"Selecting fish does not spend currency");
        Game.CancelOrder(); await Frame();
        Check(!Button("TileResource").IsVisibleInTree()&&Game.Battle.Credits(Side.Player)==80,"Deselect dismisses purchase without spending");
        Game.SelectCell(mother.Position); Game.SelectCell(fish[0]); await Frame();
        await Capture("-resource");
        Tap(ClickAt(Button("TileResource"))); await Game.CurrentOrder; await Frame();
        Check(mother.Resources==1&&Game.Battle.Credits(Side.Player)==78&&!Button("TileResource").IsVisibleInTree(),"Touch buys resource once and closes sector");
        await BuyResource(fish[1]); Check(Game.Hud.UpgradeVisible&&mother.Level==2,"Level two automatic modal"); await Capture("-level2");
        var position=mother.Position; Tap(Screen(new(5,7))); Check(mother.Position==position,"Modal blocks movement");
        Click(Button("UpgradeMobility")); await Game.CurrentOrder;
        Check(!Game.Hud.UpgradeVisible&&mother.MovementAllowance==3&&Game.BoardView.DockSites.Count>0,"Level two unlocks docks");
        foreach(var cell in fish.Skip(2).Take(3)) await BuyResource(cell);
        Check(mother.Level==3&&Game.Hud.UpgradeVisible,"Level three"); Click(Button("UpgradeBalloon")); await Game.CurrentOrder;
        var balloon=Game.Battle.OwnShips(Side.Player).Single(s=>s.IsAirborne);
        Game.SelectAtScreen(GetViewport().GetCanvasTransform()*(Game.BoardView.Projection.GridToWorld(balloon.Position)+new Vector2(0,-62)));
        Check(Game.SelectedShipId==balloon.Id,"Balloon selected above ship"); Game.SelectCell(new(7,7)); await Game.CurrentOrder;
        Check(balloon.Position==new GridPosition(7,7)&&balloon.MovementRemainingUnits==0,"Balloon crosses land within movement budget");
        await Capture("-balloon"); Game.SelectCell(mother.Position);
        foreach(var cell in fish.Skip(5).Take(4)) await BuyResource(cell);
        Check(mother.Level==4&&Game.Hud.UpgradeVisible,"Level four"); Click(Button("UpgradeFortification")); await Game.CurrentOrder;
        Check(mother.MaxHealth==85&&Game.Battle.BuildBlockReason(Side.Player,1,ShipClass.Kolonel) is null&&Game.Battle.MortarBlockReason(Side.Player,1) is not null,"Level four heavy unlock; mortar locked");
        foreach(var cell in fish.Skip(9)) await BuyResource(cell);
        Check(mother.Level==5&&mother.MaxHealth==95&&!Game.Hud.UpgradeVisible&&mother.ResourcesRequired==0,"Five resources unlock final level without modal");
        await Frame(); Click(Button("ActionRadar")); await Game.CurrentOrder; await Frame(); money=Game.Battle.Credits(Side.Player);
        Check(!Button("ActionMortar").Disabled,"Level five radar enables mortar"); Click(Button("ActionMortar")); await Game.CurrentOrder;
        Check(mother.HasMortar&&Game.Battle.Credits(Side.Player)==money-10&&mother.CurrentMortarDamage==8,"Mortar cost and damage");
        var site=Game.Battle.DockCells(1).First(); await BuyResource(site);
        Check(Game.Battle.At(site) is { IsStructure:true }&&Game.Battle.Income(Side.Player)==13,"Tile dock purchase and income at level five");
        await Capture("-dock"); Click(Button("ActionBuild")); await Frame(); Check(!Button("BuildTogus").Disabled,"Togus unlocked at five");
        Click(Button("BuildTogus")); spawn=Game.Battle.SpawnCells(1).First(); Game.SelectCell(spawn); await Game.CurrentOrder;
        Check(Game.Battle.At(spawn) is { HasMortar:true,HasRadar:true },"Built Togus includes artillery and radar"); await Frame(); await Capture("-togus");

        Game.LoadScenario(new BattleState(new GameBoard(20,20,_=>TerrainType.Water),rules,new[] {
            (Side.Player,ShipClass.Mothership,new GridPosition(0,0)),(Side.Enemy,ShipClass.Mothership,new GridPosition(19,19)),
            (Side.Player,ShipClass.Kolonel,new GridPosition(7,7)),(Side.Enemy,ShipClass.Kolonel,new GridPosition(9,7)) },Array.Empty<GridPosition>()));
        Game.FastChecks=false; Game.SelectCell(new(7,7)); Check(Game.BoardView.Targets.Contains(new GridPosition(9,7)),"Selectable targets remain outlined");
        Game.SelectCell(new(9,7)); order=Game.CurrentOrder; await ToSignal(GetTree().CreateTimer(.25),SceneTreeTimer.SignalName.Timeout);
        Check(Game.Fleet.ProjectilePosition is not null,"Cannon projectile"); await order;
        Check(Game.Battle.Find(3)!.Health<40&&Game.Battle.Find(4)!.Health<40,"Counterattack");
        Game.Battle.EndTurn(Side.Player); Game.Battle.EndTurn(Side.Enemy); Game.Refresh(); await Frame();
        double hp=Game.Battle.Find(3)!.Health; Click(Button("ActionRepair")); await Game.CurrentOrder;
        Check(Game.Battle.Find(3)!.Health>hp,"Repair sector works");

        Game.LoadScenario(new BattleState(new GameBoard(20,20,_=>TerrainType.Water),rules,new[] {
            (Side.Player,ShipClass.Mothership,new GridPosition(0,0)),(Side.Enemy,ShipClass.Mothership,new GridPosition(19,19)),
            (Side.Player,ShipClass.Togus,new GridPosition(5,5)),(Side.Enemy,ShipClass.Kolonel,new GridPosition(10,5)) },Array.Empty<GridPosition>()));
        Game.SelectCell(new(5,5)); Check(Game.Battle.FindObserved(Side.Player,4) is null&&Game.BoardView.Targets.Contains(new GridPosition(10,5)),"Radar target anonymous before shot");
        Game.SelectCell(new(10,5)); order=Game.CurrentOrder;
        await ToSignal(GetTree().CreateTimer(.35),SceneTreeTimer.SignalName.Timeout);
        Check(Game.Fleet.ProjectilePosition is not null&&!Game.Fleet.AnimatedShipIds.Contains(4)&&Game.Fleet.CombatFeedback.Length==0,"Animation never inserts hidden target or damage text");
        await Capture("-mortar"); await order;
        Check(Game.Battle.Find(4)!.Health==32&&Game.Battle.FindObserved(Side.Player,4) is null&&!Game.Hud.MessageText.Contains("8")&&!Game.Hud.MessageText.Contains("Kolonel"),"Hit applies without revealing class or damage");
        Game.FastChecks=true; await Game.EndPlayerTurn(); Check(Game.Battle.ActiveSide==Side.Player&&!Game.Busy,"Opponent returns control");
        int seed=Game.Battle.Board.Seed; Game.Restart(); Check(Game.Battle.Board.Seed!=seed&&Game.Battle.Board.Boundary.Count==5&&Game.Battle.Ships.Count==6,"Restart randomizes pentagon and resets game");
    }
}
