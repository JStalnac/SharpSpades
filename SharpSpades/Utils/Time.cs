using System.Diagnostics;

namespace SharpSpades.Utils;

public static class Time
{
    private static readonly double millisecondsPerTick = 1000d / Stopwatch.Frequency;

    /// <summary>
    /// Gets the total number of milliseconds since system restart. Monotonic if a high-resolution performance
    /// counter is available (See <see cref="Stopwatch.IsHighResolution" />)
    /// </summary>
    /// <returns></returns>
    public static ulong CurrentMillis()
        => (ulong)(Stopwatch.GetTimestamp() * millisecondsPerTick);
}