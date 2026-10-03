using System;
using System.Collections.Generic;
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
    private IReadOnlyList<ReadyActionObject> _readyObjects = Array.Empty<ReadyActionObject>();
    public bool TurnConfirmationVisible => _turnConfirmation?.Visible == true;
    public int ReadyObjectCount => _readyObjects.Count;
    public event Action? TurnConfirmed, TurnConfirmationClosed;
    public event Action<ReadyActionObject>? ReadyObjectSelected;

    private void BuildTurnGuidance()
    {
        UiHints.Initialize();
        _readyJug = new ReadyActionJug { Name = "ReadyActionsAmphora", Size = new(68, 68),
            MouseFilter = Control.MouseFilterEnum.Ignore, TooltipText = "Objects with useful actions remaining" };
        _root.AddChild(_readyJug);
        _turnConfirmation = new Control { Name = "EndTurnConfirmation", MouseFilter = Control.MouseFilterEnum.Stop };
        _root.AddChild(_turnConfirmation);
        _turnConfirmation.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        var shade = new ColorRect { Color = new Color(0, 0, 0, .35f), MouseFilter = Control.MouseFilterEnum.Stop };
        _turnConfirmation.AddChild(shade);
        shade.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        _turnPaper = Panel(_turnConfirmation);
        _turnPaper.Name = "EndTurnConfirmationPaper";
        PapyrusGrain.Apply(_turnPaper);
        _turnBody = new VBoxContainer();
        _turnBody.AddThemeConstantOverride("separation", 10);
        _turnScroll = PapyrusModal.Wrap(_turnPaper, _turnBody, "ReadyActionScroll");
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
        var cancel = TextButton("Keep exploring", CloseTurnConfirmation);
        cancel.Name = "CancelEndTurn";
        cancel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        row.AddChild(cancel);
        var confirm = TextButton("End turn", () => { CloseTurnConfirmation(); TurnConfirmed?.Invoke(); });
        confirm.Name = "ConfirmEndTurn";
        confirm.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        row.AddChild(confirm);
        _turnBody.MinimumSizeChanged += LayoutTurnGuidance;
        UiHints.Changed += HintsChanged;
        TreeExiting += () => UiHints.Changed -= HintsChanged;
        _turnConfirmation.Hide();
    }
    private void HintsChanged()
    {
        if (!UiHints.Enabled) CloseTurnConfirmation();
        UpdateReadyActions(_namedBattle, _namedBattle is not null && !_end.Disabled);
    }
    private void UpdateReadyActions(BattleState? battle, bool canAct)
    {
        _readyObjects = battle is not null && canAct ? CachedReadyActions(battle) : Array.Empty<ReadyActionObject>();
        _readyJug.Visible = UiHints.Enabled && canAct;
        if (battle is not null) _readyJug.Update(_readyObjects.Count, FleetPalette.For(battle, Side.Player));
        if (TurnConfirmationVisible && (!canAct || !UiHints.Enabled)) CloseTurnConfirmation();
        LayoutTurnGuidance();
    }
    public void ShowTurnConfirmation()
    {
        if (!UiHints.Enabled || _end.Disabled || TurnConfirmationVisible) return;
        foreach (Node child in _readyGrid.GetChildren()) { _readyGrid.RemoveChild(child); child.QueueFree(); }
        foreach (var item in _readyObjects)
        {
            var button = new Button { Name = "ReadyObject" + (item.ShipClass is null ? "Town" : "Ship") + item.Id,
                CustomMinimumSize = new(54, 56), FocusMode = Control.FocusModeEnum.None,
                TooltipText = item.Name };
            PapyrusStyle.Button(button, 13);
            _readyGrid.AddChild(button);
            var icon = new ActionGlyph { Symbol = ReadySymbol(item.ShipClass), Position = new(11, 11), Size = new(32, 32),
                MouseFilter = Control.MouseFilterEnum.Ignore };
            button.AddChild(icon);
            button.Pressed += () => { CloseTurnConfirmation(); ReadyObjectSelected?.Invoke(item); };
        }
        _turnSummary.Text = _readyObjects.Count == 0 ? "No useful actions remain." : "These objects can still act this turn.";
        _turnConfirmation.Show();
        _turnPaper.ResetSize();
        LayoutTurnGuidance();
    }
    public void CloseTurnConfirmation()
    {
        if (!TurnConfirmationVisible) return;
        _turnConfirmation.Hide();
        TurnConfirmationClosed?.Invoke();
    }
    public System.Threading.Tasks.Task AnimateEndTurnStamp(bool instant) => ((EndTurnPaper)_end).StampAsync(instant);
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

