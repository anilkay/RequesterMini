namespace Templating;

/// <summary>
/// A named set of variables — "dev", "staging", "prod" — one of which is active at a time.
/// </summary>
/// <remarks>
/// Named <c>VariableEnvironment</c> rather than <c>Environment</c> so it does not shadow
/// <see cref="System.Environment"/> at every call site.
/// <para>
/// Deliberately has only this constructor. A convenience overload taking just a name made it
/// ambiguous which constructor System.Text.Json should use, and deserialization then failed —
/// silently, because the store degrades to memory-only rather than throwing.
/// </para>
/// </remarks>
public sealed record VariableEnvironment(string Name, Dictionary<string, string> Variables);
