using System.Text.Json.Serialization.Metadata;

namespace JsonFileStore;

public class JsonFileStore<T> : JsonStore<T>
{
    public JsonFileStore(string filePath, JsonTypeInfo<T> typeInfo)
        : base(new FileBackend(filePath), typeInfo) { }
}
