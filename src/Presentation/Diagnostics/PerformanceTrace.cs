using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using Godot;

namespace DevAncientNaval.Presentation.Diagnostics;
internal sealed record TraceMetric(int Calls, double TotalMs, double MaxMs, long Bytes);
/// <summary>Opt-in CPU scope diagnostics. Disabled in ordinary play; no logging
/// or file access occurs in the draw path.</summary>
internal static class PerformanceTrace
{
    public static bool Enabled { get; } = OS.GetCmdlineUserArgs().Contains("--performance-test");

    private sealed class Metric
    {
        public int Calls;
        public double TotalMs, MaxMs;
        public long Bytes;
    }

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
            return JsonSerializer.Serialize(Metrics.ToDictionary(p => p.Key, p => new TraceMetric(p.Value.Calls, p.Value.TotalMs, p.Value.MaxMs, p.Value.Bytes)), ReportJson.TraceMetrics);
    }

    private static readonly PresentationJsonContext ReportJson = new(new JsonSerializerOptions { WriteIndented = true });

    internal readonly struct Scope : IDisposable
    {
        private readonly string _name;
        private readonly long _started, _allocated;
        private readonly bool _enabled;
        public Scope(string name, bool enabled)
        {
            _name = name;
            _enabled = enabled;
            _started = enabled ? Stopwatch.GetTimestamp() : 0;
            _allocated = enabled ? GC.GetAllocatedBytesForCurrentThread() : 0;
        }

        public void Dispose()
        {
            if (!_enabled)
                return;
            double ms = Stopwatch.GetElapsedTime(_started).TotalMilliseconds;
            long bytes = GC.GetAllocatedBytesForCurrentThread() - _allocated;
            lock (Gate)
            {
                if (!Metrics.TryGetValue(_name, out var metric))
                    Metrics.Add(_name, metric = new());
                metric.Calls++;
                metric.TotalMs += ms;
                metric.MaxMs = Math.Max(metric.MaxMs, ms);
                metric.Bytes += bytes;
            }
        }
    }
}
