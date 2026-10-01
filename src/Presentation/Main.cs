using System;
using System.Linq;
using System.Threading.Tasks;
using DevAncientNaval.Core.AI;
using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Units;
using DevAncientNaval.Core.World;
using DevAncientNaval.Presentation.Camera;
using DevAncientNaval.Presentation.Input;
using DevAncientNaval.Presentation.Map;
using DevAncientNaval.Presentation.UI;
using Godot;
using Side = DevAncientNaval.Core.Units.Side;

namespace DevAncientNaval.Presentation;
public enum OrderMode
{
    None,
    Build
}

public partial class Main : Node2D
{
    public BoardView BoardView { get; private set; } = null !;
    public MapCamera MapCamera { get; private set; } = null !;
    public MapInput MapInput { get; private set; } = null !;
    public DebugHud Hud { get; private set; } = null !;
    public FleetView Fleet { get; private set; } = null !;
    public WorldAmbience Ambience { get; private set; } = null !;
    public BattleState Battle { get; private set; } = null !;
    public int? SelectedShipId { get; private set; }
    public int? SelectedVillageId { get; private set; }
    public bool Busy { get; private set; }
    public bool FastChecks { get; set; }
    public OrderMode Mode { get; private set; }
    public Task CurrentOrder { get; private set; } = Task.CompletedTask;

    private BattleRules _rules = null !;
    private ShipClass? _building;
    private GridPosition? _resourceCell;
    private bool _resourceIsDock;
    private GridPosition? _previewCell;
    private Transform2D _lastCanvasTransform;
    private Vector2 _lastViewportSize;
    private readonly bool _mapPreview = Array.Exists(OS.GetCmdlineUserArgs(), arg => arg == "--map-preview");
    public override void _Ready()
    {
        _rules = BattleRules.FromJson(FileAccess.GetFileAsString("res://data/balance.json"));
        var board = PrototypeBoard.Create();
        var projection = new IsometricProjection(board);
        Battle = SkirmishSetup.Create(board, _rules);
        BoardView = new BoardView
        {
            Name = "Board",
            Board = board,
            Battle = Battle,
            Projection = projection
        };
        AddChild(BoardView);
        Ambience = new WorldAmbience
        {
            Name = "SeaLife",
            BoardView = BoardView
        };
        AddChild(Ambience);
        BoardView.DrawTownLife = Ambience.DrawTownLife;
        Fleet = new FleetView
        {
            Name = "Fleet",
            Landscape = BoardView,
            Battle = Battle,
            Projection = projection
        };
        AddChild(Fleet);
        MapCamera = new MapCamera
        {
            Name = "MapCamera",
            MapBounds = projection.BoardBounds(board)
        };
        AddChild(MapCamera);
        MapCamera.MakeCurrent();
        MapCamera.FitBoard();
        MapInput = new MapInput
        {
            Name = "MapInput",
            Camera = MapCamera
        };
        MapInput.Tapped += SelectAtScreen;
        MapInput.Hovered += PreviewAtScreen;
        MapInput.Canceled += CancelOrder;
        AddChild(MapInput);
        Hud = new DebugHud
        {
            Name = "DebugHud"
        };
        Hud.EndTurnRequested += () => RunSafely(EndPlayerTurn);
        Hud.RepairRequested += () => RunSafely(RepairSelected);
        Hud.BuildRequested += BeginBuild;
        Hud.MortarRequested += () => RunSafely(BuyMortar);
        Hud.ResourceRequested += () => RunSafely(ConfirmResource);
        Hud.RadarRequested += () => RunSafely(BuyRadar);
        Hud.UpgradeRequested += choice => RunSafely(() => ChooseUpgrade(choice));
        Hud.LootRequested += () => RunSafely(LootTreasury);
        Hud.BombRequested += () => RunSafely(DropBomb);
        Hud.CaptureRequested += () => RunSafely(CaptureVillage);
        Hud.FortifyRequested += () => RunSafely(FortifyVillage);
        Hud.CreativeRequested += () =>
        {
            Battle.SetCreative(!Battle.Creative);
            Refresh();
        };
        Hud.MenuChanged += () =>
        {
            MapInput.CancelGesture();
            ClearMode();
            Refresh();
        };
        Hud.ExitRequested += ExitSession;
        Hud.HomeRequested += ShowHome;
        Hud.RestartRequested += ShowColorSelection;
        AddChild(Hud);
        GetViewport().SizeChanged += OnViewportResized;
        Refresh();
        Hud.ShowMessage("Select a ship, then a tile or highlighted target. Glowing fish can be collected directly.");
        InitializeSession();
        if (OS.HasFeature("debug") && Array.Exists(OS.GetCmdlineUserArgs(), arg => arg == "--menu-test"))
            AddChild(new Tests.Runtime.MenuChecks { Game = this });
        if (OS.HasFeature("debug") && Array.Exists(OS.GetCmdlineUserArgs(), arg => arg == "--smoke-test"))
            AddChild(new Tests.Runtime.PrototypeChecks { Game = this });
        if (OS.HasFeature("debug") && Array.Exists(OS.GetCmdlineUserArgs(), arg => arg == "--battle-test"))
            AddChild(new Tests.Runtime.BattleChecks { Game = this });
        if (OS.HasFeature("debug") && Array.Exists(OS.GetCmdlineUserArgs(), arg => arg == "--effects-test"))
            AddChild(new Tests.Runtime.EffectsChecks { Game = this });
        if (OS.HasFeature("debug") && Array.Exists(OS.GetCmdlineUserArgs(), arg => arg == "--grid-test"))
            AddChild(new Tests.Runtime.GridChecks { Game = this });
        if (OS.HasFeature("debug") && Array.Exists(OS.GetCmdlineUserArgs(), arg => arg == "--sea-test"))
            AddChild(new Tests.Runtime.SeaChecks { Game = this });
        if (OS.HasFeature("debug") && Array.Exists(OS.GetCmdlineUserArgs(), arg => arg == "--performance-test"))
            AddChild(new Tests.Runtime.InteractionPerformanceChecks { Game = this });
        if (OS.HasFeature("debug") && Array.Exists(OS.GetCmdlineUserArgs(), arg => arg == "--world-test"))
            AddChild(new Tests.Runtime.WorldRefinementChecks { Game = this });
        if (OS.HasFeature("debug") && Array.Exists(OS.GetCmdlineUserArgs(), arg => arg == "--income-test"))
            AddChild(new Tests.Runtime.IncomeChecks { Game = this });
        if (OS.HasFeature("debug") && Array.Exists(OS.GetCmdlineUserArgs(), arg => arg == "--optimization-test"))
            AddChild(new Tests.Runtime.OptimizationChecks { Game = this });
    }

