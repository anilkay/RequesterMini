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

    [Fact]
    public void MemoryBackend_SaveThenLoad_RoundTrips()
    {
        var store = new JsonStore<List<TestItem>>(new MemoryBackend(), TestSourceGenerationContext.Default.ListTestItem);
        var items = new List<TestItem> { new("Alpha", 1), new("Beta", 2) };

        store.Save(items);
        var loaded = store.Load();

        Assert.NotNull(loaded);
        Assert.Equal(2, loaded.Count);
        Assert.Equal("Alpha", loaded[0].Name);
        Assert.Equal(2, loaded[1].Value);
    }

    [Fact]
    public void MemoryBackend_IsPersistenceAvailable_AlwaysTrue()
    {
        var store = new JsonStore<List<TestItem>>(new MemoryBackend(), TestSourceGenerationContext.Default.ListTestItem);
        var items = new List<TestItem> { new("X", 1) };

        store.Save(items);
        store.Load();

        Assert.True(store.IsPersistenceAvailable);
    }

    [Fact]
    public void FileBackend_Constructor_NullPath_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new FileBackend(null!));
    }

    [Fact]
    public void FileBackend_Load_FileDoesNotExist_ReturnsNull()
    {
        var backend = new FileBackend(Path.Combine(_tempDir, "nonexistent.json"));
        Assert.Null(backend.Load());
    }

    [Fact]
    public void FileBackend_Save_CreatesDirectoryAndFile()
    {
        var nestedPath = Path.Combine(_tempDir, "sub", "deep", "data.json");
        var backend = new FileBackend(nestedPath);

        backend.Save("{\"test\":1}");

        Assert.True(File.Exists(nestedPath));
        Assert.Equal("{\"test\":1}", File.ReadAllText(nestedPath));
    }

    [Fact]
    public void FileBackend_OnLoadFailed_CreatesBackupFile()
    {
        File.WriteAllText(_filePath, "corrupt data");
        var backend = new FileBackend(_filePath);

        backend.OnLoadFailed();

        Assert.True(File.Exists(_filePath + ".bak"));
    }

    [Fact]
    public void JsonStore_CorruptData_DisablesPersistenceAndCallsOnLoadFailed()
    {
        var backend = new ThrowingBackend();
        var store = new JsonStore<List<TestItem>>(backend, TestSourceGenerationContext.Default.ListTestItem);

        store.Load();

        Assert.False(store.IsPersistenceAvailable);
        Assert.True(backend.OnLoadFailedCalled);
    }

    // ── New tests ──────────────────────────────────────────────────────────────

    [Fact]
    public void JsonStore_SaveFailure_DisablesPersistence()
    {
        var backend = new ThrowingOnSaveBackend();
        var store = new JsonStore<List<TestItem>>(backend, TestSourceGenerationContext.Default.ListTestItem);

        store.Save(new List<TestItem> { new("Item", 1) });

        Assert.False(store.IsPersistenceAvailable);
    }

    [Fact]
    public void FileBackend_OnLoadFailed_WithNonExistentFile_DoesNotThrow()
    {
        var backend = new FileBackend(Path.Combine(_tempDir, "nonexistent.json"));

        var ex = Record.Exception(() => backend.OnLoadFailed());

        Assert.Null(ex);
    }

    [Fact]
    public void FileBackend_Save_CreatesCorrectJsonContent()
    {
        var backend = new FileBackend(_filePath);
        const string jsonContent = "[{\"Name\":\"Test\",\"Value\":42}]";

        backend.Save(jsonContent);

        Assert.True(File.Exists(_filePath));
        Assert.Equal(jsonContent, File.ReadAllText(_filePath));
    }
}

internal sealed class ThrowingOnSaveBackend : IStoreBackend
{
    public string? Load() => null;
    public void Save(string data) => throw new IOException("Simulated save failure");
    public void OnLoadFailed() { }
}

internal sealed class ThrowingBackend : IStoreBackend
{
    public bool OnLoadFailedCalled { get; private set; }
    public string? Load() => throw new InvalidOperationException("Simulated load failure");
    public void Save(string data) { }
    public void OnLoadFailed() => OnLoadFailedCalled = true;
}
