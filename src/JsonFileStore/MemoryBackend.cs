namespace JsonFileStore;

public sealed class MemoryBackend : IStoreBackend
{
    private string? _data;
    public string? Load() => _data;
    public void Save(string data) => _data = data;
    public void OnLoadFailed() { } // nothing to clean up
}
