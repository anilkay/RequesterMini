namespace AppLogger;

public interface ILogSink
{
    void Write(LogEntry entry);
}
