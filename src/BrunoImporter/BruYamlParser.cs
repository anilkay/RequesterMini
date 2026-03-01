namespace BrunoImporter;

/// <summary>
/// Parses both Bruno .bru (block-based DSL) and OpenCollection YAML (.yml) request files.
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

        return ParseYaml(content);
    }


    // ── YAML parser ─────────────────────────────────────────────────────────

    private static BruRequest ParseYaml(string content)
    {
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

            if (indent == 0 && trimmed.Length > 0 && !trimmed.StartsWith('#'))
            {
                var (key, _) = SplitKeyValue(trimmed);

                if (key == "info")   { i++; name = ParseYamlInfoBlock(lines, ref i); continue; }
                if (key == "http")   { i++; ParseYamlHttpBlock(lines, ref i, ref method, ref url, ref body, ref bodyType, headers); continue; }
            }

            i++;
        }

        if (method.StartsWith("'") && method.EndsWith("'") && method.Length > 2)
        {
            method = method[1..^1];
        }

        return new BruRequest(name, method, url, headers, body, bodyType);
    }

    private static string ParseYamlInfoBlock(string[] lines, ref int i)
    {
        string name = "";
        while (i < lines.Length)
        {
            var line = lines[i].TrimEnd('\r');
            var trimmed = line.TrimStart();
            if (line.Length - trimmed.Length == 0 && trimmed.Length > 0) break;

            var (key, value) = SplitKeyValue(trimmed);
            if (key == "name") name = value;
            i++;
        }
        return name;
    }

    private static void ParseYamlHttpBlock(
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

            if (indent == 0 && trimmed.Length > 0) break;

            var (key, value) = SplitKeyValue(trimmed);

            if (key == "method")  { method = value.ToUpperInvariant(); i++; continue; }
            if (key == "url")     { url = value; i++; continue; }
            if (key == "body")    { i++; ParseYamlBodyBlock(lines, ref i, indent, ref body, ref bodyType); continue; }
            if (key == "headers") { i++; ParseYamlHeadersBlock(lines, ref i, indent, headers); continue; }

            i++;
        }
    }

    private static void ParseYamlBodyBlock(
        string[] lines, ref int i, int parentIndent,
        ref string body, ref string bodyType)
    {
        while (i < lines.Length)
        {
            var line = lines[i].TrimEnd('\r');
            var trimmed = line.TrimStart();
            var indent = line.Length - trimmed.Length;

            if (indent <= parentIndent && trimmed.Length > 0) break;

            var (key, value) = SplitKeyValue(trimmed);

            if (key == "type") { bodyType = MapBodyType(value); i++; continue; }
            if (key == "data")
            {
                if (value is "|-" or "|" or ">-" or ">") { i++; body = ReadBlockScalar(lines, ref i, indent); }
                else { body = value; i++; }
                continue;
            }

            i++;
        }
    }

    private static void ParseYamlHeadersBlock(
        string[] lines, ref int i, int parentIndent,
        Dictionary<string, string> headers)
    {
        while (i < lines.Length)
        {
            var line = lines[i].TrimEnd('\r');
            var trimmed = line.TrimStart();
            var indent = line.Length - trimmed.Length;

            if (indent <= parentIndent && trimmed.Length > 0) break;

            var (key, value) = SplitKeyValue(trimmed);
            if (!string.IsNullOrEmpty(key) && key != "#")
                headers[key] = value;

            i++;
        }
    }

    private static string ReadBlockScalar(string[] lines, ref int i, int keyIndent)
    {
        var bodyLines = new List<string>();

        while (i < lines.Length)
        {
            var line = lines[i].TrimEnd('\r');

            if (string.IsNullOrWhiteSpace(line)) { bodyLines.Add(""); i++; continue; }

            var trimmed = line.TrimStart();
            var indent = line.Length - trimmed.Length;

            if (indent <= keyIndent) break;

            bodyLines.Add(trimmed);
            i++;
        }

        while (bodyLines.Count > 0 && string.IsNullOrEmpty(bodyLines[^1]))
            bodyLines.RemoveAt(bodyLines.Count - 1);

        return string.Join("\n", bodyLines);
    }

    // ── Shared helpers ───────────────────────────────────────────────────────

    private static (string key, string value) SplitKeyValue(string line)
    {
        var colonIdx = line.IndexOf(':');
        if (colonIdx <= 0) return (line.Trim(), "");
        return (line[..colonIdx].Trim(), line[(colonIdx + 1)..].Trim());
    }

    private static string MapBodyType(string value) =>
        BodyTypeMap.TryGetValue(value.Trim(), out var mapped) ? mapped : "None";
}
