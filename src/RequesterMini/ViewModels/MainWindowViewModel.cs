using System;
using System.IO;
using System.Reactive;
using ReactiveUI;
using ReactiveUI.Reactive;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Net.Http;
using System.Threading;
using RequesterMini.Constants;
using System.Text.Json;
using RequesterMini.Utils;
using RequesterMini.Models;
using AppLogger;
using CurlExporter;
using BrunoImporter;
using HttpAuth;
using OneOf;
using System.Diagnostics.CodeAnalysis;
using System.Reactive.Linq;
using UrlQuery;

namespace RequesterMini.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private readonly HttpClient? _httpClient;
    private CancellationTokenSource? _cts;

    // Re-entrancy guard: Url and QueryParams mirror each other, so whichever side is being written
    // suppresses the sync back in the other direction.
    private bool _syncingQueryParams;

    [RequiresUnreferencedCode("This constructor uses reflection.")]
    public MainWindowViewModel(HttpClient httpClient) : this()
    {
        _httpClient = httpClient;
    }

    internal ReactiveCommand<Unit, Unit> ClickCommand { get; }
    internal ReactiveCommand<Unit, Unit> CancelCommand { get; }
    internal ReactiveCommand<Unit, Unit> AddHeaderCommand { get; }
    internal ReactiveCommand<Unit, Unit> AddQueryParamCommand { get; }
    internal ReactiveCommand<Unit, Unit> ExportCurlCommand { get; }
    internal ReactiveCommand<Unit, Unit> ImportBruCommand { get; }

    internal Interaction<string, Unit> CopyToClipboard { get; } = new();
    internal Interaction<Unit, string?> OpenBruFile { get; } = new();

    internal ObservableCollection<string> HttpMethods { get; } = new(HttpConstants.MethodValues);

    internal ObservableCollection<string> BodyTypes { get; } = new(HttpConstants.BodyTypeValues);

    internal ObservableCollection<string> AuthTypes { get; } = new(HttpConstants.AuthTypeValues);

    internal ObservableCollection<HeaderItem> Headers { get; } = [];

    internal ObservableCollection<QueryParamItem> QueryParams { get; } = [];

    internal string SelectedBodyType
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = HttpConstants.SelectedBodyType;


    internal string SelectedAuthType
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = HttpConstants.SelectedAuthType;

    // Drives visibility of the username/password fields; kept as a plain property (rather than an
    // ObservableAsPropertyHelper) to stay consistent with the other properties and trimming-safe.
    internal bool IsBasicAuthSelected
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    internal string AuthUsername
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = "";

    internal string AuthPassword
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = "";

    internal bool IsPasswordRevealed
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    internal string Url
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = HttpConstants.StartUrl;

    internal string Body
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = "";

    // JSON validation state for the Body editor (only meaningful when SelectedBodyType is Json).
    internal bool IsBodyJsonInvalid
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    internal string BodyJsonError
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = "";

    // Char range of the offending token; consumed by the View to select/highlight it. -1 = none.
    internal int BodyErrorStart
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = -1;

    internal int BodyErrorEnd
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = -1;


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
        set => this.RaiseAndSetIfChanged(ref field, value);
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

    internal string CopyFeedback
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = "";

    internal string SelectedHttpMethod
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = HttpConstants.SelectedMethod;

    // GET requests don't send a body, so the Body tab is disabled while GET is selected.
    internal bool IsBodyEnabled
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = !string.Equals(HttpConstants.SelectedMethod, "GET", StringComparison.OrdinalIgnoreCase);

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

        AddQueryParamCommand = ReactiveCommand.Create(() =>
        {
            QueryParams.Add(new QueryParamItem { OnRemove = RemoveQueryParam });
        });

        // Keep Url and QueryParams in step in both directions.
        QueryParams.CollectionChanged += OnQueryParamsChanged;
        this.WhenAnyValue(x => x.Url).Subscribe(_ => SyncParamsFromUrl());

        ExportCurlCommand = ReactiveCommand.Create(() =>
        {
            var builder = new CurlCommandBuilder()
                .SetMethod(SelectedHttpMethod)
                .SetUrl(Url);

            if (HasBasicCredentials)
            {
                builder.SetBasicAuth(AuthUsername, AuthPassword);
            }

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
            CopyFeedback = "✓ Copied!";
            Observable.Timer(TimeSpan.FromSeconds(2))
                .ObserveOn(RxSchedulers.MainThreadScheduler)
                .Subscribe(_ => CopyFeedback = "");
        });

        ImportBruCommand = ReactiveCommand.CreateFromObservable(() =>
            OpenBruFile.Handle(Unit.Default)
                .ObserveOn(RxSchedulers.MainThreadScheduler)
                .Do(filePath =>
                {
                    if (string.IsNullOrEmpty(filePath)) return;
                    var req = BrunoFileImporter.ParseFile(filePath);
                    Url = req.Url;
                    SelectedHttpMethod = req.Method;
                    Body = req.Body;
                    SelectedBodyType = req.BodyType;
                    Headers.Clear();
                    var headerDict = new Dictionary<string, string>();
                    foreach (var kvp in req.Headers)
                    {
                        var headerItem = new HeaderItem { Key = kvp.Key, Value = kvp.Value };
                        headerItem.OnRemove = RemoveHeader;
                        Headers.Add(headerItem);
                        headerDict[kvp.Key] = kvp.Value;
                    }

                    var dto = new OldRequestDto(req.Method, req.Url, req.Body, "", "", headerDict);
                    MessageBus.Current.SendMessage(
                        JsonSerializer.Serialize(dto, SourceGenerationContext.Default.OldRequestDto),
                        MessageBusConstants.NewRequest);
                })
                .Select(_ => Unit.Default));

        MessageBus.Current.Listen<string>(MessageBusConstants.LoadRequest).Subscribe(value =>
        {
            if (value == null) return;

            var dto = JsonSerializer.Deserialize<OldRequestDto>(value, SourceGenerationContext.Default.OldRequestDto);
            if (dto is null) return;

            Url = dto.Url;
            SelectedHttpMethod = dto.Method;
            Body = dto.Body;
            ResponseStatusCode = dto.ResponseStatusCode;
            ResponseBody = dto.ResponseBody;

            Headers.Clear();
            foreach (var kvp in dto.Headers)
            {
                var headerItem = new HeaderItem { Key = kvp.Key, Value = kvp.Value };
                headerItem.OnRemove = RemoveHeader;
                Headers.Add(headerItem);
            }
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

            Logger.Info($"Sending {SelectedHttpMethod} request to {Url}");
            MakeRequest makeRequest = new MakeRequest(_httpClient, SelectedHttpMethod, Body, SelectedBodyType, Url, BuildRequestHeaders(headers));
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

                  Logger.Info($"Request succeeded. Method={SelectedHttpMethod}, Url={Url}, Status={success.StatusCode}, Duration={RequestTime}");
                 },
             failure =>
             {
                ResponseBody = failure.Message;
                ResponseStatusCode = "Error";
                FinishedTimeUtc = "";
                RequestTime = "";

                Logger.Error($"Request failed. Method={SelectedHttpMethod}, Url={Url}, Message={failure.Message}", failure.Exception);
             });




            MessageBus.Current.SendMessage(ResponseBody, MessageBusConstants.NewJsonGenerated);

            var oldRequestDto = new OldRequestDto(SelectedHttpMethod, Url, Body, ResponseStatusCode, ResponseBody, headers);

            MessageBus.Current.SendMessage(JsonSerializer.Serialize(oldRequestDto, SourceGenerationContext.Default.OldRequestDto), MessageBusConstants.NewRequest);

        });

        this.WhenAnyValue(x => x.SelectedAuthType)
            .Subscribe(_ => IsBasicAuthSelected = CurrentAuthScheme == AuthScheme.Basic);

        this.WhenAnyValue(x => x.SelectedHttpMethod)
            .Subscribe(_ => IsBodyEnabled = !string.Equals(SelectedHttpMethod, "GET", StringComparison.OrdinalIgnoreCase));

        // Validate the body as JSON while the user types (debounced so we don't parse on every keystroke).
        this.WhenAnyValue(x => x.Body, x => x.SelectedBodyType)
            .Throttle(TimeSpan.FromMilliseconds(400))
            .ObserveOn(RxSchedulers.MainThreadScheduler)
            .Subscribe(_ => ValidateBody());
    }

    private AuthScheme CurrentAuthScheme =>
        Enum.TryParse<AuthScheme>(SelectedAuthType, ignoreCase: true, out var scheme) ? scheme : AuthScheme.None;

    private bool HasBasicCredentials =>
        CurrentAuthScheme == AuthScheme.Basic && !(AuthUsername.Length == 0 && AuthPassword.Length == 0);

    // Returns the headers to send, which is the user's header list plus the generated Authorization
    // header. The credentials are deliberately kept out of the dictionary that gets persisted to the
    // request history, so passwords never reach the on-disk log. A Basic selection wins over an
    // Authorization header typed by hand in the Headers tab.
    private Dictionary<string, string> BuildRequestHeaders(Dictionary<string, string> headers)
    {
        if (!HasBasicCredentials) return headers;

        return new Dictionary<string, string>(headers)
        {
            [BasicAuth.HeaderName] = BasicAuth.BuildHeaderValue(AuthUsername, AuthPassword),
        };
    }

    private void RemoveHeader(HeaderItem header)
    {
        Headers.Remove(header);
    }

    private void RemoveQueryParam(QueryParamItem param)
    {
        QueryParams.Remove(param);
    }

    private void OnQueryParamsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // Editing a row (key/value/enabled) doesn't raise CollectionChanged, so each row is watched individually.
        if (e.OldItems is not null)
        {
            foreach (QueryParamItem item in e.OldItems)
            {
                item.PropertyChanged -= OnQueryParamPropertyChanged;
            }
        }

        if (e.NewItems is not null)
        {
            foreach (QueryParamItem item in e.NewItems)
            {
                item.PropertyChanged += OnQueryParamPropertyChanged;
            }
        }

        SyncUrlFromParams();
    }

    private void OnQueryParamPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        SyncUrlFromParams();
    }

    // Params -> Url. Only enabled rows with a key end up in the URL.
    private void SyncUrlFromParams()
    {
        if (_syncingQueryParams) return;
        _syncingQueryParams = true;
        try
        {
            var enabled = new List<QueryParam>();
            foreach (var param in QueryParams)
            {
                if (param.IsEnabled && !string.IsNullOrWhiteSpace(param.Key))
                {
                    enabled.Add(new QueryParam(param.Key, param.Value));
                }
            }

            Url = UrlQueryCodec.Build(Url, enabled);
        }
        finally
        {
            _syncingQueryParams = false;
        }
    }

    // Url -> Params. The URL is authoritative for enabled rows; disabled rows have no URL
    // representation, so they are carried over rather than lost when the URL is edited.
    private void SyncParamsFromUrl()
    {
        if (_syncingQueryParams) return;
        _syncingQueryParams = true;
        try
        {
            var disabled = new List<QueryParamItem>();
            foreach (var param in QueryParams)
            {
                if (!param.IsEnabled) disabled.Add(param);

                // Clear() raises a Reset with no OldItems, so detach here to avoid stale handlers.
                param.PropertyChanged -= OnQueryParamPropertyChanged;
            }

            QueryParams.Clear();

            foreach (var (key, value) in UrlQueryCodec.Parse(Url))
            {
                QueryParams.Add(new QueryParamItem { Key = key, Value = value, OnRemove = RemoveQueryParam });
            }

            foreach (var param in disabled)
            {
                QueryParams.Add(param);
            }
        }
        finally
        {
            _syncingQueryParams = false;
        }
    }

    private void ValidateBody()
    {
        if (!string.Equals(SelectedBodyType, "Json", StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrWhiteSpace(Body))
        {
            ClearBodyError();
            return;
        }

        try
        {
            using var _ = JsonDocument.Parse(Body);
            ClearBodyError();
        }
        catch (JsonException ex)
        {
            int line = (int)(ex.LineNumber ?? 0);
            int col = (int)(ex.BytePositionInLine ?? 0);
            int offset = ToCharOffset(Body, line, col);
            (int start, int end) = TokenRange(Body, offset);

            BodyJsonError = $"⚠ Invalid JSON — Line {line + 1}, Col {col + 1}: {CleanJsonMessage(ex.Message)}";
            IsBodyJsonInvalid = true;
            BodyErrorStart = start;
            BodyErrorEnd = end;
        }
    }

    private void ClearBodyError()
    {
        IsBodyJsonInvalid = false;
        BodyJsonError = "";
        BodyErrorStart = -1;
        BodyErrorEnd = -1;
    }

    // System.Text.Json reports errors as (line, byte-in-line). Convert to a char index in the string.
    private static int ToCharOffset(string text, int line, int byteInLine)
    {
        int idx = 0;
        for (int currentLine = 0; currentLine < line && idx < text.Length; idx++)
        {
            if (text[idx] == '\n') currentLine++;
        }

        long bytesLeft = byteInLine;
        while (bytesLeft > 0 && idx < text.Length && text[idx] != '\n')
        {
            bytesLeft -= Utf8ByteLength(text, idx, out int charsConsumed);
            idx += charsConsumed;
        }

        return idx;
    }

    private static int Utf8ByteLength(string text, int index, out int charsConsumed)
    {
        if (char.IsHighSurrogate(text[index]) && index + 1 < text.Length && char.IsLowSurrogate(text[index + 1]))
        {
            charsConsumed = 2;
            return 4;
        }

        charsConsumed = 1;
        char c = text[index];
        if (c < 0x80) return 1;
        if (c < 0x800) return 2;
        return 3;
    }

    // Select the whitespace-delimited token at the error offset so the highlight lands on the culprit.
    private static (int start, int end) TokenRange(string text, int offset)
    {
        if (text.Length == 0) return (-1, -1);
        if (offset >= text.Length) return (text.Length - 1, text.Length);

        int end = offset;
        while (end < text.Length && !char.IsWhiteSpace(text[end]) && "{}[]:,".IndexOf(text[end]) < 0)
        {
            end++;
        }

        if (end == offset) end = Math.Min(offset + 1, text.Length);
        return (offset, end);
    }

    // JsonException.Message tacks on " LineNumber: X | BytePositionInLine: Y." — we show those ourselves.
    private static string CleanJsonMessage(string message)
    {
        int idx = message.IndexOf(" LineNumber:", StringComparison.Ordinal);
        return idx >= 0 ? message[..idx].TrimEnd() : message;
    }
}
