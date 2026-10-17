using System.Linq;
using System.Threading.Tasks;
using DevAncientNaval.Core.Units;

namespace DevAncientNaval.Presentation;

public partial class Main
{
    private Task UpgradeSelectedVillage() => CanCommand && SelectedVillageId is { } id
        ? Perform(battle => battle.Villages.First(town => town.Id == id) is { UpgradeWorkRemaining: > 0 } town
            ? battle.SetVillageDevelopmentPaused(Side.Player, id, !town.DevelopmentPaused)
            : battle.UpgradeVillage(Side.Player, id)) : Task.CompletedTask;
}
