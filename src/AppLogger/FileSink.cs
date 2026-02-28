using System.Globalization;

namespace AppLogger;

public sealed class FileSink : ILogSink
{
    private readonly object _gate = new();
    private readonly string _logDirectory;
    private readonly string _logPath;
    private readonly long _maxFileSizeBytes;

    public FileSink(string logPath, long maxFileSizeBytes = 2 * 1024 * 1024)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(logPath);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(maxFileSizeBytes, 0);

        _logPath = logPath;
        _logDirectory = Path.GetDirectoryName(logPath) ?? string.Empty;
        _maxFileSizeBytes = maxFileSizeBytes;
    }

    public void Write(LogEntry entry)
    {
        try
        {
            lock (_gate)
            {
                if (!string.IsNullOrEmpty(_logDirectory))
                    Directory.CreateDirectory(_logDirectory);

                RotateIfNeeded();

                var timestamp = entry.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
                var levelLabel = entry.Level switch
                {
                    LogLevel.Debug => "DEBUG",
                    LogLevel.Info => "INFO",
                    LogLevel.Warning => "WARNING",
                    LogLevel.Error => "ERROR",
                    _ => "UNKNOWN"
                };
                File.AppendAllText(_logPath, $"[{timestamp}] [{levelLabel}] {entry.Message}{Environment.NewLine}");
            }
        }
        catch
        {
            // Logging must never crash the app.
        }
    }

    private void RotateIfNeeded()
    {
        if (!File.Exists(_logPath))
            return;

        var info = new FileInfo(_logPath);
        if (info.Length <= _maxFileSizeBytes)
            return;

        var archivePath = _logPath + ".1";
        if (File.Exists(archivePath))
            File.Delete(archivePath);

        File.Move(_logPath, archivePath);
    }
}
