using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Units;
using DevAncientNaval.Presentation.Map;
using Godot;
using Side = DevAncientNaval.Core.Units.Side;

namespace DevAncientNaval.Presentation.UI;

public partial class DebugHud
{
    private Control _turnConfirmation = null!;
    private PanelContainer _turnPaper = null!;
    private ScrollContainer _turnScroll = null!;
    private VBoxContainer _turnBody = null!;
    private GridContainer _readyGrid = null!;
    private ReadyActionJug _readyJug = null!;
    private Label _turnSummary = null!;
    private GuidanceLabel _turnGuidance = null!;
    private RollingModalPaper _readySidePaper = null!;
    private ScrollContainer _readySideScroll = null!;
    private VBoxContainer _readySideBody = null!;
    private IReadOnlyList<ReadyActionObject> _readyObjects = Array.Empty<ReadyActionObject>();
    private IReadOnlyList<ReadyActionObject>? _sideObjects;
    private bool _turnClosing;
    public bool TurnConfirmationVisible => _turnConfirmation?.Visible == true;
    public bool ReadyActionsMenuVisible => _readySidePaper?.Visible == true;
    public int ReadyObjectCount => _readyObjects.Count;
    public event Action? TurnConfirmed, TurnConfirmationClosed;
    public event Action<ReadyActionObject>? ReadyObjectSelected;
    public event Action? ReadyActionsMenuChanged;

