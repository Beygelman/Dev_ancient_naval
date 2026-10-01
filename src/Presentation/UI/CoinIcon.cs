using Godot;

namespace DevAncientNaval.Presentation.UI;
public partial class CoinIcon : Control
{
    public override void _Ready() => MouseFilter = MouseFilterEnum.Ignore;
    public override void _Draw() => DrawCoin(this, Size / 2, Mathf.Min(Size.X, Size.Y) * .43f);
    internal static void DrawCoin(CanvasItem canvas, Vector2 center, float radius)
    {
        canvas.DrawCircle(center + new Vector2(0, 1), radius, new Color("78502c"));
        canvas.DrawCircle(center, radius, new Color("dfb653"));
        canvas.DrawArc(center, radius - 1, 0, Mathf.Tau, 24, new Color("fff0a7"), .8f, true);
        canvas.DrawArc(center, radius * .67f, 0, Mathf.Tau, 20, new Color("b3893f"), .7f, true);
        canvas.DrawLine(center + new Vector2(-radius * .28f, -radius * .35f), center + new Vector2(radius * .28f, -radius * .35f), PapyrusStyle.Ink, 1, true);
        canvas.DrawLine(center + new Vector2(0, -radius * .35f), center + new Vector2(0, radius * .4f), PapyrusStyle.Ink, 1, true);
    }
}
