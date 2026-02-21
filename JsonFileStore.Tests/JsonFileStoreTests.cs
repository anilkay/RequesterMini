using System.Text.Json;
using System.Text.Json.Serialization;

namespace JsonFileStore.Tests;

public record TestItem(string Name, int Value);

[JsonSerializable(typeof(List<TestItem>))]
internal partial class TestSourceGenerationContext : JsonSerializerContext;

public class JsonFileStoreTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _filePath;

    public JsonFileStoreTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "JsonFileStoreTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        _filePath = Path.Combine(_tempDir, "test.json");
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }

    [Fact]
    public void Constructor_NullFilePath_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new JsonFileStore<List<TestItem>>(null!, TestSourceGenerationContext.Default.ListTestItem));
    }

    [Fact]
    public void Constructor_EmptyFilePath_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new JsonFileStore<List<TestItem>>("", TestSourceGenerationContext.Default.ListTestItem));
    }

    [Fact]
    public void Constructor_NullTypeInfo_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new JsonFileStore<List<TestItem>>(_filePath, null!));
    }

    [Fact]
    public void Load_FileDoesNotExist_ReturnsDefault()
    {
        var store = new JsonFileStore<List<TestItem>>(_filePath, TestSourceGenerationContext.Default.ListTestItem);

        var result = store.Load();

        Assert.Null(result);
        Assert.True(store.IsPersistenceAvailable);
    }

    [Fact]
    public void Save_ThenLoad_RoundTrips()
    {
        var store = new JsonFileStore<List<TestItem>>(_filePath, TestSourceGenerationContext.Default.ListTestItem);
        var items = new List<TestItem> { new("Alpha", 1), new("Beta", 2) };

        store.Save(items);
        var loaded = store.Load();

        Assert.NotNull(loaded);
        Assert.Equal(2, loaded.Count);
        Assert.Equal("Alpha", loaded[0].Name);
        Assert.Equal(2, loaded[1].Value);
    }

    [Fact]
    public void Save_CreatesDirectoryIfMissing()
    {
        var nestedPath = Path.Combine(_tempDir, "sub", "deep", "test.json");
        var store = new JsonFileStore<List<TestItem>>(nestedPath, TestSourceGenerationContext.Default.ListTestItem);

        store.Save([new("Item", 42)]);

        Assert.True(File.Exists(nestedPath));
    }

    [Fact]
    public void Save_NullData_Throws()
    {
        var store = new JsonFileStore<List<TestItem>>(_filePath, TestSourceGenerationContext.Default.ListTestItem);

        Assert.Throws<ArgumentNullException>(() => store.Save(null!));
    }

    [Fact]
    public void Load_CorruptFile_ReturnsDefaultAndDisablesPersistence()
    {
        File.WriteAllText(_filePath, "NOT VALID JSON{{{");
        var store = new JsonFileStore<List<TestItem>>(_filePath, TestSourceGenerationContext.Default.ListTestItem);

        var result = store.Load();

        Assert.Null(result);
        Assert.False(store.IsPersistenceAvailable);
        Assert.True(File.Exists(_filePath + ".bak"));
    }

    [Fact]
    public void Save_SkippedWhenPersistenceDisabled()
    {
        File.WriteAllText(_filePath, "CORRUPT");
        var store = new JsonFileStore<List<TestItem>>(_filePath, TestSourceGenerationContext.Default.ListTestItem);

        // Load corrupt file to disable persistence
        store.Load();
        Assert.False(store.IsPersistenceAvailable);

        // Clean up the .bak so we can verify Save is truly skipped
        File.Delete(_filePath + ".bak");
        if (File.Exists(_filePath)) File.Delete(_filePath);

        store.Save([new("Should not persist", 0)]);

        Assert.False(File.Exists(_filePath));
    }

    [Fact]
    public void Save_OverwritesExistingFile()
    {
        var store = new JsonFileStore<List<TestItem>>(_filePath, TestSourceGenerationContext.Default.ListTestItem);

        store.Save([new("First", 1)]);
        store.Save([new("Second", 2), new("Third", 3)]);

        var loaded = store.Load();
        Assert.NotNull(loaded);
        Assert.Equal(2, loaded.Count);
        Assert.Equal("Second", loaded[0].Name);
    }

    [Fact]
    public void Save_EmptyList_RoundTrips()
    {
        var store = new JsonFileStore<List<TestItem>>(_filePath, TestSourceGenerationContext.Default.ListTestItem);

        store.Save([]);
        var loaded = store.Load();

        Assert.NotNull(loaded);
        Assert.Empty(loaded);
    }

    [Fact]
    public void IsPersistenceAvailable_TrueByDefault()
    {
        var store = new JsonFileStore<List<TestItem>>(_filePath, TestSourceGenerationContext.Default.ListTestItem);

        Assert.True(store.IsPersistenceAvailable);
    }
}
