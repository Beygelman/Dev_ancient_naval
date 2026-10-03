using System;
using System.IO;
using System.Linq;
using Godot;

namespace DevAncientNaval.Presentation.UI;

/// <summary>Guidance is a presentation preference; it never changes a saved battle.</summary>
internal static class UiHints
{
    private static bool _initialized;
    private static string? _path;
    internal static bool Enabled { get; private set; } = true;
    internal static event Action? Changed;
    internal static void Initialize()
    {
        if (_initialized) return;
        _initialized = true;
        var args = OS.GetCmdlineUserArgs();
        string? settings = args.FirstOrDefault(a => a.StartsWith("--ui-settings-file="))?[19..];
        bool tests = args.Any(a => a.EndsWith("-test"));
        _path = settings is not null ? Path.GetFullPath(settings) + ".hints"
            : tests ? null : ProjectSettings.GlobalizePath("user://interface-hints.txt");
        try
        {
            if (_path is not null && File.Exists(_path)) Enabled = File.ReadAllText(_path).Trim() != "off";
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        { GD.PushWarning("Guidance preference: " + error.Message); }
        string? explicitValue = args.FirstOrDefault(a => a.StartsWith("--hints="))?[8..];
        if (explicitValue is not null) Enabled = explicitValue != "off";
    }
    internal static void Set(bool enabled, bool persist = true)
    {
        Initialize();
        Enabled = enabled;
        Changed?.Invoke();
        if (!persist || _path is null) return;
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            File.WriteAllText(_path, enabled ? "on" : "off");
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        { GD.PushWarning("Guidance preference: " + error.Message); }
    }
}

internal partial class UiHintsToggle : Button
{
    public override void _Ready()
    {
        Name = "HintsToggle";
        UiHints.Initialize();
        CustomMinimumSize = new(0, 42);
        FocusMode = FocusModeEnum.None;
        PapyrusStyle.Button(this, 16);
        Pressed += () => UiHints.Set(!UiHints.Enabled);
        UiHints.Changed += Refresh;
        Refresh();
    }
    private void Refresh() => Text = UiHints.Enabled ? "Hints: on ✓" : "Hints: off";
    public override void _ExitTree() => UiHints.Changed -= Refresh;
}

internal partial class GuidanceLabel : Label
{
    public override void _Ready()
    {
        UiHints.Initialize();
        MouseFilter = MouseFilterEnum.Ignore;
        HorizontalAlignment = HorizontalAlignment.Center;
        AutowrapMode = TextServer.AutowrapMode.WordSmart;
        AddThemeFontSizeOverride("font_size", 12);
        AddThemeColorOverride("font_color", new Color(new Color("b49b72"), .78f));
        UiHints.Changed += Refresh;
        Refresh();
    }
    private void Refresh() => Visible = UiHints.Enabled;
    public override void _ExitTree() => UiHints.Changed -= Refresh;
}

