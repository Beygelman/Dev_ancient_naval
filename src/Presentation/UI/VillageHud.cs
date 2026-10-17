using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Units;

namespace DevAncientNaval.Presentation.UI;
public partial class DebugHud
{
    private void UpdateVillage(BattleState battle, Village? village, bool canAct)
    {
        bool owned = village?.Owner == Side.Player;
        _villageUpgrade.SetMeta("applicable", owned);
        int upgradePrice = village is null ? 0 : battle.VillageUpgradePrice(Side.Player, village.Id);
        string? upgradeReason = village is null ? "Select a city" : battle.VillageUpgradeBlockReason(Side.Player, village.Id);
        _villageUpgrade.Cost = village?.Level >= 5 || village?.UpgradeWorkRemaining > 0 ? null : upgradePrice;
        bool pendingProject = village?.UpgradeWorkRemaining > 0;
        Availability(_villageUpgrade, canAct && village is not null && (pendingProject
            ? battle.CanSetVillageDevelopmentPaused(Side.Player, village.Id, !village.DevelopmentPaused)
            : battle.CanUpgradeVillage(Side.Player, village.Id)), "");
        _villageUpgrade.TooltipText = village?.UpgradeWorkRemaining > 0
            ? (village.DevelopmentPaused ? "Resume development" : "Pause development")
                + $" · {village.UpgradeWorkRemaining}/2 works remaining · toggle without spending city work"
            : village?.Level >= 5 ? "Town at maximum level"
            : $"Upgrade town · {upgradePrice} Thors" + (battle.Rules.ExperimentalEconomy
                ? " · Pay now; two idle city works; no healing or fleet capacity increase" : "")
                + (upgradeReason is null ? "" : $" · {upgradeReason}");
        bool canCapture = village is not null && battle.CanCaptureVillage(Side.Player, village.Id);
        _capture.SetMeta("applicable", false);
        Availability(_capture, canAct && canCapture, canCapture ? "Claim" : village?.Health <= 0 ? "Wait" : "0 HP");
        _capture.TooltipText = canCapture ? battle.Rules.ExperimentalEconomy ? "Capture this town at half health; its work resumes next turn" : "Capture this town and restore its defenses" : "Reduce to 0 HP, then keep a combat ship alongside until its next turn. Capture consumes that ship's actions.";
        _port.Cost = village?.HasPort == true ? null : battle.PortPrice(Side.Player);
        _fortify.Cost = village?.IsFortified == true ? null : battle.FortificationPrice(Side.Player);
        _port.SetMeta("applicable", owned);
        string? portReason = village is null ? "Select a city" : battle.PortBlockReason(Side.Player, village.Id);
        Availability(_port, canAct && portReason is null, "");
        _port.TooltipText = (battle.Rules.ExperimentalEconomy
            ? $"Port · {battle.PortPrice(Side.Player)} Thors · Level 3 · Trade +2/+3/+4 at levels 3/4/5 · Needs another friendly city; no shipyard discount"
            : battle.Rules.Ports.ConnectedCityIncome
            ? $"Port · {battle.PortPrice(Side.Player)} Thors · Level 3 · +1 income per other connected city · {battle.Rules.Ports.Discount:P0} shipyard discount · linked sea lanes"
            : $"Port · {battle.PortPrice(Side.Player)} Thors · Level 3 · +{battle.Rules.Ports.Income} income · {battle.Rules.Ports.Discount:P0} shipyard discount · linked sea lanes")
            + (!battle.Rules.ExperimentalEconomy && battle.Rules.Ports.ConnectedCityIncomeCap > 0 ? $" · Trade cap +{battle.Rules.Ports.ConnectedCityIncomeCap}" : "")
            + (portReason is null ? "" : $" · {portReason}");
        _fortify.SetMeta("applicable", owned);
        string? fortifyReason = village is null ? "Select a village" : battle.FortifyBlockReason(Side.Player, village.Id);
        Availability(_fortify, canAct && fortifyReason is null, village?.IsFortified == true ? "✓" : battle.FortificationPrice(Side.Player).ToString());
        string gunInfo = battle.Rules.ExperimentalEconomy ? $"one city work · damage { (village is null ? 3 : battle.VillageAttackDamage(village))}, counter { (village is null ? 4 : battle.VillageCounterDamage(village))}, range {battle.Rules.VillageCombat.AttackRange}" : battle.Rules.VillageCombat.AutomaticAttack ? $"automatic guns from level 2, range {battle.Rules.VillageCombat.AttackRange}, damage {(village is null ? 0 : battle.VillageAttackDamage(village))}; counter +{battle.Rules.VillageCombat.CounterBonus}" : "counterattack 3, range 3";
        _fortify.TooltipText = $"Outpost · {battle.FortificationPrice(Side.Player)} Thors · 25% resistance · {gunInfo} · sight 5" + (fortifyReason is null ? "" : $" · {fortifyReason}");
        if (village is null)
            return;
        string settlement = village.Level >= 3 ? "city" : "village";
        string owner = village.Owner is null ? $"Neutral {settlement}" : village.Owner == Side.Player ? $"Your {settlement}" : $"Enemy {settlement}";
        _ship.Text = $"{owner} · level {village.Level}";
        _health.AddThemeColorOverride("font_color", village.Owner is null || owned ? PapyrusStyle.Health : PapyrusStyle.EnemyHealth);
        _health.Text = $"Health {village.Health:0.##}/{village.MaxHealth:0.##}";
        string progression = village.UpgradeWorkRemaining > 0 ? $"Development · {village.UpgradeWorkRemaining}/2 works remaining" : village.Level == 5 ? "Maximum level"
            : battle.Rules.PaidVillageUpgrades ? $"Next level · {upgradePrice} Thors" : $"Grows in {2 - village.TurnsOwned % 2} turn(s)";
        bool privateTrade = !owned && village.Owner is not null && (battle.Rules.Ports.ConnectedCityIncome || battle.Rules.ExperimentalEconomy) && village.HasPort;
        int visibleIncome = battle.VillageIncome(village) + (privateTrade ? 0 : battle.PortIncome(village));
        _details.Text = village.Health <= 0 ? "Defenses defeated · Town remains on the map\nKeep a combat ship alongside for one turn, then click the hovering scroll." : owned ? $"Income +{(battle.VillageIncome(village) + battle.PortIncome(village))} · {progression}\nShipyard level {village.Level}" + (village.IsFortified ? " · Fortified" : "") : canCapture ? "Your crew is ready. Click the hovering scroll to claim this village." : "Reduce this town to 0 HP before capturing it.\nFishing Schooners and Balloons cannot capture towns.";
        if (owned && battle.Rules.ExperimentalEconomy && village.Health > 0)
            _details.Text += "\n" + EconomyUiFacts.TradeStatus(battle, village)
                + " · " + EconomyUiFacts.CityWork(village);
        _shipCard.TooltipText = battle.Rules.ExperimentalEconomy
            ? "Development, building, repair, fortification and a gun salvo share one city work. Idle development takes priority over the automatic salvo."
            : battle.Rules.PaidVillageUpgrades
            ? "Towns grow through paid upgrades. Each upgrade uses this turn's town construction."
            : $"Villages grow by one level every two owned turns, up to level 5. Income at this level: {visibleIncome} Thors."
                + (privateTrade ? "\nTrade income is not observed" : "");
    }
}

