using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using DevAncientNaval.Core.Battle;
using DevAncientNaval.Presentation;
using DevAncientNaval.Presentation.Diagnostics;
using DevAncientNaval.Presentation.UI;
using Godot;

namespace DevAncientNaval.Tests.Runtime;

/// <summary>Production JSON paths, with disposable files and no player save access.</summary>
public partial class AotJsonChecks : Node
{
    public Main Game { get; set; } = null!;
    public override void _Ready()
    {
        try
        {
            string supplied = OS.GetCmdlineUserArgs().FirstOrDefault(a => a.StartsWith("--save-file="))?[12..]
                ?? throw new InvalidOperationException("Pass an explicit disposable --save-file= path.");
            string path = System.IO.Path.GetFullPath(supplied) + ".aot-probe-" + Guid.NewGuid().ToString("N");
            int checks = 0;
            void Check(bool value, string reason)
            { if (!value) throw new InvalidOperationException(reason); checks++; }
            Check(!JsonSerializer.IsReflectionEnabledByDefault, "Run this test after a reflection-disabled Debug build.");
            var store = new SaveStore(path);
            store.WriteNewGame(Game.Battle.CaptureSnapshot(), new Vector2(33, 44), .75f);
            var loaded = store.Read();
            Check(loaded.Camera == new Vector2(33, 44) && loaded.Zoom == .75f && !loaded.Backup, "Session/camera round trip");
            Check(loaded.Battle.SaveJson() == Game.Battle.SaveJson(), "Session preserves complete world/RNG/rules state");
            store.Write(Game.Battle, new Vector2(50, 60), 1f);
            File.WriteAllText(path, "interrupted write");
            Check(store.Read().Backup && store.Read().Camera == new Vector2(33, 44), "Backup recovery works without reflection");
            var old = JsonNode.Parse(File.ReadAllText(path + ".bak"))!;
            old["Battle"]!["Rules"]!.AsObject().Remove("AutoRepairAmount");
            File.WriteAllText(path, old.ToJsonString());
            Check(store.Read().Battle.Rules.AutoRepairAmount == new BattleRules().AutoRepairAmount, "Nested historical missing rules retain their initializer defaults");
            store.WriteNewGame(Game.Battle.CaptureSnapshot(), Vector2.Zero, 1f);
            Check(!File.Exists(path + ".bak"), "New voyage replaces the sole Continue slot and its prior backup");
            var advice = new TutorialAdvice.Progress(1, "legacy-voyage", new[] { "resources", "village" });
            string adviceJson = JsonSerializer.Serialize(advice, PresentationJsonContext.Default.Progress);
            var adviceRead = JsonSerializer.Deserialize(adviceJson, PresentationJsonContext.Default.Progress)!;
            Check(adviceRead.Voyage == advice.Voyage && adviceRead.Shown.SequenceEqual(advice.Shown), "Tutorial history contract");
            foreach (string locale in new[] { "uk", "nl" })
                Check(LocalizedMessages.Translate("New game", locale) != "New game", "Bundled localization " + locale);
            using var trace = JsonDocument.Parse(PerformanceTrace.Report());
            Check(trace.RootElement.ValueKind == JsonValueKind.Object, "Diagnostics report generated metadata");
            store.Delete();
            GD.Print($"PASS: {checks} reflection-disabled production session/localization/advice checks (not native iOS).");
            GetTree().Quit();
        }
        catch (Exception error) { GD.PushError(error.ToString()); GetTree().Quit(1); }
    }
}
