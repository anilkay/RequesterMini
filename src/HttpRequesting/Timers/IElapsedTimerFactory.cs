namespace HttpRequesting.Timers;

public interface IElapsedTimerFactory
{
    IElapsedTimer StartNew();
}
