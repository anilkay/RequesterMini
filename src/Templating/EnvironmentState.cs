namespace Templating;

/// <summary>
/// What gets persisted for variables: every environment plus which one is active, in one file so
/// the choice survives a restart along with the values. Shared by both front ends, which store it
/// in their own data directories.
/// </summary>
/// <remarks>
/// Deliberately has only this constructor, for the reason given on <see cref="VariableEnvironment"/>.
/// </remarks>
public sealed record EnvironmentState(List<VariableEnvironment> Environments, string ActiveName);
