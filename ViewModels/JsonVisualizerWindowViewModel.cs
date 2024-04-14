using System;
using System.Reactive;
using ReactiveUI;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Text.Json;
using RequesterMini.Constants;

namespace RequesterMini.ViewModels;

public class JsonVisulizerWindowViewModel : ViewModelBase 
{

    public JsonVisulizerWindowViewModel(){
        MessageBus.Current.Listen<string>(MessageBusConstants.NewJsonGenerated).Subscribe(value => 
        {


            if(value==null)
            {
                return;
            }

            JsonSerializerOptions options = new JsonSerializerOptions
            {
                WriteIndented = false,
                AllowTrailingCommas = false,
                PropertyNameCaseInsensitive = true

            };
            try {
            var deserialized=JsonSerializer.Deserialize<JsonElement>(value,options );
            string prettySerialized=JsonSerializer.Serialize(deserialized, options);

            PrettyJsonValue=prettySerialized;
        }
        catch(JsonException e)
        {
            PrettyJsonValue=e.Message;
        }
        });
    }

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