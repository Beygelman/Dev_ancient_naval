using System.Threading.Tasks;
using DevAncientNaval.Core.Battle;
using Godot;
using Side = DevAncientNaval.Core.Units.Side;

namespace DevAncientNaval.Presentation.UI;

public partial class DebugHud
{
    private PanelContainer _heavenPaper = null!;
    private Label _heavenTitle = null!, _heavenDetail = null!;
    private HeavenlyLight _heavenLight = null!;
    public string HeavenlyText => _heavenPaper.Visible ? _heavenTitle.Text : "";
    public bool HeavenlyLightVisible => _heavenLight.Visible;

    private void BuildHeavenlyAssistance()
    {
        _heavenLight = new HeavenlyLight { Name = "HeavenlyLight", MouseFilter = Control.MouseFilterEnum.Ignore };
        _root.AddChild(_heavenLight);
        _root.MoveChild(_heavenLight, 0);
        _heavenLight.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        _heavenLight.Stop();
        _heavenPaper = Panel(_root);
        _heavenPaper.Name = "HeavenlyAssistance";
        _heavenPaper.MouseFilter = Control.MouseFilterEnum.Ignore;
        var text = new VBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        _heavenPaper.AddChild(text);
        _heavenTitle = Label("", 16, true);
        _heavenDetail = Label("", 12, true);
        text.AddChild(_heavenTitle);
        text.AddChild(_heavenDetail);
        _heavenPaper.Hide();
    }

    public async Task ShowHeavenlyAssistance(HeavenlyReceipt receipt, BattleState battle)
    {
        bool known = receipt.Owner == Side.Player || battle.HasMet(receipt.Owner);
        _heavenTitle.Text = receipt.IsReligiousBlessing
            ? $"Heaven blesses our faith · +{receipt.Amount} Thors"
            : known ? $"Heavenly aid · +{receipt.Amount} Thors" : "Other nations receive heavenly aid";
        _heavenDetail.Text = receipt.IsReligiousBlessing ? "A blessing for spreading our faith across the sea."
            : known ? $"{battle.FactionName(receipt.Owner)} receives help from above." : "Distant prayers rise beyond the horizon.";
        _heavenPaper.ResetSize();
        _heavenPaper.Show();
        PositionHeavenlyAssistance();
        _heavenLight.Play();
        await ToSignal(GetTree().CreateTimer(1.6), SceneTreeTimer.SignalName.Timeout);
        HideHeavenlyAssistance();
    }

    private void PositionHeavenlyAssistance()
    {
        if (_heavenPaper is null || !_heavenPaper.Visible) return;
        float lower = _metricsPaper.Position.Y + _metricsPaper.Size.Y + 8;
        if (_nationPaper.Visible) lower = _nationPaper.Position.Y + _nationPaper.Size.Y + 8;
        _heavenPaper.Position = new((UiScale.LogicalViewport(this).X - _heavenPaper.Size.X) / 2, lower);
    }

    public void HideHeavenlyAssistance()
    {
        _heavenPaper?.Hide();
        _heavenLight?.Stop();
    }
}

/// <summary>A finite screen overlay; never invalidates cached world geometry.</summary>
public partial class HeavenlyLight : Control
{
    private float _age;
    public void Play() { _age = 0; Show(); SetProcess(true); QueueRedraw(); }
    public void Stop() { Hide(); SetProcess(false); }
    public override void _Process(double delta)
    {
        _age += (float)delta;
        if (_age > 1.6f) { Stop(); return; }
        QueueRedraw();
    }
    public override void _Draw()
    {
        float alpha = Mathf.Sin(Mathf.Pi * Mathf.Clamp(_age / 1.6f, 0, 1));
        var origin = new Vector2(Size.X * .5f, -30);
        for (int i = 0; i < 7; i++)
        {
            float x = Size.X * (.15f + i * .116f);
            DrawColoredPolygon(new[] { origin + new Vector2(-5, 0), origin + new Vector2(5, 0),
                new Vector2(x + 25, Size.Y), new Vector2(x - 25, Size.Y) }, new Color(1, .91f, .59f, .045f * alpha));
        }
        for (int i = 0; i < 18; i++)
        {
            float x = Size.X * .25f + ((i * 83) % 600) / 600f * Size.X * .5f;
            float y = 90 + ((i * 47) % 240) + _age * 18;
            DrawCircle(new(x, y), 1.1f, new Color(1, .95f, .67f, .48f * alpha));
        }
    }
}
