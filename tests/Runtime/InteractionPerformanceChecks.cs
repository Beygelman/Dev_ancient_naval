using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Units;
using DevAncientNaval.Core.World;
using DevAncientNaval.Presentation;
using DevAncientNaval.Presentation.Diagnostics;
using Godot;
using Side = DevAncientNaval.Core.Units.Side;

namespace DevAncientNaval.Tests.Runtime;
/// <summary>Real organic-map selection, cursor preview and animated movement;
/// measures synchronous handlers separately from elapsed animation/frames.</summary>
public partial class InteractionPerformanceChecks : Node
{
    public Main Game { get; set; } = null !;

    private readonly List<double> _frames = new();
    private readonly List<double> _processTimes = new();
    private readonly List<double> _drawCalls = new();
    private double _videoMemoryPeak, _textureMemoryPeak, _bufferMemoryPeak;
    private bool _recordFrames;
    private long _previousFrame;
    private string _phase = "pan";
    private double _previousCanvasCompiles, _previousDrawCompiles;
    private readonly List<string> _slowFrames = new();
    private readonly int[] _previousCollections = new int[3];
    public override void _Process(double delta)
    {
        if (_recordFrames)
        {
            long now = Stopwatch.GetTimestamp();
            if (_previousFrame != 0)
            {
                double elapsed = Stopwatch.GetElapsedTime(_previousFrame, now).TotalMilliseconds;
                _frames.Add(elapsed);
                if (elapsed > 60)
                    _slowFrames.Add($"phase={_phase}, frame_ms={elapsed:F3}, GC_delta={GC.CollectionCount(0)-_previousCollections[0]}/{GC.CollectionCount(1)-_previousCollections[1]}/{GC.CollectionCount(2)-_previousCollections[2]}, process_ms={Performance.GetMonitor(Performance.Monitor.TimeProcess)*1000:F3}, focused={DisplayServer.WindowIsFocused()}, canvas_compile_delta={Performance.GetMonitor(Performance.Monitor.PipelineCompilationsCanvas)-_previousCanvasCompiles}, draw_compile_delta={Performance.GetMonitor(Performance.Monitor.PipelineCompilationsDraw)-_previousDrawCompiles}");
            }
            _previousCanvasCompiles = Performance.GetMonitor(Performance.Monitor.PipelineCompilationsCanvas);
            _previousDrawCompiles = Performance.GetMonitor(Performance.Monitor.PipelineCompilationsDraw);
            for (int generation = 0; generation < 3; generation++)
                _previousCollections[generation] = GC.CollectionCount(generation);
            _previousFrame = now;
            _processTimes.Add(Performance.GetMonitor(Performance.Monitor.TimeProcess) * 1000);
            _drawCalls.Add(Performance.GetMonitor(Performance.Monitor.RenderTotalDrawCallsInFrame));
            _videoMemoryPeak = Math.Max(_videoMemoryPeak, Performance.GetMonitor(Performance.Monitor.RenderVideoMemUsed));
            _textureMemoryPeak = Math.Max(_textureMemoryPeak, Performance.GetMonitor(Performance.Monitor.RenderTextureMemUsed));
            _bufferMemoryPeak = Math.Max(_bufferMemoryPeak, Performance.GetMonitor(Performance.Monitor.RenderBufferMemUsed));
        }
    }

    private async Task Frame() => await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    private static double Percentile(IEnumerable<double> values, double fraction)
    {
        var sorted = values.Order().ToArray();
        return sorted.Length == 0 ? 0 : sorted[Math.Min(sorted.Length - 1, (int)Math.Floor(sorted.Length * fraction))];
    }

