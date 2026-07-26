using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Components;
using RequesterMini.Web.Services;
using SyntaxHighlighter;

namespace RequesterMini.Web.Utils;

/// <summary>
/// Turns the UI-free <see cref="HighlightSegment"/>s from <c>SyntaxHighlighter</c> into span markup.
/// Each <see cref="TokenKind"/> maps to a CSS class so the palette lives in <c>app.css</c> and can
/// differ between light and dark themes.
/// </summary>
public static class CodeRenderer
{
    /// <summary>
    /// Bodies past this size are shown unhighlighted. Tokenizing megabytes of JSON and shipping the
    /// resulting markup over the circuit costs far more than the color is worth.
    /// </summary>
    public const int MaxHighlightLength = 200_000;

    public static MarkupString Render(string text, ContentKind kind)
    {
        if (string.IsNullOrEmpty(text)) return new MarkupString("");

        var segments = text.Length > MaxHighlightLength
            ? null
            : kind switch
            {
                ContentKind.Json => JsonHighlighter.Highlight(text),
                ContentKind.Xml => XmlHighlighter.Highlight(text),
                _ => null,
            };

        if (segments is null)
        {
            return new MarkupString(HtmlEncoder.Default.Encode(text));
        }

        var builder = new StringBuilder(text.Length + (segments.Count * 24));
        foreach (var (segmentText, segmentKind) in segments)
        {
            var cssClass = CssClass(segmentKind);
            if (cssClass is null)
            {
                builder.Append(HtmlEncoder.Default.Encode(segmentText));
                continue;
            }

            builder.Append("<span class=\"")
                .Append(cssClass)
                .Append("\">")
                .Append(HtmlEncoder.Default.Encode(segmentText))
                .Append("</span>");
        }

        return new MarkupString(builder.ToString());
    }

    // null means "no styling" — the run inherits the surrounding foreground.
    private static string? CssClass(TokenKind kind) => kind switch
    {
        TokenKind.Key => "tok-key",
        TokenKind.StringValue => "tok-string",
        TokenKind.Number => "tok-number",
        TokenKind.BooleanNull => "tok-bool",
        TokenKind.Punctuation => "tok-punct",
        TokenKind.ElementName => "tok-element",
        TokenKind.AttributeName => "tok-attr",
        TokenKind.AttributeValue => "tok-attrvalue",
        TokenKind.Comment => "tok-comment",
        TokenKind.Meta => "tok-meta",
        TokenKind.TagPunctuation => "tok-tagpunct",
        _ => null,
    };
}
