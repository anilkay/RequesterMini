using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace JsonFileStore;

public class JsonFileStore<T>
{
    private readonly string _filePath;
    private readonly JsonTypeInfo<T> _typeInfo;

    public bool IsPersistenceAvailable { get; private set; } = true;

    public JsonFileStore(string filePath, JsonTypeInfo<T> typeInfo)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentNullException.ThrowIfNull(typeInfo);

        _filePath = filePath;
        _typeInfo = typeInfo;
    }

    public T? Load()
    {
        try
        {
            if (!File.Exists(_filePath))
            {
                return default;
            }

            var json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize(json, _typeInfo);
        }
        catch (Exception ex)
        {
            IsPersistenceAvailable = false;
            Console.WriteLine($"[JsonFileStore] Failed to load from '{_filePath}'. Continuing in memory-only mode. {ex.Message}");

            try
            {
                if (File.Exists(_filePath))
                {
                    File.Move(_filePath, _filePath + ".bak", overwrite: true);
                }
            }
            catch
            {
                // If backup fails, still continue in memory-only mode.
            }

            return default;
        }
    }

    public void Save(T data)
    {
        ArgumentNullException.ThrowIfNull(data);

        if (!IsPersistenceAvailable)
        {
            return;
        }

        try
        {
            var directory = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(data, _typeInfo);
            var tempPath = _filePath + ".tmp";

            File.WriteAllText(tempPath, json);
            File.Move(tempPath, _filePath, overwrite: true);
        }
        catch (Exception ex)
        {
            IsPersistenceAvailable = false;
            Console.WriteLine($"[JsonFileStore] Failed to save to '{_filePath}'. Continuing in memory-only mode. {ex.Message}");
        }
    }
}
