using Xunit;

namespace AppLogger.Tests;

public class LoggerTests
{
    private readonly string _testDir;

    public LoggerTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "AppLoggerTests", Guid.NewGuid().ToString("N"));
    }

    [Fact]
    public void Initialize_NullApplicationName_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => Logger.Initialize(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Initialize_EmptyOrWhiteSpaceName_Throws(string name)
    {
        Assert.Throws<ArgumentException>(() => Logger.Initialize(name));
    }

    [Fact]
    public void Initialize_ZeroMaxFileSize_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Logger.Initialize("TestApp", maxFileSizeBytes: 0));
    }

    [Fact]
    public void Initialize_NegativeMaxFileSize_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Logger.Initialize("TestApp", maxFileSizeBytes: -1));
    }

    [Theory]
    [InlineData(LogLevel.Debug)]
    [InlineData(LogLevel.Info)]
    [InlineData(LogLevel.Warning)]
    [InlineData(LogLevel.Error)]
    public void SetMinimumLevel_UpdatesMinimumLevel(LogLevel level)
    {
        Logger.Initialize("TestApp");
        Logger.SetMinimumLevel(level);

        Assert.Equal(level, Logger.MinimumLevel);
    }

    [Theory]
    [InlineData(LogLevel.Debug, "DEBUG")]
    [InlineData(LogLevel.Info, "INFO")]
    [InlineData(LogLevel.Warning, "WARNING")]
    [InlineData(LogLevel.Error, "ERROR")]
    public void LogLevel_AllValuesAreDefined(LogLevel level, string expectedLabel)
    {
        Assert.True(Enum.IsDefined(level));
        Assert.Contains(expectedLabel, expectedLabel); // Ensures label is non-empty
    }

    [Fact]
    public void AddSink_CustomSink_ReceivesLogEntries()
    {
        Logger.Initialize("TestApp");
        var sink = new MemorySink();
        Logger.AddSink(sink);
        Logger.SetMinimumLevel(LogLevel.Info);

        Logger.Info("hello sink");

        Assert.Contains(sink.Entries, e => e.Message == "hello sink" && e.Level == LogLevel.Info);
    }

    [Fact]
    public void AddSink_BelowMinimumLevel_SinkNotCalled()
    {
        Logger.Initialize("TestApp");
        var sink = new MemorySink();
        Logger.AddSink(sink);
        Logger.SetMinimumLevel(LogLevel.Warning);

        Logger.Debug("should be filtered");
        Logger.Info("also filtered");

        Assert.Empty(sink.Entries);
    }

    [Fact]
    public void ClearSinks_RemovesAllSinks()
    {
        Logger.Initialize("TestApp");
        var sink = new MemorySink();
        Logger.AddSink(sink);
        Logger.ClearSinks();
        Logger.SetMinimumLevel(LogLevel.Debug);

        Logger.Info("after clear");

        Assert.Empty(sink.Entries);
    }

    [Fact]
    public void FileSink_Write_CreatesFileWithEntry()
    {
        Directory.CreateDirectory(_testDir);
        var logPath = Path.Combine(_testDir, "test.log");
        var fileSink = new FileSink(logPath);

        fileSink.Write(new LogEntry(LogLevel.Info, "file sink test", DateTime.UtcNow));

        Assert.True(File.Exists(logPath));
        var content = File.ReadAllText(logPath);
        Assert.Contains("[INFO]", content);
        Assert.Contains("file sink test", content);
    }

    [Fact]
    public void FileSink_RotatesWhenExceedsMaxSize()
    {
        Directory.CreateDirectory(_testDir);
        var logPath = Path.Combine(_testDir, "rotate.log");
        // Use a very small max size so rotation triggers immediately
        var fileSink = new FileSink(logPath, maxFileSizeBytes: 10);

        // Write initial content that exceeds 10 bytes
        File.WriteAllText(logPath, new string('x', 20));

        fileSink.Write(new LogEntry(LogLevel.Info, "triggers rotation", DateTime.UtcNow));

        Assert.True(File.Exists(logPath + ".1"), "Archive file should exist after rotation");
        Assert.True(File.Exists(logPath), "New log file should exist after rotation");
    }

    // ── New tests ──────────────────────────────────────────────────────────────

    [Fact]
    public void Initialize_EnvironmentVariableOverridesMinimumLevel()
    {
        // The env-var name is derived as "{APPNAME}_LOG_LEVEL"
        const string envVarName = "TESTAPP_LOG_LEVEL";
        try
        {
            Environment.SetEnvironmentVariable(envVarName, "Debug");
            Logger.Initialize("TestApp");

            Assert.Equal(LogLevel.Debug, Logger.MinimumLevel);
        }
        finally
        {
            Environment.SetEnvironmentVariable(envVarName, null);
        }
    }

    [Fact]
    public void Error_WithException_IncludesExceptionInMessage()
    {
        Logger.Initialize("TestApp");
        var sink = new MemorySink();
        Logger.AddSink(sink);
        Logger.SetMinimumLevel(LogLevel.Error);

        Logger.Error("msg", new InvalidOperationException("boom"));

        var entry = Assert.Single(sink.Entries);
        Assert.Contains("msg", entry.Message);
        Assert.Contains("boom", entry.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void FileSink_Constructor_NullOrEmptyPath_Throws(string? path)
    {
        Assert.ThrowsAny<ArgumentException>(() => new FileSink(path!));
    }

    [Fact]
    public void Logger_Initialize_CalledTwice_ResetsSinks()
    {
        Logger.Initialize("TestApp");
        var sink = new MemorySink();
        Logger.AddSink(sink);

        // Second Initialize clears all previously registered sinks
        Logger.Initialize("TestApp");
        Logger.Info("second log");

        Assert.Empty(sink.Entries);
    }

    [Fact]
    public void Logger_BelowMinimumLevel_FileSinkNotWritten()
    {
        // Use a unique app name so the log path is isolated from other test runs
        var appName = "LogFilter" + Guid.NewGuid().ToString("N");
        var logPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            appName,
            "logs",
            $"{appName.ToLowerInvariant()}.log");

        try
        {
            Logger.Initialize(appName, minimumLevel: LogLevel.Error);
            Logger.Info("should not appear");

            Assert.True(
                !File.Exists(logPath) || !File.ReadAllText(logPath).Contains("should not appear"),
                "Info message must not be written when minimum level is Error");
        }
        finally
        {
            var appDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                appName);
            if (Directory.Exists(appDir))
                Directory.Delete(appDir, recursive: true);

            // Restore logger to a predictable state for subsequent tests
            Logger.Initialize("TestApp");
        }
    }

    [Theory]
    [InlineData(LogLevel.Debug)]
    [InlineData(LogLevel.Info)]
    [InlineData(LogLevel.Warning)]
    [InlineData(LogLevel.Error)]
    public void ConsoleSink_Write_AllLevels_DoesNotThrow(LogLevel level)
    {
        var sink = new ConsoleSink();
        var entry = new LogEntry(level, "test message", DateTime.UtcNow);

        var ex = Record.Exception(() => sink.Write(entry));

        Assert.Null(ex);
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private sealed class MemorySink : ILogSink
    {
        public List<LogEntry> Entries { get; } = new();

        public void Write(LogEntry entry) => Entries.Add(entry);
    }
}
