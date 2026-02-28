using System.Globalization;

namespace AppLogger;

public enum LogLevel
{
    Debug = 0,
    Info = 1,
    Warning = 2,
    Error = 3
}

public static class Logger
{
    private static readonly object Gate = new();

    private static string _logDirectory = string.Empty;
    private static string _logPath = string.Empty;
    private static long _maxLogFileBytes = 2 * 1024 * 1024; // 2 MB
    private static LogLevel _minimumLevel = LogLevel.Info;
    private static bool _initialized;

    /// <summary>
    /// Initializes the logger with the given application name.
    /// Log files are written to <c>{LocalApplicationData}/{applicationName}/logs</c>.
    /// An environment variable named <c>{APPLICATIONNAME}_LOG_LEVEL</c> (uppercased, spaces removed)
    /// can override the minimum level at startup.
    /// </summary>
    public static void Initialize(string applicationName, LogLevel minimumLevel = LogLevel.Info, long maxFileSizeBytes = 2 * 1024 * 1024)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(applicationName);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(maxFileSizeBytes, 0);

        lock (Gate)
        {
            _logDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                applicationName,
                "logs");
            _logPath = Path.Combine(_logDirectory, $"{applicationName.ToLowerInvariant()}.log");
            _minimumLevel = minimumLevel;
            _maxLogFileBytes = maxFileSizeBytes;
            _initialized = true;

            var envVarName = $"{applicationName.ToUpperInvariant().Replace(" ", "")}_LOG_LEVEL";
            var configuredLevel = Environment.GetEnvironmentVariable(envVarName);
            if (Enum.TryParse<LogLevel>(configuredLevel, ignoreCase: true, out var parsedLevel))
            {
                _minimumLevel = parsedLevel;
            }
        }
    }

    public static LogLevel MinimumLevel
    {
        get
        {
            lock (Gate)
            {
                return _minimumLevel;
            }
        }
    }

    public static void SetMinimumLevel(LogLevel level)
    {
        lock (Gate)
        {
            _minimumLevel = level;
        }
    }

    public static void Debug(string message) => Write(LogLevel.Debug, message);

    public static void Info(string message) => Write(LogLevel.Info, message);

    public static void Warning(string message) => Write(LogLevel.Warning, message);

    public static void Error(string message, Exception? exception = null)
    {
        var fullMessage = exception is null
            ? message
            : $"{message}{Environment.NewLine}{exception}";

        Write(LogLevel.Error, fullMessage);
    }

    private static void Write(LogLevel level, string message)
    {
        lock (Gate)
        {
            if (!_initialized || level < _minimumLevel)
            {
                return;
            }
        }

        try
        {
            lock (Gate)
            {
                Directory.CreateDirectory(_logDirectory);
                RotateIfNeeded();

                var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
                var levelLabel = level switch
                {
                    LogLevel.Debug => "DEBUG",
                    LogLevel.Info => "INFO",
                    LogLevel.Warning => "WARNING",
                    LogLevel.Error => "ERROR",
                    _ => "UNKNOWN"
                };
                File.AppendAllText(_logPath, $"[{timestamp}] [{levelLabel}] {message}{Environment.NewLine}");
            }
        }
        catch
        {
            // Logging must never crash the app.
        }
    }

    private static void RotateIfNeeded()
    {
        if (!File.Exists(_logPath))
        {
            return;
        }

        var info = new FileInfo(_logPath);
        if (info.Length <= _maxLogFileBytes)
        {
            return;
        }

        var archivePath = _logPath + ".1";
        if (File.Exists(archivePath))
        {
            File.Delete(archivePath);
        }

        File.Move(_logPath, archivePath);
    }
}
