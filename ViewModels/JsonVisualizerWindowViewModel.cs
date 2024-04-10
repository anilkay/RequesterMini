using System;
using System.Reactive;
using ReactiveUI;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Text.Json;

namespace RequesterMini.ViewModels;

public class JsonVisulizerWindowViewModel : ViewModelBase 
{

    public JsonVisulizerWindowViewModel(){
        MessageBus.Current.Listen<string>("newjson").Subscribe(value => 
        {
            JsonSerializerOptions options = new JsonSerializerOptions
            {
                WriteIndented = false,
                AllowTrailingCommas = false,
                PropertyNameCaseInsensitive = true

            };

            string prettySerialized=JsonSerializer.Serialize(value, options);
            //Do Nothing
            PrettyJsonValue=prettySerialized;
        });
    }
    public string JsonValue { get; set; } = "hey";

    private string _prettyJsonValue = "";

    public string PrettyJsonValue
    {
        get => _prettyJsonValue;
        set
        {
            this.RaiseAndSetIfChanged(ref _prettyJsonValue, value);
        }
    }

}