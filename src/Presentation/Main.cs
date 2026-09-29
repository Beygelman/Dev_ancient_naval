using System;
using System.Linq;
using System.Threading.Tasks;
using DevAncientNaval.Core.AI;
using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Units;
using DevAncientNaval.Presentation.Camera;
using DevAncientNaval.Presentation.Input;
using DevAncientNaval.Presentation.Map;
using DevAncientNaval.Presentation.UI;
using Godot;
using Side = DevAncientNaval.Core.Units.Side;

namespace DevAncientNaval.Presentation;

public enum OrderMode { None, Move, Attack, Build, Collect, Dock }

public partial class Main : Node2D
{
    public BoardView BoardView { get; private set; } = null!;
    public MapCamera MapCamera { get; private set; } = null!;
    public MapInput MapInput { get; private set; } = null!;
    public DebugHud Hud { get; private set; } = null!;
    public FleetView Fleet { get; private set; } = null!;
    public BattleState Battle { get; private set; } = null!;
    public int? SelectedShipId { get; private set; }
    public bool Busy { get; private set; }
    public bool FastChecks { get; set; }
    public OrderMode Mode { get; private set; }
    public Task CurrentOrder { get; private set; } = Task.CompletedTask;
    private BattleRules _rules = null!;
    private ShipClass? _building;
    private GridPosition? _resourceCell;
    private bool _resourceIsDock;
    private ConfirmationDialog _restartDialog = null!;

    public override void _Ready()
    {
        _rules=BattleRules.FromJson(FileAccess.GetFileAsString("res://data/balance.json"));
        var board=PrototypeBoard.Create(); var projection=new IsometricProjection(seed:board.Seed);
        var board=PrototypeBoard.Create(); var projection=new IsometricProjection(seed:board.Seed,width:board.Width,height:board.Height);
        Battle=SkirmishSetup.Create(board,_rules);
        BoardView=new BoardView { Name="Board",Board=board,Battle=Battle,Projection=projection }; AddChild(BoardView);
        Fleet=new FleetView { Name="Fleet",Battle=Battle,Projection=projection }; AddChild(Fleet);
        MapCamera=new MapCamera { Name="MapCamera",MapBounds=projection.BoardBounds(board.Width,board.Height) }; AddChild(MapCamera);
        MapCamera=new MapCamera { Name="MapCamera",MapBounds=projection.BoardBounds(board) }; AddChild(MapCamera);
        MapCamera.MakeCurrent(); MapCamera.FitBoard();
        MapInput=new MapInput { Name="MapInput",Camera=MapCamera };
        MapInput.Tapped+=SelectAtScreen; MapInput.Hovered+=PreviewAtScreen; AddChild(MapInput);
        Hud=new DebugHud { Name="DebugHud" };
        Hud.EndTurnRequested+=()=>RunSafely(EndPlayerTurn);
        Hud.RepairRequested+=()=>RunSafely(RepairSelected); Hud.BuildRequested+=BeginBuild;
        Hud.MortarRequested+=()=>RunSafely(BuyMortar); Hud.DockRequested+=BeginDock;
        Hud.CollectRequested+=BeginCollect; Hud.RadarRequested+=()=>RunSafely(BuyRadar);
        Hud.MortarRequested+=()=>RunSafely(BuyMortar);
        Hud.ResourceRequested+=()=>RunSafely(ConfirmResource); Hud.RadarRequested+=()=>RunSafely(BuyRadar);
        Hud.UpgradeRequested+=choice=>RunSafely(()=>ChooseUpgrade(choice));
        Hud.CreativeRequested+=()=> { Battle.SetCreative(!Battle.Creative); Refresh(); };
        Hud.MenuChanged+=()=> { MapInput.CancelGesture(); Refresh(); };
        Hud.MenuChanged+=()=> { MapInput.CancelGesture(); ClearMode(); Refresh(); };
        Hud.ExitRequested+=()=>GetTree().Quit();
        Hud.RestartRequested+=()=>_restartDialog.PopupCentered(); AddChild(Hud);
        _restartDialog=new ConfirmationDialog { Title="Начать заново?",DialogText="Текущий бой будет заменён новым.",
            OkButtonText="Новый бой",CancelButtonText="Продолжить бой" };
        _restartDialog.Confirmed+=Restart; AddChild(_restartDialog);
        GetViewport().SizeChanged+=OnViewportResized;
        Refresh(); Hud.ShowMessage("Выберите корабль, затем клетку или подсвеченную цель.");
        if(OS.HasFeature("debug")&&Array.Exists(OS.GetCmdlineUserArgs(),arg=>arg=="--smoke-test"))
            AddChild(new Tests.Runtime.PrototypeChecks { Game=this });
        if(OS.HasFeature("debug")&&Array.Exists(OS.GetCmdlineUserArgs(),arg=>arg=="--battle-test"))
            AddChild(new Tests.Runtime.BattleChecks { Game=this });
    }

