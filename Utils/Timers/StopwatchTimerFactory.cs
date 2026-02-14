using System.Diagnostics;

namespace RequesterMini.Utils.Timers;

public sealed class StopwatchTimerFactory : IElapsedTimerFactory
{
    public IElapsedTimer StartNew() => new StopwatchTimer(Stopwatch.StartNew());
}
