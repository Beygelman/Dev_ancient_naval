using System;
using System.Globalization;
using System.IO;
using System.Linq;
using Godot;

namespace DevAncientNaval.Presentation.UI;

/// <summary>Presentation-only preference. Native CanvasLayer transforms preserve hit testing.</summary>
internal static class UiScale
{
    internal const float Minimum = .8f, Maximum = 1.25f;
    private static bool _initialized;
    private static string? _preferencePath;
    internal static float Value { get; private set; } = 1;
    private static Vector2 _safeOrigin;
    internal static event Action? Changed;
    internal static void Initialize()
    {
        if (_initialized) return;
        _initialized = true;
        var args = OS.GetCmdlineUserArgs();
        bool tests = args.Any(arg => arg.EndsWith("-test"));
        string? explicitPath = args.FirstOrDefault(arg => arg.StartsWith("--ui-settings-file="))?[19..];
        _preferencePath = explicitPath is not null ? Path.GetFullPath(explicitPath)
            : tests ? null : ProjectSettings.GlobalizePath("user://interface-scale.txt");
        if (_preferencePath is null || !File.Exists(_preferencePath)) return;
        try
        {
            if (float.TryParse(File.ReadAllText(_preferencePath), NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
                Set(value, persist: false);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        { GD.PushWarning("Interface preference: " + error.Message); }
    }
    internal static void Set(float value, bool persist = true)
    {
        if (!float.IsFinite(value)) value = 1;
        Value = Math.Clamp(MathF.Round(value * 100) / 100, Minimum, Maximum);
        Changed?.Invoke();
        if (!persist || _preferencePath is null) return;
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_preferencePath)!);
            File.WriteAllText(_preferencePath, Value.ToString("0.00", CultureInfo.InvariantCulture));
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        { GD.PushWarning("Interface preference: " + error.Message); }
    }
    internal static Rect2 ViewportSafeArea(Node node)
    {
        var viewport = node.GetViewport();
        var visible = viewport.GetVisibleRect();
        if (OS.GetName() is not ("iOS" or "Android")) return visible;
        var area = DisplayServer.GetDisplaySafeArea();
        if (area.Size.X <= 0 || area.Size.Y <= 0) return visible;
        return ConvertSafeArea(new Rect2(area.Position, area.Size), viewport.GetScreenTransform(), visible);
    }
    internal static Rect2 ConvertSafeArea(Rect2 screenArea, Transform2D viewportToScreen, Rect2 visible)
    {
        var inverse = viewportToScreen.AffineInverse();
        var start = inverse * screenArea.Position;
        var end = inverse * screenArea.End;
        var clipped = new Rect2(start, end - start).Abs().Intersection(visible);
        return clipped.Size.X > 0 && clipped.Size.Y > 0 ? clipped : visible;
    }
    internal static Vector2 LogicalViewport(Node node) => ViewportSafeArea(node).Size / Value;
    internal static Vector2 ScreenToUi(Vector2 point) => (point - _safeOrigin) / Value;
    internal static void Bind(CanvasLayer layer, Control root, Action layout)
    {
        Initialize();
        void Update()
        {
            layer.Scale = Vector2.One * Value;
            var safe = ViewportSafeArea(layer);
            _safeOrigin = safe.Position;
            layer.Offset = safe.Position;
            root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.TopLeft);
            root.Size = safe.Size / Value;
            layout();
        }
        var viewport = layer.GetViewport();
        Changed += Update;
        viewport.SizeChanged += Update;
        layer.TreeExiting += () => { Changed -= Update; viewport.SizeChanged -= Update; };
        Update();
    }
}

internal partial class UiScaleSlider : VBoxContainer
{
    internal bool OverArtwork { get; init; }
    private Label _caption = null!;
    private HSlider _slider = null!;
    public override void _Ready()
    {
        Name = "InterfaceScaleControl";
        _caption = new Label { Name = "InterfaceScaleCaption", HorizontalAlignment = HorizontalAlignment.Center };
        _caption.AddThemeFontSizeOverride("font_size", 15);
        _caption.AddThemeColorOverride("font_color", PapyrusStyle.Ink);
        if (OverArtwork)
        {
            _caption.AddThemeColorOverride("font_color", PapyrusStyle.Ink);
            _caption.AddThemeColorOverride("font_shadow_color", new Color("183742"));
            _caption.AddThemeConstantOverride("shadow_offset_x", 1);
            _caption.AddThemeConstantOverride("shadow_offset_y", 1);
        }
        AddChild(_caption);
        _slider = new HSlider { Name = "InterfaceScale", MinValue = 80, MaxValue = 125, Step = 5,
            CustomMinimumSize = new(0, 28), SizeFlagsHorizontal = SizeFlags.ExpandFill };
        AddChild(_slider);
        _slider.ValueChanged += value => UiScale.Set((float)value / 100);
        UiScale.Changed += Refresh;
        Refresh();
    }
    private void Refresh()
    {
        _caption.Text = $"Interface scale · {MathF.Round(UiScale.Value * 100)}%";
        _slider.SetValueNoSignal(UiScale.Value * 100);
    }
    public override void _ExitTree() => UiScale.Changed -= Refresh;
}
