using System;
using System.Reactive;
using ReactiveUI;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace RequesterMini.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    public ReactiveCommand<Unit, Unit> ClickCommand { get; }
    public   ObservableCollection<string> HttpMethods { get; } = new ObservableCollection<string>
     { "GET", "POST", "PUT", "DELETE", "PATCH"};
    

    public ObservableCollection<string> BodyTypes { get; } = new ObservableCollection<string> { "Json", "Xml", "Form", "TEXT" };
   
     public string SelectedBodyType { get; set; } = "Json";


    public int SelectedIndex { get; set; } = 0;


    public string _selectedHttpMethod="GET";

    public string Url { get; set; } = "https://jsonplaceholder.typicode.com/posts/1";

    public string Body { get; set; } = "";

    private string _responseStatusCode="";



    public string ResponseStatusCode{
        get=>_responseStatusCode;
        set {
            this.RaiseAndSetIfChanged(ref 
            _responseStatusCode,value);
        }
    }

    private string _responseBody="";

    public string ResponseBody{
        get =>_responseBody;
        set {
            Console.WriteLine(value);
            this.RaiseAndSetIfChanged(ref _responseBody,value);
        }
    }
    
    public string SelectedHttpMethod {
        get => _selectedHttpMethod;
        set  {
            Console.WriteLine("SelectedHttpMethod set to: " + value);
            this.RaiseAndSetIfChanged(ref _selectedHttpMethod, value);
            }
    }





    public MainWindowViewModel()
    {
        ClickCommand = ReactiveCommand.CreateFromTask(async () => { 
            MakeRequest makeRequest=new MakeRequest(SelectedHttpMethod,Body,SelectedBodyType,Url);
             (var statusCode,var response,_)=await makeRequest.Execute();

             ResponseBody=response;
             ResponseStatusCode=statusCode;

             Console.WriteLine(ResponseBody);

             });
    }

}
