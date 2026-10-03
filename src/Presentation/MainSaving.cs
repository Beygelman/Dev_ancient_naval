using System;
using System.Threading.Tasks;
using DevAncientNaval.Core.Battle;
using Godot;

namespace DevAncientNaval.Presentation;
<<<<<<< Updated upstream
=======

>>>>>>> Stashed changes
public partial class Main
{
    private async Task SaveSessionAsync(bool newGame = false)
    {
<<<<<<< Updated upstream
        if (!_saveEnabled || !_sessionStarted)
            return;
        if (Battle.IsOver || Battle.PlayerDefeated)
        {
            await Task.Run(_saveStore.Delete);
            return;
        }
        BattleSave snapshot;
        using (Diagnostics.PerformanceTrace.Measure("Save.Capture"))
            snapshot = Battle.CaptureSnapshot();
=======
        if (!_saveEnabled || !_sessionStarted) return;
        BattleSave snapshot;
        using (Diagnostics.PerformanceTrace.Measure("Save.Capture")) snapshot = Battle.CaptureSnapshot();
>>>>>>> Stashed changes
        var camera = MapCamera.Position;
        float zoom = MapCamera.Zoom.X;
        try
        {
            // Only immutable DTOs cross threads. Serialization and Flush(true)
            // cannot stall the animation/draw thread during normal commands.
            await Task.Run(() => _saveStore.WriteSnapshot(snapshot, camera, zoom, newGame));
        }
<<<<<<< Updated upstream
        catch (Exception error)when (error is System.IO.IOException or UnauthorizedAccessException)
=======
        catch (Exception error) when (error is System.IO.IOException or UnauthorizedAccessException)
>>>>>>> Stashed changes
        {
            Hud.ShowMessage("Could not save this turn: " + error.Message);
            GD.PushWarning(error.Message);
        }
    }
}