    private void RunSafely(Func<Task> action) => CurrentOrder = Guard(action);
    private async Task Guard(Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (Exception e)
        {
            GD.PushError(e.ToString());
            Busy = false;
            Hud.ShowMessage("Action failed. See the Godot log for details.");
            Refresh();
        }
    }

    private bool CanCommand => !Busy && !_sessionLoading && _home?.IsOpen != true && !Hud.MenuVisible && !Battle.IsOver && !Battle.PlayerDefeated && Battle.ActiveSide == Side.Player;
    private Ship? Selected => SelectedShipId is { } id ? Battle.FindObserved(Side.Player, id) : null;
    private Village? SelectedVillage => SelectedVillageId is { } id ? Battle.ObservedVillages(Side.Player).FirstOrDefault(v => v.Id == id) : null;

    public void Restart()
    {
        if (Busy)
            return;
        bool creative = Battle.Creative;
        LoadScenario(SkirmishSetup.Create(PrototypeBoard.Create(), _rules));
        Battle.SetCreative(creative);
        Refresh();
        Hud.ShowMessage("Explore the sea and protect your Mothership.");
    }

    internal void LoadScenario(BattleState battle)
    {
        var projection = new IsometricProjection(battle.Board);
        BoardView.Projection = projection;
        Fleet.Projection = projection;
        Battle = battle;
        InvalidateGameplayPresentation();
        Fleet.Battle = battle;
        BoardView.Battle = battle;
        BoardView.Board = battle.Board;
        MapCamera.MapBounds = BoardView.Projection.BoardBounds(battle.Board);
        CancelOrder();
        MapInput.CancelGesture();
        MapCamera.FitBoard();
    }

    private void OnViewportResized()
    {
        MapInput.CancelGesture();
        MapCamera.FitBoard();
    }

    public override void _ExitTree() => GetViewport().SizeChanged -= OnViewportResized;
}