    private void RunSafely(Func<Task> action)=>CurrentOrder=Guard(action);
    private async Task Guard(Func<Task> action)
    {
        try { await action(); }
        catch(Exception e) { GD.PushError(e.ToString()); Busy=false; Hud.ShowMessage("Ошибка действия. Подробности в журнале Godot."); Refresh(); }
    }
    private bool CanCommand=>!Busy&&!Hud.MenuVisible&&!Battle.IsOver&&Battle.ActiveSide==Side.Player;
    private Ship? Selected=>SelectedShipId is { } id?Battle.FindObserved(Side.Player,id):null;

    public void SelectAtScreen(Vector2 screen)
    {
        if(Busy || Hud.MenuVisible || Battle.PendingUpgrade(Side.Player) is not null) return;
        var air=Battle.ObservedShips(Side.Player).Where(s=>s.IsAirborne).FirstOrDefault(s=>
            (GetViewport().GetCanvasTransform()*(BoardView.Projection.GridToWorld(s.Position)+new Vector2(0,-62))).DistanceTo(screen)<24*MapCamera.Zoom.X);
        if(air is not null) { ClearMode(); SelectedShipId=air.Id; BoardView.Select(air.Position); Refresh(); return; }
        SelectCell(BoardView.Projection.WorldToGrid(BoardView.ToLocal(MapCamera.ScreenToWorld(screen))));
    }
    public void SelectCell(GridPosition cell)
    {
        if(Busy || Hud.MenuVisible || Battle.PendingUpgrade(Side.Player) is not null) return;
        if(!Battle.Board.Contains(cell)) { CancelOrder(); return; }
        BoardView.Select(cell); Hud.ShowTile(Battle,cell);
        var hit=Battle.ObservedAt(Side.Player,cell); var selected=Selected;
        _resourceCell=null; Hud.HideResource();
        if(selected?.Owner==Side.Player&&CanCommand)
        {
            if(Mode==OrderMode.Build&&_building is { } kind)
            {
                if(Battle.SpawnCells(selected.Id).Contains(cell)) { int id=selected.Id; RunSafely(()=>Perform(()=>Battle.Build(Side.Player,id,kind,cell))); return; }
                CancelOrder(); return;
            }
            if(Mode==OrderMode.Dock)
            {
                if(Battle.DockCells(selected.Id).Contains(cell)) { int id=selected.Id; RunSafely(()=>Perform(()=>Battle.BuildDock(Side.Player,id,cell))); return; }
                CancelOrder(); return;
            }
            if(Mode==OrderMode.Collect)
            {
                if(Battle.CollectionCells(selected.Id).Contains(cell)) { int id=selected.Id; RunSafely(()=>Perform(()=>Battle.Collect(Side.Player,id,cell))); return; }
                CancelOrder(); return;
            }
            if(Battle.TargetCells(selected.Id).Contains(cell))
            { int id=selected.Id; RunSafely(()=>Perform(()=>Battle.AttackAt(Side.Player,id,cell))); return; }
            if(Mode==OrderMode.None && (hit is null || hit.Id==selected.Id) &&
                (Battle.CollectionCells(selected.Id).Contains(cell) || Battle.DockCells(selected.Id).Contains(cell)))
            {
                _resourceCell=cell; _resourceIsDock=Battle.DockCells(selected.Id).Contains(cell);
                int price=_resourceIsDock?Battle.DockPrice(Side.Player):Battle.CollectionCost(Side.Player);
                Hud.ShowResource(_resourceIsDock,price,Battle.Credits(Side.Player)>=price); Refresh(); return;
            }
            if((hit is null||hit.IsAirborne||selected.IsAirborne)&&Battle.PathTo(selected.Id,cell).Count>1)
            {
                int id=selected.Id; RunSafely(()=>Perform(()=>Battle.Move(Side.Player,id,cell))); return;
            }
        }
        ClearMode(); SelectedShipId=hit?.Id;
        Hud.ShowMessage(""); Refresh();
    }

