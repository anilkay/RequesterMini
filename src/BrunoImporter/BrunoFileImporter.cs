namespace BrunoImporter;

/// <summary>
/// Auto-detects and parses both .bru and OpenCollection YAML (.yml) files.
/// </summary>
public static class BrunoFileImporter
{
    /// <summary>
    /// Parses a Bruno request file, auto-detecting the format from content.
    /// YAML format is detected by top-level "http:" or "info:" keys without braces.
    /// </summary>
    public static BruRequest Parse(string content)
    {
        ArgumentNullException.ThrowIfNull(content);

        return IsYamlFormat(content)
            ? BruYamlParser.Parse(content)
            : BruParser.Parse(content);
    }

    /// <summary>
    /// Parses a Bruno request file, using the file extension to choose the parser.
    /// Falls back to content-based detection if extension is ambiguous.
    /// </summary>
    public static BruRequest ParseFile(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        var content = File.ReadAllText(filePath);
        var ext = Path.GetExtension(filePath);

        return ext.Equals(".yml", StringComparison.OrdinalIgnoreCase)
            || ext.Equals(".yaml", StringComparison.OrdinalIgnoreCase)
            ? BruYamlParser.Parse(content)
            : ext.Equals(".bru", StringComparison.OrdinalIgnoreCase)
                ? BruParser.Parse(content)
                : Parse(content); // auto-detect
    }

    private static bool IsYamlFormat(string content)
    {
        // YAML format has top-level keys like "http:", "info:" without curly braces
        foreach (var rawLine in content.Split('\n'))
        {
            var line = rawLine.Trim();
            if (string.IsNullOrEmpty(line) || line.StartsWith('#'))
                continue;

            // YAML: first meaningful line starts with "key:" (no brace)
            // Bru: first meaningful line ends with "{"
            if (line.EndsWith('{'))
                return false;
            if (line.EndsWith(':') || (line.Contains(':') && !line.Contains('{')))
                return true;

            break;
        }

        return false;
    }
}
