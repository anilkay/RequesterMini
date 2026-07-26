using AppLogger;
using BrunoImporter;
using CurlExporter;
using HttpAuth;
using HttpRequesting;
using RequesterMini.Web.Constants;
using RequesterMini.Web.Models;
using UrlQuery;

namespace RequesterMini.Web.Services;

/// <summary>
/// The request the user is currently composing, plus its last response. Scoped, so each Blazor
/// circuit (browser tab) edits its own request while sharing the history singleton.
/// </summary>
public sealed class RequestWorkspace : IDisposable
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly RequestHistoryService _history;

    private CancellationTokenSource? _cts;

    // Url and QueryParams mirror each other; whichever side is being written suppresses the sync back.
    private bool _syncing;
    private string _url = HttpConstants.StartUrl;
    private string _body = "";
    private string _bodyType = HttpConstants.DefaultBodyType;

    public RequestWorkspace(IHttpClientFactory httpClientFactory, RequestHistoryService history)
    {
        ArgumentNullException.ThrowIfNull(httpClientFactory);
        ArgumentNullException.ThrowIfNull(history);
        _httpClientFactory = httpClientFactory;
        _history = history;
        SyncParamsFromUrl();
    }

    public string Method { get; set; } = HttpConstants.DefaultMethod;

    public string Url
    {
        get => _url;
        set
        {
            if (_url == value) return;
            _url = value;
            SyncParamsFromUrl();
        }
    }

    public string Body
    {
        get => _body;
        set
        {
            if (_body == value) return;
            _body = value;
            ValidateBody();
        }
    }

    public string BodyType
    {
        get => _bodyType;
        set
        {
            if (_bodyType == value) return;
            _bodyType = value;
            ValidateBody();
        }
    }

    public string AuthType { get; set; } = HttpConstants.DefaultAuthType;
    public string AuthUsername { get; set; } = "";
    public string AuthPassword { get; set; } = "";

    public List<HeaderRow> Headers { get; } = [];
    public List<QueryParamRow> QueryParams { get; } = [];

    public bool IsSending { get; private set; }
    public ResponseView? Response { get; private set; }

    /// <summary>Non-null while the body is selected as JSON and does not parse.</summary>
    public string? BodyJsonError { get; private set; }

    /// <summary>GET carries no body, so the Body tab is disabled for it — same rule as the desktop app.</summary>
    public bool IsBodyEnabled => !string.Equals(Method, "GET", StringComparison.OrdinalIgnoreCase);

    public bool IsBasicAuth => string.Equals(AuthType, "Basic", StringComparison.OrdinalIgnoreCase);

    public int EnabledHeaderCount => Headers.Count(h => h.IsEnabled && !string.IsNullOrWhiteSpace(h.Key));

    public int EnabledQueryParamCount => QueryParams.Count(p => p.IsEnabled && !string.IsNullOrWhiteSpace(p.Key));

    public void AddHeader() => Headers.Add(new HeaderRow());

    public void RemoveHeader(HeaderRow row) => Headers.Remove(row);

    public void AddQueryParam()
    {
        QueryParams.Add(new QueryParamRow());
        SyncUrlFromParams();
    }

    public void RemoveQueryParam(QueryParamRow row)
    {
        QueryParams.Remove(row);
        SyncUrlFromParams();
    }

    public async Task SendAsync()
    {
        if (IsSending || string.IsNullOrWhiteSpace(Url)) return;

        var headers = CollectHeaders();

        _cts?.Cancel();
        _cts?.Dispose();
        _cts = new CancellationTokenSource();

        IsSending = true;
        try
        {
            Logger.Info($"Sending {Method} request to {Url}");

            var runner = new HttpRequestRunner(_httpClientFactory.CreateClient(HttpConstants.RequestClientName));
            var spec = new HttpRequestSpec(Method, Url, Body, BodyType, WithAuthHeader(headers));
            var result = await runner.SendAsync(spec, _cts.Token);

            Response = result.Match(
                success =>
                {
                    Logger.Info($"Request succeeded. Method={Method}, Url={Url}, Status={success.StatusCode}");
                    return new ResponseView
                    {
                        StatusText = success.StatusCode,
                        StatusNumber = success.StatusNumber,
                        Body = success.ResponseBody,
                        ContentType = success.ContentType,
                        Duration = success.RequestTime,
                        FinishedUtc = success.FinishedTimeUtc,
                        Headers = success.Headers,
                    };
                },
                failure =>
                {
                    Logger.Error($"Request failed. Method={Method}, Url={Url}, Message={failure.Message}", failure.Exception);
                    return new ResponseView
                    {
                        StatusText = "Error",
                        Body = failure.Message,
                        IsFailure = true,
                    };
                });
        }
        finally
        {
            IsSending = false;
        }

        _history.Add(new StoredRequest(
            Method,
            Url,
            Body,
            BodyType,
            Response.StatusText,
            Response.Body,
            headers,
            DateTime.UtcNow));
    }

    public void Cancel() => _cts?.Cancel();

    public string BuildCurl()
    {
        var builder = new CurlCommandBuilder()
            .SetMethod(Method)
            .SetUrl(Url);

        if (HasBasicCredentials)
        {
            builder.SetBasicAuth(AuthUsername, AuthPassword);
        }

        foreach (var (key, value) in CollectHeaders())
        {
            builder.AddHeader(key, value);
        }

        if (!string.IsNullOrEmpty(Body) && Enum.TryParse<CurlExporter.BodyType>(_bodyType, ignoreCase: true, out var parsed))
        {
            builder.SetBody(Body, parsed);
        }

        return builder.Build();
    }

    /// <summary>Replaces the current request with one imported from a Bruno file.</summary>
    public void ApplyBruRequest(BruRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        Method = request.Method;
        Body = request.Body;
        BodyType = request.BodyType;
        Url = request.Url;

        Headers.Clear();
        foreach (var (key, value) in request.Headers)
        {
            Headers.Add(new HeaderRow { Key = key, Value = value });
        }

        ValidateBody();
    }

    /// <summary>Restores a history entry into the editor. The response panel is left as it was saved.</summary>
    public void LoadFrom(StoredRequest stored)
    {
        ArgumentNullException.ThrowIfNull(stored);

        Method = stored.Method;
        Body = stored.Body;
        BodyType = string.IsNullOrWhiteSpace(stored.BodyType) ? HttpConstants.DefaultBodyType : stored.BodyType;
        Url = stored.Url;

        Headers.Clear();
        foreach (var (key, value) in stored.Headers)
        {
            Headers.Add(new HeaderRow { Key = key, Value = value });
        }

        Response = new ResponseView
        {
            StatusText = stored.ResponseStatusCode,
            StatusNumber = 0,
            Body = stored.ResponseBody,
            FinishedUtc = stored.SavedAtUtc,
            IsFailure = string.Equals(stored.ResponseStatusCode, "Error", StringComparison.Ordinal),
        };

        ValidateBody();
    }

    /// <summary>Params -> Url. Only enabled rows with a key reach the URL.</summary>
    public void SyncUrlFromParams()
    {
        if (_syncing) return;
        _syncing = true;
        try
        {
            var enabled = QueryParams
                .Where(p => p.IsEnabled && !string.IsNullOrWhiteSpace(p.Key))
                .Select(p => new QueryParam(p.Key, p.Value));

            _url = UrlQueryCodec.Build(_url, enabled);
        }
        finally
        {
            _syncing = false;
        }
    }

    /// <summary>
    /// Url -> Params. The URL is authoritative for enabled rows; disabled rows have no URL
    /// representation, so they are carried over rather than lost when the URL is edited.
    /// </summary>
    public void SyncParamsFromUrl()
    {
        if (_syncing) return;
        _syncing = true;
        try
        {
            var disabled = QueryParams.Where(p => !p.IsEnabled).ToList();
            QueryParams.Clear();

            foreach (var (key, value) in UrlQueryCodec.Parse(_url))
            {
                QueryParams.Add(new QueryParamRow { Key = key, Value = value });
            }

            QueryParams.AddRange(disabled);
        }
        finally
        {
            _syncing = false;
        }
    }

    public void ValidateBody()
    {
        BodyJsonError = string.Equals(_bodyType, "Json", StringComparison.OrdinalIgnoreCase)
            ? JsonBodyValidator.Validate(_body)
            : null;
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }

    private bool HasBasicCredentials =>
        IsBasicAuth && !(AuthUsername.Length == 0 && AuthPassword.Length == 0);

    /// <summary>The user's enabled header rows. This is also what gets persisted to history.</summary>
    private Dictionary<string, string> CollectHeaders()
    {
        var headers = new Dictionary<string, string>();
        foreach (var row in Headers)
        {
            if (row.IsEnabled && !string.IsNullOrWhiteSpace(row.Key))
            {
                headers[row.Key] = row.Value;
            }
        }

        return headers;
    }

    /// <summary>
    /// Adds the generated Authorization header for sending only — credentials stay out of the
    /// dictionary that reaches the history file, so passwords never hit disk. A Basic selection wins
    /// over an Authorization header typed by hand in the Headers tab.
    /// </summary>
    private Dictionary<string, string> WithAuthHeader(Dictionary<string, string> headers)
    {
        if (!HasBasicCredentials) return headers;

        return new Dictionary<string, string>(headers)
        {
            [BasicAuth.HeaderName] = BasicAuth.BuildHeaderValue(AuthUsername, AuthPassword),
        };
    }
}
