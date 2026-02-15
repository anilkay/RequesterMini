using System;
using System.Collections.Generic;
using System.Text;

namespace RequesterMini.Utils;

internal static class CurlExporter
{
    internal static string Export(string httpMethod, string url, string body, string bodyType, Dictionary<string, string> headers)
    {
        var sb = new StringBuilder();
        sb.Append("curl");

        if (!string.Equals(httpMethod, "GET", StringComparison.OrdinalIgnoreCase))
        {
            sb.Append(" -X ");
            sb.Append(httpMethod);
        }

        sb.Append(" '");
        sb.Append(EscapeSingleQuote(url));
        sb.Append('\'');

        string? contentType = bodyType.ToLowerInvariant() switch
        {
            "json" => "application/json",
            "xml" => "application/xml",
            "form" => "multipart/form-data",
            _ => null
        };

        bool contentTypeInHeaders = false;
        foreach (var header in headers)
        {
            if (string.Equals(header.Key, "Content-Type", StringComparison.OrdinalIgnoreCase))
            {
                contentTypeInHeaders = true;
            }

            sb.Append(" -H '");
            sb.Append(EscapeSingleQuote(header.Key));
            sb.Append(": ");
            sb.Append(EscapeSingleQuote(header.Value));
            sb.Append('\'');
        }

        if (contentType is not null && !contentTypeInHeaders)
        {
            sb.Append(" -H 'Content-Type: ");
            sb.Append(contentType);
            sb.Append('\'');
        }

        if (!string.IsNullOrEmpty(body))
        {
            sb.Append(" -d '");
            sb.Append(EscapeSingleQuote(body));
            sb.Append('\'');
        }

        return sb.ToString();
    }

    private static string EscapeSingleQuote(string value) => value.Replace("'", "'\\''");
}
