namespace RequesterMini.Web.Constants;

/// <summary>
/// The same choices the desktop app offers, so a request built here means the same thing there.
/// </summary>
public static class HttpConstants
{
    public static readonly IReadOnlyList<string> Methods = ["GET", "POST", "PUT", "DELETE", "PATCH", "QUERY"];
    public static readonly IReadOnlyList<string> BodyTypes = ["Json", "Xml", "Form", "Text"];
    public static readonly IReadOnlyList<string> AuthTypes = ["None", "Basic", "Bearer"];

    public const string DefaultMethod = "GET";
    public const string DefaultBodyType = "Json";
    public const string DefaultAuthType = "None";
    public const string StartUrl = "https://jsonplaceholder.typicode.com/posts/1";

    /// <summary>Name of the <see cref="IHttpClientFactory"/> client used for outgoing user requests.</summary>
    public const string RequestClientName = "requester";

    /// <summary>
    /// The most of a response the web app will buffer. The default is effectively 2 GB, buffered into a
    /// string, then kept in history and rendered — enough for one large download to exhaust a
    /// single-replica container. A response over this comes back as an ordinary failure the page shows.
    /// </summary>
    public const long MaxResponseBytes = 10 * 1024 * 1024;
}
