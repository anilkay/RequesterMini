using System;
using System.Reactive;
using ReactiveUI;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Threading;
using RequesterMini.Constants;
using System.Text.Json;
using RequesterMini.Utils;
using RequesterMini.Models;
using CurlExporter;
using OneOf;
using System.Diagnostics.CodeAnalysis;
using System.Reactive.Linq;

namespace RequesterMini.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private readonly HttpClient? _httpClient;
    private CancellationTokenSource? _cts;

    [RequiresUnreferencedCode("This constructor uses reflection.")]
    public MainWindowViewModel(HttpClient httpClient) : this()
    {
        _httpClient = httpClient;
    }

    internal ReactiveCommand<Unit, Unit> ClickCommand { get; }
    internal ReactiveCommand<Unit, Unit> CancelCommand { get; }
    internal ReactiveCommand<Unit, Unit> AddHeaderCommand { get; }
    internal ReactiveCommand<Unit, Unit> ExportCurlCommand { get; }

    internal Interaction<string, Unit> CopyToClipboard { get; } = new();

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

    internal string FinishedTimeUtc
    {
        get;
        set =>
            this.RaiseAndSetIfChanged(ref
                field, value);
    } = "";

    internal string RequestTime
    {
        get;
        set =>
            this.RaiseAndSetIfChanged(ref
                field, value);
    } = "";

    internal string SelectedHttpMethod
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = HttpConstants.SelectedMethod;

    [RequiresUnreferencedCode("ReactiveCommand methods use reflection.")]
    public MainWindowViewModel()
    {
        CancelCommand = ReactiveCommand.Create(() =>
        {
            _cts?.Cancel();
        });

        AddHeaderCommand = ReactiveCommand.Create(() =>
        {
            var headerItem = new HeaderItem();
            headerItem.OnRemove = RemoveHeader;
            Headers.Add(headerItem);
        });

        ExportCurlCommand = ReactiveCommand.Create(() =>
        {
            var builder = new CurlCommandBuilder()
                .SetMethod(SelectedHttpMethod)
                .SetUrl(Url);

            foreach (var header in Headers)
            {
                if (header.IsEnabled && !string.IsNullOrWhiteSpace(header.Key))
                {
                    builder.AddHeader(header.Key, header.Value);
                }
            }

            if (!string.IsNullOrEmpty(Body)
                && Enum.TryParse<BodyType>(SelectedBodyType, ignoreCase: true, out var bodyType))
            {
                builder.SetBody(Body, bodyType);
            }

            var curl = builder.Build();
            CopyToClipboard.Handle(curl).Subscribe();
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

            _cts?.Cancel();
            _cts?.Dispose();
            _cts = new CancellationTokenSource();

            MakeRequest makeRequest = new MakeRequest(_httpClient, SelectedHttpMethod, Body, SelectedBodyType, Url, headers);
            OneOf<RequestSuccess, RequestFailure> result = await makeRequest.Execute(_cts.Token);

            result.Switch(
                 success =>
                  {
                  ResponseBody = success.ResponseBody;
                  ResponseStatusCode = success.StatusCode;
                  FinishedTimeUtc = success.FinishedTimeUtc?.ToString("yyyy-MM-dd HH:mm:ss") + " UTC";
                  RequestTime = success.RequestTime is { } elapsed
                      ? elapsed.TotalSeconds < 1
                          ? $"{elapsed.TotalMilliseconds:F0} ms"
                          : $"{elapsed.TotalSeconds:F1} sec"
                      : "";
                 },
             failure =>
             {
                ResponseBody = failure.Message;
                ResponseStatusCode = "Error";
                FinishedTimeUtc = "";
                RequestTime = "";
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