    public override async void _Ready()
    {
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            await Frame();
            var args = OS.GetCmdlineUserArgs();
            int opponents = int.Parse(args.FirstOrDefault(a => a.StartsWith("--opponents="))?[12..] ?? "1");
            var kind = Enum.Parse<WorldKind>(args.FirstOrDefault(a => a.StartsWith("--world-kind="))?[13..] ?? "Oceans");
            var board = ArchipelagoGenerator.Create(731, args.Contains("--scaled-map") ? opponents : 3, kind);
            var battle = SkirmishSetup.Create(board, Game.Battle.Rules, opponents);
            Game.LoadScenario(battle);
            // The timing harness calls the production selection/preview paths
            // directly. Desktop clicks must not end a turn or replace this fixture
            // while the user keeps working in another window.
            GetViewport().GuiDisableInput = true;
            Game.MapInput.SetProcessInput(false);
            Game.MapInput.SetProcessUnhandledInput(false);
            Game.Hud.SetProcessUnhandledInput(false);
            if (!args.Contains("--live-fog"))
            {
                // Use the actual whole-map setting. Combat flashes expire on the
                // first move and would measure a synthetic map-wide fog transition.
                battle.SetGodEye(true);
            }

            var ship = battle.OwnShips(Side.Player).First(s => s.Definition.Class == ShipClass.Garrison);
            Game.SelectCell(ship.Position);
            Game.MapCamera.Position = Game.BoardView.Projection.GridToWorld(ship.Position);
            Game.MapCamera.Zoom = Vector2.One;
            if (args.Contains("--wide-view")) Game.MapCamera.FitBoard();
            Game.MapCamera.ForceUpdateScroll();
            if (args.Any(a => a.StartsWith("--save-file=")))
                Game.SaveSession();
            for (int warmup = 0; warmup < 24; warmup++)
                await Frame();
            var hovered = typeof(Main).GetMethod("PreviewAtScreen", BindingFlags.Instance | BindingFlags.NonPublic)!;
            var destinations = board.Tiles.Where(t => t.Terrain != TerrainType.Land).OrderBy(t => board.Distance(ship.Position, t.Position)).Skip(1).Take(64).Select(t => t.Position).ToArray();
            var handlers = new List<double>();
            PerformanceTrace.Reset();
            _recordFrames = true;
            for (int pan = 0; pan < 120; pan++)
            {
                Game.MapCamera.Pan(new Vector2(MathF.Sin(pan * .08f) * 6, MathF.Cos(pan * .05f) * 3));
                await Frame();
            }
            string panReport = $"PAN frames={_frames.Count}, p50_ms={Percentile(_frames, .5):F3}, p95_ms={Percentile(_frames, .95):F3}, max_ms={_frames.Max():F3}\n";
            double panP95 = Percentile(_frames, .95), panMax = _frames.Max();
            _frames.Clear();
            _previousFrame = 0;
            _phase = "hover";
            foreach (var cell in destinations)
            {
                var screen = GetViewport().GetCanvasTransform() * Game.BoardView.Projection.GridToWorld(cell);
                long start = Stopwatch.GetTimestamp();
                hovered.Invoke(Game, new object[] { screen });
                handlers.Add(Stopwatch.GetElapsedTime(start).TotalMilliseconds);
                await Frame();
            }

            var resourceCells = battle.KnownFish(Side.Player).Concat(battle.KnownShoals(Side.Player)).ToHashSet();
            var movementOptions = battle.Reachable(ship.Id).OrderByDescending(p => p.Value)
                .Where(p => p.Key != ship.Position && !resourceCells.Contains(p.Key)).ToArray();
            if (movementOptions.Length == 0)
                throw new InvalidOperationException($"No profiling route: ship={ship.Id}, pos={ship.Position}, movement={ship.MovementRemainingUnits}, side={battle.ActiveSide}, over={battle.IsOver}, sameBattle={ReferenceEquals(battle, Game.Battle)}, busy={Game.Busy}, knownResources={resourceCells.Count}, reachable={battle.Reachable(ship.Id).Count}.");
            var destination = movementOptions[0].Key;
            var origin = ship.Position;
            _phase = "move/encounter/save";
            long movementStart = Stopwatch.GetTimestamp();
            Game.SelectCell(destination);
            await Game.CurrentOrder;
            if (ship.Position == origin)
                throw new InvalidOperationException("The measured UI order did not move the ship.");
            double movementMs = Stopwatch.GetElapsedTime(movementStart).TotalMilliseconds;
            for (int frame = 0; frame < 20; frame++)
                await Frame();
            _recordFrames = false;
            string report = panReport + $"INTERACTION world={kind}, wide_view={args.Contains("--wide-view")}, tiles={board.Tiles.Count}, opponents={opponents}, live_fog={args.Contains("--live-fog")}, hover_calls={handlers.Count}, " + $"hover_p50_ms={Percentile(handlers, .5):F3}, hover_p95_ms={Percentile(handlers, .95):F3}, " + $"hover_max_ms={handlers.Max():F3}, frame_p95_ms={Percentile(_frames, .95):F3}, " + $"frame_max_ms={_frames.Max():F3}, process_p95_ms={Percentile(_processTimes, .95):F3}, " + $"draw_calls_p95={Percentile(_drawCalls, .95):F0}, movement_elapsed_ms={movementMs:F3}\n" + "SLOW_FRAMES\n" + string.Join("\n", _slowFrames) + "\n" + PerformanceTrace.Report();
            report += $"\nRENDER_MEMORY video_peak_mib={_videoMemoryPeak / (1024 * 1024):F2}, texture_peak_mib={_textureMemoryPeak / (1024 * 1024):F2}, buffer_peak_mib={_bufferMemoryPeak / (1024 * 1024):F2}\n";
            GD.Print(report);
            var output = OS.GetCmdlineUserArgs().FirstOrDefault(a => a.StartsWith("--report="));
            if (output is not null)
                File.WriteAllText(output[9..], report);
            if (args.Contains("--verify-smoothness") && (DisplayServer.GetName() == "headless"
                || panP95 > 25 || panMax > 80 || Percentile(_frames, .95) > 25
                || _frames.Max() > 100 || handlers.Max() > 16))
                throw new InvalidOperationException("Native frame-pacing gate failed: inspect the recorded slow frames before packaging.");
            GD.Print("PASS: real-map interaction profiling completed.");
            GetTree().Quit();
        }
        catch (Exception error)
        {
            GD.PushError(error.ToString());
            GetTree().Quit(1);
        }
    }
}
