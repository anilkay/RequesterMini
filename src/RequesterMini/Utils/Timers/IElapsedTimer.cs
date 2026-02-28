using System;

namespace RequesterMini.Utils.Timers;

public interface IElapsedTimer
{
    TimeSpan Elapsed { get; }
    void Stop();
}