    private void BuildTurnGuidance()
    {
        UiHints.Initialize();
        _readyJug = new ReadyActionJug { Name = "ReadyActionsAmphora", Size = new(68, 68),
            MouseFilter = Control.MouseFilterEnum.Stop, TooltipText = "Objects with useful actions remaining" };
        _root.AddChild(_readyJug);
        _readyJug.Pressed += ToggleReadyActionsMenu;
        _readySidePaper = new RollingModalPaper { Name = "ReadyActionsSidePaper", MouseFilter = Control.MouseFilterEnum.Stop };
        _root.AddChild(_readySidePaper);
        _readySideBody = new VBoxContainer();
        _readySideBody.AddThemeConstantOverride("separation", 7);
        _readySideScroll = PapyrusModal.Wrap(_readySidePaper, _readySideBody, "ReadyActionsSideScroll");
        _readySideScroll.VerticalScrollMode = ScrollContainer.ScrollMode.ShowNever;
        _readySidePaper.Hide();
        _turnConfirmation = new Control { Name = "EndTurnConfirmation", MouseFilter = Control.MouseFilterEnum.Stop };
        _root.AddChild(_turnConfirmation);
        _turnConfirmation.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        var shade = new ColorRect { Color = new Color(0, 0, 0, .35f), MouseFilter = Control.MouseFilterEnum.Stop };
        _turnConfirmation.AddChild(shade);
        shade.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        _turnPaper = new RollingModalPaper { Name = "EndTurnConfirmationPaper", MouseFilter = Control.MouseFilterEnum.Stop };
        _turnConfirmation.AddChild(_turnPaper);
        _turnBody = new VBoxContainer();
        _turnBody.AddThemeConstantOverride("separation", 10);
        _turnScroll = PapyrusModal.Wrap(_turnPaper, _turnBody, "ReadyActionScroll");
        _turnScroll.VerticalScrollMode = ScrollContainer.ScrollMode.ShowNever;
        _turnBody.AddChild(Label("End your turn?", 23, true));
        _turnSummary = Label("", 15, true);
        _turnSummary.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _turnBody.AddChild(_turnSummary);
        _readyGrid = new GridContainer { Name = "ReadyActionGrid", Columns = 5,
            SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter };
        _readyGrid.AddThemeConstantOverride("h_separation", 8);
        _readyGrid.AddThemeConstantOverride("v_separation", 8);
        _turnBody.AddChild(_readyGrid);
        _turnGuidance = new GuidanceLabel { Name = "TurnGuidance", Text = "Choose an object to return to its remaining actions." };
        _turnConfirmation.AddChild(_turnGuidance);
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 12);
        _turnBody.AddChild(row);
        var cancel = MenuBrush("CancelEndTurn", "Keep exploring", CloseTurnConfirmation);
        cancel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        row.AddChild(cancel);
        var confirm = MenuBrush("ConfirmEndTurn", "End turn", async () =>
        {
            await CloseTurnPaper();
            TurnConfirmed?.Invoke();
        });
        confirm.Underline = true;
        confirm.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        row.AddChild(confirm);
        _turnBody.MinimumSizeChanged += LayoutTurnGuidance;
        UiHints.Changed += HintsChanged;
        TreeExiting += () => UiHints.Changed -= HintsChanged;
        _turnConfirmation.Hide();
    }
    private void HintsChanged()
    {
        if (!UiHints.Enabled)
        {
            CloseTurnConfirmation();
            CloseReadyActionsMenu();
        }
        UpdateReadyActions(_namedBattle, _namedBattle is not null && !_end.Disabled);
    }
    private void UpdateReadyActions(BattleState? battle, bool canAct)
    {
        _readyObjects = battle is not null && canAct ? CachedReadyActions(battle) : Array.Empty<ReadyActionObject>();
        _readyJug.Visible = UiHints.Enabled && canAct;
        if (battle is not null)
        {
            var color = FleetPalette.For(battle, Side.Player);
            _readyJug.Update(_readyObjects.Count, color, battle.PlayerColor);
            ((EndTurnPaper)_end).NationInk = color.Darkened(.25f);
            ((EndTurnPaper)_end).SetHumanTurn(battle.ActiveSide == Side.Player && !battle.IsOver && !battle.PlayerDefeated,
                InstantPaperAnimations);
        }
        if (TurnConfirmationVisible && (!canAct || !UiHints.Enabled)) CloseTurnConfirmation();
        if (ReadyActionsMenuVisible)
        {
            if (!canAct || !UiHints.Enabled) CloseReadyActionsMenu();
            else if (!ReferenceEquals(_sideObjects, _readyObjects)) PopulateReadySide();
        }
        LayoutTurnGuidance();
    }
    public void ShowTurnConfirmation()
    {
        if (!UiHints.Enabled || _end.Disabled || TurnConfirmationVisible) return;
        CloseReadyActionsMenu();
        ClearChildren(_readyGrid);
        foreach (var item in _readyObjects) _readyGrid.AddChild(ReadyButton(item, sideMenu: false));
        _turnSummary.Text = _readyObjects.Count == 0 ? "No useful actions remain." : "These objects can still act this turn.";
        _turnConfirmation.Show();
        _turnClosing = false;
        _turnPaper.ResetSize();
        LayoutTurnGuidance();
        _ = ((RollingModalPaper)_turnPaper).OpenAsync(InstantPaperAnimations);
    }
    public void CloseTurnConfirmation() => _ = CloseTurnPaper();
    private async Task CloseTurnPaper()
    {
        if (!TurnConfirmationVisible || _turnClosing) return;
        _turnClosing = true;
        await ((RollingModalPaper)_turnPaper).FoldAsync(InstantPaperAnimations);
        _turnConfirmation.Hide();
        _turnClosing = false;
        TurnConfirmationClosed?.Invoke();
    }
    public Task AnimateEndTurnStamp(bool instant) => ((EndTurnPaper)_end).StampAsync(instant);
    public Task AnimateEndTurnFold(bool instant) => ((EndTurnPaper)_end).FoldAsync(instant);

    private void ToggleReadyActionsMenu()
    {
        if (ReadyActionsMenuVisible)
        {
            CloseReadyActionsMenu();
            return;
        }
        if (!UiHints.Enabled || _end.Disabled || MenuVisible || TurnConfirmationVisible) return;
        PopulateReadySide();
        _readySidePaper.Show();
        LayoutTurnGuidance();
        _ = _readySidePaper.OpenAsync(InstantPaperAnimations);
        ReadyActionsMenuChanged?.Invoke();
    }
    public void CloseReadyActionsMenu()
    {
        if (_readySidePaper is null) return;
        _readySidePaper.Hide();
        _sideObjects = null;
        ReadyActionsMenuChanged?.Invoke();
    }
    private void PopulateReadySide()
    {
        ClearChildren(_readySideBody);
        _sideObjects = _readyObjects;
        if (_readyObjects.Count == 0)
        {
            var empty = Label("No useful actions remain.", 13, true);
            empty.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            _readySideBody.AddChild(empty);
        }
        foreach (var item in _readyObjects) _readySideBody.AddChild(ReadyButton(item, sideMenu: true));
    }
    private BrushPaperButton ReadyButton(ReadyActionObject item, bool sideMenu)
    {
        var button = new BrushPaperButton { Name = (sideMenu ? "SideReady" : "ReadyObject")
                + (item.ShipClass is null ? "Town" : "Ship") + item.Id,
            CustomMinimumSize = new(sideMenu ? 92 : 54, 56), TooltipText = ReadyDescription(item),
            SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter };
        button.AddChild(new ActionGlyph { Symbol = ReadySymbol(item.ShipClass),
            Position = new(sideMenu ? 30 : 11, 11), Size = new(32, 32), MouseFilter = Control.MouseFilterEnum.Ignore });
        button.Pressed += async () =>
        {
            if (!sideMenu) await CloseTurnPaper();
            CloseReadyActionsMenu();
            ReadyObjectSelected?.Invoke(item);
        };
        return button;
    }
    internal static string ReadyDescription(ReadyActionObject item)
    {
        var lines = new List<string> { item.Name };
        void Add(ReadyActionKind flag, string text)
        {
            if ((item.Actions & flag) != 0) lines.Add(text);
        }
        Add(ReadyActionKind.Move, item.ShipClass == ShipClass.Balloon ? "Fly to an available tile" : "Sail to an available tile");
        Add(ReadyActionKind.Attack, "Fire at a target and survive its reply");
        Add(ReadyActionKind.Build, item.ShipClass switch
        {
            ShipClass.Fishing => "Build a gun tower or lighthouse",
            ShipClass.Mothership => "Build a combat ship or sea tower",
            _ => "Build a combat ship"
        });
        Add(ReadyActionKind.Upgrade, "Upgrade this town");
        Add(ReadyActionKind.Collect, "Collect a nearby fish resource");
        Add(ReadyActionKind.Dock, "Build a fishing dock on a nearby reef");
        Add(ReadyActionKind.Capture, "Capture a nearby village");
        Add(ReadyActionKind.Loot, "Collect nearby ancient relics");
        return string.Join("\n", lines);
    }
    private static void ClearChildren(Node node)
    {
        foreach (Node child in node.GetChildren())
        {
            node.RemoveChild(child);
            child.QueueFree();
        }
    }
    private void LayoutTurnGuidance()
    {
        if (_readyJug is null || _end is null) return;
        _readyJug.Position = _end.Position + new Vector2((_end.Size.X - _readyJug.Size.X) / 2, -_readyJug.Size.Y - 6);
        if (_turnPaper is null) return;
        var viewport = UiScale.LogicalViewport(this);
        PapyrusModal.Layout(_turnPaper, _turnScroll, _turnBody, viewport);
        _turnPaper.Position = (viewport - _turnPaper.Size) / 2;
        _turnGuidance.Position = _turnPaper.Position + new Vector2(0, _turnPaper.Size.Y + 8);
        _turnGuidance.Size = new(_turnPaper.Size.X, 36);
        if (_readySidePaper is null || !ReadyActionsMenuVisible) return;
        var margin = _readySidePaper.GetThemeStylebox("panel").GetMinimumSize();
        float width = Math.Min(148, viewport.X - 36);
        float height = Math.Min(viewport.Y * .6f, _readySideBody.GetCombinedMinimumSize().Y + margin.Y);
        _readySidePaper.CustomMinimumSize = new(width, 0);
        _readySideScroll.CustomMinimumSize = new(0, Math.Max(1, height - margin.Y));
        _readySidePaper.Size = new(width, height);
        _readySidePaper.Position = new(viewport.X - width - 18,
            Math.Max(18, _readyJug.Position.Y - height - 10));
    }
    private static ActionSymbol ReadySymbol(ShipClass? kind) => kind switch
    {
        null => ActionSymbol.City,
        ShipClass.Mothership => ActionSymbol.City,
        ShipClass.Fishing => ActionSymbol.Support,
        ShipClass.Garrison => ActionSymbol.Scout,
        ShipClass.Invader => ActionSymbol.Standard,
        ShipClass.Kolonel => ActionSymbol.Heavy,
        ShipClass.Togus or ShipClass.AncientGun => ActionSymbol.Mortar,
        ShipClass.CannonTower => ActionSymbol.Tower,
        ShipClass.Lighthouse => ActionSymbol.Lighthouse,
        ShipClass.FishingDock => ActionSymbol.Fishing,
        _ => ActionSymbol.Balloon
    };
}
