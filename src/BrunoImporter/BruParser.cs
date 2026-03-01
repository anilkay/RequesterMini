namespace BrunoImporter;

public static class BruParser
{
    private static readonly HashSet<string> HttpMethodBlocks = new(StringComparer.OrdinalIgnoreCase)
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

        var lines = content.Split('\n');

        string name      = "";
        string method    = "GET";
        string url       = "";
        string body      = "";
        string bodyType  = "None";
        var headers      = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        int i = 0;
        while (i < lines.Length)
        {
            var trimmed = lines[i].Trim();

            // Detect block start: one or two word-segments separated by optional colon, then " {"
            if (TryParseBlockStart(trimmed, out var blockName))
            {
                i++;
                var blockLines = new List<string>();
                while (i < lines.Length && lines[i].Trim() != "}")
                {
                    blockLines.Add(lines[i]);
                    i++;
                }
                // skip closing "}"
                i++;

                ProcessBlock(blockName!, blockLines, ref name, ref method, ref url, ref body, ref bodyType, headers);
            }
            else
            {
                i++;
            }
        }

        return new BruRequest(name, method, url, headers, body, bodyType);
    }

    // -----------------------------------------------------------------------

    private static bool TryParseBlockStart(string trimmedLine, out string? blockName)
    {
        blockName = null;

        if (!trimmedLine.EndsWith('{'))
            return false;

        // strip trailing "{"
        var candidate = trimmedLine[..^1].Trim();

        if (string.IsNullOrEmpty(candidate))
            return false;

        // Allow: word  OR  word:word  (e.g. "body:json")
        // A word here means one or more non-whitespace, non-brace chars
        foreach (var ch in candidate)
        {
            if (char.IsWhiteSpace(ch) || ch == '{' || ch == '}')
                return false;
        }

        blockName = candidate;
        return true;
    }

    private static void ProcessBlock(
        string blockName,
        List<string> blockLines,
        ref string name,
        ref string method,
        ref string url,
        ref string body,
        ref string bodyType,
        Dictionary<string, string> headers)
    {
        var lowerBlock = blockName.ToLowerInvariant();

        if (lowerBlock == "meta")
        {
            var kvs = ParseKeyValues(blockLines);
            if (kvs.TryGetValue("name", out var n))
                name = n;
            return;
        }

        if (HttpMethodBlocks.Contains(lowerBlock))
        {
            method = lowerBlock.ToUpperInvariant();
            var kvs = ParseKeyValues(blockLines);
            if (kvs.TryGetValue("url", out var u))
                url = u;
            if (kvs.TryGetValue("body", out var bt))
                bodyType = MapBodyType(bt);
            return;
        }

        if (lowerBlock == "headers")
        {
            var kvs = ParseKeyValues(blockLines);
            foreach (var kv in kvs)
                headers[kv.Key] = kv.Value;
            return;
        }

        // body:* blocks — raw content
        if (lowerBlock.StartsWith("body:"))
        {
            body = string.Join("\n", blockLines).Trim();
            // Override bodyType from the block suffix if it maps
            var suffix = lowerBlock["body:".Length..];
            if (BodyTypeMap.TryGetValue(suffix, out var explicitType))
                bodyType = explicitType;
            return;
        }

        // plain "body" block — defaults to JSON per Bruno spec
        if (lowerBlock == "body")
        {
            body = string.Join("\n", blockLines).Trim();
            bodyType = "Json";
            return;
        }
    }

    private static Dictionary<string, string> ParseKeyValues(List<string> lines)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();
            if (string.IsNullOrEmpty(line) || line.StartsWith('#'))
                continue;

            // Skip disabled entries (prefixed with ~)
            if (line.StartsWith('~'))
                continue;

            var colonIdx = line.IndexOf(':');
            if (colonIdx <= 0)
                continue;

            var key   = line[..colonIdx].Trim();
            var value = line[(colonIdx + 1)..].Trim();
            result[key] = value;
        }

        return result;
    }

    private static string MapBodyType(string value) =>
        BodyTypeMap.TryGetValue(value.Trim(), out var mapped) ? mapped : "None";
}
