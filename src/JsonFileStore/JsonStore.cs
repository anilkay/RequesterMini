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
        try
        {
            var raw = _backend.Load();
            if (raw is null) return default;
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
