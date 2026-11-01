using AppLogger;
using JsonFileStore;
using RequesterMini.Web.Models;
using RequesterMini.Web.Serialization;

namespace RequesterMini.Web.Services;

/// <summary>
/// Named collections of saved requests, backed by one file. Registered as a singleton for the same
/// reason as <see cref="RequestHistoryService"/>: one file, so one writer.
/// <para>
/// Collection names are unique case-insensitively — two collections called "Api" and "api" would be
/// indistinguishable in the UI — and they double as the identity used by every mutation here.
/// </para>
/// </summary>
public sealed class CollectionService
{
    private readonly JsonFileStore<List<StoredCollection>> _store;
    private readonly List<StoredCollection> _collections;
    private readonly Lock _gate = new();

    public CollectionService()
        : this(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "RequesterMini.Web",
            "collections.json"))
    {
    }

    public CollectionService(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        _store = new JsonFileStore<List<StoredCollection>>(filePath, WebSerializerContext.Default.ListStoredCollection);
        _collections = _store.Load() ?? [];
    }

    public event Action? Changed;

    public bool IsPersistenceAvailable => _store.IsPersistenceAvailable;

    public IReadOnlyList<StoredCollection> Snapshot()
    {
        lock (_gate)
        {
            return _collections.Select(c => c with { Requests = [.. c.Requests] }).ToList();
        }
    }

    /// <summary>Creates an empty collection. Returns false when the name is blank or already taken.</summary>
    public bool Create(string name)
    {
        var trimmed = (name ?? "").Trim();
        if (trimmed.Length == 0) return false;

        lock (_gate)
        {
            if (Find(trimmed) is not null) return false;

            _collections.Add(new StoredCollection(trimmed, []));
            Save();
        }

        Logger.Info($"Collection created: {trimmed}");
        Changed?.Invoke();
        return true;
    }

    public void Delete(string name)
    {
        lock (_gate)
        {
            var existing = Find(name);
            if (existing is null) return;

            _collections.Remove(existing);
            Save();
        }

        Changed?.Invoke();
    }

    /// <summary>
    /// Adds a request to a collection, creating the collection when it does not exist yet. The
    /// response is stripped: a collection holds the request, not the reply it happened to get.
    /// </summary>
    public void AddRequest(string collectionName, StoredRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var trimmed = (collectionName ?? "").Trim();
        if (trimmed.Length == 0) return;

        var stripped = request with { ResponseStatusCode = "", ResponseBody = "" };

        lock (_gate)
        {
            var existing = Find(trimmed);
            if (existing is null)
            {
                existing = new StoredCollection(trimmed, []);
                _collections.Add(existing);
            }

            existing.Requests.Add(stripped);
            Save();
        }

        Logger.Info($"Request saved to collection {trimmed}: {stripped.Method} {stripped.Url}");
        Changed?.Invoke();
    }

    /// <summary>Removes the request at <paramref name="index"/>, which is its position in the collection.</summary>
    public void RemoveRequest(string collectionName, int index)
    {
        lock (_gate)
        {
            var existing = Find(collectionName);
            if (existing is null || index < 0 || index >= existing.Requests.Count) return;

            existing.Requests.RemoveAt(index);
            Save();
        }

        Changed?.Invoke();
    }

    // Callers already hold the lock.
    private StoredCollection? Find(string name) =>
        _collections.FirstOrDefault(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase));

    private void Save() => _store.Save(_collections);
}
