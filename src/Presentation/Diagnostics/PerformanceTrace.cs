using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using Godot;

namespace DevAncientNaval.Presentation.Diagnostics;
<<<<<<< Updated upstream
=======

>>>>>>> Stashed changes
/// <summary>Opt-in CPU scope diagnostics. Disabled in ordinary play; no logging
/// or file access occurs in the draw path.</summary>
internal static class PerformanceTrace
{
    public static bool Enabled { get; } = OS.GetCmdlineUserArgs().Contains("--performance-test");
<<<<<<< Updated upstream

=======
>>>>>>> Stashed changes
    private sealed class Metric
    {
        public int Calls;
        public double TotalMs, MaxMs;
        public long Bytes;
    }
<<<<<<< Updated upstream

    private static readonly Dictionary<string, Metric> Metrics = new();
    private static readonly object Gate = new();
    public static Scope Measure(string name) => new(name, Enabled);
    public static void Reset()
    {
        lock (Gate)
            Metrics.Clear();
    }

    public static string Report()
    {
        lock (Gate)
            return JsonSerializer.Serialize(Metrics.ToDictionary(p => p.Key, p => new { p.Value.Calls, p.Value.TotalMs, p.Value.MaxMs, p.Value.Bytes }), new JsonSerializerOptions { WriteIndented = true });
    }

=======
    private static readonly Dictionary<string, Metric> Metrics = new();
    private static readonly object Gate = new();
    public static Scope Measure(string name) => new(name, Enabled);
    public static void Reset() { lock (Gate) Metrics.Clear(); }
    public static string Report() { lock (Gate) return JsonSerializer.Serialize(Metrics.ToDictionary(p => p.Key,
        p => new { p.Value.Calls, p.Value.TotalMs, p.Value.MaxMs, p.Value.Bytes }),
        new JsonSerializerOptions { WriteIndented = true }); }
>>>>>>> Stashed changes
    internal readonly struct Scope : IDisposable
    {
        private readonly string _name;
        private readonly long _started, _allocated;
        private readonly bool _enabled;
        public Scope(string name, bool enabled)
        {
<<<<<<< Updated upstream
            _name = name;
            _enabled = enabled;
            _started = enabled ? Stopwatch.GetTimestamp() : 0;
            _allocated = enabled ? GC.GetAllocatedBytesForCurrentThread() : 0;
        }

        public void Dispose()
        {
            if (!_enabled)
                return;
=======
            _name = name; _enabled = enabled;
            _started = enabled ? Stopwatch.GetTimestamp() : 0;
            _allocated = enabled ? GC.GetAllocatedBytesForCurrentThread() : 0;
        }
        public void Dispose()
        {
            if (!_enabled) return;
>>>>>>> Stashed changes
            double ms = Stopwatch.GetElapsedTime(_started).TotalMilliseconds;
            long bytes = GC.GetAllocatedBytesForCurrentThread() - _allocated;
            lock (Gate)
            {
<<<<<<< Updated upstream
                if (!Metrics.TryGetValue(_name, out var metric))
                    Metrics.Add(_name, metric = new());
                metric.Calls++;
                metric.TotalMs += ms;
                metric.MaxMs = Math.Max(metric.MaxMs, ms);
=======
                if (!Metrics.TryGetValue(_name, out var metric)) Metrics.Add(_name, metric = new());
                metric.Calls++; metric.TotalMs += ms; metric.MaxMs = Math.Max(metric.MaxMs, ms);
>>>>>>> Stashed changes
                metric.Bytes += bytes;
            }
        }
    }
}
