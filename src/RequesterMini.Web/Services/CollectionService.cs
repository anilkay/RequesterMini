using AppLogger;
using FluentResults;
using JsonFileStore;
using RequesterMini.Web.Models;
using RequesterMini.Web.Serialization;

namespace RequesterMini.Web.Services;

/// <summary>
/// Named collections of saved requests, backed by one file. Registered as a singleton for the same
/// reason as <see cref="RequestHistoryService"/>: one file, so one writer.
/// <para>
/// Collection names are unique case-insensitively — see <see cref="NameRules"/> — and they double
/// as the identity used by every mutation here.
/// </para>
/// <para>
/// A method returns <see cref="Result"/> when the caller has something to tell the user: a name was
/// blank or taken. Removals stay <c>void</c> on purpose — deleting something that is already gone
/// is the outcome the caller wanted, not a failure worth reporting.
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

    /// <summary>Creates an empty collection, or reports why the name was rejected.</summary>
    public Result Create(string name)
    {
        string created;

        lock (_gate)
        {
            var validated = NameRules.Validate(name, _collections.Select(c => c.Name), "collection");
            if (validated.IsFailed) return validated.ToResult();

            created = validated.Value;
            _collections.Add(new StoredCollection(created, []));
            Save();
        }

        Logger.Info($"Collection created: {created}");
        Changed?.Invoke();
        return Result.Ok();
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
    public Result AddRequest(string collectionName, StoredRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var trimmed = (collectionName ?? "").Trim();
        if (trimmed.Length == 0) return Result.Fail("A collection name cannot be blank.");

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
        return Result.Ok();
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
