using System;
using Godot;

namespace DevAncientNaval.Presentation.UI;
public partial class DebugHud
{
    private RadialPapyrus _salvoChoice = null!;
    private SectorButton _singleShot = null!, _doubleShot = null!;
    public event Action<bool>? SalvoRequested;
    internal bool SalvoChoiceVisible => _salvoChoice?.Visible == true;

    private void BuildSalvoChoice()
    {
        _salvoChoice = new RadialPapyrus { Name = "TargetSalvoPapyrus", Size = SectorButton.Center * 2,
            MouseFilter = Control.MouseFilterEnum.Ignore };
        _root.AddChild(_salvoChoice);
        _singleShot = IconButton("SingleShot", ActionSymbol.SingleShot, "Single shot", () => SalvoRequested?.Invoke(false), _salvoChoice);
        _doubleShot = IconButton("DoubleShot", ActionSymbol.DoubleShot, "Double salvo", () => SalvoRequested?.Invoke(true), _salvoChoice);
        _singleShot.SetSector(0, 2); _doubleShot.SetSector(1, 2);
        _salvoChoice.Hide();
    }

    internal void ShowSalvoChoice(bool canDouble)
    {
        _singleShot.Disabled = false;
        _doubleShot.Disabled = !canDouble;
        _singleShot.Cost = _doubleShot.Cost = null;
        _salvoChoice.Configure(new[] { _singleShot, _doubleShot }, !_salvoChoice.Visible);
        _salvoChoice.Show();
        _radial.Hide();
    }
    internal void HideSalvoChoice() => _salvoChoice?.Hide();
    internal void PositionSalvoChoice(Vector2 target, float progressOffset)
    {
        if (!SalvoChoiceVisible) return;
        target = UiScale.ScreenToUi(target);
        progressOffset /= UiScale.Value;
        _radial.Hide();
        _salvoChoice.SetWrapping(Mathf.Clamp(progressOffset + 18, 64, 142), Mathf.Pi);
        _salvoChoice.Position = target - _salvoChoice.RingCenter;
    }
}
