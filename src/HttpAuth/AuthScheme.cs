namespace HttpAuth;

/// <summary>
/// Authentication scheme applied to an outgoing request. Kept as an enum (rather than raw strings)
/// so adding Bearer/API-key later is a compile-time change rather than a string comparison hunt.
/// </summary>
public enum AuthScheme
{
    None,
    Basic,
}
