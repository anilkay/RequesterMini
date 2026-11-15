namespace JsonFileStore;

public sealed class FileBackend : IStoreBackend
{
    private readonly string _filePath;

    public FileBackend(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        _filePath = filePath;
    }

    /// <remarks>
    /// A read that throws propagates untouched. It used to call <see cref="OnLoadFailed"/> first, which
    /// moved the file to <c>.bak</c> — but <see cref="IStoreBackend.OnLoadFailed"/> is for content that
    /// cannot be deserialized, and an I/O error says nothing about the content.
    /// </remarks>
    public string? Load() => File.Exists(_filePath) ? File.ReadAllText(_filePath) : null;

    public void Save(string data)
    {
        try
        {
            var directory = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);

            var tempPath = _filePath + ".tmp";
            File.WriteAllText(tempPath, data);
            File.Move(tempPath, _filePath, overwrite: true);
        }
        catch
        {
            throw;
        }
    }

    public void OnLoadFailed()
    {
        try
        {
            if (File.Exists(_filePath))
                File.Move(_filePath, _filePath + ".bak", overwrite: true);
        }
        catch
        {
            // Silently swallow — backup failure should not block memory-only mode.
        }
    }
}
