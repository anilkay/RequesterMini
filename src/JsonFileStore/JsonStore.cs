using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace JsonFileStore;

public class JsonStore<T>
{
    private readonly IStoreBackend _backend;
    private readonly JsonTypeInfo<T> _typeInfo;

    public bool IsPersistenceAvailable { get; private set; } = true;

    public JsonStore(IStoreBackend backend, JsonTypeInfo<T> typeInfo)
    {
        ArgumentNullException.ThrowIfNull(backend);
        ArgumentNullException.ThrowIfNull(typeInfo);
        _backend = backend;
        _typeInfo = typeInfo;
    }

    public T? Load()
    {
        // Two different failures, handled differently on purpose. Failing to READ says nothing about
        // what the file contains — a sharing violation, a permission error, a disk hiccup — so the
        // file is left exactly where it is. Only content that was read fine and then could not be
        // DESERIALIZED is genuinely bad, and only then is the backend asked to move it aside.
        // Backing up on a read error used to rename a healthy file, and the next start began empty.
        string? raw;
        try
        {
            raw = _backend.Load();
        }
        catch (Exception ex)
        {
            IsPersistenceAvailable = false;  // also stops Save from overwriting what we could not read
            Console.WriteLine($"[JsonStore] Failed to read. Continuing in memory-only mode. {ex.Message}");
            return default;
        }

        if (raw is null) return default;

        try
        {
            return JsonSerializer.Deserialize(raw, _typeInfo);
        }
        catch (Exception ex)
        {
            IsPersistenceAvailable = false;
            Console.WriteLine($"[JsonStore] Failed to load. Continuing in memory-only mode. {ex.Message}");
            _backend.OnLoadFailed();
            return default;
        }
    }

    public void Save(T data)
    {
        ArgumentNullException.ThrowIfNull(data);
        if (!IsPersistenceAvailable) return;
        try
        {
            _backend.Save(JsonSerializer.Serialize(data, _typeInfo));
        }
        catch (Exception ex)
        {
            IsPersistenceAvailable = false;
            Console.WriteLine($"[JsonStore] Failed to save. Continuing in memory-only mode. {ex.Message}");
        }
    }
}
