using System.Collections.Generic;
using System.Text.RegularExpressions;
using Avalonia.Media;

namespace RequesterMini.Utils;

internal static partial class JsonColorizer
{
    private static readonly SolidColorBrush KeyBrush      = new(Color.Parse("#9CDCFE"));
    private static readonly SolidColorBrush StringBrush   = new(Color.Parse("#CE9178"));
    private static readonly SolidColorBrush NumberBrush   = new(Color.Parse("#B5CEA8"));
    private static readonly SolidColorBrush BoolNullBrush = new(Color.Parse("#569CD6"));
    private static readonly SolidColorBrush PunctBrush    = new(Color.Parse("#D4D4D4"));

    // Matches: strings, booleans, null, numbers, punctuation, whitespace
    [GeneratedRegex(@"""(?:\\.|[^""\\])*""|true|false|null|-?\d+(?:\.\d+)?(?:[eE][+-]?\d+)?|[{}\[\]:,]|\s+")]
    private static partial Regex TokenRegex();

    internal record struct Segment(string Text, SolidColorBrush? Brush);

    internal static List<Segment> Colorize(string json)
    {
        var result = new List<Segment>();
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
                result.Add(new Segment(json[pos..index], null));

            SolidColorBrush? brush;

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
                brush = isKey ? KeyBrush : StringBrush;
            }
            else if (text is "true" or "false" or "null")
                brush = BoolNullBrush;
            else if (text.Length > 0 && (char.IsDigit(text[0]) || text[0] == '-'))
                brush = NumberBrush;
            else if (text.Length > 0 && !char.IsWhiteSpace(text[0]))
                brush = PunctBrush;
            else
                brush = null; // whitespace — inherits foreground

            result.Add(new Segment(text, brush));
            pos = index + text.Length;
        }

        // Any trailing unmatched text
        if (pos < json.Length)
            result.Add(new Segment(json[pos..], null));

        return result;
    }
}
