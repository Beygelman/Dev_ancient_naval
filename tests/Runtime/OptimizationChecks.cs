using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Units;
using DevAncientNaval.Core.World;
using DevAncientNaval.Presentation;
using DevAncientNaval.Presentation.Map;
using Godot;
using Side = DevAncientNaval.Core.Units.Side;

namespace DevAncientNaval.Tests.Runtime;
/// <summary>
/// Checks unchanged depth/visibility semantics and compares managed allocation
/// of draw-order preparation. This is not a GPU or whole-frame FPS benchmark.
/// </summary>
public partial class OptimizationChecks : Node
{
    public Main Game { get; set; } = null!;

    private int _checks;
    private float _checksum;
    private readonly HashSet<int> _baselineSuppressed = new();
    private readonly Dictionary<int, ShipSnapshot> _baselineSnapshots = new();
    private void Check(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }

        _checks++;
    }

    public override async void _Ready()
    {
        try
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            CheckMenuLayout();
            CheckVisibilityChanges();
            foreach (int fleetSize in new[]
            {
                16,
                128,
                512
            }

            )
            {
                BenchmarkDrawOrder(fleetSize);
            }

            GD.Print($"PASS: {_checks} rendering optimization checks; checksum={_checksum:0.##}.");
            GetTree().Quit();
        }
        catch (Exception error)
        {
            GD.PushError(error.ToString());
            GetTree().Quit(1);
        }
    }

    private void CheckMenuLayout()
    {
        Game.Home.ShowHome(false);
        int before = Game.Home.LayoutPasses;
        for (int frame = 0; frame < 120; frame++)
        {
            Game.Home._Process(1d / 60);
        }

        Check(Game.Home.LayoutPasses == before, "Unchanged menu size must not relayout each frame.");
        var root = Game.Home.GetNode<Control>("HomeRoot");
        var original = root.Size;
        root.Size = original + new Vector2(1, 1);
        Check(Game.Home.LayoutPasses > before, "Resizing must trigger title layout.");
        root.Size = original;
        Game.Home.Hide();
    }

    private BattleState Scenario(int fleetSize)
    {
        var board = new GameBoard(40, 40, _ => TerrainType.Water);
        var setup = new List<(Side, ShipClass, GridPosition)>
        {
            (Side.Player, ShipClass.Mothership, new(0, 0)),
            (Side.Enemy, ShipClass.Mothership, new(39, 39))
        };
        for (int i = 0; i < fleetSize - 2; i++)
        {
            // Fishing ships have no fleet cap, allowing stress fixtures without
            // changing balance or creating an invalid battle state.
            setup.Add((Side.Player, ShipClass.Fishing, new(i % 38 + 1, i / 38 + 1)));
        }

        return new BattleState(board, Game.Battle.Rules, setup, Array.Empty<GridPosition>(), villageSpots: Array.Empty<GridPosition>());
    }

    private void CheckVisibilityChanges()
    {
        Game.LoadScenario(Scenario(2));
        var before = Game.Fleet.PrepareDrawOrder().Select(entry => entry.Ship.Id).ToArray();
        Check(before.SequenceEqual(new[] { 1 }), "Hidden enemy must not enter the render list.");
        Game.Battle.Vision.RevealCombat(Side.Player, new(39, 39));
        Game.Battle.Vision.Recompute(Game.Battle.Ships, 1, Game.Battle.Villages);
        var after = Game.Fleet.PrepareDrawOrder().Select(entry => entry.Ship.Id).ToArray();
        Check(after.Contains(2), "New visibility must be reflected without a stale cached fleet.");
        Game.LoadScenario(Scenario(2));
        Check(Game.Fleet.PrepareDrawOrder().Count == 1, "Switching battle must discard the previous draw list.");
    }

    private void BenchmarkDrawOrder(int fleetSize)
    {
        Game.LoadScenario(Scenario(fleetSize));
        var fleet = Game.Fleet;
        var expected = LegacyOrder().Select(ship => ship.Id).ToArray();
        var actual = fleet.PrepareDrawOrder().Select(entry => entry.Ship.Id).ToArray();
        Check(expected.SequenceEqual(actual), $"Stable depth order must match for {fleetSize} ships.");
        void Original()
        {
            foreach (var ship in LegacyOrder())
            {
                _checksum += fleet.Projection.GridToWorld(ship.Position).Y;
            }
        }

        void Refactored()
        {
            var entries = fleet.PrepareDrawOrder();
            for (int i = 0; i < entries.Count; i++)
            {
                _checksum += entries[i].Center.Y;
            }
        }

        for (int warmup = 0; warmup < 30; warmup++)
        {
            Original();
            Refactored();
        }

        const int frames = 300;
        var original = Measure(Original, frames);
        var refactored = Measure(Refactored, frames);
        Check(refactored.Bytes < original.Bytes, $"Draw preparation allocations should decrease for {fleetSize} ships.");
        GD.Print($"DRAW_ORDER ships={fleetSize}, iterations={frames}, " + $"before_bytes={original.Bytes}, after_bytes={refactored.Bytes}, " + $"before_ms={original.Milliseconds:0.###}, after_ms={refactored.Milliseconds:0.###}");
    }

    private IEnumerable<ShipSnapshot> LegacyOrder()
    {
        var fleet = Game.Fleet;
        // Exact pre-refactor idle path, retained only as the measurement reference.
        return fleet.Battle.ObservedShips(Side.Player).Where(ship => !_baselineSuppressed.Contains(ship.Id) && ship.Id != 0).Select(ShipSnapshot.From).Where(ship => !_baselineSnapshots.ContainsKey(ship.Id)).Concat(_baselineSnapshots.Values).OrderBy(ship => fleet.Projection.GridToWorld(ship.Position).Y);
    }

    private static (long Bytes, double Milliseconds) Measure(Action action, int iterations)
    {
        long started = Stopwatch.GetTimestamp();
        long allocated = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < iterations; i++)
        {
            action();
        }

        return (GC.GetAllocatedBytesForCurrentThread() - allocated, Stopwatch.GetElapsedTime(started).TotalMilliseconds);
    }
}
