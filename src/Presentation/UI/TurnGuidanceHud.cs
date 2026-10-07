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
    private IReadOnlyList<ReadyActionObject> _readyObjects = Array.Empty<ReadyActionObject>();
    private BattleState? _readyCycleBattle;
    private int _readyCycleTurn = -1, _nextReadyIndex;
    private (int Id, bool Town)? _lastReadyObject;
    private bool _turnClosing;
    private bool _relicEnding;
    public bool TurnConfirmationVisible => _turnConfirmation?.Visible == true;
    // Compatibility with the existing map-input/menu guards. There is no side list:
    // ready objects exist only inside the hints-on end-turn confirmation.
    public bool ReadyActionsMenuVisible => false;
    public int ReadyObjectCount => _readyObjects.Count;
    public event Action? TurnConfirmed, TurnConfirmationClosed;
    public event Action<ReadyActionObject>? ReadyObjectSelected;

    private void BuildTurnGuidance()
    {
        UiHints.Initialize();
        _readyJug = (ReadyActionJug)_end;
        _readyJug.MouseForcePassScrollEvents = false;
        _readyJug.CounterRequested += FocusNextReadyObject;
        _readyJug.Pressed += () =>
        {
            if (_readyJug.GetLocalMousePosition().Y < _readyJug.PrintedCountCenter.Y - 20)
                EndTurnRequested?.Invoke();
        };
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
        _readyGrid = new GridContainer { Name = "ReadyActionGrid", Columns = 3,
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
            if (_turnClosing) return;
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
        if (!UiHints.Enabled) CloseTurnConfirmation();
        UpdateReadyActions(_namedBattle, _namedBattle is not null && !_end.Disabled);
    }
    private void UpdateReadyActions(BattleState? battle, bool canAct)
    {
        bool humanTurn = battle is not null && battle.ActiveSide == Side.Player
            && !battle.IsOver && !battle.PlayerDefeated;
        if (!ReferenceEquals(_readyCycleBattle, battle) || _readyCycleTurn != battle?.TurnSerial)
        {
            _readyCycleBattle = battle;
            _readyCycleTurn = battle?.TurnSerial ?? -1;
            _nextReadyIndex = 0;
            _lastReadyObject = null;
            _readyObjects = Array.Empty<ReadyActionObject>();
            if (humanTurn) _relicEnding = false;
        }
        // Keep the previous count during a staged visual order. The current Core
        // query becomes authoritative again at its completion. Menus/hints do not
        // hide the counter or change which owned objects qualify.
        if (humanTurn && battle!.PendingPresentation is null && battle.PendingUpgrade(Side.Player) is null)
            _readyObjects = CachedReadyActions(battle);
        else if (!humanTurn || battle!.PendingUpgrade(Side.Player) is not null)
            _readyObjects = Array.Empty<ReadyActionObject>();
        _readyJug.Visible = battle is not null && !battle.IsOver && !battle.PlayerDefeated;
        _readyJug.Disabled = !canAct || TurnConfirmationVisible;
        if (battle is not null)
        {
            var color = FleetPalette.For(battle, Side.Player);
            _readyJug.Update(_readyObjects.Count, color, battle.PlayerColor);
            _readyJug.SetHumanTurn(humanTurn && !_relicEnding, InstantPaperAnimations);
        }
        if (TurnConfirmationVisible && (!canAct || !UiHints.Enabled)) CloseTurnConfirmation();
        LayoutTurnGuidance();
    }
    private void FocusNextReadyObject()
    {
        if (_end.Disabled || MenuVisible || TurnConfirmationVisible || _readyObjects.Count == 0) return;
        int index = _nextReadyIndex % _readyObjects.Count;
        if (_lastReadyObject is { } last)
        {
            for (int i = 0; i < _readyObjects.Count; i++)
                if (_readyObjects[i].Id == last.Id && (_readyObjects[i].ShipClass is null) == last.Town)
                { index = (i + 1) % _readyObjects.Count; break; }
        }
        var item = _readyObjects[index];
        _lastReadyObject = (item.Id, item.ShipClass is null);
        _nextReadyIndex = (index + 1) % _readyObjects.Count;
        ReadyObjectSelected?.Invoke(item);
    }
    public void ShowTurnConfirmation()
    {
        if (!UiHints.Enabled || _end.Disabled || TurnConfirmationVisible) return;
        ClearChildren(_readyGrid);
        foreach (var item in _readyObjects) _readyGrid.AddChild(ReadyButton(item));
        _turnSummary.Text = _readyObjects.Count == 0 ? "No useful actions remain." : "These objects can still act this turn.";
        _turnConfirmation.Show();
        _turnClosing = false;
        _readyJug.Disabled = true;
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
    public Task AnimateEndTurnStamp(bool instant) => Task.CompletedTask;
    public async Task AnimateEndTurnFold(bool instant)
    {
        _relicEnding = true;
        _readyJug.SetHumanTurn(false, instant);
        if (!instant) await ToSignal(GetTree().CreateTimer(.48), SceneTreeTimer.SignalName.Timeout);
    }
    public void CancelEndTurnTransition()
    {
        _relicEnding = false;
        _readyJug.SetHumanTurn(_namedBattle is { ActiveSide: Side.Player, IsOver: false, PlayerDefeated: false },
            InstantPaperAnimations);
    }
    public void CloseReadyActionsMenu() { }

    private BrushPaperButton ReadyButton(ReadyActionObject item)
    {
        int hints = ReadyActionHints.Count(item.Actions);
        float height = Math.Max(62, (hints + 1) / 2 * 18 + 12);
        var button = new BrushPaperButton { Name = "ReadyObject" + (item.ShipClass is null ? "Town" : "Ship") + item.Id,
            CustomMinimumSize = new(88, height), TooltipText = ReadyDescription(item),
            SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter };
        button.AddChild(new ActionGlyph { Name = "ReadyObjectClass", Symbol = ReadySymbol(item.ShipClass),
            Position = new(4, (height - 32) / 2), Size = new(32, 32), MouseFilter = Control.MouseFilterEnum.Ignore });
        button.AddChild(new ReadyActionHints { Name = "ReadyObjectActions", Actions = item.Actions,
            Position = new(42, 6), Size = new(40, height - 12), MouseFilter = Control.MouseFilterEnum.Ignore });
        button.Pressed += async () =>
        {
            if (_turnClosing) return;
            await CloseTurnPaper();
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
        if (_turnPaper is null) return;
        var viewport = UiScale.LogicalViewport(this);
        float width = Math.Min(PapyrusModal.Width, Math.Max(220, viewport.X - 24));
        var margin = _turnPaper.GetThemeStylebox("panel").GetMinimumSize();
        int columns = Math.Clamp((int)((width - margin.X + 8) / 96), 1, 3);
        if (_readyGrid.Columns != columns) _readyGrid.Columns = columns;
        PapyrusModal.Layout(_turnPaper, _turnScroll, _turnBody, viewport);
        _turnPaper.Position = (viewport - _turnPaper.Size) / 2;
        _turnGuidance.Position = _turnPaper.Position + new Vector2(0, _turnPaper.Size.Y + 8);
        _turnGuidance.Size = new(_turnPaper.Size.X, 36);
    }
    private static ActionSymbol ReadySymbol(ShipClass? kind) => NavalGlyphArt.Symbol(kind);
}
