using System.Threading.Tasks;
using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Units;
using DevAncientNaval.Presentation.UI;

namespace DevAncientNaval.Presentation;

public partial class Main
{
    private CityInfrastructureHud _infrastructure = null!;
    private BattleState? _infrastructureBattle;
    private int _infrastructureTown;
    private void InitializeCityInfrastructure()
    {
        _infrastructure = new CityInfrastructureHud { Name = "CityInfrastructureHud" };
        AddChild(_infrastructure);
        Hud.InfrastructureRequested += ShowCityInfrastructure;
        _infrastructure.ConfigureRequested += (slot, kind) => RunSafely(() => ConfigureCityInfrastructure(slot, kind));
    }
    private void ShowCityInfrastructure()
    {
        if (!CanCommand || SelectedVillage is not { Owner: Side.Player } town || !Battle.Rules.Infrastructure.Enabled) return;
        Fleet.FinishAnimation();
        _infrastructureBattle = Battle;
        _infrastructureTown = town.Id;
        MapInput.CancelGesture();
        _infrastructure.ShowCity(Battle, town);
    }
    private Task ConfigureCityInfrastructure(int slot, CityFacility kind) =>
        !CanCommand || !ReferenceEquals(_infrastructureBattle, Battle) || SelectedVillageId != _infrastructureTown
            ? Task.CompletedTask : Perform(b => b.ConfigureCityFacility(Side.Player, _infrastructureTown, slot, kind));
}

