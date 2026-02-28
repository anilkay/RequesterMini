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
    private static readonly List<ILogSink> _sinks = new();

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
            _minimumLevel = minimumLevel;

            var envVarName = $"{applicationName.ToUpperInvariant().Replace(" ", "")}_LOG_LEVEL";
            var configuredLevel = Environment.GetEnvironmentVariable(envVarName);
            if (Enum.TryParse<LogLevel>(configuredLevel, ignoreCase: true, out var parsedLevel))
            {
                _minimumLevel = parsedLevel;
            }

            _sinks.Clear();
            var logDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                applicationName,
                "logs");
            var logPath = Path.Combine(logDirectory, $"{applicationName.ToLowerInvariant()}.log");
            _sinks.Add(new FileSink(logPath, maxFileSizeBytes));

            _initialized = true;
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

    public static void AddSink(ILogSink sink)
    {
        lock (Gate)
        {
            _sinks.Add(sink);
        }
    }

    public static void ClearSinks()
    {
        lock (Gate)
        {
            _sinks.Clear();
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
        ILogSink[] sinks;
        lock (Gate)
        {
            if (!_initialized || level < _minimumLevel)
                return;

            sinks = _sinks.ToArray();
        }

        var entry = new LogEntry(level, message, DateTime.UtcNow);
        foreach (var sink in sinks)
        {
            sink.Write(entry);
        }
    }
}
