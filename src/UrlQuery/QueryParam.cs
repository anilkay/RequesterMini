namespace UrlQuery;

/// <summary>
/// One decoded query-string entry. Values are held unescaped (human readable); escaping happens
/// when a URL is rebuilt by <see cref="UrlQueryCodec.Build"/>.
/// Duplicate keys are legal in a query string, so callers use ordered lists rather than dictionaries.
/// </summary>
public readonly record struct QueryParam(string Key, string Value);
