using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
    private bool _recordFrames;
    private long _previousFrame;
    public override void _Process(double delta)
    {
        if (_recordFrames)
        {
            long now = Stopwatch.GetTimestamp();
            if (_previousFrame != 0)
                _frames.Add(Stopwatch.GetElapsedTime(_previousFrame, now).TotalMilliseconds);
            _previousFrame = now;
            _processTimes.Add(Performance.GetMonitor(Performance.Monitor.TimeProcess) * 1000);
            _drawCalls.Add(Performance.GetMonitor(Performance.Monitor.RenderTotalDrawCallsInFrame));
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
            await Frame();
            var args = OS.GetCmdlineUserArgs();
            int opponents = int.Parse(args.FirstOrDefault(a => a.StartsWith("--opponents="))?[12..] ?? "1");
            var kind = Enum.Parse<WorldKind>(args.FirstOrDefault(a => a.StartsWith("--world-kind="))?[13..] ?? "Oceans");
            var board = ArchipelagoGenerator.Create(731, args.Contains("--scaled-map") ? opponents : 3, kind);
            var battle = SkirmishSetup.Create(board, Game.Battle.Rules, opponents);
            Game.LoadScenario(battle);
            if (!args.Contains("--live-fog"))
            {
                foreach (var tile in board.Tiles)
                    battle.Vision.RevealCombat(Side.Player, tile.Position);
                battle.Vision.Recompute(battle.Ships, battle.TurnSerial, battle.Villages);
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
            _frames.Clear();
            _previousFrame = 0;
            foreach (var cell in destinations)
            {
                var screen = GetViewport().GetCanvasTransform() * Game.BoardView.Projection.GridToWorld(cell);
                long start = Stopwatch.GetTimestamp();
                hovered.Invoke(Game, new object[] { screen });
                handlers.Add(Stopwatch.GetElapsedTime(start).TotalMilliseconds);
                await Frame();
            }

            var resourceCells = battle.KnownFish(Side.Player).Concat(battle.KnownShoals(Side.Player)).ToHashSet();
            var destination = battle.Reachable(ship.Id).OrderByDescending(p => p.Value).First(p => p.Key != ship.Position && !resourceCells.Contains(p.Key)).Key;
            var origin = ship.Position;
            long movementStart = Stopwatch.GetTimestamp();
            Game.SelectCell(destination);
            await Game.CurrentOrder;
            if (ship.Position == origin)
                throw new InvalidOperationException("The measured UI order did not move the ship.");
            double movementMs = Stopwatch.GetElapsedTime(movementStart).TotalMilliseconds;
            for (int frame = 0; frame < 20; frame++)
                await Frame();
            _recordFrames = false;
            string report = panReport + $"INTERACTION world={kind}, wide_view={args.Contains("--wide-view")}, tiles={board.Tiles.Count}, opponents={opponents}, live_fog={args.Contains("--live-fog")}, hover_calls={handlers.Count}, " + $"hover_p50_ms={Percentile(handlers, .5):F3}, hover_p95_ms={Percentile(handlers, .95):F3}, " + $"hover_max_ms={handlers.Max():F3}, frame_p95_ms={Percentile(_frames, .95):F3}, " + $"frame_max_ms={_frames.Max():F3}, process_p95_ms={Percentile(_processTimes, .95):F3}, " + $"draw_calls_p95={Percentile(_drawCalls, .95):F0}, movement_elapsed_ms={movementMs:F3}\n" + PerformanceTrace.Report();
            GD.Print(report);
            var output = OS.GetCmdlineUserArgs().FirstOrDefault(a => a.StartsWith("--report="));
            if (output is not null)
                File.WriteAllText(output[9..], report);
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
