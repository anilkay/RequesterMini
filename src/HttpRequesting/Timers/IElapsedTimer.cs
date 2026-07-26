namespace HttpRequesting.Timers;

public interface IElapsedTimer
{
    TimeSpan Elapsed { get; }
    void Stop();
}
