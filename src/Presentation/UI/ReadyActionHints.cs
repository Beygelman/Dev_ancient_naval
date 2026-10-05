using System;
using DevAncientNaval.Core.Battle;
using Godot;

namespace DevAncientNaval.Presentation.UI;

/// <summary>Only command-derived ready flags are shown; no forecast or hidden target data.</summary>
internal partial class ReadyActionHints : Control
{
    private static readonly (ReadyActionKind Kind, ActionSymbol Symbol)[] Symbols =
    {
        (ReadyActionKind.Move, ActionSymbol.Move), (ReadyActionKind.Attack, ActionSymbol.Attack),
        (ReadyActionKind.Build, ActionSymbol.Build), (ReadyActionKind.Upgrade, ActionSymbol.Upgrade),
        (ReadyActionKind.Collect, ActionSymbol.Fishing), (ReadyActionKind.Dock, ActionSymbol.Dock),
        (ReadyActionKind.Capture, ActionSymbol.Flag), (ReadyActionKind.Loot, ActionSymbol.Treasure)
    };
    internal ReadyActionKind Actions { get; init; }
    internal static int Count(ReadyActionKind actions)
    {
        int count = 0;
        foreach (var pair in Symbols) if ((actions & pair.Kind) != 0) count++;
        return count;
    }
    public override void _Ready()
    {
        int count = Count(Actions), index = 0;
        float top = Math.Max(0, (Size.Y - ((count + 1) / 2) * 18) / 2);
        foreach (var pair in Symbols)
        {
            if ((Actions & pair.Kind) == 0) continue;
            var glyph = new ReadyActionGlyph { Name = "ReadyHint" + pair.Kind, Symbol = pair.Symbol,
                Position = new(index % 2 * 20 + 1, top + index / 2 * 18),
                Size = new(32, 32), Scale = Vector2.One * .50f, MouseFilter = MouseFilterEnum.Ignore };
            AddChild(glyph);
            index++;
        }
        SetProcess(false);
    }
}

/// <summary>Move is a winding chart route ending at a cross, rather than a modern arrow.</summary>
internal partial class ReadyActionGlyph : ActionGlyph
{
    private static readonly Vector2[] MoveDashes = RouteDashes();
    private static Vector2[] RouteDashes()
    {
        var pairs = new Vector2[22];
        Vector2 Curve(float t)
        {
            var from = new Vector2(-13, 11);
            var a = new Vector2(9, 10);
            var b = new Vector2(-12, -10);
            var to = new Vector2(8, -11);
            float rest = 1 - t;
            return rest * rest * rest * from + 3 * rest * rest * t * a + 3 * rest * t * t * b + t * t * t * to;
        }
        for (int i = 0; i < 11; i++)
        {
            pairs[i * 2] = Curve(i / 11f);
            pairs[i * 2 + 1] = Curve((i + .59f) / 11f);
        }
        return pairs;
    }
    public override void _Draw()
    {
        if (Symbol != ActionSymbol.Move) { base._Draw(); return; }
        DrawSetTransform(Size * .5f);
        DrawMultiline(MoveDashes, new Color(PapyrusStyle.Ink, .94f), 2.1f, true);
        DrawCircle(new(-13, 11), 1.8f, PapyrusStyle.Ink);
        DrawLine(new(5, -14), new(11, -8), PapyrusStyle.Ink, 2.2f, true);
        DrawLine(new(11, -14), new(5, -8), PapyrusStyle.Ink, 2.2f, true);
        DrawSetTransform(Vector2.Zero);
    }
}
