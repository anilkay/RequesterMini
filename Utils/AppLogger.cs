using System;
using System.Globalization;
using System.IO;

namespace RequesterMini.Utils;

public enum LogLevel
{
    Debug = 0,
    Info = 1,
    Warning = 2,
    Error = 3
}

public static class AppLogger
{
    private static readonly object Gate = new();
    private static readonly string LogDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "RequesterMini",
        "logs");

    private static readonly string LogPath = Path.Combine(LogDirectory, "requestermini.log");
    private const long MaxLogFileBytes = 2 * 1024 * 1024; // 2 MB

#if DEBUG
    private static LogLevel _minimumLevel = LogLevel.Debug;
#else
    private static LogLevel _minimumLevel = LogLevel.Info;
#endif

    static AppLogger()
    {
        // Optional override: REQUESTERMINI_LOG_LEVEL=Debug|Info|Warning|Error
        var configuredLevel = Environment.GetEnvironmentVariable("REQUESTERMINI_LOG_LEVEL");
        if (Enum.TryParse<LogLevel>(configuredLevel, ignoreCase: true, out var parsedLevel))
        {
            _minimumLevel = parsedLevel;
        }
    }

    public static LogLevel MinimumLevel => _minimumLevel;

    public static void SetMinimumLevel(LogLevel level) => _minimumLevel = level;

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
        if (level < _minimumLevel)
        {
            return;
        }

        try
        {
            lock (Gate)
            {
                Directory.CreateDirectory(LogDirectory);
                RotateIfNeeded();

                var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
                File.AppendAllText(LogPath, $"[{timestamp}] [{level.ToString().ToUpperInvariant()}] {message}{Environment.NewLine}");
            }
        }
        catch
        {
            // Logging must never crash the app.
        }
    }

    private static void RotateIfNeeded()
    {
        if (!File.Exists(LogPath))
        {
            return;
        }

        var info = new FileInfo(LogPath);
        if (info.Length <= MaxLogFileBytes)
        {
            return;
        }

        var archivePath = Path.Combine(LogDirectory, "requestermini.log.1");
        if (File.Exists(archivePath))
        {
            File.Delete(archivePath);
        }

        File.Move(LogPath, archivePath);
    }
}
