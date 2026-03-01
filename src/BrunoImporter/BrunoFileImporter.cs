namespace BrunoImporter;

/// <summary>
/// Entry point for importing Bruno request files (.bru and .yml).
/// </summary>
public static class BrunoFileImporter
{
    /// <summary>Parses a Bruno request file by path (.bru or .yml).</summary>
    public static BruRequest ParseFile(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        return BruYamlParser.Parse(File.ReadAllText(filePath));
    }

    /// <summary>Parses Bruno request content (auto-detects .bru vs YAML).</summary>
    public static BruRequest Parse(string content) => BruYamlParser.Parse(content);
}
