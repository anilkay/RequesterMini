namespace SyntaxHighlighter;

/// <summary>
/// A run of text paired with its semantic <see cref="TokenKind"/>. Concatenating every segment's
/// <see cref="Text"/> reproduces the original input verbatim (highlighting never adds or drops text).
/// </summary>
public readonly record struct HighlightSegment(string Text, TokenKind Kind);
