using RequesterMini.Web.Models;
using Templating;

namespace RequesterMini.Web.Services;

/// <summary>
/// Repairs what the stores read from disk before anything else touches it.
/// <para>
/// The C# nullability annotations are not enforced by the deserializer, so a file with a missing
/// list, a <c>null</c> entry or an absent field produces an object graph the compiler believes is
/// non-null and that throws the first time something dereferences it. That used to happen in a
/// singleton's constructor, which took every page down until someone fixed the file by hand — and
/// the files can legitimately differ from what this version writes (an older build, a hand edit).
/// </para>
/// <para>
/// The rule is to keep whatever is usable and drop what is not: an entry with no method or URL, a
/// collection with no name. Missing optional pieces become empty rather than an error.
/// </para>
/// </summary>
internal static class StoreSanitizer
{
    /// <summary>Returns the request with every field made safe to use, or null when it cannot be saved.</summary>
    internal static StoredRequest? Request(StoredRequest? request)
    {
        // Without a method or a URL there is nothing to show or to load back into the editor.
        if (request is null || string.IsNullOrWhiteSpace(request.Method) || string.IsNullOrWhiteSpace(request.Url))
        {
            return null;
        }

        // Each `??` looks redundant to the compiler, which trusts the annotations. The file need not.
        return request with
        {
            Body = request.Body ?? "",
            BodyType = request.BodyType ?? "",
            ResponseStatusCode = request.ResponseStatusCode ?? "",
            ResponseBody = request.ResponseBody ?? "",
            Headers = request.Headers ?? [],
            ProxyUrl = request.ProxyUrl ?? "",
        };
    }

    internal static List<StoredRequest> Requests(IEnumerable<StoredRequest?>? requests) =>
        (requests ?? []).Select(Request).OfType<StoredRequest>().ToList();

    internal static List<StoredCollection> Collections(IEnumerable<StoredCollection?>? collections) =>
        (collections ?? [])
            .Where(c => c is not null && !string.IsNullOrWhiteSpace(c.Name))
            .Select(c => c! with { Requests = Requests(c.Requests) })
            .ToList();

    internal static List<VariableEnvironment> Environments(IEnumerable<VariableEnvironment?>? environments) =>
        (environments ?? [])
            .Where(e => e is not null && !string.IsNullOrWhiteSpace(e.Name))
            .Select(e => e! with { Variables = e.Variables ?? [] })
            .ToList();
}
