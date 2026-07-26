using AppLogger;
using JsonFileStore;
using RequesterMini.Web.Models;
using RequesterMini.Web.Serialization;

namespace RequesterMini.Web.Services;

/// <summary>
/// File-backed request history, capped at the most recent <see cref="MaxEntries"/>. Registered as a
/// singleton: the store is a single file, so writes are funnelled through one instance.
/// <para>
/// Persistence is degradation-tolerant by way of <see cref="JsonStore{T}"/> — if the file cannot be
/// read or written the history simply lives in memory for the lifetime of the process.
/// </para>
/// </summary>
public sealed class RequestHistoryService
{
    private const int MaxEntries = 50;

    private readonly JsonFileStore<List<StoredRequest>> _store;
    private readonly List<StoredRequest> _items;
    private readonly Lock _gate = new();

    public RequestHistoryService()
        : this(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "RequesterMini.Web",
            "history.json"))
    {
    }

    public RequestHistoryService(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        _store = new JsonFileStore<List<StoredRequest>>(filePath, WebSerializerContext.Default.ListStoredRequest);
        _items = _store.Load() ?? [];

        if (_items.Count > MaxEntries)
        {
            _items.RemoveRange(0, _items.Count - MaxEntries);
        }
    }

    /// <summary>Raised after any mutation so open circuits can re-render.</summary>
    public event Action? Changed;

    public bool IsPersistenceAvailable => _store.IsPersistenceAvailable;

    /// <summary>Newest first.</summary>
    public IReadOnlyList<StoredRequest> Snapshot()
    {
        lock (_gate)
        {
            var copy = new List<StoredRequest>(_items);
            copy.Reverse();
            return copy;
        }
    }

    public void Add(StoredRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        lock (_gate)
        {
            _items.Add(request);
            if (_items.Count > MaxEntries)
            {
                _items.RemoveAt(0);
            }
            _store.Save(_items);
        }

        Logger.Debug($"History entry added: {request.Method} {request.Url}");
        Changed?.Invoke();
    }

    public void Remove(StoredRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        lock (_gate)
        {
            if (!_items.Remove(request)) return;
            _store.Save(_items);
        }

        Changed?.Invoke();
    }

    public void Clear()
    {
        lock (_gate)
        {
            if (_items.Count == 0) return;
            _items.Clear();
            _store.Save(_items);
        }

        Changed?.Invoke();
    }
}
