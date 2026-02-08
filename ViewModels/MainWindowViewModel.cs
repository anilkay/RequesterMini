using System;
using System.Reactive;
using ReactiveUI;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net.Http;
using RequesterMini.Constants;
using System.Text.Json;
using RequesterMini.Utils;
using OneOf;

namespace RequesterMini.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private readonly HttpClient? _httpClient;
    public MainWindowViewModel(HttpClient httpClient) : this()
    {
        _httpClient = httpClient;
    }

    internal ReactiveCommand<Unit, Unit> ClickCommand { get; }
    internal ReactiveCommand<Unit, Unit> AddHeaderCommand { get; }

    internal ObservableCollection<string> HttpMethods { get; } = new(HttpConstants.MethodValues);

    internal ObservableCollection<string> BodyTypes { get; } = new(HttpConstants.BodyTypeValues);

    internal ObservableCollection<HeaderItem> Headers { get; } = [];

    internal string SelectedBodyType { get; set; } = HttpConstants.SelectedBodyType;


    internal string Url { get; set; } = HttpConstants.StartUrl;

    internal string Body { get; set; } = "";


    internal string ResponseStatusCode
    {
        get;
        set =>
            this.RaiseAndSetIfChanged(ref
                field, value);
    } = "";

    internal string ResponseBody
    {
        get;
        set
        {
            Console.WriteLine(value);
            this.RaiseAndSetIfChanged(ref field, value);
        }
    } = "";

    internal string SelectedHttpMethod
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = HttpConstants.SelectedMethod;

    public MainWindowViewModel()
    {
        AddHeaderCommand = ReactiveCommand.Create(() =>
        {
            var headerItem = new HeaderItem();
            headerItem.OnRemove = RemoveHeader;
            Headers.Add(headerItem);
        });

        ClickCommand = ReactiveCommand.CreateFromTask(async () =>
        {

            if (_httpClient is null)
            {
                return;
            }

            // Prepare headers dictionary
            var headers = new Dictionary<string, string>();
            foreach (var header in Headers)
            {
                if (header.IsEnabled && !string.IsNullOrWhiteSpace(header.Key))
                {
                    headers[header.Key] = header.Value;
                }
            }

            MakeRequest makeRequest = new MakeRequest(_httpClient, SelectedHttpMethod, Body, SelectedBodyType, Url, headers);
            OneOf<RequestSuccess, RequestFailure> result = await makeRequest.Execute();

            result.Switch(
                 success =>
                  {
                  ResponseBody = success.ResponseBody;
                  ResponseStatusCode = success.StatusCode;
                 },
             failure =>
             {
                ResponseBody = failure.Message;
                ResponseStatusCode = "Error";
             });




            MessageBus.Current.SendMessage(ResponseBody, MessageBusConstants.NewJsonGenerated);

            var oldRequestDto = new OldRequestDto(SelectedHttpMethod, Url, Body, ResponseStatusCode, ResponseBody, headers);

            MessageBus.Current.SendMessage(JsonSerializer.Serialize(oldRequestDto, SourceGenerationContext.Default.OldRequestDto), MessageBusConstants.NewRequest);

        });
    }

    private void RemoveHeader(HeaderItem header)
    {
        Headers.Remove(header);
    }

}
