using System;
using System.Linq;
using System.Threading.Tasks;
using DevAncientNaval.Core.AI;
using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Units;
using DevAncientNaval.Core.World;
using DevAncientNaval.Presentation.Camera;
using DevAncientNaval.Presentation.Input;
using DevAncientNaval.Presentation.Map;
using DevAncientNaval.Presentation.UI;
using Godot;
using Side = DevAncientNaval.Core.Units.Side;

namespace DevAncientNaval.Presentation;
public partial class Main
{
    public void BeginMove()
    {
        if (!CanCommand || Selected is not { Owner: Side.Player, CanMove: true })
            return;
        ClearMode();
        Refresh();
    }

    public void BeginAttack()
    {
        if (!CanCommand || Selected is not { Owner: Side.Player, AttacksRemaining: > 0 })
            return;
        ClearMode();
        Refresh();
    }

    public void BeginBuild(ShipClass kind)
    {
        if (!CanCommand || (SelectedShipId is null && SelectedVillageId is null))
            return;
        var reason = SelectedVillage is { } village ? Battle.VillageBuildBlockReason(Side.Player, village.Id, kind) : Battle.BuildBlockReason(Side.Player, SelectedShipId!.Value, kind);
        if (reason is not null)
        {
            Hud.ShowMessage(reason);
            return;
        }

        ClearMode();
        _building = kind;
        Mode = OrderMode.Build;
        Hud.ShowMessage($"{Battle.Rules.Get(kind).Name} · {Battle.BuildPrice(Side.Player, kind)} Thors. Choose a tile inside the green outline.");
        Refresh();
    }

    public Task BuyMortar() => !CanCommand || SelectedShipId is not { } id ? Task.CompletedTask : Perform(b => b.BuyMortar(Side.Player, id));
    private Task ConfirmResource() => !CanCommand || _resourceCell is not { } cell ? Task.CompletedTask : Perform(b => _resourceIsDock ? b.BuildDock(Side.Player, cell) : b.Collect(Side.Player, cell));
    public Task DropBomb() => !CanCommand || SelectedShipId is not { } id ? Task.CompletedTask : Perform(b => b.DropBomb(Side.Player, id), deferImpacts: true);
    public Task FortifyVillage() => !CanCommand || SelectedVillageId is not { } id ? Task.CompletedTask : Perform(b => b.FortifyVillage(Side.Player, id));
    public Task BuyRadar() => !CanCommand || SelectedShipId is not { } id ? Task.CompletedTask : Perform(b => b.BuyRadar(Side.Player, id));
    public Task ChooseUpgrade(UpgradeChoice choice) => !CanCommand || Battle.PendingUpgrade(Side.Player)is not { } ship ? Task.CompletedTask : Perform(b => b.ChooseUpgrade(Side.Player, ship.Id, choice));
    public Task RepairSelected()
    {
        if (CanCommand && SelectedVillageId is { } village)
            return Perform(b => b.RepairVillage(Side.Player, village));
        if (!CanCommand || Selected is not { Owner: Side.Player } selected)
            return Task.CompletedTask;
        int id = selected.Id;
        return Perform(b => b.Repair(Side.Player, id));
    }

    private async Task Perform(Func<BattleState, CommandResult> action, bool deferImpacts = false)
    {
        if (!CanCommand)
            return;
        CommandResult result;
        PresentedCommand? presentation = null;
        using (DevAncientNaval.Presentation.Diagnostics.PerformanceTrace.Measure("Command.Model"))
            result = deferImpacts ? (presentation = Battle.Prepare(action)).Result : action(Battle);
        if (!result.Success)
        {
            Hud.ShowMessage(result.Message);
            Refresh();
            return;
        }

        InvalidateGameplayPresentation();
        ClearMode();
        if (result.Kind == CommandKind.Build)
        {
            SelectedShipId = result.TargetId;
            SelectedVillageId = null;
        }

        if (Selected is { } current)
            BoardView.Select(current.Position);
        Busy = true;
        Refresh();
        Hud.ShowMessage(result.Kind is CommandKind.Attack or CommandKind.Bomb ? "Ordnance in flight…" : result.Message);
        try
        {
            if (!FastChecks)
            {
                Task animation;
                using (Diagnostics.PerformanceTrace.Measure("Animation.Start"))
                    animation = Fleet.Animate(result, presentation: presentation);
                await animation;
            }
            else
                presentation?.Finish();
            Hud.ShowMessage(result.Message);
            await PresentEncounters();
        }
        finally
        {
            presentation?.Finish();
            await SaveSessionAsync();
            Busy = false;
            Refresh();
        }
    }

    public async Task EndPlayerTurn()
    {
        if (!CanCommand)
            return;
        _endingStamp = true;
        try { await Hud.AnimateEndTurnFold(FastChecks); }
        finally { _endingStamp = false; }
        if (!CanCommand) { Hud.CancelEndTurnTransition(); return; }
        var turnPresentation = Battle.Prepare(b => b.EndTurn(Side.Player));
        var ended = turnPresentation.Result;
        if (!ended.Success)
        { Hud.CancelEndTurnTransition(); return; }
        InvalidateGameplayPresentation();
        ClearMode();
        SelectedShipId = null;
        SelectedVillageId = null;
        BoardView.Select(null);
        Busy = true;
        Refresh();
        Hud.ShowMessage("");
        try
        {
            if (!FastChecks)
                await Fleet.Animate(ended, presentation: turnPresentation);
            else
                turnPresentation.Finish();
            await PresentHeavenlyAssistance(ended);
            if (!Battle.IsOver)
                await RunOpponents();
        }
        finally
        {
            turnPresentation.Finish();
            await SaveSessionAsync();
            Busy = false;
            Hud.HideOpponentTurn();
            if (!Battle.IsOver && !Battle.PlayerDefeated)
                Hud.ShowPlayerTurn();
            Refresh();
        }
    }
}
