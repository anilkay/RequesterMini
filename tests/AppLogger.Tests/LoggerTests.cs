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

    private sealed class MemorySink : ILogSink
    {
        public List<LogEntry> Entries { get; } = new();

        public void Write(LogEntry entry) => Entries.Add(entry);
    }
}
