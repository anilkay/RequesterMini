using System.Text.RegularExpressions;

namespace SyntaxHighlighter;

/// <summary>
/// Tokenizes JSON into semantic <see cref="HighlightSegment"/>s. Purely lexical (regex-based) so it
/// highlights even slightly malformed input and never throws.
/// </summary>
public static partial class JsonHighlighter
{
    // Matches: strings, booleans, null, numbers, punctuation, whitespace
    [GeneratedRegex(@"""(?:\\.|[^""\\])*""|true|false|null|-?\d+(?:\.\d+)?(?:[eE][+-]?\d+)?|[{}\[\]:,]|\s+")]
    private static partial Regex TokenRegex();

    public static List<HighlightSegment> Highlight(string json)
    {
        var result = new List<HighlightSegment>();
        int pos = 0;

        var matches = TokenRegex().Matches(json);

        // Build a list first so we can look ahead for key detection
        var tokens = new (string text, int index)[matches.Count];
        for (int i = 0; i < matches.Count; i++)
            tokens[i] = (matches[i].Value, matches[i].Index);

        for (int i = 0; i < tokens.Length; i++)
        {
            var (text, index) = tokens[i];

            // Fill any unmatched gap
            if (index > pos)
                result.Add(new HighlightSegment(json[pos..index], TokenKind.None));

            TokenKind kind;

            if (text.Length > 0 && text[0] == '"')
            {
                // A string is a key when the next non-whitespace token is ':'
                bool isKey = false;
                for (int j = i + 1; j < tokens.Length; j++)
                {
                    if (string.IsNullOrWhiteSpace(tokens[j].text)) continue;
                    isKey = tokens[j].text == ":";
                    break;
                }
                kind = isKey ? TokenKind.Key : TokenKind.StringValue;
            }
            else if (text is "true" or "false" or "null")
                kind = TokenKind.BooleanNull;
            else if (text.Length > 0 && (char.IsDigit(text[0]) || text[0] == '-'))
                kind = TokenKind.Number;
            else if (text.Length > 0 && !char.IsWhiteSpace(text[0]))
                kind = TokenKind.Punctuation;
            else
                kind = TokenKind.None; // whitespace — inherits foreground

            result.Add(new HighlightSegment(text, kind));
            pos = index + text.Length;
        }

        // Any trailing unmatched text
        if (pos < json.Length)
            result.Add(new HighlightSegment(json[pos..], TokenKind.None));

        return result;
    }
}
