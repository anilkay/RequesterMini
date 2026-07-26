namespace HttpRequesting;

/// <summary>
/// A completed HTTP exchange. <paramref name="StatusCode"/> is the reason phrase form ("OK",
/// "NotFound") and <paramref name="StatusNumber"/> the numeric one, so callers can both display and
/// classify it without re-parsing.
/// </summary>
public sealed record RequestSuccess(
    string StatusCode,
    int StatusNumber,
    string ResponseBody,
    DateTime? FinishedTimeUtc,
    TimeSpan? RequestTime,
    string? ContentType = null,
    IReadOnlyDictionary<string, string>? ResponseHeaders = null)
{
    public IReadOnlyDictionary<string, string> Headers => ResponseHeaders ?? new Dictionary<string, string>();

    public bool IsSuccessStatus => StatusNumber is >= 200 and < 300;
}
