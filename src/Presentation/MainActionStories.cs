using System;
using System.Linq;
using System.Threading.Tasks;
using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Units;

namespace DevAncientNaval.Presentation;
public partial class Main
{
    public async Task CaptureVillage()
    {
        if (!CanCommand || SelectedVillageId is not { } id || !Battle.CanCaptureVillage(Side.Player, id)) return;
        await CaptureVillage(id);
    }
    public async Task CaptureVillage(int id)
    {
        if (!CanCommand || !Battle.CanCaptureVillage(Side.Player, id)) return;
        ClearMode();
        SelectedShipId = null;
        SelectedVillageId = id;
        BoardView.Select(Battle.Villages.First(v => v.Id == id).Position);
        await CommitAfterStory(() => Battle.CanCaptureVillage(Side.Player, id), b => b.CaptureVillage(Side.Player, id), true, id);
    }
    public async Task LootTreasury()
    {
        if (!CanCommand || SelectedShipId is not { } id || !Battle.CanLootTreasury(Side.Player, id)) return;
        await LootTreasury(id);
    }
    public async Task LootTreasury(int id)
    {
        if (!CanCommand || !Battle.CanLootTreasury(Side.Player, id)) return;
        ClearMode();
        SelectedVillageId = null;
        SelectedShipId = id;
        BoardView.Select(Battle.Find(id)!.Position);
        await CommitAfterStory(() => Battle.CanLootTreasury(Side.Player, id), b => b.LootTreasury(Side.Player, id), false, id);
    }
    private async Task CommitAfterStory(Func<bool> stillReady, Func<BattleState, CommandResult> command, bool capture, int id)
    {
        var battle = Battle;
        bool canceled = false;
        Busy = true;
        try
        {
            // Begin before refreshing: the busy HUD retains the consuming banner.
            Task animation = FastChecks ? Task.CompletedTask : Hud.ConsumeStory(capture, id);
            MapInput.CancelGesture();
            Refresh();
            await animation;
        }
        catch (OperationCanceledException)
        {
            // A removed/replaced paper acknowledges no command. Cancellation must
            // release the input lock without committing a claim or reward.
            canceled = true;
        }
        finally { Busy = false; }
        if (canceled)
        {
            if (Godot.GodotObject.IsInstanceValid(Hud) && Hud.IsInsideTree()) Refresh();
            return;
        }
        if (ReferenceEquals(battle, Battle) && stillReady())
            await Perform(command, deferImpacts: !capture);
        else Refresh();
    }
}
