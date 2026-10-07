using System;
using System.Runtime.CompilerServices;
using Godot;

namespace DevAncientNaval.Presentation.UI;

/// <summary>Readable modal paper with an explicit bounded viewport and native scrolling.</summary>
internal static class PapyrusModal
{
    internal const float Width = 340, HeightFraction = .6f;
    private sealed class LayoutCache
    {
        internal Vector2 Viewport, BodyMinimum, Margin, PaperSize;
        internal bool Known;
    }
    private static readonly ConditionalWeakTable<PanelContainer, LayoutCache> Layouts = new();
    internal static ScrollContainer Wrap(PanelContainer paper, Control body, string name)
    {
        var scroll = new ScrollContainer { Name = name,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            VerticalScrollMode = ScrollContainer.ScrollMode.Auto,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill, MouseForcePassScrollEvents = false };
        Attach(paper, scroll);
        body.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        scroll.AddChild(body);
        return scroll;
    }
    internal static ScrollContainer WrapWithFooter(PanelContainer paper, Control body, Control footer, string name)
    {
        var host = new VBoxContainer();
        host.AddThemeConstantOverride("separation", 10);
        Attach(paper, host);
        var scroll = new ScrollContainer { Name = name, HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            VerticalScrollMode = ScrollContainer.ScrollMode.ShowNever, SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, MouseForcePassScrollEvents = false };
        host.AddChild(scroll);
        body.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        scroll.AddChild(body);
        host.AddChild(footer);
        return scroll;
    }
    private static void Attach(PanelContainer paper, Control content)
    {
        paper.MouseForcePassScrollEvents = false;
        content.MouseForcePassScrollEvents = false;
        if (paper is RollingModalPaper rolled) rolled.AttachContent(content);
        else paper.AddChild(content);
    }
    internal static void LayoutWithFooter(PanelContainer paper, ScrollContainer scroll, Control body,
        Control footer, Vector2 viewport, float maximumWidth = 420, float heightFraction = HeightFraction)
    {
        float width = Math.Min(maximumWidth, Math.Max(220, viewport.X - 24));
        float ceiling = Math.Max(80, MathF.Floor(viewport.Y * heightFraction));
        var margin = paper.GetThemeStylebox("panel").GetMinimumSize();
        float fixedHeight = MathF.Ceiling(footer.GetCombinedMinimumSize().Y + margin.Y + 10);
        // Native containers ceil their minima to whole pixels. Reserve the footer
        // first and use an integral viewport height, so 258.00003 cannot become259
        // and push a command one pixel beneath an otherwise432-pixel sheet.
        float content = Math.Min(MathF.Ceiling(body.GetCombinedMinimumSize().Y), Math.Max(1, ceiling - fixedHeight));
        paper.CustomMinimumSize = new(width, 0);
        scroll.CustomMinimumSize = new(0, content);
        paper.Size = new(width, Math.Min(ceiling, content + fixedHeight));
        paper.Position = (viewport - paper.Size) * .5f;
        if (paper is RollingModalPaper rolled) rolled.SynchronizeContentBounds();
    }
    internal static void Layout(PanelContainer paper, ScrollContainer scroll, Control body, Vector2 viewport)
    {
        var margin = paper.GetThemeStylebox("panel").GetMinimumSize();
        var minimum = body.GetCombinedMinimumSize();
        var cache = Layouts.GetValue(paper, static _ => new LayoutCache());
        if (cache.Known && cache.Viewport == viewport && cache.BodyMinimum == minimum && cache.Margin == margin && cache.PaperSize == paper.Size)
            return;
        // Stop steady HUD layout passes from writing native geometry again. Text,
        // language, viewport and style changes still invalidate this bounded cache.
        cache.Known = true;
        cache.Viewport = viewport;
        cache.BodyMinimum = minimum;
        cache.Margin = margin;
        float width = Math.Min(Width, Math.Max(220, viewport.X - 24));
        float ceiling = Math.Max(80, MathF.Floor(viewport.Y * HeightFraction));
        // ScrollContainer does not propagate its child's vertical minimum to its parent.
        float verticalMargin = MathF.Ceiling(margin.Y);
        float content = Math.Min(MathF.Ceiling(minimum.Y), Math.Max(1, ceiling - verticalMargin));
        paper.CustomMinimumSize = new(width, 0);
        scroll.CustomMinimumSize = new(0, content);
        paper.Size = new(width, Math.Min(ceiling, content + verticalMargin));
        cache.PaperSize = paper.Size;
        paper.Position = (viewport - paper.Size) * .5f;
        if (paper is RollingModalPaper rolled) rolled.SynchronizeContentBounds();
    }
}
