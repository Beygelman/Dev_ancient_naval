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

<<<<<<< Updated upstream
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
=======
    public Task BuyMortar() => !CanCommand || SelectedShipId is not { } id ? Task.CompletedTask : Perform(() => Battle.BuyMortar(Side.Player, id));
    private Task ConfirmResource() => !CanCommand || _resourceCell is not { } cell ? Task.CompletedTask : Perform(() => _resourceIsDock ? Battle.BuildDock(Side.Player, cell) : Battle.Collect(Side.Player, cell));
    public Task LootTreasury() => !CanCommand || SelectedShipId is not { } id ? Task.CompletedTask : Perform(() => Battle.LootTreasury(Side.Player, id));
    public Task DropBomb() => !CanCommand || SelectedShipId is not { } id ? Task.CompletedTask : Perform(() => Battle.DropBomb(Side.Player, id));
    public Task CaptureVillage() => !CanCommand || SelectedVillageId is not { } id ? Task.CompletedTask : Perform(() => Battle.CaptureVillage(Side.Player, id));
    public Task FortifyVillage() => !CanCommand || SelectedVillageId is not { } id ? Task.CompletedTask : Perform(() => Battle.FortifyVillage(Side.Player, id));
    public Task BuyRadar() => !CanCommand || SelectedShipId is not { } id ? Task.CompletedTask : Perform(() => Battle.BuyRadar(Side.Player, id));
    public Task ChooseUpgrade(UpgradeChoice choice) => !CanCommand || Battle.PendingUpgrade(Side.Player) is not { } ship ? Task.CompletedTask : Perform(() => Battle.ChooseUpgrade(Side.Player, ship.Id, choice));
    public Task RepairSelected()
    {
        if (CanCommand && SelectedVillageId is { } village)
            return Perform(() => Battle.RepairVillage(Side.Player, village));
        if (!CanCommand || Selected is not { Owner: Side.Player } selected)
            return Task.CompletedTask;
        int id = selected.Id;
        return Perform(() => Battle.Repair(Side.Player, id));
    }

    private async Task Perform(Func<CommandResult> action)
>>>>>>> Stashed changes
    {
        if (!CanCommand)
            return;
        CommandResult result;
<<<<<<< Updated upstream
        PresentedCommand? presentation = null;
        using (DevAncientNaval.Presentation.Diagnostics.PerformanceTrace.Measure("Command.Model"))
            result = deferImpacts ? (presentation = Battle.Prepare(action)).Result : action(Battle);
=======
        using (DevAncientNaval.Presentation.Diagnostics.PerformanceTrace.Measure("Command.Model")) result = action();
>>>>>>> Stashed changes
        if (!result.Success)
        {
            Hud.ShowMessage(result.Message);
            Refresh();
            return;
        }

        InvalidateGameplayPresentation();
<<<<<<< Updated upstream
=======

>>>>>>> Stashed changes
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
<<<<<<< Updated upstream
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
=======
        Hud.ShowMessage(result.Message);
        try
        {
            if (!FastChecks)
                await Fleet.Animate(result);
        }
        finally
        {
>>>>>>> Stashed changes
            await SaveSessionAsync();
            Busy = false;
            Refresh();
        }
    }

    public async Task EndPlayerTurn()
    {
        if (!CanCommand)
            return;
<<<<<<< Updated upstream
        var turnPresentation = Battle.Prepare(b => b.EndTurn(Side.Player));
        var ended = turnPresentation.Result;
=======
        var ended = Battle.EndTurn(Side.Player);
>>>>>>> Stashed changes
        if (!ended.Success)
            return;
        InvalidateGameplayPresentation();
        ClearMode();
        SelectedShipId = null;
        SelectedVillageId = null;
        BoardView.Select(null);
        Busy = true;
        Refresh();
<<<<<<< Updated upstream
=======
        await SaveSessionAsync();
>>>>>>> Stashed changes
        Hud.ShowMessage("");
        try
        {
            if (!FastChecks)
<<<<<<< Updated upstream
                await Fleet.Animate(ended, presentation: turnPresentation);
            else
                turnPresentation.Finish();
            await PresentHeavenlyAssistance(ended);
=======
                await Fleet.Animate(ended);
>>>>>>> Stashed changes
            if (!Battle.IsOver)
                await RunOpponents();
        }
        finally
        {
<<<<<<< Updated upstream
            turnPresentation.Finish();
            await SaveSessionAsync();
            Busy = false;
            Hud.HideOpponentTurn();
            if (!Battle.IsOver && !Battle.PlayerDefeated)
                Hud.ShowPlayerTurn();
=======
            await SaveSessionAsync();
            Busy = false;
            Hud.HideOpponentTurn();
>>>>>>> Stashed changes
            Refresh();
        }
    }
}
