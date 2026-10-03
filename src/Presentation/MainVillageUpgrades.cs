using System.Threading.Tasks;
using DevAncientNaval.Core.Units;

namespace DevAncientNaval.Presentation;

public partial class Main
{
    private Task UpgradeSelectedVillage() => CanCommand && SelectedVillageId is { } id
        ? Perform(battle => battle.UpgradeVillage(Side.Player, id)) : Task.CompletedTask;
}
