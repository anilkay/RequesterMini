namespace JsonFileStore;

public interface IStoreBackend
{
    string? Load();
    void Save(string data);
    void OnLoadFailed(); // called when Load() result can't be deserialized; implementors should do cleanup (e.g., backup corrupt file)
}
