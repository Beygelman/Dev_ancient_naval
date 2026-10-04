using System.Linq;
using System.Threading.Tasks;
using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Units;
using DevAncientNaval.Presentation.Map;
using DevAncientNaval.Presentation.UI;

namespace DevAncientNaval.Presentation;
public partial class Main
{
    private RewardPapyrusHud? _rewards;
    private void InitializeRewards()
    {
        _rewards = new RewardPapyrusHud { Name = "RewardPapyrus" };
        AddChild(_rewards);
        _rewards.ClaimRequested += id => RunSafely(() => AcceptReward(id));
    }
    private async Task AcceptReward(string id)
    {
        if (Busy || _sessionLoading || Battle.ActiveSide != Side.Player) { _rewards?.RejectClaim(); return; }
        var result = Battle.ClaimAward(Side.Player, id);
        if (!result.Success) { _rewards?.RejectClaim(); Hud.ShowMessage(result.Message); return; }
        _rewards?.Close();
        await SaveSessionAsync();
        Refresh();
    }
    private void PresentPendingRewards()
    {
        if (_rewards is null || _rewards.IsOpen || Busy || _sessionLoading || _home?.IsOpen == true || Hud.MenuVisible || Battle.IsOver || Battle.ActiveSide != Side.Player) return;
        var award = Battle.PendingAwards.FirstOrDefault(a => a.Owner == Side.Player);
        if (award is null) return;
        MapInput.CancelGesture();
        ClearMode();
        if (award.Kind == AwardKind.Heavenly)
            _rewards.ShowHeavenly(award.Id, award.Amount, Language.Translate("The heavens bless your spreading faith. Every city and your Mothership bring an offering every fifth turn."));
        else if (award.Nation is { } nation)
        {
            var (first, second) = Battle.ColorFor(nation) switch
            {
                FleetColor.Blue => ("Seafarers of the golden dome chart distant shores.", "Their church honours the celestial light that guides every voyage."),
                FleetColor.Purple => ("Keepers of the silver shrine sail with quiet resolve.", "They honour moonlit spirits beneath the diamond sanctuary."),
                FleetColor.Yellow => ("Children of the rose pyramid guard the sun's old wisdom.", "Their white summit receives offerings to the radiant heavens."),
                FleetColor.White => ("Mariners of the crimson R raise steadfast stone cities.", "Their faith follows the radiant R through storm and silence."),
                FleetColor.Green => ("The ancient forest folk carry living sanctuaries across the sea.", "They honour the great tree and the spirits within its roots."),
                _ => ("The crimson clans raise crystal sanctuaries among their cities.", "They honour the mountain spirits and the fire sleeping within the stone.")
            };
            _rewards.ShowNation(award.Id, Battle.FactionName(nation), FleetPalette.For(Battle, nation), Language.Translate(first), Language.Translate(second), award.Amount);
        }
    }
}
