using System;
using DevAncientNaval.Core.Units;

namespace DevAncientNaval.Presentation.UI;

public partial class DebugHud
{
    private bool _upgradeFolding;
    private int _upgradeCeremony;

    private async void ChooseUpgradeFromPaper(UpgradeChoice choice)
    {
        if (_upgradeFolding || !UpgradeVisible || _namedBattle is not { } battle
            || battle.PendingUpgrade(Side.Player) is not { } pending) return;
        int ceremony = _upgradeCeremony;
        int id = pending.Id;
        int level = pending.Level;
        _upgradeFolding = true;
        foreach (var button in _choices.Values) button.Disabled = true;
        await ((RollingModalPaper)_upgradePanel).FoldAsync(InstantPaperAnimations);
        if (ceremony != _upgradeCeremony || !ReferenceEquals(_namedBattle, battle)
            || battle.PendingUpgrade(Side.Player) is not { } current
            || current.Id != id || current.Level != level) return;
        _upgradeFolding = false;
        UpgradeRequested?.Invoke(choice);
    }
}
