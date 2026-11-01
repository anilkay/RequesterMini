using System;
using System.Diagnostics.CodeAnalysis;
using System.Reactive;
using ReactiveUI;
using ReactiveUI.Reactive;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using RequesterMini.Utils;
using Templating;

namespace RequesterMini.ViewModels;

public record OldRequestDto(string Method, string Url, string Body, string ResponseStatusCode, string ResponseBody, Dictionary<string, string> Headers, string ProxyUrl = "")
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string MethodColor
    {
        get
        {
            return Method.ToUpper() switch
            {
                "GET" => "#007BFF",      // Blue
                "POST" => "#28A745",     // Green
                "PUT" => "#FD7E14",      // Orange
                "DELETE" => "#DC3545",   // Red
                "PATCH" => "#17A2B8",    // Teal
                "QUERY" => "#6F42C1",    // Purple
                "HEAD" => "#6C757D",     // Gray
                "OPTIONS" => "#6C757D",  // Gray
                _ => "#6C757D"           // Default Gray
            };
        }
    }
}

public class OldRequestsWindowViewModel : ViewModelBase
{
    /// <summary>
    /// Where history, collections and environments are kept. Overridable through the constructor so
    /// tests can point at a temp directory instead of the user's real files.
    /// </summary>
    internal static string DefaultDataDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "RequesterMini");

    private readonly JsonFileStore.JsonFileStore<List<OldRequestDto>> _store;

    public ObservableCollection<OldRequestDto> OldRequests { get; } = [];

    internal ReactiveCommand<OldRequestDto, Unit> RemoveCommand { get; }

    internal ReactiveCommand<OldRequestDto, Unit> LoadCommand { get; }

    private readonly JsonFileStore.JsonFileStore<List<RequestCollectionDto>> _collectionStore;

    public ObservableCollection<CollectionItem> Collections { get; } = [];

    internal string NewCollectionName
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = "";

    internal CollectionItem? SelectedCollection
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    internal ReactiveCommand<Unit, Unit> AddCollectionCommand { get; }

    internal ReactiveCommand<CollectionItem, Unit> RemoveCollectionCommand { get; }

    internal ReactiveCommand<OldRequestDto, Unit> RemoveCollectionRequestCommand { get; }

    internal ReactiveCommand<OldRequestDto, Unit> SaveHistoryToCollectionCommand { get; }

    private readonly JsonFileStore.JsonFileStore<EnvironmentState> _environmentStore;

    public ObservableCollection<EnvironmentItem> Environments { get; } = [];

    internal string NewEnvironmentName
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = "";

    internal EnvironmentItem? ActiveEnvironment
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    internal ReactiveCommand<Unit, Unit> AddEnvironmentCommand { get; }

    internal ReactiveCommand<EnvironmentItem, Unit> RemoveEnvironmentCommand { get; }

    internal ReactiveCommand<EnvironmentItem, Unit> ActivateEnvironmentCommand { get; }

    internal ReactiveCommand<EnvironmentItem, Unit> AddVariableCommand { get; }

    private void SaveEnvironments()
    {
        _environmentStore.Save(new EnvironmentState(
            Environments.Select(e => e.ToDto()).ToList(),
            ActiveEnvironment?.Name ?? ""));
    }

    // The main window resolves {{placeholders}} at send time, so it needs the active set. Sent as
    // a VariableEnvironment rather than a bare map so the name travels with it.
    private void BroadcastActiveEnvironment()
    {
        var dto = ActiveEnvironment?.ToDto() ?? new VariableEnvironment("", []);

        MessageBus.Current.SendMessage(
            JsonSerializer.Serialize(dto, SourceGenerationContext.Default.VariableEnvironment),
            Constants.MessageBusConstants.ActiveEnvironmentChanged);
    }

    private void OnEnvironmentEdited()
    {
        SaveEnvironments();
        BroadcastActiveEnvironment();
    }

    private void RemoveVariable(EnvironmentItem owner, VariableItem variable)
    {
        owner.Variables.Remove(variable);
        OnEnvironmentEdited();
    }

    private void AttachVariable(EnvironmentItem owner, VariableItem variable)
    {
        variable.OnRemove = item => RemoveVariable(owner, item);

        // Editing a row does not raise CollectionChanged, so each row is watched individually.
        variable.PropertyChanged += (_, _) => OnEnvironmentEdited();
    }

    private void MarkActiveEnvironment()
    {
        foreach (var environment in Environments)
        {
            environment.IsActive = environment == ActiveEnvironment;
        }
    }

    private void SaveCollections() => _collectionStore.Save(Collections.Select(c => c.ToDto()).ToList());

    private void AddToCollection(OldRequestDto dto)
    {
        var target = SelectedCollection ?? Collections.FirstOrDefault();
        if (target is null)
        {
            target = new CollectionItem("Default");
            Collections.Add(target);
            SelectedCollection = target;
        }

        target.Requests.Add(dto);
        SaveCollections();
    }

    [RequiresUnreferencedCode("Uses ReactiveCommand")]
    public OldRequestsWindowViewModel() : this(DefaultDataDirectory)
    {
    }

    [RequiresUnreferencedCode("Uses ReactiveCommand")]
    internal OldRequestsWindowViewModel(string dataDirectory)
    {
        _store = new JsonFileStore.JsonFileStore<List<OldRequestDto>>(
            Path.Combine(dataDirectory, "history.json"), SourceGenerationContext.Default.ListOldRequestDto);
        _collectionStore = new JsonFileStore.JsonFileStore<List<RequestCollectionDto>>(
            Path.Combine(dataDirectory, "collections.json"), SourceGenerationContext.Default.ListRequestCollectionDto);
        _environmentStore = new JsonFileStore.JsonFileStore<EnvironmentState>(
            Path.Combine(dataDirectory, "environments.json"), SourceGenerationContext.Default.EnvironmentState);

        RemoveCommand = ReactiveCommand.Create<OldRequestDto>(item =>
        {
            OldRequests.Remove(item);
            _store.Save(OldRequests.ToList());
        });

        LoadCommand = ReactiveCommand.Create<OldRequestDto>(item =>
        {
            var json = JsonSerializer.Serialize(item, SourceGenerationContext.Default.OldRequestDto);
            MessageBus.Current.SendMessage(json, Constants.MessageBusConstants.LoadRequest);
        });

        var items = _store.Load();
        if (items is not null)
        {
            foreach (var item in items.TakeLast(50))
            {
                OldRequests.Add(item);
            }
        }

        var savedCollections = _collectionStore.Load();
        if (savedCollections is not null)
        {
            foreach (var c in savedCollections)
            {
                Collections.Add(new CollectionItem(c.Name, c.Requests));
            }
        }

        var savedEnvironments = _environmentStore.Load();
        if (savedEnvironments is not null)
        {
            foreach (var e in savedEnvironments.Environments)
            {
                var item = new EnvironmentItem(e.Name, e.Variables);
                foreach (var variable in item.Variables)
                {
                    AttachVariable(item, variable);
                }

                Environments.Add(item);
            }

            ActiveEnvironment = Environments.FirstOrDefault(
                e => string.Equals(e.Name, savedEnvironments.ActiveName, StringComparison.OrdinalIgnoreCase));
        }

        MarkActiveEnvironment();

        AddEnvironmentCommand = ReactiveCommand.Create(() =>
        {
            var name = NewEnvironmentName.Trim();
            if (name.Length == 0 || Environments.Any(e => string.Equals(e.Name, name, StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            var environment = new EnvironmentItem(name);
            Environments.Add(environment);

            // The first one becomes active: creating an environment and seeing nothing resolve
            // would just be a second step to forget.
            ActiveEnvironment ??= environment;

            NewEnvironmentName = "";
            MarkActiveEnvironment();
            OnEnvironmentEdited();
        });

        RemoveEnvironmentCommand = ReactiveCommand.Create<EnvironmentItem>(environment =>
        {
            Environments.Remove(environment);
            if (ActiveEnvironment == environment)
            {
                ActiveEnvironment = Environments.FirstOrDefault();
            }

            MarkActiveEnvironment();
            OnEnvironmentEdited();
        });

        ActivateEnvironmentCommand = ReactiveCommand.Create<EnvironmentItem>(environment =>
        {
            ActiveEnvironment = environment;
            MarkActiveEnvironment();
            OnEnvironmentEdited();
        });

        AddVariableCommand = ReactiveCommand.Create<EnvironmentItem>(environment =>
        {
            var variable = new VariableItem();
            AttachVariable(environment, variable);
            environment.Variables.Add(variable);
        });

        AddCollectionCommand = ReactiveCommand.Create(() =>
        {
            var name = NewCollectionName.Trim();
            if (name.Length == 0 || Collections.Any(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            var collection = new CollectionItem(name);
            Collections.Add(collection);
            SelectedCollection = collection;
            NewCollectionName = "";
            SaveCollections();
        });

        RemoveCollectionCommand = ReactiveCommand.Create<CollectionItem>(collection =>
        {
            Collections.Remove(collection);
            if (SelectedCollection == collection)
            {
                SelectedCollection = null;
            }
            SaveCollections();
        });

        RemoveCollectionRequestCommand = ReactiveCommand.Create<OldRequestDto>(item =>
        {
            var owner = Collections.FirstOrDefault(c => c.Requests.Contains(item));
            if (owner is null)
            {
                return;
            }

            owner.Requests.Remove(item);
            SaveCollections();
        });

        SaveHistoryToCollectionCommand = ReactiveCommand.Create<OldRequestDto>(item =>
            AddToCollection(item with { ResponseStatusCode = "", ResponseBody = "" }));

        MessageBus.Current.Listen<string>(Constants.MessageBusConstants.SaveToCollection).Subscribe(value =>
        {
            if (value == null)
            {
                return;
            }

            var dto = JsonSerializer.Deserialize<OldRequestDto>(value, SourceGenerationContext.Default.OldRequestDto);
            if (dto is not null)
            {
                AddToCollection(dto);
            }
        });

        MessageBus.Current.Listen<string>(Constants.MessageBusConstants.NewRequest).Subscribe(value =>
        {
            if (value == null)
            {
                return;
            }

            var oldRequestObject = JsonSerializer.Deserialize<OldRequestDto>(value, SourceGenerationContext.Default.OldRequestDto);
            if (oldRequestObject is not null)
            {
                OldRequests.Add(oldRequestObject);
                if (OldRequests.Count > 50)
                    OldRequests.RemoveAt(0);
                _store.Save(OldRequests.ToList());
            }
        });

        // This control is built as part of the main window, so the main ViewModel is already
        // listening — it needs the restored selection before the first request goes out.
        BroadcastActiveEnvironment();
    }
}
