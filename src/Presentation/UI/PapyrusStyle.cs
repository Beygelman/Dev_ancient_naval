using Godot;

namespace DevAncientNaval.Presentation.UI;
<<<<<<< Updated upstream
=======

>>>>>>> Stashed changes
internal static class PapyrusStyle
{
    public static readonly Color Ink = new("493421");
    public static readonly Color FaintInk = new("846b49");
    public static readonly Color Paper = new("e9d7ad");
    public static readonly Color Bronze = new("a48246");
    public static readonly Color Health = new("397445");
    public static readonly Color EnemyHealth = new("b13328");
<<<<<<< Updated upstream
=======

>>>>>>> Stashed changes
    public static Theme ChartTheme()
    {
        var theme = new Theme();
        theme.SetStylebox("panel", "TooltipPanel", Panel());
        theme.SetColor("font_color", "TooltipLabel", Ink);
        theme.SetFontSize("font_size", "TooltipLabel", 15);
        theme.SetColor("font_color", "Label", Ink);
        return theme;
    }

    public static StyleBoxFlat Panel(float alpha = .95f) => new()
    {
        BgColor = new Color(Paper, alpha),
        BorderColor = new Color(Bronze, .72f),
        BorderWidthTop = 1,
        BorderWidthBottom = 1,
        BorderWidthLeft = 1,
        BorderWidthRight = 1,
        ContentMarginLeft = 16,
        ContentMarginRight = 16,
        ContentMarginTop = 12,
        ContentMarginBottom = 12,
        CornerRadiusTopLeft = 4,
        CornerRadiusTopRight = 4,
        CornerRadiusBottomLeft = 4,
        CornerRadiusBottomRight = 4,
        ShadowColor = new Color(.12f, .08f, .035f, .16f),
        ShadowSize = 5
    };
<<<<<<< Updated upstream
=======

>>>>>>> Stashed changes
    public static void Button(Button button, int fontSize = 17)
    {
        button.AddThemeFontSizeOverride("font_size", fontSize);
        button.AddThemeColorOverride("font_color", Ink);
        button.AddThemeColorOverride("font_hover_color", Ink);
        button.AddThemeColorOverride("font_pressed_color", Ink);
        button.AddThemeColorOverride("font_disabled_color", new Color(FaintInk, .58f));
<<<<<<< Updated upstream
        foreach (var state in new[]
        {
            "normal",
            "hover",
            "pressed",
            "disabled",
            "focus"
        }

        )
        {
            var style = Panel(state == "disabled" ? .5f : .94f);
            if (state == "hover")
                style.BgColor = new Color("f4e5bf");
            if (state == "pressed")
                style.BgColor = new Color("d0ba89");
            if (state == "focus")
                style.BorderColor = Bronze;
=======
        foreach (var state in new[] { "normal", "hover", "pressed", "disabled", "focus" })
        {
            var style = Panel(state == "disabled" ? .5f : .94f);
            if (state == "hover") style.BgColor = new Color("f4e5bf");
            if (state == "pressed") style.BgColor = new Color("d0ba89");
            if (state == "focus") style.BorderColor = Bronze;
>>>>>>> Stashed changes
            button.AddThemeStyleboxOverride(state, style);
        }
    }
}
