using System;
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

    public ObservableCollection<OldRequestDto> OldRequests { get; } = [];

    public OldRequestsWindowViewModel()
    {
        LoadHistory();

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
                SaveHistory();
            }
        });
    }

    private void LoadHistory()
    {
        if (!File.Exists(HistoryPath))
        {
            return;
        }

        var json = File.ReadAllText(HistoryPath);
        var items = JsonSerializer.Deserialize(json, SourceGenerationContext.Default.ListOldRequestDto);

        if (items is null)
        {
            return;
        }

        foreach (var item in items)
        {
            OldRequests.Add(item);
        }
    }

    private void SaveHistory()
    {
        var directory = Path.GetDirectoryName(HistoryPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(OldRequests.ToList(), SourceGenerationContext.Default.ListOldRequestDto);
        File.WriteAllText(HistoryPath, json);
    }
}
