namespace HttpRequesting;

/// <summary>
/// Everything needed to issue one request. Kept as plain strings (rather than <see cref="HttpMethod"/>
/// and enums) because it is filled in directly from user-typed UI fields.
/// </summary>
public sealed record HttpRequestSpec(
    string Method,
    string Url,
    string Body = "",
    string BodyType = "Json",
    IReadOnlyDictionary<string, string>? Headers = null);
