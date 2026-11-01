using AppLogger;
using JsonFileStore;
using RequesterMini.Web.Models;
using RequesterMini.Web.Serialization;
using Templating;

namespace RequesterMini.Web.Services;

/// <summary>
/// Named variable environments and the choice of which is active, backed by one file. Singleton for
/// the same reason as the other stores: one file, so one writer.
/// <para>
/// Environment names are unique case-insensitively and identify an environment across every method
/// here, matching how collections are keyed.
/// </para>
/// </summary>
public sealed class EnvironmentService
{
    private readonly JsonFileStore<EnvironmentState> _store;
    private readonly List<VariableEnvironment> _environments;
    private readonly Lock _gate = new();

    private string _activeName;

    public EnvironmentService()
        : this(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "RequesterMini.Web",
            "environments.json"))
    {
    }

    public EnvironmentService(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        _store = new JsonFileStore<EnvironmentState>(filePath, WebSerializerContext.Default.EnvironmentState);

        var state = _store.Load() ?? new EnvironmentState([], "");
        _environments = state.Environments;
        _activeName = Find(state.ActiveName) is null ? "" : state.ActiveName;
    }

    public event Action? Changed;

    public bool IsPersistenceAvailable => _store.IsPersistenceAvailable;

    /// <summary>The active environment's name, or empty when none is selected.</summary>
    public string ActiveName
    {
        get { lock (_gate) { return _activeName; } }
    }

    public IReadOnlyList<VariableEnvironment> Snapshot()
    {
        lock (_gate)
        {
            return _environments
                .Select(e => e with { Variables = new Dictionary<string, string>(e.Variables) })
                .ToList();
        }
    }

    /// <summary>
    /// The variables a request should be resolved against: the active environment's, or empty when
    /// none is active.
    /// </summary>
    public IReadOnlyDictionary<string, string> ActiveVariables()
    {
        lock (_gate)
        {
            var active = Find(_activeName);
            return active is null
                ? new Dictionary<string, string>()
                : new Dictionary<string, string>(active.Variables);
        }
    }

    /// <summary>Selects the active environment. A blank or unknown name clears the selection.</summary>
    public void Activate(string? name)
    {
        lock (_gate)
        {
            var resolved = Find(name ?? "") is null ? "" : (name ?? "").Trim();
            if (_activeName == resolved) return;

            _activeName = resolved;
            Save();
        }

        Changed?.Invoke();
    }

    /// <summary>Creates an empty environment. Returns false when the name is blank or taken.</summary>
    public bool Create(string name)
    {
        var trimmed = (name ?? "").Trim();
        if (trimmed.Length == 0) return false;

        lock (_gate)
        {
            if (Find(trimmed) is not null) return false;

            _environments.Add(new VariableEnvironment(trimmed, []));

            // The first environment becomes active: creating one and seeing nothing resolve would
            // just be a second step to forget.
            if (_activeName.Length == 0) _activeName = trimmed;

            Save();
        }

        Logger.Info($"Environment created: {trimmed}");
        Changed?.Invoke();
        return true;
    }

    public void Delete(string name)
    {
        lock (_gate)
        {
            var existing = Find(name);
            if (existing is null) return;

            _environments.Remove(existing);
            if (string.Equals(_activeName, existing.Name, StringComparison.OrdinalIgnoreCase))
            {
                _activeName = _environments.Count > 0 ? _environments[0].Name : "";
            }

            Save();
        }

        Changed?.Invoke();
    }

    /// <summary>Adds or updates a variable. A blank key is ignored.</summary>
    public void SetVariable(string environmentName, string key, string value)
    {
        var trimmedKey = (key ?? "").Trim();
        if (trimmedKey.Length == 0) return;

        lock (_gate)
        {
            var existing = Find(environmentName);
            if (existing is null) return;

            existing.Variables[trimmedKey] = value ?? "";
            Save();
        }

        Changed?.Invoke();
    }

    public void RemoveVariable(string environmentName, string key)
    {
        lock (_gate)
        {
            var existing = Find(environmentName);
            if (existing is null || !existing.Variables.Remove(key)) return;

            Save();
        }

        Changed?.Invoke();
    }

    /// <summary>
    /// Renames a variable, keeping its value and leaving the rest of the environment alone.
    /// Returns false when the new name is blank or already used.
    /// </summary>
    public bool RenameVariable(string environmentName, string oldKey, string newKey)
    {
        var trimmed = (newKey ?? "").Trim();
        if (trimmed.Length == 0) return false;
        if (string.Equals(oldKey, trimmed, StringComparison.Ordinal)) return true;

        lock (_gate)
        {
            var existing = Find(environmentName);
            if (existing is null || !existing.Variables.TryGetValue(oldKey, out var value)) return false;
            if (existing.Variables.ContainsKey(trimmed)) return false;

            existing.Variables.Remove(oldKey);
            existing.Variables[trimmed] = value;
            Save();
        }

        Changed?.Invoke();
        return true;
    }

    // Callers already hold the lock.
    private VariableEnvironment? Find(string name) =>
        _environments.FirstOrDefault(e => string.Equals(e.Name, name, StringComparison.OrdinalIgnoreCase));

    private void Save() => _store.Save(new EnvironmentState(_environments, _activeName));
}
