using System;
using ReactiveUI;
using System.Text.Json;
using RequesterMini.Constants;

namespace RequesterMini.ViewModels;

public class JsonVisualizerWindowViewModel : ViewModelBase 
{

    public JsonVisualizerWindowViewModel(){
        MessageBus.Current.Listen<string>(MessageBusConstants.NewJsonGenerated).Subscribe(value => 
        {


            if(value==null)
            {
                return;
            }

           
            try 
            { 
                PrettyJsonValue=value;
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