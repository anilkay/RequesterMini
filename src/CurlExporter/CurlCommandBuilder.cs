namespace CurlExporter;

public enum BodyType
{
    Json,
    Xml,
    Form,
    Text
}

public class CurlCommandBuilder
{
    private static readonly Dictionary<BodyType, string> BodyTypeToContentType = new()
    {
        [BodyType.Json] = "application/json",
        [BodyType.Xml] = "application/xml",
        [BodyType.Form] = "multipart/form-data",
        [BodyType.Text] = "text/plain"
    };

    private string _method = "GET";
    private string _url = "";
    private string? _body;
    private string? _basicAuth;
    private readonly Dictionary<string, string> _headers = new(StringComparer.OrdinalIgnoreCase);

    public CurlCommandBuilder SetMethod(string method)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(method);
        _method = method.ToUpperInvariant();
        return this;
    }

    public CurlCommandBuilder SetUrl(string url)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url);
        _url = url;
        return this;
    }

    public CurlCommandBuilder SetBody(string body, string contentType)
    {
        ArgumentNullException.ThrowIfNull(body);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);
        _body = body;
        if (!_headers.ContainsKey("Content-Type"))
        {
            _headers["Content-Type"] = contentType;
        }
        return this;
    }

    public CurlCommandBuilder SetBody(string body, BodyType bodyType)
    {
        return SetBody(body, BodyTypeToContentType[bodyType]);
    }

    /// <summary>
    /// Emits HTTP Basic credentials as curl's own <c>-u user:password</c> flag, which reads better
    /// than a pre-encoded Authorization header and lets curl do the Base64 encoding.
    /// </summary>
    public CurlCommandBuilder SetBasicAuth(string username, string password)
    {
        ArgumentNullException.ThrowIfNull(username);
        ArgumentNullException.ThrowIfNull(password);
        _basicAuth = $"{username}:{password}";
        return this;
    }

    public CurlCommandBuilder AddHeader(string key, string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(value);
        _headers[key] = value;
        return this;
    }

    public string Build()
    {
        if (string.IsNullOrWhiteSpace(_url))
        {
            throw new InvalidOperationException("URL must be set before building a cURL command.");
        }

        var parts = new List<string> { "curl" };

        if (!string.Equals(_method, "GET", StringComparison.OrdinalIgnoreCase))
        {
            parts.Add("-X");
            parts.Add(_method);
        }

        parts.Add(Quote(_url));

        if (_basicAuth is not null)
        {
            parts.Add("-u");
            parts.Add(Quote(_basicAuth));
        }

        foreach (var header in _headers)
        {
            parts.Add("-H");
            parts.Add(Quote($"{header.Key}: {header.Value}"));
        }

        if (!string.IsNullOrEmpty(_body))
        {
            parts.Add("-d");
            parts.Add(Quote(_body));
        }

        return string.Join(' ', parts);
    }

    private static string Quote(string value)
    {
        return "'" + value.Replace("'", "'\\''") + "'";
    }
}
