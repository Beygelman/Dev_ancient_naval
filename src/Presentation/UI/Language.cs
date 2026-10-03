using System;
using System.IO;
using System.Linq;
using Godot;

namespace DevAncientNaval.Presentation.UI;

/// <summary>UI language is a preference, independent of a voyage's rules and names.</summary>
internal static class Language
{
    private static readonly string[] Supported = { "en", "uk", "nl" };
    private static readonly Translation[] Catalogs = new Translation[2];
    private static string? _preferencePath;
    internal static string Current { get; private set; } = "en";
    internal static event Action? Changed;

    internal static void Initialize()
    {
        LocalizedMessages.Initialize();
        if (Catalogs[0] is null)
        {
            Catalogs[0] = new VoyageTranslation { Locale = "uk" };
            Catalogs[1] = new VoyageTranslation { Locale = "nl" };
            foreach (var catalog in Catalogs) TranslationServer.AddTranslation(catalog);
        }
        var args = OS.GetCmdlineUserArgs();
        bool tests = args.Any(arg => arg.EndsWith("-test"));
        string? save = args.FirstOrDefault(arg => arg.StartsWith("--save-file="))?[12..];
        _preferencePath = save is not null ? Path.GetFullPath(save) + ".language" :
            tests ? null : ProjectSettings.GlobalizePath("user://language.txt");
        string selected = args.FirstOrDefault(arg => arg.StartsWith("--language="))?[11..] ?? "en";
        if ((!tests || save is not null) && !args.Any(arg => arg.StartsWith("--language="))
            && _preferencePath is not null && System.IO.File.Exists(_preferencePath))
        {
            try { selected = System.IO.File.ReadAllText(_preferencePath).Trim(); }
            catch (IOException error) { GD.PushWarning(error.Message); }
        }
        Set(selected, persist: false);
    }

    internal static void Set(string locale, bool persist = true)
    {
        if (!Supported.Contains(locale)) locale = "en";
        Current = locale;
        TranslationServer.SetLocale(locale);
        Changed?.Invoke();
        if (!persist || _preferencePath is null) return;
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_preferencePath)!);
            System.IO.File.WriteAllText(_preferencePath, locale);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        { GD.PushWarning("Language preference: " + error.Message); }
    }

    internal static string Translate(string source) => LocalizedMessages.Translate(source, Current);
}

internal partial class VoyageTranslation : Translation
{
    public override StringName _GetMessage(StringName source, StringName context)
    {
        string original = source.ToString();
        string translated = LocalizedMessages.Translate(original, Locale);
        return translated == original ? new StringName("") : new StringName(translated);
    }
}

internal partial class LanguageButtons : HBoxContainer
{
    public override void _Ready()
    {
        Name = "LanguageButtons";
        Alignment = AlignmentMode.Center;
        AddThemeConstantOverride("separation", 5);
        foreach (var (locale, caption) in new[] { ("en", "English"), ("uk", "Українська"), ("nl", "Nederlands") })
        {
            var button = new Button { Name = "Language_" + locale, Text = caption,
                AutoTranslateMode = AutoTranslateModeEnum.Disabled, CustomMinimumSize = new(80, 38),
                AutowrapMode = TextServer.AutowrapMode.WordSmart, SizeFlagsHorizontal = SizeFlags.ExpandFill };
            button.SetMeta("locale", locale);
            PapyrusStyle.Button(button, 14);
            button.Pressed += () => Language.Set(locale);
            AddChild(button);
        }
        Language.Changed += UpdateActive;
        UpdateActive();
    }

    private void UpdateActive()
    {
        foreach (Button button in GetChildren())
        {
            bool active = button.GetMeta("locale").AsString() == Language.Current;
            button.ButtonPressed = active;
            button.Text = (active ? "✓ " : "") + (button.GetMeta("locale").AsString() switch
                { "uk" => "Українська", "nl" => "Nederlands", _ => "English" });
            button.Modulate = new Color(1, 1, 1, active ? 1 : .72f);
        }
    }

    public override void _ExitTree() => Language.Changed -= UpdateActive;
}
