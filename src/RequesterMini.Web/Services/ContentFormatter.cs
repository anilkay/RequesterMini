using System.Text.Json;
using System.Xml.Linq;

namespace RequesterMini.Web.Services;

/// <summary>Which colorizer the response preview should use.</summary>
public enum ContentKind
{
    Plain,
    Json,
    Xml,
}

/// <summary>
/// Decides how a response body should be displayed and pretty-prints it. Every method falls back to
/// the original text rather than throwing — a malformed body still has to be readable.
/// </summary>
public static class ContentFormatter
{
    public static ContentKind Detect(string? contentType, string body)
    {
        if (contentType is not null)
        {
            if (contentType.Contains("json", StringComparison.OrdinalIgnoreCase)) return ContentKind.Json;
            if (contentType.Contains("xml", StringComparison.OrdinalIgnoreCase)) return ContentKind.Xml;
            if (contentType.StartsWith("text/", StringComparison.OrdinalIgnoreCase)
                && !contentType.Contains("html", StringComparison.OrdinalIgnoreCase))
            {
                return ContentKind.Plain;
            }
        }

        // No usable content type (or an HTML one): fall back to sniffing the first meaningful char.
        var trimmed = body.AsSpan().TrimStart();
        if (trimmed.Length == 0) return ContentKind.Plain;
        if (trimmed[0] is '{' or '[') return ContentKind.Json;
        if (trimmed[0] == '<') return ContentKind.Xml;
        return ContentKind.Plain;
    }

    public static string Pretty(string body, ContentKind kind) => kind switch
    {
        ContentKind.Json => PrettyJson(body),
        ContentKind.Xml => PrettyXml(body),
        _ => body,
    };

    private static string PrettyJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return json;

        try
        {
            using var document = JsonDocument.Parse(json);
            using var stream = new MemoryStream();
            using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
            {
                document.WriteTo(writer);
            }

            return System.Text.Encoding.UTF8.GetString(stream.ToArray());
        }
        catch (JsonException)
        {
            return json;
        }
    }

    private static string PrettyXml(string xml)
    {
        if (string.IsNullOrWhiteSpace(xml)) return xml;

        try
        {
            return XDocument.Parse(xml).ToString();
        }
        catch (System.Xml.XmlException)
        {
            return xml;
        }
    }
}
