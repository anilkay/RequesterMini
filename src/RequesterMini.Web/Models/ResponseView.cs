using System.Text;

namespace RequesterMini.Web.Models;

/// <summary>The last response, in the shape the UI renders it.</summary>
public sealed class ResponseView
{
    public required string StatusText { get; init; }
    public int StatusNumber { get; init; }
    public required string Body { get; init; }
    public string? ContentType { get; init; }
    public TimeSpan? Duration { get; init; }
    public DateTime? FinishedUtc { get; init; }
    public IReadOnlyDictionary<string, string> Headers { get; init; } = new Dictionary<string, string>();

    /// <summary>True when the request never produced a response (network error, cancellation).</summary>
    public bool IsFailure { get; init; }

    public bool IsSuccessStatus => StatusNumber is >= 200 and < 300;

    public string DurationText => Duration is not { } elapsed
        ? ""
        : elapsed.TotalSeconds < 1
            ? $"{elapsed.TotalMilliseconds:F0} ms"
            : $"{elapsed.TotalSeconds:F2} s";

    public string FinishedText => FinishedUtc is { } finished
        ? finished.ToString("yyyy-MM-dd HH:mm:ss") + " UTC"
        : "";

    public string SizeText
    {
        get
        {
            var bytes = Encoding.UTF8.GetByteCount(Body);
            return bytes < 1024 ? $"{bytes} B" : $"{bytes / 1024.0:F1} KB";
        }
    }
}
