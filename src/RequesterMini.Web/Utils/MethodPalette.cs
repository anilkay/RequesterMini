using MudBlazor;

namespace RequesterMini.Web.Utils;

/// <summary>
/// Maps an HTTP method to the theme color its chip is drawn in — the web counterpart of the
/// desktop app's per-method colors.
/// </summary>
public static class MethodPalette
{
    public static Color For(string method) => method.ToUpperInvariant() switch
    {
        "GET" => Color.Info,
        "POST" => Color.Success,
        "PUT" => Color.Warning,
        "DELETE" => Color.Error,
        "PATCH" => Color.Tertiary,
        "QUERY" => Color.Secondary,
        _ => Color.Default,
    };

    /// <summary>Color for a response status line: 2xx success, 3xx info, 4xx warning, 5xx error.</summary>
    public static Color ForStatus(int statusNumber) => statusNumber switch
    {
        >= 200 and < 300 => Color.Success,
        >= 300 and < 400 => Color.Info,
        >= 400 and < 500 => Color.Warning,
        >= 500 => Color.Error,
        _ => Color.Default,
    };
}
