namespace BrunoImporter;

public record BruRequest(
    string Name,
    string Method,
    string Url,
    IReadOnlyDictionary<string, string> Headers,
    string Body,
    string BodyType);
