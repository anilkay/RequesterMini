namespace JsonFileStore;

public sealed class FileBackend : IStoreBackend
{
    private readonly string _filePath;

    public FileBackend(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        _filePath = filePath;
    }

    public string? Load()
    {
        try
        {
            if (!File.Exists(_filePath))
                return null;

            return File.ReadAllText(_filePath);
        }
        catch
        {
            OnLoadFailed();
            throw;
        }
    }

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
