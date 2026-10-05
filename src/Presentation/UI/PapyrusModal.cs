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
        internal Vector2 Viewport, BodyMinimum, Margin;
        internal bool Known;
    }
    private static readonly ConditionalWeakTable<PanelContainer, LayoutCache> Layouts = new();
    internal static ScrollContainer Wrap(PanelContainer paper, Control body, string name)
    {
        var scroll = new ScrollContainer { Name = name,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            VerticalScrollMode = ScrollContainer.ScrollMode.Auto,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        paper.AddChild(scroll);
        body.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        scroll.AddChild(body);
        return scroll;
    }
    internal static ScrollContainer WrapWithFooter(PanelContainer paper, Control body, Control footer, string name)
    {
        var host = new VBoxContainer();
        host.AddThemeConstantOverride("separation", 10);
        paper.AddChild(host);
        var scroll = new ScrollContainer { Name = name, HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            VerticalScrollMode = ScrollContainer.ScrollMode.ShowNever, SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        host.AddChild(scroll);
        body.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        scroll.AddChild(body);
        host.AddChild(footer);
        return scroll;
    }
    internal static void LayoutWithFooter(PanelContainer paper, ScrollContainer scroll, Control body,
        Control footer, Vector2 viewport)
    {
        float width = Math.Min(420, Math.Max(220, viewport.X - 24));
        float ceiling = Math.Max(80, viewport.Y * HeightFraction);
        var margin = paper.GetThemeStylebox("panel").GetMinimumSize();
        float fixedHeight = footer.GetCombinedMinimumSize().Y + margin.Y + 10;
        float content = Math.Min(body.GetCombinedMinimumSize().Y, Math.Max(1, ceiling - fixedHeight));
        paper.CustomMinimumSize = new(width, 0);
        scroll.CustomMinimumSize = new(0, content);
        paper.Size = new(width, Math.Min(ceiling, content + fixedHeight));
        paper.Position = (viewport - paper.Size) * .5f;
    }
    internal static void Layout(PanelContainer paper, ScrollContainer scroll, Control body, Vector2 viewport)
    {
        var margin = paper.GetThemeStylebox("panel").GetMinimumSize();
        var minimum = body.GetCombinedMinimumSize();
        var cache = Layouts.GetValue(paper, static _ => new LayoutCache());
        if (cache.Known && cache.Viewport == viewport && cache.BodyMinimum == minimum && cache.Margin == margin)
            return;
        // Stop steady HUD layout passes from writing native geometry again. Text,
        // language, viewport and style changes still invalidate this bounded cache.
        cache.Known = true;
        cache.Viewport = viewport;
        cache.BodyMinimum = minimum;
        cache.Margin = margin;
        float width = Math.Min(Width, Math.Max(220, viewport.X - 24));
        float ceiling = Math.Max(80, viewport.Y * HeightFraction);
        // ScrollContainer does not propagate its child's vertical minimum to its parent.
        float content = Math.Min(minimum.Y, Math.Max(1, ceiling - margin.Y));
        paper.CustomMinimumSize = new(width, 0);
        scroll.CustomMinimumSize = new(0, content);
        paper.Size = new(width, Math.Min(ceiling, content + margin.Y));
        paper.Position = (viewport - paper.Size) * .5f;
    }
}
