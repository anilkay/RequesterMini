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
    private static readonly string HistoryPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "RequesterMini",
        "history.json");

    private readonly JsonFileStore.JsonFileStore<List<OldRequestDto>> _store = new(
        HistoryPath, SourceGenerationContext.Default.ListOldRequestDto);

    public ObservableCollection<OldRequestDto> OldRequests { get; } = [];

    internal ReactiveCommand<OldRequestDto, Unit> RemoveCommand { get; }

    internal ReactiveCommand<OldRequestDto, Unit> LoadCommand { get; }

    private static readonly string CollectionsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "RequesterMini",
        "collections.json");

    private readonly JsonFileStore.JsonFileStore<List<RequestCollectionDto>> _collectionStore = new(
        CollectionsPath, SourceGenerationContext.Default.ListRequestCollectionDto);

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
    public OldRequestsWindowViewModel()
    {
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
    }
}
