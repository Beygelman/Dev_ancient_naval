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
    private BattleRules _rules = null!;
    private GridPosition? _pendingCell;
    private int? _pendingTarget;
    private ShipClass? _building;
    private bool _repairPending;
    private ConfirmationDialog _restartDialog = null!;

    public override void _Ready()
    {
        _rules = BattleRules.FromJson(FileAccess.GetFileAsString("res://data/balance.json"));
        var board = PrototypeBoard.Create();
        var projection = new IsometricProjection();
        Battle = SkirmishSetup.Create(board, _rules);
        BoardView = new BoardView { Name = "Board", Board = board, Projection = projection };
        AddChild(BoardView);
        Fleet = new FleetView { Name = "Fleet", Battle = Battle, Projection = projection };
        AddChild(Fleet);
        MapCamera = new MapCamera { Name = "MapCamera", MapBounds = projection.BoardBounds(board.Width, board.Height) };
        AddChild(MapCamera);
        MapCamera.MakeCurrent(); MapCamera.FitBoard();
        MapInput = new MapInput { Name = "MapInput", Camera = MapCamera };
        MapInput.Tapped += SelectAtScreen;
        AddChild(MapInput);
        Hud = new DebugHud { Name = "DebugHud" };
        Hud.ResetRequested += MapCamera.FitBoard;
        Hud.ZoomRequested += factor => MapCamera.ZoomAt(GetViewportRect().Size / 2, factor);
        Hud.ConfirmRequested += () => RunSafely(ConfirmOrder);
        Hud.CancelRequested += CancelOrder;
        Hud.EndTurnRequested += () => RunSafely(EndPlayerTurn);
        Hud.RepairRequested += PreviewRepair;
        Hud.BuildRequested += BeginBuild;
        Hud.RestartRequested += () => _restartDialog.PopupCentered();
        AddChild(Hud);
        _restartDialog = new ConfirmationDialog { Title = "Начать заново?", DialogText = "Текущий бой будет заменён новым.", OkButtonText = "Новый бой", CancelButtonText = "Продолжить бой" };
        _restartDialog.Confirmed += Restart;
        AddChild(_restartDialog);
        GetViewport().SizeChanged += OnViewportResized;
        Refresh();
        if (OS.HasFeature("debug") && Array.Exists(OS.GetCmdlineUserArgs(), arg => arg == "--smoke-test"))
            AddChild(new Tests.Runtime.PrototypeChecks { Game = this });
        if (OS.HasFeature("debug") && Array.Exists(OS.GetCmdlineUserArgs(), arg => arg == "--battle-test"))
            AddChild(new Tests.Runtime.BattleChecks { Game = this });
    }

    private async void RunSafely(Func<Task> action)
    {
        try { await action(); }
        catch (Exception e) { GD.PushError(e.ToString()); Busy = false; Hud.ShowMessage("Ошибка действия. Подробности в журнале Godot."); Refresh(); }
    }

    private bool CanCommand => !Busy && !Battle.IsOver && Battle.ActiveSide == Side.Player;

    public void SelectAtScreen(Vector2 screen)
    {
        if (Busy) return;
        var local = BoardView.ToLocal(MapCamera.ScreenToWorld(screen));
        var cell = BoardView.Projection.WorldToGrid(local);
        SelectCell(cell);
    }

    public void SelectCell(GridPosition cell)
    {
        if (Busy) return;
        BoardView.Board.TryGetTile(cell, out var tile);
        BoardView.Select(tile?.Position);
        Hud.ShowTile(tile);
        var hit = Battle.At(cell);
        var selected = SelectedShipId is { } id ? Battle.Find(id) : null;
        _repairPending = false;
        if (tile is null) { ClearPending(); SelectedShipId = null; Refresh(); return; }
        if (_building is not null && selected is not null && CanCommand)
        {
            _pendingCell = Battle.SpawnCells(selected.Id).Contains(cell) ? cell : null;
            Hud.ShowMessage(_pendingCell is null ? "Нужна зелёная клетка рядом с Mothership." : $"Построить {_building} здесь? Подтвердите приказ.");
        }
        else if (selected?.Owner == Side.Player && hit?.Owner == Side.Enemy && CanCommand)
        {
            _pendingCell = null;
            _pendingTarget = Battle.CanAttack(selected.Id, hit.Id) ? hit.Id : null;
            Hud.ShowMessage(_pendingTarget is null ? "Цель вне дальности или атаки закончились." :
                $"Атака {hit.Definition.Name}: {Math.Min(hit.Health, Battle.Damage(selected, hit))} урона. Подтвердите приказ.");
        }
        else if (hit is not null)
        {
            ClearPending(); SelectedShipId = hit.Id;
            Hud.ShowMessage(hit.Owner == Side.Player ? ProfileHint(hit) : "Коралловый флот — противник. Для атаки сначала выберите свой корабль.");
        }
        else if (selected?.Owner == Side.Player && CanCommand)
        {
            _pendingTarget = null;
            var path = Battle.PathTo(selected.Id, cell);
            _pendingCell = path.Count > 1 ? cell : null;
            Hud.ShowMessage(_pendingCell is null ? "Сюда нельзя дойти в этом ходу." : $"Маршрут: {path.Count - 1} кл. Подтвердите перемещение.");
        }
        else ClearPending();
        Refresh();
    }

    private static string ProfileHint(Ship ship) => ship.Definition.ActionProfile switch
    {
        ActionProfile.Scout => $"Garrison: движение → атака → движение. Общий запас движения — {ship.Definition.Movement}.",
        ActionProfile.Heavy => "Kolonel: движение и одна атака либо две атаки с места. Ремонт вместо действий.",
        _ => ship.Definition.Class == ShipClass.Mothership ? "Mothership: доход и одна постройка за ход. Потеря флагмана означает поражение." :
            "Invader: движение → атака либо атака → движение. Ремонт вместо действий."
    };

    public void BeginBuild(ShipClass shipClass)
    {
        if (!CanCommand || SelectedShipId is not { } id) return;
        var reason = Battle.BuildBlockReason(Side.Player, id, shipClass);
        if (reason is not null) { Hud.ShowMessage(reason); return; }
        ClearPending(); _building = shipClass;
        Hud.ShowMessage($"{shipClass}: выберите зелёную клетку для постройки. Цена {_rules.Get(shipClass).Price}.");
        Refresh();
    }

    public void PreviewRepair()
    {
        if (!CanCommand || SelectedShipId is not { } id || Battle.Find(id)?.CanRepair != true) return;
        ClearPending(); _repairPending = true;
        Hud.ShowMessage($"Ремонт до +{_rules.RepairAmount} HP вместо движения и атак. Подтвердите приказ.");
        Refresh();
    }

    public async Task ConfirmOrder()
    {
        if (!CanCommand || SelectedShipId is not { } id) return;
        Vector2? targetPosition = _pendingTarget is { } targetId && Battle.Find(targetId) is { } enemy
            ? BoardView.Projection.GridToWorld(enemy.Position) : null;
        CommandResult? result = _repairPending ? Battle.Repair(Side.Player, id) :
            _building is { } kind && _pendingCell is { } spawn ? Battle.Build(Side.Player, id, kind, spawn) :
            _pendingTarget is { } target ? Battle.Attack(Side.Player, id, target) :
            _pendingCell is { } destination ? Battle.Move(Side.Player, id, destination) : null;
        if (result is null) return;
        ClearPending();
        if (result.Success)
        {
            if (result.Kind == CommandKind.Build) SelectedShipId = result.TargetId;
            if (SelectedShipId is { } selectedId && Battle.Find(selectedId) is { } current)
            {
                BoardView.Select(current.Position);
                Hud.ShowTile(BoardView.Board.GetTile(current.Position));
            }
        }
        Busy = true; Refresh();
        Hud.ShowMessage(result.Message);
        try { if (result.Success && !FastChecks) await Fleet.Animate(result, targetPosition); }
        finally { Busy = false; Refresh(); }
    }

    public async Task EndPlayerTurn()
    {
        if (!CanCommand) return;
        var ended = Battle.EndTurn(Side.Player);
        if (!ended.Success) return;
        ClearPending(); SelectedShipId = null; Busy = true; Refresh();
        Hud.ShowMessage("Противник отдаёт приказы…");
        try
        {
            for (int commands = 0; commands < 256 && Battle.ActiveSide == Side.Enemy && !Battle.IsOver; commands++)
            {
                var oldPositions = Battle.Ships.ToDictionary(s => s.Id, s => BoardView.Projection.GridToWorld(s.Position));
                var result = SimpleOpponent.Step(Battle);
                if (!result.Success) throw new InvalidOperationException(result.Message);
                Hud.ShowMessage(result.Message); Refresh();
                if (!FastChecks)
                {
                    await Fleet.Animate(result, oldPositions.TryGetValue(result.TargetId, out var point) ? point : null);
                    await ToSignal(GetTree().CreateTimer(0.08), SceneTreeTimer.SignalName.Timeout);
                }
            }
            if (Battle.ActiveSide == Side.Enemy && !Battle.IsOver)
            {
                GD.PushWarning("Opponent command budget reached; ending turn.");
                Battle.EndTurn(Side.Enemy);
            }
        }
        finally { Busy = false; Refresh(); }
        Hud.ShowMessage(Battle.IsOver ? "Бой окончен. Нажмите «Заново», чтобы сыграть ещё раз." : "Ваш ход. Выберите корабль.");
    }

    public void CancelOrder()
    {
        if (Busy) return;
        ClearPending(); SelectedShipId = null; BoardView.Select(null); Hud.ShowTile(null);
        Hud.ShowMessage("Приказ отменён. Выберите корабль."); Refresh();
    }
    private void ClearPending() { _pendingCell = null; _pendingTarget = null; _building = null; _repairPending = false; }

    public void Restart()
    {
        if (Busy) return;
        Battle = SkirmishSetup.Create(BoardView.Board, _rules); Fleet.Battle = Battle;
        CancelOrder(); MapInput.CancelGesture(); MapCamera.FitBoard();
        Hud.ShowMessage("Новый бой. Уничтожьте вражеский Mothership и сохраните свой.");
    }

    public void Refresh()
    {
        var selected = SelectedShipId is { } id ? Battle.Find(id) : null;
        if (selected is null) SelectedShipId = null;
        Fleet.SelectedId = SelectedShipId;
        BoardView.Reachable = CanCommand && selected?.Owner == Side.Player ?
            (_building is not null ? Battle.SpawnCells(selected.Id).ToArray() : Battle.Reachable(selected.Id).Keys.Where(p => p != selected.Position).ToArray()) : Array.Empty<GridPosition>();
        BoardView.Targets = CanCommand && selected?.Owner == Side.Player && _building is null ?
            Battle.Ships.Where(s => Battle.CanAttack(selected.Id, s.Id)).Select(s => s.Position).ToArray() : Array.Empty<GridPosition>();
        BoardView.Building = _building is not null;
        BoardView.PreviewPath = selected is not null && _pendingCell is { } cell && _building is null ? Battle.PathTo(selected.Id, cell) : Array.Empty<GridPosition>();
        string? confirm = _repairPending ? "Ремонт +HP" : _pendingTarget is not null ? "Атаковать" :
            _pendingCell is not null ? (_building is not null ? "Построить" : "Переместить") : null;
        Hud.UpdateBattle(Battle, selected, Busy, confirm, SelectedShipId is not null);
        BoardView.QueueRedraw(); Fleet.QueueRedraw();
    }

    public override void _Process(double delta) => Hud.ShowZoom(MapCamera.Zoom.X);
    private void OnViewportResized() { MapInput.CancelGesture(); MapCamera.FitBoard(); }
    public override void _ExitTree() => GetViewport().SizeChanged -= OnViewportResized;
}
