namespace BrunoImporter;

/// <summary>
/// Parses both Bruno .bru (block-based DSL) and OpenCollection YAML (.yml) request files.
/// Uses simple line-based parsing — no YAML library dependency.
/// </summary>
public static class BruYamlParser
{
    private static readonly HashSet<string> BruHttpMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        "get", "post", "put", "patch", "delete", "head", "options", "trace", "connect"
    };

    private static readonly Dictionary<string, string> BodyTypeMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "json",             "Json"           },
        { "text",             "Text"           },
        { "form-urlencoded",  "FormUrlEncoded" },
    };

    public static BruRequest Parse(string content)
    {
        ArgumentNullException.ThrowIfNull(content);

        return IsBruFormat(content)
            ? ParseBru(content)
            : ParseYaml(content);
    }

    // ── Format detection ────────────────────────────────────────────────────

    private static bool IsBruFormat(string content)
    {
        foreach (var rawLine in content.Split('\n'))
        {
            var line = rawLine.Trim();
            if (string.IsNullOrEmpty(line) || line.StartsWith('#'))
                continue;
            return line.EndsWith('{');
        }
        return false;
    }

    // ── .bru parser ─────────────────────────────────────────────────────────

    private static BruRequest ParseBru(string content)
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
            var trimmed = lines[i].Trim();

            if (TryParseBruBlockStart(trimmed, out var blockName))
            {
                i++;
                var blockLines = new List<string>();
                while (i < lines.Length && lines[i].Trim() != "}")
                {
                    blockLines.Add(lines[i]);
                    i++;
                }
                i++; // skip closing "}"

                ProcessBruBlock(blockName!, blockLines, ref name, ref method, ref url, ref body, ref bodyType, headers);
            }
            else
            {
                i++;
            }
        }

        return new BruRequest(name, method, url, headers, body, bodyType);
    }

    private static bool TryParseBruBlockStart(string trimmedLine, out string? blockName)
    {
        blockName = null;
        if (!trimmedLine.EndsWith('{'))
            return false;

        var candidate = trimmedLine[..^1].Trim();
        if (string.IsNullOrEmpty(candidate))
            return false;

        foreach (var ch in candidate)
        {
            if (char.IsWhiteSpace(ch) || ch == '{' || ch == '}')
                return false;
        }

        blockName = candidate;
        return true;
    }

    private static void ProcessBruBlock(
        string blockName, List<string> blockLines,
        ref string name, ref string method, ref string url,
        ref string body, ref string bodyType,
        Dictionary<string, string> headers)
    {
        var lower = blockName.ToLowerInvariant();

        if (lower == "meta")
        {
            var kvs = ParseBruKeyValues(blockLines);
            if (kvs.TryGetValue("name", out var n)) name = n;
            return;
        }

        if (BruHttpMethods.Contains(lower))
        {
            method = lower.ToUpperInvariant();
            var kvs = ParseBruKeyValues(blockLines);
            if (kvs.TryGetValue("url", out var u)) url = u;
            if (kvs.TryGetValue("body", out var bt)) bodyType = MapBodyType(bt);
            return;
        }

        if (lower == "headers")
        {
            foreach (var kv in ParseBruKeyValues(blockLines))
                headers[kv.Key] = kv.Value;
            return;
        }

        if (lower.StartsWith("body:"))
        {
            body = string.Join("\n", blockLines).Trim();
            var suffix = lower["body:".Length..];
            if (BodyTypeMap.TryGetValue(suffix, out var t)) bodyType = t;
            return;
        }

        if (lower == "body")
        {
            body = string.Join("\n", blockLines).Trim();
            bodyType = "Json";
            return;
        }
    }

    private static Dictionary<string, string> ParseBruKeyValues(List<string> lines)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();
            if (string.IsNullOrEmpty(line) || line.StartsWith('#') || line.StartsWith('~'))
                continue;

            var colonIdx = line.IndexOf(':');
            if (colonIdx <= 0) continue;

            result[line[..colonIdx].Trim()] = line[(colonIdx + 1)..].Trim();
        }

        return result;
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
