using System;
using System.Diagnostics;

namespace RequesterMini.Utils.Timers;

public sealed class StopwatchTimer(Stopwatch stopwatch) : IElapsedTimer
{
    public TimeSpan Elapsed => stopwatch.Elapsed;
    public void Stop() => stopwatch.Stop();
}
