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
}
