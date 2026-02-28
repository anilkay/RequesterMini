using System;
using System.Diagnostics.CodeAnalysis;
using System.Reactive;
using ReactiveUI;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using RequesterMini.Utils;

namespace RequesterMini.ViewModels;

public record OldRequestDto(string Method, string Url, string Body, string ResponseStatusCode, string ResponseBody, Dictionary<string, string> Headers)
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
            foreach (var item in items)
            {
                OldRequests.Add(item);
            }
        }

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
                _store.Save(OldRequests.ToList());
            }
        });
    }
}
