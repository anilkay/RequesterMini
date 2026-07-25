namespace SyntaxHighlighter;

/// <summary>
/// Semantic classification of a highlighted run. UI-free on purpose: the consumer maps each
/// kind to whatever color/brush it wants. <see cref="None"/> means "no styling" (whitespace,
/// plain text) and should inherit the surrounding foreground.
/// </summary>
public enum TokenKind
{
    None,
    // JSON
    Key,
    StringValue,
    Number,
    BooleanNull,
    Punctuation,
    // XML
    ElementName,
    AttributeName,
    AttributeValue,
    Comment,
    Meta,
    TagPunctuation,
}
