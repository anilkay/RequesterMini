using FluentResults;

namespace RequesterMini.Web.Services;

/// <summary>
/// The naming rule collections and environments share: not blank, and unique ignoring case — two
/// entries called "Api" and "api" would be indistinguishable in the UI.
/// <para>
/// It lives in one place so the dialog that asks for a name does not re-implement what the store
/// enforces. The dialog hands the name to the store and shows whatever comes back; before this,
/// both sides checked uniqueness and the dialog's copy was the one the user actually saw.
/// </para>
/// </summary>
internal static class NameRules
{
    /// <summary>
    /// Returns the trimmed name to use, or the reason it was rejected. Succeeding with the trimmed
    /// value keeps callers from storing one form and comparing against another.
    /// </summary>
    internal static Result<string> Validate(string? name, IEnumerable<string> existingNames, string noun)
    {
        var trimmed = (name ?? "").Trim();

        if (trimmed.Length == 0)
        {
            return Result.Fail($"A {noun} name cannot be blank.");
        }

        return existingNames.Any(existing => string.Equals(existing, trimmed, StringComparison.OrdinalIgnoreCase))
            ? Result.Fail($"A {noun} called \"{trimmed}\" already exists.")
            : Result.Ok(trimmed);
    }
}
