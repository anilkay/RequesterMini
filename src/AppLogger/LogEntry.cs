namespace AppLogger;

public readonly record struct LogEntry(LogLevel Level, string Message, DateTime Timestamp);
