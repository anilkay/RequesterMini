using System.Diagnostics;

namespace HttpRequesting.Timers;

public sealed class StopwatchTimerFactory : IElapsedTimerFactory
{
    public IElapsedTimer StartNew() => new StopwatchTimer(Stopwatch.StartNew());
}
