using System;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using DevAncientNaval.Core.Battle;
using DevAncientNaval.Presentation;
using Godot;

namespace DevAncientNaval.Tests.Runtime;
/// <summary>Writes only a caller-supplied test directory; never the user's save slot.</summary>
public static class SaveRecoveryChecks
{
    public static int Run(string directory, BattleState battle)
    {
        Directory.CreateDirectory(directory);
        string path = System.IO.Path.Combine(directory, "recovery-" + Guid.NewGuid().ToString("N") + ".json");
        var store = new SaveStore(path);
        int checks = 0;
        void Check(bool condition, string name)
        {
            if (!condition)
                throw new Exception("Save recovery: " + name);
            checks++;
        }

        store.Write(battle, new Vector2(10, 20), .75f);
        store.Write(battle, new Vector2(30, 40), .9f);
        byte[] backup = File.ReadAllBytes(path + ".bak");
        File.WriteAllText(path, "interrupted save");
        var recovered = store.Read();
        Check(recovered.Backup && recovered.Camera == new Vector2(10, 20), "Corrupt primary loads checkpoint");
        store.Write(recovered.Battle, recovered.Camera, recovered.Zoom);
        Check(File.ReadAllBytes(path + ".bak").SequenceEqual(backup), "First autosave preserves known-good backup bytes");
        Check(!store.Read().Backup, "Autosave restores a readable primary");
        // A second failure after recovery must still be recoverable, including
        // when a new process creates a new SaveStore instance.
        File.WriteAllText(path, "second interruption");
        var nextProcess = new SaveStore(path);
        Check(nextProcess.Read().Backup, "Backup survives a second primary failure");
        nextProcess.Write(battle, new Vector2(50, 60), 1f);
        byte[] previousPrimary = File.ReadAllBytes(path);
        nextProcess.Write(battle, new Vector2(70, 80), 1.1f);
        Check(File.ReadAllBytes(path + ".bak").SequenceEqual(previousPrimary), "Normal subsequent save rotates healthy primary");
        var malformed = JsonNode.Parse(File.ReadAllText(path))!;
        malformed["Battle"]!["Ships"] = null;
        File.WriteAllText(path, malformed.ToJsonString());
        Check(nextProcess.Read().Backup, "Malformed nested state falls back to checkpoint");
        byte[] primaryBeforeRejectedWrite = File.ReadAllBytes(path);
        bool rejected = false;
        try
        {
            nextProcess.Write(battle, Vector2.Zero, float.NaN);
        }
        catch (InvalidDataException)
        {
            rejected = true;
        }

        Check(rejected && File.ReadAllBytes(path).SequenceEqual(primaryBeforeRejectedWrite), "Invalid camera never replaces existing save");
        string newGamePath = System.IO.Path.Combine(directory, "new-game-" + Guid.NewGuid().ToString("N") + ".json");
        File.WriteAllBytes(newGamePath + ".bak", backup);
        File.WriteAllText(newGamePath, "corrupt slot before New Game");
        new SaveStore(newGamePath).Write(battle, Vector2.Zero, 1f);
        Check(File.ReadAllBytes(newGamePath + ".bak").SequenceEqual(backup), "Recovery preserves backup without a preceding Read call");
        Check(!new SaveStore(newGamePath).Read().Backup, "Recovery replaces corrupt primary with valid state");
        var newSlot = new SaveStore(newGamePath);
        newSlot.WriteNewGame(battle.CaptureSnapshot(), new Vector2(90, 100), 1.2f);
        Check(!File.Exists(newGamePath + ".bak"), "New voyage deletes the previous voyage's backup");
        Check(newSlot.Read().Camera == new Vector2(90, 100), "Only the new voyage can Continue");
        File.WriteAllText(newGamePath, "corrupt new voyage");
        bool oldVoyageRecovered = true;
        try { new SaveStore(newGamePath).Read(); }
        catch (InvalidDataException) { oldVoyageRecovered = false; }
        Check(!oldVoyageRecovered, "A new voyage cannot resurrect an older game");
        newSlot.WriteNewGame(battle.CaptureSnapshot(), Vector2.Zero, 1f);
        newSlot.Write(battle, new Vector2(10, 20), 1f);
        Check(File.Exists(newGamePath + ".bak"), "Recovery checkpoint belongs to the new voyage only");
        return checks;
    }
}
