using System;
using System.Collections.Generic;
using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Units;
using DevAncientNaval.Core.World;
using Godot;
using Side = DevAncientNaval.Core.Units.Side;

namespace DevAncientNaval.Presentation.UI;

public partial class DebugHud : CanvasLayer
{
    private Label _selection = null!, _ship = null!, _status = null!, _message = null!, _zoom = null!;
    private Button _repair = null!, _confirm = null!, _cancel = null!, _end = null!, _restart = null!;
    private readonly Dictionary<ShipClass, Button> _build = new();
    public string SelectionText => _selection.Text;
    public string StatusText => _status.Text;
    public event Action? ResetRequested, ConfirmRequested, CancelRequested, EndTurnRequested, RepairRequested, RestartRequested;
    public event Action<float>? ZoomRequested;
    public event Action<ShipClass>? BuildRequested;

    public override void _Ready()
    {
        var root = new Control { MouseFilter = Control.MouseFilterEnum.Ignore };
        AddChild(root);
        root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        var top = Panel(root, new Vector2(16, 12));
        top.AddChild(MakeLabel("ANCIENT NAVAL  /  ПРОБНЫЙ БОЙ", 22));
        _status = MakeLabel("", 20); top.AddChild(_status);
        _selection = MakeLabel("Выберите голубой корабль", 16); top.AddChild(_selection);

        var cameraBar = new HBoxContainer();
        root.AddChild(cameraBar);
        cameraBar.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.TopRight);
        cameraBar.GrowHorizontal = Control.GrowDirection.Begin;
        cameraBar.OffsetRight = -16;
        cameraBar.OffsetTop = 12;
        cameraBar.AddThemeConstantOverride("separation", 8);
        _zoom = MakeLabel("", 18); cameraBar.AddChild(_zoom);
        AddButton(cameraBar, "−", () => ZoomRequested?.Invoke(1 / 1.2f));
        AddButton(cameraBar, "+", () => ZoomRequested?.Invoke(1.2f));
        AddButton(cameraBar, "Обзор", () => ResetRequested?.Invoke());
        _restart = AddButton(cameraBar, "Заново", () => RestartRequested?.Invoke());

        var bottom = new PanelContainer();
        root.AddChild(bottom);
        bottom.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.BottomWide);
        bottom.OffsetLeft = 16; bottom.OffsetRight = -16;
        bottom.OffsetTop = -188; bottom.OffsetBottom = -12;
        bottom.AddThemeStyleboxOverride("panel", Style());
        var column = new VBoxContainer(); bottom.AddChild(column);
        column.AddThemeConstantOverride("separation", 5);
        _ship = MakeLabel("Голубой флот — ваш. Коралловый — противник.", 19); column.AddChild(_ship);
        _message = MakeLabel("Выберите корабль → клетку или цель → подтвердите приказ.", 17); column.AddChild(_message);
        var actions = new HBoxContainer(); column.AddChild(actions);
        actions.AddThemeConstantOverride("separation", 8);
        _confirm = AddButton(actions, "Подтвердить", () => ConfirmRequested?.Invoke());
        _cancel = AddButton(actions, "Отмена", () => CancelRequested?.Invoke());
        _repair = AddButton(actions, "Ремонт", () => RepairRequested?.Invoke());
        foreach (var kind in new[] { ShipClass.Garrison, ShipClass.Invader, ShipClass.Kolonel })
            _build[kind] = AddButton(actions, kind.ToString(), () => BuildRequested?.Invoke(kind));
        actions.AddChild(new Control { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, MouseFilter = Control.MouseFilterEnum.Ignore });
        _end = AddButton(actions, "Закончить ход", () => EndTurnRequested?.Invoke());
        column.AddChild(MakeLabel("Тап — выбор / приказ · Перетаскивание — обзор · Щипок / колесо — масштаб · Вся карта открыта", 15));
    }

    public void UpdateBattle(BattleState battle, Ship? selected, bool busy, string? confirmation, bool hasSelection)
    {
        _status.Text = battle.IsOver ? (battle.Winner == Side.Player ? "ПОБЕДА · вражеский Mothership уничтожен" : "ПОРАЖЕНИЕ · ваш Mothership уничтожен") :
            $"Раунд {battle.Round}  ·  {(battle.ActiveSide == Side.Player ? "Ваш ход" : "Ход противника")}  ·  Монеты: {battle.Credits(Side.Player)}  (+{battle.Income(Side.Player)}/ход)";
        _ship.Text = selected is null ? "Голубой флот — ваш. Коралловый — противник." :
            $"{(selected.Owner == Side.Player ? "Ваш" : "Вражеский")} {selected.Definition.Name}   HP {selected.Health}/{selected.Definition.MaxHealth}   " +
            $"Ход {selected.MovementRemaining}/{selected.Definition.Movement}   Атаки {selected.AttacksRemaining}   Урон {selected.Definition.Damage}   Броня {selected.Definition.Armor}   Дальность {selected.Definition.AttackRange}";
        bool canAct = !busy && !battle.IsOver && battle.ActiveSide == Side.Player;
        _confirm.Text = confirmation ?? "Подтвердить";
        _confirm.Disabled = !canAct || confirmation is null;
        _cancel.Disabled = busy || !hasSelection;
        _repair.Disabled = !canAct || selected is null || selected.Owner != Side.Player || !selected.CanRepair;
        _end.Disabled = !canAct;
        _restart.Disabled = busy;
        foreach (var (kind, button) in _build)
        {
            button.Text = $"{kind} · {battle.Rules.Get(kind).Price}";
            button.Disabled = !canAct || selected is null || battle.BuildBlockReason(Side.Player, selected.Id, kind) is not null;
        }
    }

    public void ShowMessage(string message) => _message.Text = message;
    public void ShowTile(Tile? tile) => _selection.Text = tile is null ? "Выберите голубой корабль" :
        $"X: {tile.Position.X}    Y: {tile.Position.Y}    Terrain: {tile.Terrain}";
    public void ShowZoom(float zoom) => _zoom.Text = $"{zoom:P0} ";

    private static StyleBoxFlat Style() => new() { BgColor = new Color("102a39"), ContentMarginLeft = 14,
        ContentMarginRight = 14, ContentMarginTop = 10, ContentMarginBottom = 10,
        CornerRadiusTopLeft = 8, CornerRadiusTopRight = 8, CornerRadiusBottomLeft = 8, CornerRadiusBottomRight = 8 };
    private static VBoxContainer Panel(Control root, Vector2 position)
    {
        var panel = new PanelContainer { Position = position };
        panel.AddThemeStyleboxOverride("panel", Style());
        root.AddChild(panel);
        var column = new VBoxContainer(); panel.AddChild(column); return column;
    }
    private static Label MakeLabel(string text, int size)
    {
        var label = new Label { Text = text, MouseFilter = Control.MouseFilterEnum.Ignore };
        label.AddThemeFontSizeOverride("font_size", size);
        label.AddThemeColorOverride("font_color", new Color("dfedf0"));
        return label;
    }
    private static Button AddButton(Container parent, string text, Action pressed)
    {
        var button = new Button { Text = text, CustomMinimumSize = new Vector2(52, 52), FocusMode = Control.FocusModeEnum.None };
        button.AddThemeFontSizeOverride("font_size", 17);
        button.Pressed += pressed;
        parent.AddChild(button);
        return button;
    }
}
