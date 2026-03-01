namespace BrunoImporter;

/// <summary>
/// Parses Bruno OpenCollection YAML (.yml) request files.
/// Uses simple line-based parsing — no YAML library dependency.
/// </summary>
public static class BruYamlParser
{
    private static readonly Dictionary<string, string> BodyTypeMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "json",             "Json"           },
        { "text",             "Text"           },
        { "form-urlencoded",  "FormUrlEncoded" },
    };

    public static BruRequest Parse(string content)
    {
        ArgumentNullException.ThrowIfNull(content);

        var lines = content.Split('\n');

        string name     = "";
        string method   = "GET";
        string url      = "";
        string body     = "";
        string bodyType = "None";
        var headers     = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        int i = 0;
        while (i < lines.Length)
        {
            var line = lines[i].TrimEnd('\r');
            var trimmed = line.TrimStart();
            var indent = line.Length - trimmed.Length;

            // Top-level keys (indent 0)
            if (indent == 0 && trimmed.Length > 0 && !trimmed.StartsWith('#'))
            {
                var (key, value) = SplitKeyValue(trimmed);

                if (key == "info")
                {
                    i++;
                    name = ParseInfoBlock(lines, ref i);
                    continue;
                }

                if (key == "http")
                {
                    i++;
                    ParseHttpBlock(lines, ref i, ref method, ref url, ref body, ref bodyType, headers);
                    continue;
                }
            }

            i++;
        }

        return new BruRequest(name, method, url, headers, body, bodyType);
    }

    private static string ParseInfoBlock(string[] lines, ref int i)
    {
        string name = "";
        while (i < lines.Length)
        {
            var line = lines[i].TrimEnd('\r');
            var trimmed = line.TrimStart();
            var indent = line.Length - trimmed.Length;

            if (indent == 0 && trimmed.Length > 0)
                break; // left the block

            var (key, value) = SplitKeyValue(trimmed);
            if (key == "name")
                name = value;

            i++;
        }
        return name;
    }

    private static void ParseHttpBlock(
        string[] lines, ref int i,
        ref string method, ref string url,
        ref string body, ref string bodyType,
        Dictionary<string, string> headers)
    {
        while (i < lines.Length)
        {
            var line = lines[i].TrimEnd('\r');
            var trimmed = line.TrimStart();
            var indent = line.Length - trimmed.Length;

            if (indent == 0 && trimmed.Length > 0)
                break;

            var (key, value) = SplitKeyValue(trimmed);

            if (key == "method")
            {
                method = value.ToUpperInvariant();
                i++;
                continue;
            }

            if (key == "url")
            {
                url = value;
                i++;
                continue;
            }

            if (key == "body")
            {
                i++;
                ParseBodyBlock(lines, ref i, indent, ref body, ref bodyType);
                continue;
            }

            if (key == "headers")
            {
                i++;
                ParseHeadersBlock(lines, ref i, indent, headers);
                continue;
            }

            i++;
        }
    }

    private static void ParseBodyBlock(
        string[] lines, ref int i, int parentIndent,
        ref string body, ref string bodyType)
    {
        while (i < lines.Length)
        {
            var line = lines[i].TrimEnd('\r');
            var trimmed = line.TrimStart();
            var indent = line.Length - trimmed.Length;

            if (indent <= parentIndent && trimmed.Length > 0)
                break;

            var (key, value) = SplitKeyValue(trimmed);

            if (key == "type")
            {
                bodyType = MapBodyType(value);
                i++;
                continue;
            }

            if (key == "data")
            {
                // value may be a block scalar indicator (|- or |) or inline value
                if (value is "|-" or "|" or ">-" or ">")
                {
                    i++;
                    body = ReadBlockScalar(lines, ref i, indent);
                }
                else
                {
                    body = value;
                    i++;
                }
                continue;
            }

            i++;
        }
    }

    private static void ParseHeadersBlock(
        string[] lines, ref int i, int parentIndent,
        Dictionary<string, string> headers)
    {
        while (i < lines.Length)
        {
            var line = lines[i].TrimEnd('\r');
            var trimmed = line.TrimStart();
            var indent = line.Length - trimmed.Length;

            if (indent <= parentIndent && trimmed.Length > 0)
                break;

            var (key, value) = SplitKeyValue(trimmed);
            if (!string.IsNullOrEmpty(key) && key != "#")
                headers[key] = value;

            i++;
        }
    }

    /// <summary>
    /// Reads a YAML block scalar (lines indented deeper than the parent key).
    /// </summary>
    private static string ReadBlockScalar(string[] lines, ref int i, int keyIndent)
    {
        var bodyLines = new List<string>();

        while (i < lines.Length)
        {
            var line = lines[i].TrimEnd('\r');

            // Empty lines are part of the block
            if (string.IsNullOrWhiteSpace(line))
            {
                bodyLines.Add("");
                i++;
                continue;
            }

            var trimmed = line.TrimStart();
            var indent = line.Length - trimmed.Length;

            // Stop when we hit a line at same or lesser indentation
            if (indent <= keyIndent)
                break;

            bodyLines.Add(trimmed);
            i++;
        }

        // Trim trailing empty lines
        while (bodyLines.Count > 0 && string.IsNullOrEmpty(bodyLines[^1]))
            bodyLines.RemoveAt(bodyLines.Count - 1);

        return string.Join("\n", bodyLines);
    }

    private static (string key, string value) SplitKeyValue(string line)
    {
        var colonIdx = line.IndexOf(':');
        if (colonIdx <= 0)
            return (line.Trim(), "");

        var key = line[..colonIdx].Trim();
        var value = line[(colonIdx + 1)..].Trim();
        return (key, value);
    }

    private static string MapBodyType(string value) =>
        BodyTypeMap.TryGetValue(value.Trim(), out var mapped) ? mapped : "None";
}
