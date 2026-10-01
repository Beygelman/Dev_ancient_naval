using System;
using System.Collections.Generic;
using DevAncientNaval.Core.Battle;
using Godot;
using Side = DevAncientNaval.Core.Units.Side;

namespace DevAncientNaval.Presentation.Map;
public partial class FleetView
{
    private readonly record struct IncomeLabel(Vector2 Position, int Amount, float Born);
    private readonly List<IncomeLabel> _incomeLabels = new();
    internal int IncomeLabelCount => _incomeLabels.Count;

    private void ShowIncome(CommandResult result)
    {
        foreach (var receipt in result.IncomeReceipts ?? Array.Empty<IncomeReceipt>())
        {
            // Opponent income is private, even when its source is in view.
            if (receipt.Owner != Side.Player || receipt.Position is not { } cell || receipt.Amount == 0)
                continue;
            if (_incomeLabels.Count >= 100)
                _incomeLabels.RemoveAt(0);
            _incomeLabels.Add(new(Projection.GridToWorld(cell), receipt.Amount, _clock));
        }
    }

    private void DrawIncome()
    {
        foreach (var label in _incomeLabels)
        {
            float age = _clock - label.Born;
            float alpha = Math.Clamp((2.2f - age) / .6f, 0, 1);
            var at = label.Position + new Vector2(-14, -28 - age * 18);
            var coin = new Color(label.Amount > 0 ? "f6d579" : "f05d55")
            {
                A = alpha
            };
            Ink.DrawCircle(at, 7, coin.Darkened(.35f));
            Ink.DrawArc(at, 6, 0, Mathf.Tau, 16, coin, 1.3f, true);
            Ink.DrawLine(at + new Vector2(-2, -3), at + new Vector2(2, 3), coin, 1.5f, true);
            Ink.DrawString(ThemeDB.FallbackFont, at + new Vector2(11, 6), (label.Amount > 0 ? "+" : "") + label.Amount, fontSize: 20, modulate: coin);
        }
    }
}
