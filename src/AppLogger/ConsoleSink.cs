namespace AppLogger;

public sealed class ConsoleSink : ILogSink
{
    public void Write(LogEntry entry)
    {
        var color = entry.Level switch
        {
            LogLevel.Debug => ConsoleColor.Gray,
            LogLevel.Info => ConsoleColor.White,
            LogLevel.Warning => ConsoleColor.Yellow,
            LogLevel.Error => ConsoleColor.Red,
            _ => ConsoleColor.White
        };

        var levelLabel = entry.Level switch
        {
            LogLevel.Debug => "DEBUG",
            LogLevel.Info => "INFO",
            LogLevel.Warning => "WARNING",
            LogLevel.Error => "ERROR",
            _ => "UNKNOWN"
        };

        var timestamp = entry.Timestamp.ToString("HH:mm:ss.fff");
        var line = $"[{timestamp}] [{levelLabel}] {entry.Message}";

        var previous = Console.ForegroundColor;
        Console.ForegroundColor = color;
        Console.WriteLine(line);
        Console.ForegroundColor = previous;
    }
}
