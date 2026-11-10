using AppLogger;
using FluentResults;
using JsonFileStore;
using RequesterMini.Web.Models;
using RequesterMini.Web.Serialization;
using Templating;

namespace RequesterMini.Web.Services;

/// <summary>
/// Named variable environments and the choice of which is active, backed by one file. Singleton for
/// the same reason as the other stores: one file, so one writer.
/// <para>
/// Environment names are unique case-insensitively — see <see cref="NameRules"/> — and identify an
/// environment across every method here, matching how collections are keyed.
/// </para>
/// <para>
/// A method returns <see cref="Result"/> when the caller has something to tell the user: a name was
/// blank, taken, or pointed at nothing. Removals and <see cref="Activate"/> stay <c>void</c> — they
/// are idempotent, and an environment that is already gone is the outcome the caller wanted.
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

    /// <summary>Creates an empty environment, or reports why the name was rejected.</summary>
    public Result Create(string name)
    {
        string created;

        lock (_gate)
        {
            var validated = NameRules.Validate(name, _environments.Select(e => e.Name), "environment");
            if (validated.IsFailed) return validated.ToResult();

            created = validated.Value;
            _environments.Add(new VariableEnvironment(created, []));

            // The first environment becomes active: creating one and seeing nothing resolve would
            // just be a second step to forget.
            if (_activeName.Length == 0) _activeName = created;

            Save();
        }

        Logger.Info($"Environment created: {created}");
        Changed?.Invoke();
        return Result.Ok();
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

    /// <summary>Adds or updates a variable, or reports why it was rejected.</summary>
    public Result SetVariable(string environmentName, string key, string value)
    {
        var trimmedKey = (key ?? "").Trim();
        if (trimmedKey.Length == 0) return Result.Fail("A variable name cannot be blank.");

        lock (_gate)
        {
            var existing = Find(environmentName);
            if (existing is null) return Result.Fail(UnknownEnvironment(environmentName));

            existing.Variables[trimmedKey] = value ?? "";
            Save();
        }

        Changed?.Invoke();
        return Result.Ok();
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
    /// Renames a variable, keeping its value and leaving the rest of the environment alone, or
    /// reports why the new name was rejected. Renaming to the name it already has succeeds.
    /// </summary>
    public Result RenameVariable(string environmentName, string oldKey, string newKey)
    {
        var trimmed = (newKey ?? "").Trim();
        if (trimmed.Length == 0) return Result.Fail("A variable name cannot be blank.");
        if (string.Equals(oldKey, trimmed, StringComparison.Ordinal)) return Result.Ok();

        lock (_gate)
        {
            var existing = Find(environmentName);
            if (existing is null) return Result.Fail(UnknownEnvironment(environmentName));

            if (!existing.Variables.TryGetValue(oldKey, out var value))
            {
                return Result.Fail($"There is no variable called \"{oldKey}\" in {existing.Name}.");
            }

            if (existing.Variables.ContainsKey(trimmed))
            {
                return Result.Fail($"A variable called \"{trimmed}\" already exists in {existing.Name}.");
            }

            existing.Variables.Remove(oldKey);
            existing.Variables[trimmed] = value;
            Save();
        }

        Changed?.Invoke();
        return Result.Ok();
    }

    private static string UnknownEnvironment(string name) =>
        $"There is no environment called \"{name}\".";

    // Callers already hold the lock.
    private VariableEnvironment? Find(string name) =>
        _environments.FirstOrDefault(e => string.Equals(e.Name, name, StringComparison.OrdinalIgnoreCase));

    private void Save() => _store.Save(new EnvironmentState(_environments, _activeName));
}
