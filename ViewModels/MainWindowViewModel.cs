using System;
using System.Reactive;
using ReactiveUI;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net.Http;
using RequesterMini.Constants;
using System.Text.Json;

namespace RequesterMini.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private readonly HttpClient _httpClient;
   public  MainWindowViewModel(HttpClient httpClient):this()
    {
        _httpClient = httpClient;
    }

    internal ReactiveCommand<Unit, Unit> ClickCommand { get; }
    internal ObservableCollection<string> HttpMethods { get; } = new ObservableCollection<string>(HttpConstants.MethodValues);



    internal ObservableCollection<string> BodyTypes { get; } = new ObservableCollection<string>(HttpConstants.BodyTypeValues);

    internal string SelectedBodyType { get; set; } = HttpConstants.SelectedBodyType;




    internal string _selectedHttpMethod = HttpConstants.SelectedMethod;

    internal string Url { get; set; } = HttpConstants.StartUrl;

    internal string Body { get; set; } = "";

    private string _responseStatusCode="";



    internal string ResponseStatusCode{
        get=>_responseStatusCode;
        set {
            this.RaiseAndSetIfChanged(ref 
            _responseStatusCode,value);
        }
    }

    private string _responseBody="";

    internal string ResponseBody{
        get =>_responseBody;
        set {
            Console.WriteLine(value);
            this.RaiseAndSetIfChanged(ref _responseBody,value);
        }
    }
    
    internal string SelectedHttpMethod {
        get => _selectedHttpMethod;
        set  {
            Console.WriteLine("SelectedHttpMethod set to: " + value);
            this.RaiseAndSetIfChanged(ref _selectedHttpMethod, value);
            }
    }





    public MainWindowViewModel()
    {
        ClickCommand = ReactiveCommand.CreateFromTask(async () => {
            MakeRequest makeRequest=new MakeRequest(_httpClient,SelectedHttpMethod,Body,SelectedBodyType,Url);
             (var statusCode,var response,_)=await makeRequest.Execute();

             ResponseBody=response;
             ResponseStatusCode=statusCode;

             MessageBus.Current.SendMessage(ResponseBody,MessageBusConstants.NewJsonGenerated);

             var OldRequestDto=new OldRequestDto(SelectedHttpMethod,Url,Body,ResponseStatusCode,ResponseBody);

             MessageBus.Current.SendMessage(JsonSerializer.Serialize(OldRequestDto),MessageBusConstants.NewRequest);

             //Console.WriteLine(ResponseBody);

             });
    }

}
