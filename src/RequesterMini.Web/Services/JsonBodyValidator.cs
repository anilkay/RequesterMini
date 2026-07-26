using System.Text.Json;

namespace RequesterMini.Web.Services;

/// <summary>
/// Validates the request body as JSON, reporting the position in the human 1-based line/column form
/// rather than the byte offsets <see cref="JsonException"/> gives.
/// </summary>
public static class JsonBodyValidator
{
    /// <summary>Returns null when the text parses, otherwise a display-ready error message.</summary>
    public static string? Validate(string body)
    {
        if (string.IsNullOrWhiteSpace(body)) return null;

        try
        {
            using var _ = JsonDocument.Parse(body);
            return null;
        }
        catch (JsonException ex)
        {
            var line = (ex.LineNumber ?? 0) + 1;
            var column = (ex.BytePositionInLine ?? 0) + 1;
            return $"Invalid JSON — Line {line}, Col {column}: {StripPosition(ex.Message)}";
        }
    }

    // JsonException.Message appends " LineNumber: X | BytePositionInLine: Y." — we render those ourselves.
    private static string StripPosition(string message)
    {
        var idx = message.IndexOf(" LineNumber:", StringComparison.Ordinal);
        return idx >= 0 ? message[..idx].TrimEnd() : message;
    }
}
