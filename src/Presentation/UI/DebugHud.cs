using System;
using DevAncientNaval.Core.World;
using Godot;

namespace DevAncientNaval.Presentation.UI;

public partial class DebugHud : CanvasLayer
{
    private Label _selection = null!;
    private Label _zoom = null!;
    public string SelectionText => _selection.Text;
    public event Action? ResetRequested;
    public event Action<float>? ZoomRequested;

    public override void _Ready()
    {
        var root = new Control { MouseFilter = Control.MouseFilterEnum.Ignore };
        AddChild(root);
        root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

        var panel = new PanelContainer { Position = new Vector2(20, 16), MouseFilter = Control.MouseFilterEnum.Stop };
        var style = new StyleBoxFlat { BgColor = new Color("102a39"), ContentMarginLeft = 18,
            ContentMarginRight = 18, ContentMarginTop = 12, ContentMarginBottom = 12,
            CornerRadiusTopLeft = 10, CornerRadiusTopRight = 10, CornerRadiusBottomLeft = 10, CornerRadiusBottomRight = 10 };
        panel.AddThemeStyleboxOverride("panel", style);
        root.AddChild(panel);
        var column = new VBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        panel.AddChild(column);
        column.AddChild(MakeLabel("ANCIENT NAVAL  /  0.01", 24));
        _selection = MakeLabel("Нажмите на клетку карты", 20);
        column.AddChild(_selection);

        var bar = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        root.AddChild(bar);
        bar.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.BottomRight);
        bar.GrowHorizontal = Control.GrowDirection.Begin;
        bar.GrowVertical = Control.GrowDirection.Begin;
        bar.OffsetRight = -20;
        bar.OffsetBottom = -20;
        bar.AddThemeConstantOverride("separation", 10);
        _zoom = MakeLabel("", 20);
        bar.AddChild(_zoom);
        AddButton(bar, "−", () => ZoomRequested?.Invoke(1 / 1.2f));
        AddButton(bar, "+", () => ZoomRequested?.Invoke(1.2f));
        AddButton(bar, "Вся карта", () => ResetRequested?.Invoke());

        var hint = MakeLabel("Тап: выбор  ·  Перетаскивание: обзор  ·  Щипок / колесо: масштаб", 18);
        root.AddChild(hint);
        hint.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.BottomLeft);
        hint.OffsetLeft = 20;
        hint.OffsetTop = -48;
        hint.OffsetBottom = -20;
    }

    public void ShowTile(Tile? tile) => _selection.Text = tile is null
        ? "Нажмите на клетку карты"
        : $"X: {tile.Position.X}    Y: {tile.Position.Y}    Terrain: {tile.Terrain}";

    public void ShowZoom(float zoom) => _zoom.Text = $"{zoom:P0}  ";

    private static Label MakeLabel(string text, int size)
    {
        var label = new Label { Text = text, MouseFilter = Control.MouseFilterEnum.Ignore };
        label.AddThemeFontSizeOverride("font_size", size);
        label.AddThemeColorOverride("font_color", new Color("dfedf0"));
        return label;
    }

    private static void AddButton(HBoxContainer parent, string text, Action pressed)
    {
        var button = new Button { Text = text, CustomMinimumSize = new Vector2(64, 58),
            FocusMode = Control.FocusModeEnum.None };
        button.AddThemeFontSizeOverride("font_size", 22);
        button.Pressed += pressed;
        parent.AddChild(button);
    }
}
