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
        Language.Initialize();
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
        Fleet.GunFired = Ambience.ScareGulls;
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
        MapInput.Held += SelectAtScreen;
        MapInput.Hovered += PreviewAtScreen;
        MapInput.Canceled += CancelOrder;
        MapInput.KeyboardEnabled = () => !_endingStamp && !Hud.TurnConfirmationVisible && _voyageWelcome?.IsOpen != true && _victory?.IsOpen != true && _rewards?.IsOpen != true && !Busy && !_sessionLoading && _home?.IsOpen != true && !Hud.MenuVisible && Battle.PendingUpgrade(Side.Player) is null;
        MapInput.GameplayShortcutsEnabled = () => CanCommand && Battle.PendingUpgrade(Side.Player) is null;
        MapInput.EndTurnRequested += () => RunSafely(RequestEndPlayerTurn);
        MapInput.RepairRequested += () => RunSafely(RepairSelected);
        AddChild(MapInput);
        Hud = new DebugHud
        {
            Name = "DebugHud"
        };
        Hud.EndTurnRequested += () => RunSafely(RequestEndPlayerTurn);
        Hud.RepairRequested += () => RunSafely(RepairSelected);
        Hud.BuildRequested += BeginBuild;
        Hud.CanScuttleShip = ship => CanCommand && Battle.CanScuttle(Side.Player, ship.Id);
        Hud.ScuttleRequested += () => RunSafely(() => SelectedShipId is { } id ? Perform(b => b.Scuttle(Side.Player, id)) : Task.CompletedTask);
        Hud.SalvoRequested += twice => RunSafely(() => ChooseSalvo(twice));
        Hud.MortarRequested += () => RunSafely(BuyMortar);
        Hud.ResourceRequested += () => RunSafely(ConfirmResource);
        Hud.RadarRequested += () => RunSafely(BuyRadar);
        Hud.UpgradeRequested += choice => RunSafely(() => ChooseUpgrade(choice));
        Hud.LootRequested += () => RunSafely(LootTreasury);
        Hud.BombRequested += () => RunSafely(DropBomb);
        Hud.CaptureRequested += () => RunSafely(CaptureVillage);
        Hud.CaptureStoryRequested += id => RunSafely(() => CaptureVillage(id));
        Hud.TreasuryStoryRequested += id => RunSafely(() => LootTreasury(id));
        Hud.FortifyRequested += () => RunSafely(FortifyVillage);
        Hud.PortRequested += () => RunSafely(() => CanCommand && SelectedVillageId is { } id ? Perform(b => b.BuildPort(Side.Player, id)) : Task.CompletedTask);
        Hud.VillageUpgradeRequested += () => RunSafely(UpgradeSelectedVillage);
        Hud.GodEyeRequested += () => RunSafely(async () =>
        {
            Battle.SetGodEye(!Battle.GodEye);
            Refresh();
            await SaveSessionAsync();
        });
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
        InitializeTurnGuidance();
        InitializeRewards();
        Fleet.FocusTarget = FocusVisibleTarget;
        MapCamera.ViewChanged += PositionActions;
        InitializeOutcome();
        GetViewport().SizeChanged += OnViewportResized;
        Refresh();
        Hud.ShowMessage("Select a ship, then a tile or highlighted target. Glowing fish can be collected directly.");
        InitializeSession();
        InitializeTutorials();
        UiScale.Changed += Refresh;
        if (OS.HasFeature("debug") && Array.Exists(OS.GetCmdlineUserArgs(), arg => arg == "--tutorial-capture0206-test"))
            AddChild(new Tests.Runtime.TutorialCapture0206Checks { Game = this });
        if (OS.HasFeature("debug") && Array.Exists(OS.GetCmdlineUserArgs(), arg => arg == "--tutorial0206-test"))
            AddChild(new Tests.Runtime.Tutorial0206Checks { Game = this });
        if (OS.HasFeature("debug") && Array.Exists(OS.GetCmdlineUserArgs(), arg => arg == "--hints0206-test"))
            AddChild(new Tests.Runtime.Hints0206Checks { Game = this });
        if (OS.HasFeature("debug") && Array.Exists(OS.GetCmdlineUserArgs(), arg => arg == "--world-visual0206-test"))
            AddChild(new Tests.Runtime.NativeWorld0206Checks { Game = this });
        if (OS.HasFeature("debug") && Array.Exists(OS.GetCmdlineUserArgs(), arg => arg == "--animation0206-test"))
            AddChild(new Tests.Runtime.Animation0206Checks { Game = this });
        if (OS.HasFeature("debug") && Array.Exists(OS.GetCmdlineUserArgs(), arg => arg == "--ui0205-test"))
            AddChild(new Tests.Runtime.Ui0205Checks { Game = this });
        if (OS.HasFeature("debug") && Array.Exists(OS.GetCmdlineUserArgs(), arg => arg == "--construction0205-test"))
            AddChild(new Tests.Runtime.Construction0205Checks { Game = this });
        if (OS.HasFeature("debug") && Array.Exists(OS.GetCmdlineUserArgs(), arg => arg == "--world-visual0205-test"))
            AddChild(new Tests.Runtime.WorldVisual0205Checks { Game = this });
        if (OS.HasFeature("debug") && Array.Exists(OS.GetCmdlineUserArgs(), arg => arg == "--ui0204-test"))
            AddChild(new Tests.Runtime.Ui0204Checks { Game = this });
        if (OS.HasFeature("debug") && Array.Exists(OS.GetCmdlineUserArgs(), arg => arg == "--trade-glyph0204-test"))
            AddChild(new Tests.Runtime.TradeGlyph0204Checks { Game = this });
        if (OS.HasFeature("debug") && Array.Exists(OS.GetCmdlineUserArgs(), arg => arg == "--world-visual0204-test"))
            AddChild(new Tests.Runtime.WorldVisual0204Checks { Game = this });
        if (OS.HasFeature("debug") && Array.Exists(OS.GetCmdlineUserArgs(), arg => arg == "--world-visual0203-test"))
            AddChild(new DevAncientNaval.Tests.Runtime.WorldVisual0203Checks { Game = this });
        if (OS.HasFeature("debug") && Array.Exists(OS.GetCmdlineUserArgs(), arg => arg == "--ui0203-test"))
            AddChild(new Tests.Runtime.Ui0203Checks { Game = this });
        if (OS.HasFeature("debug") && Array.Exists(OS.GetCmdlineUserArgs(), arg => arg == "--language0202-test"))
            AddChild(new Tests.Runtime.Language0202Checks { Game = this });
        if (OS.HasFeature("debug") && Array.Exists(OS.GetCmdlineUserArgs(), arg => arg == "--lighthouse-town0202-test"))
            AddChild(new Tests.Runtime.LighthouseTown0202Checks { Game = this });
        if (OS.HasFeature("debug") && Array.Exists(OS.GetCmdlineUserArgs(), arg => arg == "--world-art0202-test"))
            AddChild(new Tests.Runtime.WorldArt0202Checks { Game = this });
        if (OS.HasFeature("debug") && Array.Exists(OS.GetCmdlineUserArgs(), arg => arg == "--heavens0202-test"))
            AddChild(new Tests.Runtime.Heavens0202Checks { Game = this });
        if (OS.HasFeature("debug") && Array.Exists(OS.GetCmdlineUserArgs(), arg => arg == "--refinement021-test"))
            AddChild(new Tests.Runtime.Refinement021Checks { Game = this });
        if (OS.HasFeature("debug") && Array.Exists(OS.GetCmdlineUserArgs(), arg => arg == "--refinement020-test"))
            AddChild(new Tests.Runtime.Refinement020ArtChecks { Game = this });
        if (OS.HasFeature("debug") && Array.Exists(OS.GetCmdlineUserArgs(), arg => arg == "--world-mode-test"))
            AddChild(new Tests.Runtime.WorldModeChecks { Game = this });
        if (OS.HasFeature("debug") && Array.Exists(OS.GetCmdlineUserArgs(), arg => arg == "--story-test"))
            AddChild(new Tests.Runtime.ActionStoryChecks { Game = this });
        if (OS.HasFeature("debug") && Array.Exists(OS.GetCmdlineUserArgs(), arg => arg == "--fleet-art-test"))
            AddChild(new Tests.Runtime.FleetArtChecks { Game = this });
        if (OS.HasFeature("debug") && Array.Exists(OS.GetCmdlineUserArgs(), arg => arg == "--victory-test"))
            AddChild(new Tests.Runtime.VictoryChecks { Game = this });
        if (OS.HasFeature("debug") && Array.Exists(OS.GetCmdlineUserArgs(), arg => arg == "--ports-test"))
            AddChild(new Tests.Runtime.PortCityChecks { Game = this });
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

    private bool CanCommand => !_endingStamp && !Hud.TurnConfirmationVisible && _voyageWelcome?.IsOpen != true && _victory?.IsOpen != true && _rewards?.IsOpen != true && !Busy && !_sessionLoading && _home?.IsOpen != true && !Hud.MenuVisible && !Battle.IsOver && !Battle.PlayerDefeated && Battle.ActiveSide == Side.Player;
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
        SuspendTutorials();
        Hud.CloseTurnConfirmation();
        HideOutcome();
        Hud.HideHeavenlyAssistance();
        var projection = new IsometricProjection(battle.Board);
        BoardView.Projection = projection;
        Fleet.Projection = projection;
        Battle = battle;
        ResetEncounterPresentation();
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

    public override void _ExitTree()
    {
        GetViewport().SizeChanged -= OnViewportResized;
        UiScale.Changed -= Refresh;
    }
}
