using System.Text.RegularExpressions;

namespace Templating;

/// <summary>
/// Substitutes <c>{{name}}</c> placeholders with values from the active variable set.
/// <para>
/// An unknown placeholder is left exactly as written rather than blanked. A request that silently
/// loses a host is far harder to diagnose than one that visibly still says <c>{{baseUrl}}</c>, and
/// <see cref="FindUnresolved"/> lets the UI say which names are missing.
/// </para>
/// </summary>
public static partial class VariableInterpolator
{
    /// <summary>
    /// How many times a value is re-scanned for placeholders, so a variable may be defined in terms
    /// of another. Bounded rather than recursive: a pair that refers to each other would otherwise
    /// never settle.
    /// </summary>
    public const int MaxPasses = 5;

    // Deliberately narrow: a placeholder name is letters, digits, '_', '-' or '.'. Anything else
    // between braces — a JSON object inside a body, a templating syntax from somewhere else — is
    // left untouched.
    [GeneratedRegex(@"\{\{\s*(?<name>[A-Za-z0-9_.-]+)\s*\}\}")]
    private static partial Regex PlaceholderRegex();

    /// <summary>Returns <paramref name="template"/> with every known placeholder replaced.</summary>
    public static string Resolve(string? template, IReadOnlyDictionary<string, string>? variables)
    {
        if (string.IsNullOrEmpty(template)) return template ?? "";
        if (variables is null || variables.Count == 0) return template;

        var current = template;

        for (var pass = 0; pass < MaxPasses; pass++)
        {
            var next = PlaceholderRegex().Replace(current, match =>
                variables.TryGetValue(match.Groups["name"].Value, out var value) ? value : match.Value);

            if (next == current) break;  // nothing left that this set can resolve
            current = next;
        }

        return current;
    }

    /// <summary>
    /// The placeholder names in <paramref name="template"/> that <paramref name="variables"/> does
    /// not define, in first-seen order and without duplicates.
    /// </summary>
    public static List<string> FindUnresolved(string? template, IReadOnlyDictionary<string, string>? variables)
    {
        var missing = new List<string>();
        if (string.IsNullOrEmpty(template)) return missing;

        foreach (Match match in PlaceholderRegex().Matches(template))
        {
            var name = match.Groups["name"].Value;

            if (variables is not null && variables.ContainsKey(name)) continue;
            if (missing.Contains(name)) continue;

            missing.Add(name);
        }

        return missing;
    }

    /// <summary>True when <paramref name="template"/> contains at least one placeholder.</summary>
    public static bool HasPlaceholders(string? template) =>
        !string.IsNullOrEmpty(template) && PlaceholderRegex().IsMatch(template);
}
