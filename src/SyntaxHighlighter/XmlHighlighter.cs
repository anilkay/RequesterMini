using System.Text.RegularExpressions;

namespace SyntaxHighlighter;

/// <summary>
/// Tokenizes XML into semantic <see cref="HighlightSegment"/>s, mirroring <see cref="JsonHighlighter"/>.
/// Purely lexical (regex-based) so it highlights even slightly malformed markup and never throws.
/// </summary>
public static partial class XmlHighlighter
{
    // Top-level split: comments, CDATA, processing instructions/declarations, doctype, tags, and text between tags.
    [GeneratedRegex(
        @"(?<comment><!--.*?-->)" +
        @"|(?<cdata><!\[CDATA\[.*?\]\]>)" +
        @"|(?<pi><\?.*?\?>)" +
        @"|(?<doctype><!DOCTYPE[^>]*>)" +
        @"|(?<tag></?[A-Za-z_][\w.:-]*(?:\s+[^<>]*?)?/?>)" +
        @"|(?<text>[^<]+)",
        RegexOptions.Singleline)]
    private static partial Regex TokenRegex();

    // Inside a single tag: opening/closing punctuation, names, '=', quoted values, whitespace.
    [GeneratedRegex(@"(?<punc></|/?>|<)|(?<name>[A-Za-z_][\w.:-]*)|(?<eq>=)|(?<value>""[^""]*""|'[^']*')|(?<ws>\s+)")]
    private static partial Regex TagRegex();

    public static List<HighlightSegment> Highlight(string xml)
    {
        var result = new List<HighlightSegment>();
        int pos = 0;

        foreach (Match m in TokenRegex().Matches(xml))
        {
            // Fill any gap the tokenizer skipped (keeps output byte-for-byte identical to the input).
            if (m.Index > pos)
                result.Add(new HighlightSegment(xml[pos..m.Index], TokenKind.None));
            pos = m.Index + m.Length;

            if (m.Groups["comment"].Success)
                result.Add(new HighlightSegment(m.Value, TokenKind.Comment));
            else if (m.Groups["pi"].Success || m.Groups["doctype"].Success || m.Groups["cdata"].Success)
                result.Add(new HighlightSegment(m.Value, TokenKind.Meta));
            else if (m.Groups["tag"].Success)
                HighlightTag(m.Value, result);
            else // text
                result.Add(new HighlightSegment(m.Value, TokenKind.None));
        }

        if (pos < xml.Length)
            result.Add(new HighlightSegment(xml[pos..], TokenKind.None));

        return result;
    }

    private static void HighlightTag(string tag, List<HighlightSegment> result)
    {
        int pos = 0;
        bool elementNameSeen = false; // the first name in a tag is the element; the rest are attributes.

        foreach (Match m in TagRegex().Matches(tag))
        {
            if (m.Index > pos)
                result.Add(new HighlightSegment(tag[pos..m.Index], TokenKind.None));
            pos = m.Index + m.Length;

            if (m.Groups["name"].Success)
            {
                result.Add(new HighlightSegment(m.Value, elementNameSeen ? TokenKind.AttributeName : TokenKind.ElementName));
                elementNameSeen = true;
            }
            else if (m.Groups["value"].Success)
                result.Add(new HighlightSegment(m.Value, TokenKind.AttributeValue));
            else if (m.Groups["ws"].Success)
                result.Add(new HighlightSegment(m.Value, TokenKind.None));
            else // punc or eq
                result.Add(new HighlightSegment(m.Value, TokenKind.TagPunctuation));
        }

        if (pos < tag.Length)
            result.Add(new HighlightSegment(tag[pos..], TokenKind.None));
    }
}