    public void BeginMove()
    {
        if(!CanCommand||Selected is not { Owner:Side.Player,CanMove:true }) return;
        ClearMode(); Refresh();
    }
    public void BeginAttack()
    {
        if(!CanCommand||Selected is not { Owner:Side.Player,AttacksRemaining:>0 }) return;
        ClearMode(); Refresh();
    }
    public void BeginBuild(ShipClass kind)
    {
        if(!CanCommand||SelectedShipId is not { } id) return;
        var reason=Battle.BuildBlockReason(Side.Player,id,kind);
        if(reason is not null) { Hud.ShowMessage(reason); return; }
        ClearMode(); _building=kind; Mode=OrderMode.Build;
        Hud.ShowMessage($"{Battle.Rules.Get(kind).Name} · {Battle.BuildPrice(Side.Player,kind)} Thors. Выберите клетку внутри зелёного контура.");
        Refresh();
    }
    public void BeginCollect()
    {
        if(!CanCommand||Selected is not { Owner:Side.Player } selected||Battle.CollectionCells(selected.Id).Count==0) return;
        ClearMode(); Mode=OrderMode.Collect; Hud.ShowMessage($"Выберите рыбу в зоне сбора · {Battle.CollectionCost(Side.Player)} Thors → 1 ресурс"); Refresh();
    }
    public void BeginDock()
    {
        if(!CanCommand||Selected is not { Owner:Side.Player } ship||Battle.DockCells(ship.Id).Count==0) return;
        ClearMode(); Mode=OrderMode.Dock; Hud.ShowMessage($"Выберите косяк · док {Battle.DockPrice(Side.Player)} Thors → 2 ресурса и +1 доход"); Refresh();
    }
    public Task BuyMortar()=>!CanCommand||SelectedShipId is not { } id?Task.CompletedTask:Perform(()=>Battle.BuyMortar(Side.Player,id));
    private Task ConfirmResource()=>!CanCommand||SelectedShipId is not { } id||_resourceCell is not { } cell
        ?Task.CompletedTask:Perform(()=>_resourceIsDock?Battle.BuildDock(Side.Player,id,cell):Battle.Collect(Side.Player,id,cell));
    public Task BuyRadar()=>!CanCommand||SelectedShipId is not { } id?Task.CompletedTask:Perform(()=>Battle.BuyRadar(Side.Player,id));
    public Task ChooseUpgrade(UpgradeChoice choice)=>!CanCommand||Battle.PendingUpgrade(Side.Player) is not { } ship?Task.CompletedTask:Perform(()=>Battle.ChooseUpgrade(Side.Player,ship.Id,choice));
    public Task RepairSelected()
    {
        if(!CanCommand||Selected is not { Owner:Side.Player } selected) return Task.CompletedTask;
        int id=selected.Id; return Perform(()=>Battle.Repair(Side.Player,id));
    }
    private async Task Perform(Func<CommandResult> action)
    {
        if(!CanCommand) return;
        var result=action();
        if(!result.Success) { Hud.ShowMessage(result.Message); Refresh(); return; }
        ClearMode();
        if(result.Kind==CommandKind.Build) SelectedShipId=result.TargetId;
        if(Selected is { } current) BoardView.Select(current.Position);
        Busy=true; Refresh(); Hud.ShowMessage(result.Message);
        try { if(!FastChecks) await Fleet.Animate(result); }
        finally { Busy=false; Refresh(); }
    }

    private void PreviewAtScreen(Vector2 screen)
    {
        if(!CanCommand||Selected is not { Owner:Side.Player } ship) return;
        var cell=BoardView.Projection.WorldToGrid(BoardView.ToLocal(MapCamera.ScreenToWorld(screen)));
        if(Mode==OrderMode.None)
        {
            BoardView.PreviewPath=Battle.PathTo(ship.Id,cell); BoardView.QueueRedraw();
        }
        if(Mode==OrderMode.None&&Battle.ObservedAt(Side.Player,cell) is { Owner:Side.Enemy } target&&Battle.CanAttack(ship.Id,target.Id))
            Hud.ShowMessage($"Урон {Math.Min(target.Health,Battle.Damage(ship,target)):0.##} · Ответ {Math.Min(ship.Health,Battle.PreviewCounterDamage(ship,target)):0.##}");
    }

    public async Task EndPlayerTurn()
    {
        if(!CanCommand) return;
        var ended=Battle.EndTurn(Side.Player); if(!ended.Success) return;
        ClearMode(); SelectedShipId=null; BoardView.Select(null); Busy=true; Refresh();
        Hud.ShowMessage(""); Hud.ShowOpponentTurn();
        try
        {
            if(!FastChecks) await ToSignal(GetTree().CreateTimer(1.25),SceneTreeTimer.SignalName.Timeout);
            for(int commands=0;commands<256&&Battle.ActiveSide==Side.Enemy&&!Battle.IsOver;commands++)
            {
                var result=SimpleOpponent.Step(Battle);
                if(!result.Success) throw new InvalidOperationException(result.Message);
                if(result.Kind==CommandKind.Attack) Hud.ShowMessage(result.Message);
                Refresh();
                if(!FastChecks)
                {
                    await Fleet.Animate(result);
                    await ToSignal(GetTree().CreateTimer(0.08),SceneTreeTimer.SignalName.Timeout);
                }
            }
            if(Battle.ActiveSide==Side.Enemy&&!Battle.IsOver)
            {
                GD.PushWarning("Opponent command budget reached; ending turn."); Battle.EndTurn(Side.Enemy);
            }
        }
        finally { Busy=false; Hud.HideOpponentTurn(); Refresh(); }
    }

