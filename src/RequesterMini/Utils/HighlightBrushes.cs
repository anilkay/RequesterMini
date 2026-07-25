using Avalonia.Media;
using SyntaxHighlighter;

namespace RequesterMini.Utils;

/// <summary>
/// Maps the UI-free <see cref="TokenKind"/>s produced by <see cref="SyntaxHighlighter"/> to the
/// brushes used in the preview (VS Code dark theme colors). Returns null for <see cref="TokenKind.None"/>
/// so the run inherits the control's foreground.
/// </summary>
internal static class HighlightBrushes
{
    private static readonly SolidColorBrush Key            = new(Color.Parse("#9CDCFE"));
    private static readonly SolidColorBrush StringValue    = new(Color.Parse("#CE9178"));
    private static readonly SolidColorBrush Number         = new(Color.Parse("#B5CEA8"));
    private static readonly SolidColorBrush BooleanNull    = new(Color.Parse("#569CD6"));
    private static readonly SolidColorBrush Punctuation    = new(Color.Parse("#D4D4D4"));
    private static readonly SolidColorBrush ElementName    = new(Color.Parse("#569CD6"));
    private static readonly SolidColorBrush AttributeName  = new(Color.Parse("#9CDCFE"));
    private static readonly SolidColorBrush AttributeValue = new(Color.Parse("#CE9178"));
    private static readonly SolidColorBrush Comment        = new(Color.Parse("#6A9955"));
    private static readonly SolidColorBrush Meta           = new(Color.Parse("#C586C0"));
    private static readonly SolidColorBrush TagPunctuation = new(Color.Parse("#808080"));

    internal static SolidColorBrush? For(TokenKind kind) => kind switch
    {
        TokenKind.Key            => Key,
        TokenKind.StringValue    => StringValue,
        TokenKind.Number         => Number,
        TokenKind.BooleanNull    => BooleanNull,
        TokenKind.Punctuation    => Punctuation,
        TokenKind.ElementName    => ElementName,
        TokenKind.AttributeName  => AttributeName,
        TokenKind.AttributeValue => AttributeValue,
        TokenKind.Comment        => Comment,
        TokenKind.Meta           => Meta,
        TokenKind.TagPunctuation => TagPunctuation,
        _                        => null,
    };
}
