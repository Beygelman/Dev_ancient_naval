using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Units;
using Godot;

namespace DevAncientNaval.Presentation.UI;

/// <summary>Level choices name the blessing; its concrete effects live in hover counsel.</summary>
public partial class MysticUpgradeButton : Button
{
    internal static string Title(UpgradeChoice choice) => choice switch
    {
        UpgradeChoice.Mobility => "Breath of Zephyr",
        UpgradeChoice.FishingBoat => "Gift of the Tides",
        UpgradeChoice.Vision => "Eye of the Firmament",
        UpgradeChoice.Restoration => "Covenant of Stone",
        UpgradeChoice.Balloon => "Herald of the Sky",
        UpgradeChoice.SecondAttack => "Thunder's Echo",
        UpgradeChoice.Shipwright => "Blessing of the Keel",
        UpgradeChoice.Firepower => "Wrath of the Deep",
        _ => UpgradeDescriptions.Title(choice)
    };

    public override Control _MakeCustomTooltip(string forText)
    {
        var panel = new PanelContainer { Theme = PapyrusStyle.ChartTheme() };
        panel.AddThemeStyleboxOverride("panel", PapyrusStyle.Panel());
        PapyrusGrain.Apply(panel);
        panel.AddChild(new Label
        {
            Text = DevAncientNaval.Presentation.UI.Language.Translate(forText),
            AutoTranslateMode = AutoTranslateModeEnum.Disabled,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            CustomMinimumSize = new Vector2(330, 0)
        });
        return panel;
    }
}