    public void CancelOrder()
    {
        if(Busy) return;
        ClearMode(); SelectedShipId=null; BoardView.Select(null); Hud.ShowTile(Battle,null); Hud.ShowMessage(""); Refresh();
    }
    private void ClearMode() { Mode=OrderMode.None; _building=null; Hud.CloseMenus(); BoardView.PreviewPath=Array.Empty<GridPosition>(); }
    private void ClearMode() { Mode=OrderMode.None; _building=null; _resourceCell=null; Hud.CloseMenus(); BoardView.PreviewPath=Array.Empty<GridPosition>(); }
    public void Restart()
    {
        if(Busy) return;
        bool creative=Battle.Creative; LoadScenario(SkirmishSetup.Create(PrototypeBoard.Create(),_rules)); Battle.SetCreative(creative); Refresh();
        Hud.ShowMessage("Исследуйте море и сохраните свой Mothership.");
    }
    internal void LoadScenario(BattleState battle)
    {
        var projection=new IsometricProjection(seed:battle.Board.Seed); BoardView.Projection=projection; Fleet.Projection=projection;
        var projection=new IsometricProjection(seed:battle.Board.Seed,width:battle.Board.Width,height:battle.Board.Height); BoardView.Projection=projection; Fleet.Projection=projection;
        Battle=battle; Fleet.Battle=battle; BoardView.Battle=battle; BoardView.Board=battle.Board;
        MapCamera.MapBounds=BoardView.Projection.BoardBounds(battle.Board.Width,battle.Board.Height);
        MapCamera.MapBounds=BoardView.Projection.BoardBounds(battle.Board);
        CancelOrder(); MapInput.CancelGesture(); MapCamera.FitBoard();
    }

    public void Refresh()
    {
        var selected=Selected;
        if(selected is null) SelectedShipId=null;
        Fleet.SelectedId=SelectedShipId; BoardView.SelectedShipId=SelectedShipId;
        Hud.ShowTile(Battle,BoardView.Selected);
        BoardView.Reachable=CanCommand&&selected?.Owner==Side.Player
            ? Mode==OrderMode.Build?Battle.SpawnCells(selected.Id).ToArray()
              : Mode==OrderMode.None?Battle.Reachable(selected.Id).Keys.Where(p=>p!=selected.Position).ToArray():Array.Empty<GridPosition>()
              : Mode==OrderMode.None?Battle.Reachable(selected.Id).Keys.ToArray():Array.Empty<GridPosition>()
            : Array.Empty<GridPosition>();
        BoardView.Targets=CanCommand&&selected?.Owner==Side.Player&&Mode==OrderMode.None?
            Battle.TargetCells(selected.Id):Array.Empty<GridPosition>();
        BoardView.AttackArea=CanCommand&&selected?.Owner==Side.Player&&Mode==OrderMode.None?Battle.AttackCells(selected.Id):Array.Empty<GridPosition>();
        BoardView.AttackArea=Array.Empty<GridPosition>();
        BoardView.Collection=CanCommand&&selected?.Owner==Side.Player&&Mode==OrderMode.None?Battle.CollectionCells(selected.Id):Array.Empty<GridPosition>();
        BoardView.DockSites=CanCommand&&selected?.Owner==Side.Player&&Mode==OrderMode.None?Battle.DockCells(selected.Id):Array.Empty<GridPosition>();
        BoardView.Building=Mode==OrderMode.Build;
        Hud.UpdateBattle(Battle,selected,Busy,Mode); PositionActions();
        BoardView.QueueRedraw(); Fleet.QueueRedraw();
    }
    private void PositionActions()=>Hud.PositionActions(Selected is { } ship?GetViewport().GetCanvasTransform()*BoardView.ToGlobal(BoardView.Projection.GridToWorld(ship.Position)):null);
    private void PositionActions()
    {
        Vector2 Screen(GridPosition p)=>GetViewport().GetCanvasTransform()*BoardView.ToGlobal(BoardView.Projection.GridToWorld(p));
        Hud.PositionActions(_resourceCell is null&&Selected is { } ship?Screen(ship.Position):null);
        Hud.PositionResource(_resourceCell is { } cell?Screen(cell):null);
    }
    public override void _Process(double delta)=>PositionActions();
    private void OnViewportResized() { MapInput.CancelGesture(); MapCamera.FitBoard(); }
    public override void _ExitTree()=>GetViewport().SizeChanged-=OnViewportResized;
}
